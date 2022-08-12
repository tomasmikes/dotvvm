using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using DotVVM.Framework.Configuration;
using DotVVM.Framework.Hosting.Maui.Services.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace DotVVM.Framework.Hosting.Maui.Services
{
    public class WebViewMessageHandler
    {
        private readonly Lazy<JsonSerializerSettings> serializerSettings;
        private readonly Lazy<JsonSerializer> serializer;

        private int messageIdCounter;

        private Dictionary<int, TaskCompletionSource<string>> incomingMessageQueue = new();
        private readonly DotvvmWebRequestHandler dotvvmWebRequestHandler;

        public WebViewMessageHandler(DotvvmWebRequestHandler dotvvmWebRequestHandler)
        {
            serializerSettings = new(() =>
            {
                var settings = DefaultSerializerSettingsProvider.Instance.GetSettingsCopy();
                settings.ContractResolver = new DefaultContractResolver() { NamingStrategy = new CamelCaseNamingStrategy() };
                return settings;
            });
            serializer = new(() => JsonSerializer.Create(serializerSettings.Value));

            this.dotvvmWebRequestHandler = dotvvmWebRequestHandler;
        }
        private async Task<T> WaitForMessage<T>(int messageId)
        {
            var source = new TaskCompletionSource<string>();
            incomingMessageQueue[messageId] = source;
            var result = await source.Task;
            return JsonConvert.DeserializeObject<T>(result);
        }

        internal async Task<string?> ProcessRequestOrResponse(string json)
        {
            var message = JsonConvert.DeserializeObject<WebViewMessageEnvelope>(json, serializerSettings.Value);

            object? response;
            if (message.Type == "HttpRequest")
            {
                var request = message.Payload.ToObject<HttpRequestInputMessage>(serializer.Value);
                response = await ProcessHttpRequest(request);
            }
            else
            {
                // TODO: maybe throw exception about unknown command
                response = null;
            }

            // if the message was request, produce a response; otherwise return null
            if (response != null)
            {
                return JsonConvert.SerializeObject(new WebViewMessageEnvelope()
                {
                    MessageId = message.MessageId,
                    Type = message.Type,
                    Payload = JObject.FromObject(response, serializer.Value)
                }, serializerSettings.Value);
            }
            return null;
        }

        private async Task<HttpRequestOutputMessage> ProcessHttpRequest(HttpRequestInputMessage request)
        {
            var response = await dotvvmWebRequestHandler.ProcessRequest
            (
                new Uri(new Uri("https://0.0.0.0/"), request.Url),
                request.Method,
                request.Headers,
                new MemoryStream(Encoding.UTF8.GetBytes(request.BodyString))
            );
            return new HttpRequestOutputMessage()
            {
                StatusCode = response.StatusCode,
                Headers = response.Headers,
                BodyString = Encoding.UTF8.GetString(response.Content.ToArray())
            };
        }
    }
}
