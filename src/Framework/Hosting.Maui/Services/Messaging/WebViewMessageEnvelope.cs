using Newtonsoft.Json.Linq;

namespace DotVVM.Framework.Hosting.Maui.Services.Messaging;

public class WebViewMessageEnvelope
{
    public string Type { get; set; }

    public int MessageId { get; set; }

    public JObject Payload { get; set; }

    public string ErrorMessage { get; set; }
}
