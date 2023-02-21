using Android.Webkit;
using AWebView = Android.Webkit.WebView;
using AUri = Android.Net.Uri;
using System.Runtime.Versioning;

namespace DotVVM.Framework.Hosting.Maui.Services;

[SupportedOSPlatform("android23.0")]
public class AndroidWebViewManager : WebViewManager
{
    internal static readonly string AppHostAddress = "0.0.0.0";

    protected static readonly string AppOrigin = $"https://{AppHostAddress}/";
    protected static readonly AUri AndroidAppOrigin = AUri.Parse(AppOrigin);

    private readonly AWebView _webview;

    /// <summary>
    /// Constructs an instance of <see cref="AndroidWebViewManager"/>.
    /// </summary>
    /// <param name="webview">A wrapper to access platform-specific WebView APIs.</param>
    /// <param name="dispatcher">A <see cref="Dispatcher"/> instance that can marshal calls to the required thread or sync context.</param>
    /// <param name="fileProvider">Provides static content to the webview.</param>
    /// <param name="contentRootRelativeToAppRoot">Path to the directory containing application content files.</param>
    /// <param name="hostPageRelativePath">Path to the host page within the <paramref name="fileProvider"/>.</param>
    public AndroidWebViewManager(AWebView webview,
        WebViewMessageHandler messageHandler,
        IDispatcher dispatcher
        //IFileProvider fileProvider,
        //JSComponentConfigurationStore jsComponents,
        //string contentRootRelativeToAppRoot,
        //string hostPageRelativePath
    )
        : base(messageHandler, dispatcher, new Uri(AppOrigin))
    {
        ArgumentNullException.ThrowIfNull(webview);

        _webview = webview;
    }

    protected override void NavigateCore(Uri absoluteUri)
    {
        _webview.LoadUrl(absoluteUri.AbsoluteUri);
    }

    public override void SendMessage(string message)
    {
        _webview.PostWebMessage(new WebMessage(message), AndroidAppOrigin);
    }
}
