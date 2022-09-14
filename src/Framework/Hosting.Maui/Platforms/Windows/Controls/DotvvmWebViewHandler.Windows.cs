using DotVVM.Framework.Hosting.Maui.Services;
using Microsoft.Maui.Handlers;
using WebView2Control = Microsoft.UI.Xaml.Controls.WebView2;

namespace DotVVM.Framework.Hosting.Maui.Controls;

public partial class DotvvmWebViewHandler : ViewHandler<IDotvvmWebView, WebView2Control>
{
    private WindowsWebViewManager? _webviewManager;

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
        if (_webviewManager != null)
        {
            // Dispose this component's contents and block on completion so that user-written disposal logic and
            // DotVVM disposal logic will complete.
            _webviewManager?
                .DisposeAsync()
                .AsTask()
                .GetAwaiter()
                .GetResult();

            _webviewManager = null;
        }
    }

    private bool RequiredStartupPropertiesSet =>
        Services != null
        && (!string.IsNullOrEmpty(RouteName) || !string.IsNullOrEmpty(Url));

    partial void StartWebViewCoreIfPossible()
    {
        if (!RequiredStartupPropertiesSet ||
            _webviewManager != null)
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

        _webviewManager = new WindowsWebViewManager(
            PlatformView,
            webViewMessageHandler,
            Dispatcher.GetForCurrentThread()!,
            webRequestHandler,
            this);

        // triggers the navigation to the default route
        if (!string.IsNullOrEmpty(RouteName))
        {
            NavigateToRoute(RouteName);
        }
        else
        {
            _webviewManager.Navigate(Url);
        }
    }
}
