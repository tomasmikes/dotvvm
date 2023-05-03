using System.Collections;
using System.Collections.Specialized;

namespace DotVVM.Framework.Hosting.Maui;

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
