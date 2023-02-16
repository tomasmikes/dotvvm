using System.Runtime.Versioning;
using DotVVM.Framework.Hosting.Maui.Services;
using Microsoft.Maui.Handlers;
using static Android.Views.ViewGroup;
using AWebView = Android.Webkit.WebView;

namespace DotVVM.Framework.Hosting.Maui.Controls;

public partial class DotvvmWebViewHandler : ViewHandler<IDotvvmWebView, AWebView>
{
    private AndroidWebViewManager? _webviewManager;

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
        }

        return dotvvmAndroidWebView;
    }

    /// <inheritdoc />
    protected override void DisconnectHandler(AWebView platformView)
    {
        platformView.StopLoading();

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

        _webviewManager = new AndroidWebViewManager(
            PlatformView,
            webViewMessageHandler,
            Dispatcher.GetForCurrentThread()!);

        webViewMessageHandler.AttachWebViewHandler(this);

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
