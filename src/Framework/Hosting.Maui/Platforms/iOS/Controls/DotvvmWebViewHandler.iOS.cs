using Foundation;
using System.Drawing;
using Microsoft.Maui.Handlers;
using UIKit;
using WebKit;
using System.Runtime.Versioning;
using DotVVM.Framework.Hosting.Maui.iOS.Services;
using DotVVM.Framework.Hosting.Maui.Services;

namespace DotVVM.Framework.Hosting.Maui.Controls;

public partial class DotvvmWebViewHandler : ViewHandler<IDotvvmWebView, WKWebView>
{
    internal static readonly string AppHostAddress = "0.0.0.0";
    internal static readonly string AppOrigin = $"dotvvm://{AppHostAddress}/";

    private IOSWebViewManager? webviewManager;

    protected override WKWebView CreatePlatformView()
    {
        var config = new WKWebViewConfiguration();

        if (OperatingSystem.IsMacCatalystVersionAtLeast(10) || OperatingSystem.IsIOSVersionAtLeast(10))
        {
            config.AllowsPictureInPictureMediaPlayback = true;
            config.AllowsInlineMediaPlayback = true;
            config.MediaTypesRequiringUserActionForPlayback = WKAudiovisualMediaTypes.None;
        }

        config.UserContentController.AddScriptMessageHandler(new WebViewScriptMessageHandler(MessageReceived), "webwindowinterop");
        config.UserContentController.AddUserScript(new WKUserScript(
            new NSString("window.dotvvm.initWebViewMessaging();"), WKUserScriptInjectionTime.AtDocumentEnd, true));

        config.SetUrlSchemeHandler(new DotvvmSchemeHandler(this), urlScheme: "dotvvm");

        var webview = new WKWebView(RectangleF.Empty, config) {
            BackgroundColor = UIColor.Clear,
            AutosizesSubviews = true
        };

        return webview;
    }

    private void MessageReceived(Uri uri, string message)
    {
        webviewManager.OnMessageReceivedPublic(uri, message);
    }

    private partial string GetUrl()
        => PlatformView.Url?.ToString();

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
            throw new InvalidOperationException($"Can't start {nameof(DotvvmWebView)} without native web view instance.");
        }

        var webRequestHandler = Services!.GetRequiredService<DotvvmWebRequestHandler>();
        var webViewMessageHandler = Services!.GetRequiredService<WebViewMessageHandler>();

        webviewManager = new IOSWebViewManager(
            this,
            PlatformView,
            webViewMessageHandler,
            webRequestHandler,
            Dispatcher.GetForCurrentThread(),
            new Uri(AppOrigin));

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

    /// <inheritdoc />
    protected override void DisconnectHandler(WKWebView platformView)
    {
        platformView.StopLoading();

        if (webviewManager != null)
        {
            // Dispose this component's contents and block on completion so that user-written disposal logic
            webviewManager?
                .DisposeAsync()
                .AsTask()
                .GetAwaiter()
                .GetResult();

            webviewManager = null;
        }
    }

    private sealed class WebViewScriptMessageHandler : NSObject, IWKScriptMessageHandler
    {
        private Action<Uri, string> _messageReceivedAction;

        public WebViewScriptMessageHandler(Action<Uri, string> messageReceivedAction)
        {
            _messageReceivedAction = messageReceivedAction ?? throw new ArgumentNullException(nameof(messageReceivedAction));
        }

        public void DidReceiveScriptMessage(WKUserContentController userContentController, WKScriptMessage message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            _messageReceivedAction(new Uri(AppOrigin), ((NSString)message.Body).ToString());
        }
    }

    private class DotvvmSchemeHandler : NSObject, IWKUrlSchemeHandler
    {
        private readonly DotvvmWebViewHandler _webViewHandler;
        
        public DotvvmSchemeHandler(DotvvmWebViewHandler webViewHandler)
        {
            _webViewHandler = webViewHandler;
        }
        
        [Export("webView:startURLSchemeTask:")]
        [SupportedOSPlatform("ios11.0")]
        public void StartUrlSchemeTask(WKWebView webView, IWKUrlSchemeTask urlSchemeTask)
        {
            var url = urlSchemeTask.Request.Url?.ToString();
            var requestUri = url != null ? new Uri(url) : null;
            var isHostedAppRequested = url != null && new Uri(AppOrigin).IsBaseOf(requestUri);

            if (requestUri == null || !isHostedAppRequested)
            {
                return;
            }

            try
            {
                // TODO: implement conversion between headers
                var requestHeaders = urlSchemeTask.Request.Headers
                    .Select(x => new KeyValuePair<string, string>(x.Key.ToString(), x.Value.ToString()));

                var responseTask = _webViewHandler.webviewManager!.ProcessRequest(new Uri(url.Replace("dotvvm", "http")), urlSchemeTask.Request.HttpMethod, requestHeaders, Stream.Null);
                var response = responseTask.GetAwaiter().GetResult();
                response.Content.Position = 0;

                if (response.StatusCode != 200)
                {
                    return;
                }

                // TODO: validate correct conversion
                var headerKeys = response.Headers.Select(x => new NSString(x.Key) as NSObject).ToArray();
                var headerValues = response.Headers.Select(x => new NSString(x.Value) as NSObject).ToArray();

                var httpResponse = new NSHttpUrlResponse(urlSchemeTask.Request.Url, response.StatusCode, "HTTP/1.1", NSDictionary.FromObjectsAndKeys(headerValues, headerKeys));
                urlSchemeTask.DidReceiveResponse(httpResponse);

                urlSchemeTask.DidReceiveData(NSData.FromArray(response.Content.ToArray()));
                urlSchemeTask.DidFinish();
            }
            catch (Exception ex)
            {
            }
        }

        [Export("webView:stopURLSchemeTask:")]
        public void StopUrlSchemeTask(WKWebView webView, IWKUrlSchemeTask urlSchemeTask)
        {
        }
    }
}
