using DotVVM.Framework.Hosting.Maui.Controls;
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
}
