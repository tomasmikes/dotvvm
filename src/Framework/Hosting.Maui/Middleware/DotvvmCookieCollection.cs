using System.Collections;

namespace DotVVM.Framework.Hosting.Maui;

public class DotvvmCookieCollection : ICookieCollection
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
