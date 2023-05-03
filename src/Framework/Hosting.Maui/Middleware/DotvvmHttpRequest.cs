using DotVVM.Framework.Hosting.Maui.Services;

namespace DotVVM.Framework.Hosting.Maui;

public class DotvvmHttpRequest : IHttpRequest
{
    public IHttpContext HttpContext { get; }
    public string Method { get; }
    public string Scheme { get; }
    public string ContentType { get; }
    public bool IsHttps { get; }
    public IPathString Path { get; }
    public IPathString PathBase { get; }
    public Stream Body { get; }
    public IQueryCollection Query { get; }
    public string QueryString { get; }
    public ICookieCollection Cookies { get; }
    public IHeaderCollection Headers { get; }
    public Uri Url { get; }

    public DotvvmHttpRequest(DotvvmHttpContext dotvvmHttpContext, DotvvmRequest dotvvmRequest)
    {
        HttpContext = dotvvmHttpContext;
        Method = dotvvmRequest.Method;
        Scheme = "http";
        ContentType = dotvvmRequest.Headers
            .FirstOrDefault(x => String.Equals(x.Key, "content-type", StringComparison.OrdinalIgnoreCase))
            .Value;
            
        IsHttps = false;
        Path = new DotvvmPathString(
            dotvvmRequest.RequestUri.GetComponents(UriComponents.Path, UriFormat.Unescaped));
        PathBase = new DotvvmPathString("");
        Body = dotvvmRequest.ContentStream;
        Query = new DotvvmQueryCollection(System.Web.HttpUtility.ParseQueryString(dotvvmRequest.RequestUri.Query));
        QueryString = "";
        Cookies = new DotvvmCookieCollection(dotvvmRequest.Headers
            .FirstOrDefault(x => string.Equals(x.Key, "cookie", StringComparison.OrdinalIgnoreCase))
            .Value);
        Headers = new DotvvmHeaderCollection(dotvvmRequest.Headers);
        Url = dotvvmRequest.RequestUri;
    }
}
