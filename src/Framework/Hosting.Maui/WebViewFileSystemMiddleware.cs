using DotVVM.Framework.Configuration;
using DotVVM.Framework.Hosting.Middlewares;
using HeyRed.Mime;

namespace DotVVM.Framework.Hosting.Maui;

public class WebViewFileSystemMiddleware : IMiddleware
{
    private readonly DotvvmConfiguration _configuration;
    private readonly IDotvvmFileProvider _mauiDotvvmFileProvider;

    public WebViewFileSystemMiddleware(DotvvmConfiguration configuration, IDotvvmFileProvider mauiDotvvmFileProvider)
    {
        _configuration = configuration;
        _mauiDotvvmFileProvider = mauiDotvvmFileProvider;
    }

    public async Task<bool> Handle(IDotvvmRequestContext request)
    {
        var filePath = request.HttpContext.Request.Path.Value;

        if (!await _mauiDotvvmFileProvider.FileExistsAsync(filePath))
        {
            return false;
        }

        var fileStream = await _mauiDotvvmFileProvider.OpenFileAsync(filePath);
        var mimeType = MimeTypesMap.GetMimeType(Path.GetFileName(filePath));

        request.HttpContext.Response.ContentType = mimeType;
        request.HttpContext.Response.Headers.Add("Cache-Control", new[] { "no-cache, max-age=0, must-revalidate, no-store" });

        await fileStream.CopyToAsync(request.HttpContext.Response.Body);

        return true;
    }
}
