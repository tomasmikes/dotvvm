namespace DotVVM.Framework.Hosting.Maui;

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
