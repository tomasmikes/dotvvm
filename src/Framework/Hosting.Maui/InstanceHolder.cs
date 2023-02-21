using DotVVM.Framework.Hosting.Maui.Services;

namespace DotVVM.Framework.Hosting.Maui;

// TODO: Remove this workaround. WebViewMessageHandler can not be resolved during runtime even its registered.
public static class InstanceHolder
{
    public static WebViewMessageHandler WebViewMessageHandler { get; set; }
}
