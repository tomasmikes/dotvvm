using Android.Webkit;
using DotVVM.Framework.Hosting.Maui.Services;
using Microsoft.Maui.Handlers;
using static Android.Views.ViewGroup;
using AWebView = Android.Webkit.WebView;

namespace DotVVM.Framework.Hosting.Maui.Controls;

public partial class DotvvmWebViewHandler : ViewHandler<IDotvvmWebView, AWebView>
{
    private WebViewClient? _webViewClient;
    private WebChromeClient? _webChromeClient;
    private AndroidWebViewManager? webviewManager;
    public AndroidWebViewManager? WebviewManager => webviewManager;

    private partial string GetUrl() => PlatformView.Url;

    /// <inheritdoc />
    protected override AWebView CreatePlatformView()
    {
        var dotvvmAndroidWebView = new AWebView(Context)
        {
            LayoutParameters = new Android.Widget.AbsoluteLayout.LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent, 0, 0)
        };

        if (dotvvmAndroidWebView.Settings != null)
        {
            // To allow overriding UrlLoadingStrategy.OpenInWebView and open links in browser with a _blank target
            dotvvmAndroidWebView.Settings.SetSupportMultipleWindows(true);

            dotvvmAndroidWebView.Settings.JavaScriptEnabled = true;
            dotvvmAndroidWebView.Settings.DomStorageEnabled = true;
            AWebView.SetWebContentsDebuggingEnabled(true);
        }

        _webViewClient = new DotvvmWebViewClient(this);
        dotvvmAndroidWebView.SetWebViewClient(_webViewClient);

        _webChromeClient = new DotvvmWebChromeClient();
        dotvvmAndroidWebView.SetWebChromeClient(_webChromeClient);

        return dotvvmAndroidWebView;
    }

    /// <inheritdoc />
    protected override void DisconnectHandler(AWebView platformView)
    {
        platformView.StopLoading();

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

        webviewManager = new AndroidWebViewManager(
            PlatformView,
            webViewMessageHandler,
            webRequestHandler,
            Dispatcher.GetForCurrentThread()!);

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
