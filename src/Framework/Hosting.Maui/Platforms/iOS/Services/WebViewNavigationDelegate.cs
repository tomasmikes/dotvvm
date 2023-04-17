using DotVVM.Framework.Hosting.Maui.Controls;
using Foundation;
using UIKit;
using WebKit;

namespace DotVVM.Framework.Hosting.Maui.iOS.Services;

internal class WebViewNavigationDelegate : WKNavigationDelegate
{
    public WebViewNavigationDelegate()
    {
    }

    public override void DecidePolicy(WKWebView webView, WKNavigationAction navigationAction, Action<WKNavigationActionPolicy> decisionHandler)
    {
        var requestUrl = navigationAction.Request.Url;
        var uri = new Uri(requestUrl.ToString());

        if (navigationAction.TargetFrame == null
            || !new Uri(DotvvmWebViewHandler.AppOrigin).IsBaseOf(uri))
        {
            UIApplication.SharedApplication.OpenUrl(requestUrl);
        }
        decisionHandler(WKNavigationActionPolicy.Allow);
    }

    public override void DidReceiveServerRedirectForProvisionalNavigation(WKWebView webView, WKNavigation navigation)
    {
    }

    public override void DidFailNavigation(WKWebView webView, WKNavigation navigation, NSError error)
    {
    }

    public override void DidFailProvisionalNavigation(WKWebView webView, WKNavigation navigation, NSError error)
    {
    }

    public override void DidCommitNavigation(WKWebView webView, WKNavigation navigation)
    {
    }

    public override void DidFinishNavigation(WKWebView webView, WKNavigation navigation)
    {
    }

    // TODO: fix webview being terminated
    public override void ContentProcessDidTerminate(WKWebView webView) => webView.Reload();
}
