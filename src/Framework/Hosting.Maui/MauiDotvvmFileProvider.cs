namespace DotVVM.Framework.Hosting.Maui;

public class MauiDotvvmFileProvider : IDotvvmFileProvider
{
    public Task<Stream> OpenFileAsync(string path) => throw new NotImplementedException();

    public Task<bool> FileExistsAsync(string path) => throw new NotImplementedException();

    public Task WriteFileAsync(string path) => throw new NotImplementedException();
}
