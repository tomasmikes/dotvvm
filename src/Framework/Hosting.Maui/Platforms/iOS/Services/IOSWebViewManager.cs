// Adapted from https://github.com/dotnet/maui

using System.Text.Encodings.Web;
using DotVVM.Framework.Hosting.Maui.Controls;
using DotVVM.Framework.Hosting.Maui.Services;
using Foundation;
using WebKit;

namespace DotVVM.Framework.Hosting.Maui.iOS.Services;

public class IOSWebViewManager : WebViewManager
{
    private readonly WKWebView _webview;

    public IOSWebViewManager(
        DotvvmWebViewHandler dotvvmWebViewHandler,
        WKWebView webview,
        WebViewMessageHandler messageHandler,
        DotvvmWebRequestHandler dotvvmWebRequestHandler,
        IDispatcher dispatcher,
        Uri appBaseUri)
        : base(messageHandler, dotvvmWebRequestHandler, dispatcher, appBaseUri)
    {
        ArgumentNullException.ThrowIfNull(nameof(webview));

        _webview = webview;

        InitializeWebView();
    }

    private void InitializeWebView()
    {
        _webview.NavigationDelegate = new WebViewNavigationDelegate();
        // TODO: resolve
        //_webview.UIDelegate = new WebViewNavigationDelegate.WebViewUIDelegate(_dotvvmWebViewHandler);
    }

    protected override void NavigateCore(Uri absoluteUri)
    {
        using var nsUrl = new NSUrl(absoluteUri.ToString());
        using var request = new NSUrlRequest(nsUrl);
        _webview.LoadRequest(request);
    }

    public override void SendMessage(string message)
    {
        var messageJSStringLiteral = JavaScriptEncoder.Default.Encode(message);
        _webview.EvaluateJavaScript(
            javascript: $"__dispatchMessageCallback(\"{messageJSStringLiteral}\")",
            completionHandler: (NSObject result, NSError error) => { });
    }
    
    public void OnMessageReceivedPublic(Uri uri, string message)
    {
        OnMessageReceived(uri, message);
    }
}
