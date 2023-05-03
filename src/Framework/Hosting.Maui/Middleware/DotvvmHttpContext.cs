using System.Security.Claims;
using DotVVM.Framework.Hosting.Maui.Services;

namespace DotVVM.Framework.Hosting.Maui;

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
