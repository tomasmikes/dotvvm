using DotVVM.Framework.Configuration;
using DotVVM.Framework.ResourceManagement;

namespace DotVVM.Framework.Hosting.Maui.Services;

public class WebViewResourceProcessor : IResourceProcessor
{
    private readonly DotvvmConfiguration config;

    public WebViewResourceProcessor(DotvvmConfiguration config)
    {
        this.config = config;
    }

    public IEnumerable<NamedResource> Process(IEnumerable<NamedResource> source)
    {
        foreach (var r in source)
        {
            if (r.Name == ResourceConstants.DotvvmResourceName + ".internal")
                yield return this.config.Resources.FindNamedResource(ResourceConstants.DotvvmResourceName +
                                                                     ".internal-webview");
            else
                yield return r;
        }
    }
}
