namespace DotVVM.Framework.Hosting.Maui.Services.Messaging;

public class HttpRequestInputMessage
{
    public string Url { get; set; }

    public string Method { get; set; }

    public IEnumerable<KeyValuePair<string, string>> Headers { get; set; }

    public string BodyString { get; set; }
}
