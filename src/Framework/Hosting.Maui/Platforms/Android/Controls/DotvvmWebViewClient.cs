using System.Runtime.Versioning;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Webkit;
using Java.Net;
using Microsoft.Maui.Platform;
using AWebView = Android.Webkit.WebView;

namespace DotVVM.Framework.Hosting.Maui.Controls;

[SupportedOSPlatform("android23.0")]
public class DotvvmWebViewClient : WebViewClient
{
    // Using an IP address means that WebView2 doesn't wait for any DNS resolution,
    // making it substantially faster. Note that this isn't real HTTP traffic, since
    // we intercept all the requests within this origin.
    internal static readonly string AppHostAddress = "0.0.0.0";

    /// <summary>
    /// Gets the application's base URI. Defaults to <c>https://0.0.0.0/</c>
    /// </summary>
    protected static readonly string AppOrigin = $"http://{AppHostAddress}/";

    private readonly DotvvmWebViewHandler _webViewHandler;
    
    public DotvvmWebViewClient(DotvvmWebViewHandler webViewHandler)
    {
        ArgumentNullException.ThrowIfNull(webViewHandler);
        _webViewHandler = webViewHandler;
    }

    protected DotvvmWebViewClient(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
    {
        // This constructor is called whenever the .NET proxy was disposed, and it was recreated by Java. It also
        // happens when overridden methods are called between execution of this constructor and the one above.
        // because of these facts, we have to check all methods below for null field references and properties.
    }

    public override WebResourceResponse? ShouldInterceptRequest(AWebView? view, IWebResourceRequest? request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var url = request.Url?.ToString();
        var requestUri = url != null ? new Uri(url) : null;
        var isHostedAppRequested = url != null && new Uri(AppOrigin).IsBaseOf(requestUri);

        if (requestUri == null || !isHostedAppRequested)
        {
            return base.ShouldInterceptRequest(view, request);
        }

        try
        {
            // TODO: implement conversion between headers
            var requestHeaders = request.RequestHeaders.Select(x => new KeyValuePair<string, string>(x.Key, x.Value));

            var responseTask =
                _webViewHandler.WebviewManager!.ProcessRequest(requestUri, request.Method, requestHeaders, Stream.Null);

            var response = responseTask.Result;
            response.Content.Position = 0;

            var webResponse = new WebResourceResponse(
                response.MimeType,
                response.CharEncoding,
                response.StatusCode,
                "OK", // TODO: Add status code description
                response.Headers.ToDictionary(x => x.Key,
                    x => x.Value),
                response.Content);
            return webResponse;
        }
        catch (Exception ex)
        {

        }
        return base.ShouldInterceptRequest(view, request);
    }

    public override bool ShouldOverrideUrlLoading(AWebView view, IWebResourceRequest request)
    {
        var url = request.Url?.ToString();

        if (_webViewHandler != null
            && Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var requestUri)
            && !new Uri(AppOrigin).IsBaseOf(requestUri))
        {
            var intent = Intent.ParseUri(url, IntentUriType.Scheme);
            _webViewHandler.Context.StartActivity(intent);
            return true;
        }

        return false;
    }

    public override void OnPageStarted(AWebView view, string url, Bitmap favicon)
    {
        base.OnPageStarted(view, url, favicon);
    }

    public override void OnPageFinished(AWebView view, string url) => base.OnPageFinished(view, url);
}
