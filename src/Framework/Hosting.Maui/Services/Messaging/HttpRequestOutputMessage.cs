namespace DotVVM.Framework.Hosting.Maui.Services.Messaging
{
    public class HttpRequestOutputMessage
    {
        public int StatusCode { get; set; }

        public IEnumerable<KeyValuePair<string, string>> Headers { get; set; }

        public string BodyString { get; set; }
    }
}
