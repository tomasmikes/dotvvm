using System.Text;

namespace DotVVM.Framework.Hosting.Maui;

public class DotvvmHttpResponse : IHttpResponse
{
    public IHeaderCollection Headers { get; }
    public IHttpContext Context { get; }
    public int StatusCode { get; set; }
    public string ContentType
    {
        get => Headers["content-type"];
        set => Headers["content-type"] = value;
    }
    public string MimeType
    {
        get => ContentType?.Split(';').FirstOrDefault()?.Trim();
    }
    public string CharEncoding
    {
        get => ContentType?.Split(';').Length == 2 ? ContentType.Split(';')[1].Replace("charset=", "").Trim() : null;
    }
    public Stream Body { get; set; }

    public DotvvmHttpResponse(DotvvmHttpContext dotvvmHttpContext)
    {
        Context = dotvvmHttpContext;
        Headers = new DotvvmHeaderCollection();
        Body = new MemoryStream();
        StatusCode = 200;
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
