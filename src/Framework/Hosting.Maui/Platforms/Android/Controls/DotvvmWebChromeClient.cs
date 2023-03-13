using Android.Content;
using Android.OS;
using Android.Webkit;
using Uri = Android.Net.Uri;

namespace DotVVM.Framework.Hosting.Maui.Controls;

public class DotvvmWebChromeClient : WebChromeClient
{
    public override bool OnCreateWindow(Android.Webkit.WebView view, bool isDialog, bool isUserGesture, Message resultMsg)
    {
        var handler = new Handler();
        var result = view.GetHitTestResult();

        string requestUrl = null;
        if (result.Type == HitTestResult.SrcImageAnchorType)
        {
            var message = handler.ObtainMessage();
            view.RequestFocusNodeHref(message);

            var url = message.Data?.GetString("url");
            requestUrl = url;
        }

        requestUrl ??= result.Extra;

        if (requestUrl != null)
        {
            var browserIntent = new Intent(Intent.ActionView, Uri.Parse(requestUrl));
            view.Context?.StartActivity(browserIntent);
        }

        return false;
    }
}
