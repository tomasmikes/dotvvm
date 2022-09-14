using System.Text;
using DotVVM.Framework.Configuration;
using DotVVM.Framework.Hosting.Maui.Services.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace DotVVM.Framework.Hosting.Maui.Services;

public class WebViewMessageHandler
{
    private readonly DotvvmWebRequestHandler dotvvmWebRequestHandler;

    private readonly Lazy<JsonSerializerSettings> serializerSettings;
    private readonly Lazy<JsonSerializer> serializer;

    private Dictionary<int, TaskCompletionSource<string>> incomingMessageQueue = new();
    private int messageIdCounter;

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

    public async Task<T> WaitForMessage<T>(int messageId)
    {
        var source = new TaskCompletionSource<string>();
        incomingMessageQueue[messageId] = source;
        var result = await source.Task;
        return JsonConvert.DeserializeObject<T>(result, serializerSettings.Value);
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
        else if (message.Type == "HandlerCommand")
        {
            var request = message.Payload.ToObject<HandlerCommandMessage>(serializer.Value);

            switch (request.Action)
            {
                case "GetViewModelState":
                    break;
                case "PatchViewModelState":
                    break;
                default:
                    // TODO: maybe throw exception about unknown command
                    break;
            }

            incomingMessageQueue[message.MessageId]
                .SetResult(JsonConvert.SerializeObject(request.Content, serializerSettings.Value));
            return null;
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

    public WebViewMessageEnvelope CreateCommandMessage(string commandName, string jsonPayload = null)
    {
        var messageId = Interlocked.Increment(ref messageIdCounter);
        var message = new WebViewMessageEnvelope { MessageId = messageId, Type = commandName };

        if (jsonPayload != null)
        {
            message.Payload = JObject.Parse(jsonPayload);
        }

        return message;
    }

    public string SerializeObject(object obj, bool camelCaseNamingStrategy = true)
    {
        return camelCaseNamingStrategy
            ? JsonConvert.SerializeObject(obj, serializerSettings.Value)
            : JsonConvert.SerializeObject(obj);
    }
}
