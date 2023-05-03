// Adapted from https://github.com/dotnet/maui

using DotVVM.Framework.Hosting.Maui.Platforms.Windows.Services;
using DotVVM.Framework.Hosting.Maui.Services;
using Microsoft.Maui.Handlers;
using WebView2Control = Microsoft.UI.Xaml.Controls.WebView2;

namespace DotVVM.Framework.Hosting.Maui.Controls;

public partial class DotvvmWebViewHandler : ViewHandler<IDotvvmWebView, WebView2Control>
{
    private WindowsWebViewManager? webviewManager;

    /// <inheritdoc />
    protected override WebView2Control CreatePlatformView()
    {
        return new WebView2Control();
    }

    protected override void ConnectHandler(WebView2Control nativeView)
    {
        base.ConnectHandler(nativeView);

        StartWebViewCoreIfPossible();
    }

    /// <inheritdoc />
    protected override void DisconnectHandler(WebView2Control platformView)
    {
        if (webviewManager != null)
        {
            // Dispose this component's contents and block on completion so that user-written disposal logic and
            // DotVVM disposal logic will complete.
            webviewManager?
                .DisposeAsync()
                .AsTask()
                .GetAwaiter()
                .GetResult();

            webviewManager = null;
        }
    }

    private bool RequiredStartupPropertiesSet =>
        Services != null
        && (!string.IsNullOrEmpty(RouteName) || !string.IsNullOrEmpty(Url));

    private partial string GetUrl() => PlatformView.Source?.ToString();

    partial void StartWebViewCoreIfPossible()
    {
        if (!RequiredStartupPropertiesSet ||
            webviewManager != null)
        {
            return;
        }

        if (PlatformView == null)
        {
            throw new InvalidOperationException(
                $"Can't start {nameof(DotvvmWebView)} without native web view instance.");
        }

        var webRequestHandler = Services!.GetRequiredService<DotvvmWebRequestHandler>();
        var webViewMessageHandler = Services!.GetRequiredService<WebViewMessageHandler>();

        webviewManager = new WindowsWebViewManager(
            PlatformView,
            webViewMessageHandler,
            Dispatcher.GetForCurrentThread()!,
            webRequestHandler,
            this);

        webViewMessageHandler.AttachWebViewHandler(this);

        // triggers the navigation to the default route
        if (!string.IsNullOrEmpty(RouteName))
        {
            NavigateToRoute(RouteName);
        }
        else
        {
            webviewManager.Navigate(Url);
        }
    }
}
