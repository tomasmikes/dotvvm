using System.Collections;
using System.Collections.Specialized;
using System.Net;
using System.Security.Claims;
using System.Text;
using DotVVM.Framework.Configuration;
using DotVVM.Framework.Hosting.ErrorPages;
using DotVVM.Framework.Hosting.Maui.Services;
using DotVVM.Framework.Hosting.Middlewares;
using IDotvvmMiddleware = DotVVM.Framework.Hosting.Middlewares.IMiddleware;

namespace DotVVM.Framework.Hosting.Maui
{
    /// <summary>
    /// A middleware that handles DotVVM HTTP requests.
    /// </summary>
    public class DotvvmMiddleware : DotvvmMiddlewareBase
    {
        public readonly DotvvmConfiguration Configuration;
        private readonly IList<IDotvvmMiddleware> middlewares;
        private readonly bool useErrorPage;

        /// <summary>
        /// Initializes a new instance of the <see cref="DotvvmMiddleware" /> class.
        /// </summary>
        public DotvvmMiddleware(DotvvmConfiguration configuration, IList<IDotvvmMiddleware> middlewares, bool useErrorPage)
        {
            Configuration = configuration;
            this.middlewares = middlewares;
            this.useErrorPage = useErrorPage;
        }

        /// <summary>
        /// Process an individual request.
        /// </summary>
        public async Task<IHttpContext> Invoke(DotvvmRequest dotvvmRequest, IServiceScope scope)
        {
            // create the context
            var dotvvmContext = CreateDotvvmContext(dotvvmRequest, scope);
            dotvvmContext.Services.GetRequiredService<DotvvmRequestContextStorage>().Context = dotvvmContext;
            dotvvmContext.HttpContext.SetItem(HostingConstants.DotvvmRequestContextOwinKey, dotvvmContext);

            try
            {
                foreach (var middleware in middlewares)
                {
                    if (await middleware.Handle(dotvvmContext))
                    {
                        return dotvvmContext.HttpContext;
                    }
                }

                dotvvmContext.HttpContext.Response.StatusCode = 404;
                dotvvmContext.HttpContext.Response.Body.SetLength(0);
                dotvvmContext.HttpContext.Response.Write("Not found");
                return dotvvmContext.HttpContext;
            }
            catch (DotvvmInterruptRequestExecutionException)
            {
                return dotvvmContext.HttpContext;
            }
            catch (Exception ex) when (useErrorPage)
            {
                dotvvmContext.HttpContext.Response.StatusCode = 500;
                var dotvvmErrorPageRenderer = dotvvmContext.Services.GetRequiredService<DotvvmErrorPageRenderer>();
                await dotvvmErrorPageRenderer.RenderErrorResponse(dotvvmContext.HttpContext, ex);
                return dotvvmContext.HttpContext;
            }
        }

        public static IHttpContext ConvertHttpContext(DotvvmRequest dotvvmRequest)
        {
            return new DotvvmHttpContext(dotvvmRequest);
        }

        protected DotvvmRequestContext CreateDotvvmContext(DotvvmRequest dotvvmRequest, IServiceScope scope)
        {
            return new DotvvmRequestContext(
                ConvertHttpContext(dotvvmRequest),
                Configuration,
                scope.ServiceProvider
            );
        }
    }

    public class DotvvmHttpContext : IHttpContext
    {
        private Dictionary<string, object> items = new();

        public ClaimsPrincipal User => Thread.CurrentPrincipal as ClaimsPrincipal;
        public IHttpRequest Request { get; }
        public IHttpResponse Response { get; }

        public T GetItem<T>(string key) => items.TryGetValue(key, out var result) ? (T)result : default;

        public void SetItem<T>(string key, T value) => items[key] = value;

        public IEnumerable<Tuple<string, IEnumerable<KeyValuePair<string, object>>>> GetEnvironmentTabs() => Enumerable.Empty<Tuple<string, IEnumerable<KeyValuePair<string, object>>>>();

        public DotvvmHttpContext(DotvvmRequest dotvvmRequest)
        {
            Request = new DotvvmHttpRequest(this, dotvvmRequest);
            Response = new DotvvmHttpResponse(this);
        }
    }

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

    public class DotvvmPathString : IPathString
    {
        public bool Equals(IPathString other) => Equals(Value, other?.Value);

