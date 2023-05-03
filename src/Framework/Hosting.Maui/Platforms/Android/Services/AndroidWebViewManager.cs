// Adapted from https://github.com/dotnet/maui

using Android.Webkit;
using AWebView = Android.Webkit.WebView;
using AUri = Android.Net.Uri;
using System.Runtime.Versioning;

namespace DotVVM.Framework.Hosting.Maui.Services;

[SupportedOSPlatform("android23.0")]
public class AndroidWebViewManager : WebViewManager
{
    internal static readonly string AppHostAddress = "0.0.0.0";

    protected static readonly string AppOrigin = $"http://{AppHostAddress}/";
    protected static readonly AUri AppOriginAndroidUri = AUri.Parse(AppOrigin);

    private readonly AWebView _webview;

    /// <summary>
    /// Constructs an instance of <see cref="AndroidWebViewManager"/>.
    /// </summary>
    /// <param name="webview">A wrapper to access platform-specific WebView APIs.</param>
    /// <param name="dispatcher">A <see cref="Dispatcher"/> instance that can marshal calls to the required thread or sync context.</param>
    public AndroidWebViewManager(AWebView webview,
        WebViewMessageHandler messageHandler,
        DotvvmWebRequestHandler dotvvmWebRequestHandler,
        IDispatcher dispatcher)
        : base(messageHandler, dotvvmWebRequestHandler, dispatcher, new Uri(AppOrigin))
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
        _webview.PostWebMessage(new WebMessage(message), AppOriginAndroidUri);
    }
    
    public void SetUpMessageChannel()
    {
        // These ports will be closed automatically when the webview gets disposed.
        var nativeToJSPorts = _webview.CreateWebMessageChannel();

        var nativeToJs = new DotvvmWebMessageCallback(message => {
            OnMessageReceived(new Uri(AppOrigin), message!);
        });

        var destPort = new[] { nativeToJSPorts[1] };

        nativeToJSPorts[0].SetWebMessageCallback(nativeToJs);
        
        _webview.PostWebMessage(new WebMessage("capturePort", destPort), AppOriginAndroidUri);
    }

    private class DotvvmWebMessageCallback : WebMessagePort.WebMessageCallback
    {
        private readonly Action<string?> _onMessageReceived;

        public DotvvmWebMessageCallback(Action<string?> onMessageReceived)
        {
            _onMessageReceived = onMessageReceived ?? throw new ArgumentNullException(nameof(onMessageReceived));
        }

        public override void OnMessage(WebMessagePort? port, WebMessage? message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            _onMessageReceived(message.Data);
        }
    }
}
