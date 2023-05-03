using System.Collections;

namespace DotVVM.Framework.Hosting.Maui;

public class DotvvmHeaderCollection : IHeaderCollection
{
    private Dictionary<string, string[]> items = new(StringComparer.OrdinalIgnoreCase);

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
        get => items.TryGetValue(key, out var result) ? string.Join(",", result) : null;
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