        public string Value { get; }
        public bool HasValue() => Value != null;

        public DotvvmPathString(string value)
        {
            Value = value;
        }
    }
    
    class DotvvmCookieCollection : ICookieCollection
    {
        private Dictionary<string, string> cookies = new();

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => cookies.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public string this[string key] => cookies.TryGetValue(key, out var result) ? result : null;

        public DotvvmCookieCollection(string cookieHeader = null)
        {
            this.cookies = (cookieHeader ?? "")
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(c => 
                {
                    var equalsIndex = c.IndexOf("=");
                    return new KeyValuePair<string, string>(c.Substring(0, equalsIndex), c.Substring(equalsIndex + 1));
                })
                .ToDictionary(c => c.Key, c => c.Value);
        }
    }

    public class DotvvmQueryCollection : IQueryCollection
    {
        private NameValueCollection QueryNameValueCollection { get; }
        
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            foreach (var key in QueryNameValueCollection.AllKeys)
            {
                var values = QueryNameValueCollection.GetValues(key);
                foreach (var value in values)
                {
                    yield return new KeyValuePair<string, string>(key, value);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public string this[string key] => QueryNameValueCollection[key];

        public bool TryGetValue(string key, out string value)
        {
            value = QueryNameValueCollection[key];

            return value != null;
        }

        public bool ContainsKey(string key) => QueryNameValueCollection.GetValues(key) != null;


        public DotvvmQueryCollection(NameValueCollection queryCollection)
        {
            QueryNameValueCollection = queryCollection;
        }
    }

    public class DotvvmHttpResponse : IHttpResponse
    {
        public IHeaderCollection Headers { get; }
        public IHttpContext Context { get; }
        public int StatusCode { get; set; }
        public string ContentType { get; set; }
        public Stream Body { get; set; }

        public DotvvmHttpResponse(DotvvmHttpContext dotvvmHttpContext)
        {
            Context = dotvvmHttpContext;
            Headers = new DotvvmHeaderCollection();
            Body = new MemoryStream();
        }

        public void Write(string text) => Body.Write(Encoding.UTF8.GetBytes(text));

        public void Write(byte[] data) => Body.Write(data);

        public void Write(byte[] data, int offset, int count) => Body.Write(data, offset, count);

        public Task WriteAsync(string text)
        {
            Write(text);
            return Task.CompletedTask;
        }

        public Task WriteAsync(string text, CancellationToken token)
        {
            Write(text);
            return Task.CompletedTask;
        }
    }

    public class DotvvmHeaderCollection : IHeaderCollection
    {
        private Dictionary<string, string[]> items = new();

        public IEnumerator<KeyValuePair<string, string[]>> GetEnumerator() => items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Add(KeyValuePair<string, string[]> item) => items.Add(item.Key, item.Value);

        public void Clear() => items.Clear();

        public bool Contains(KeyValuePair<string, string[]> item) => items.Contains(item);

        public void CopyTo(KeyValuePair<string, string[]>[] array, int arrayIndex) => ((IDictionary<string, string[]>)items).CopyTo(array, arrayIndex);

        public bool Remove(KeyValuePair<string, string[]> item) => items.Remove(item.Key);

        public int Count => items.Count;

        public bool IsReadOnly => false;

        public void Add(string key, string[] value)
        {
            foreach (var v in value)
            {
                Append(key, v);
            }
        }

        public bool ContainsKey(string key) => items.ContainsKey(key);

        public bool Remove(string key) => items.Remove(key);

        public bool TryGetValue(string key, out string[] value) => items.TryGetValue(key, out value);

        public string this[string key]
        {
            get => string.Join(",", items[key]);
            set => items[key] = new[] { value };
        }

        public void Append(string key, string value)
        {
            if (!items.TryGetValue(key, out var currentValues))
            {
                currentValues = new[] { value };
            }
            else
            {
                currentValues = currentValues.Concat(new[] { value }).ToArray();
            }
            items[key] = currentValues;
        }

        string[] IDictionary<string, string[]>.this[string key]
        {
            get => items[key];
            set => items[key] = value;
        }

        public ICollection<string> Keys => items.Keys;
        public ICollection<string[]> Values => items.Values;

        public DotvvmHeaderCollection(IEnumerable<KeyValuePair<string, string>> headers = null)
        {
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    Append(header.Key, header.Value);
                }
            }
        }
    }
}
