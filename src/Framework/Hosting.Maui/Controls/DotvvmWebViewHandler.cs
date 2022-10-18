using DotVVM.Framework.Configuration;
using DotVVM.Framework.Hosting.Maui.Services;

namespace DotVVM.Framework.Hosting.Maui.Controls;

public partial class DotvvmWebViewHandler
{
    internal readonly WebViewMessageHandler _messageHandler;
    internal Action<ExternalLinkNavigationEventArgs>? ExternalNavigationStarting;
    internal Action<PageNotificationEventArgs>? PageNotificationReceived;

    private string routeName;
    public string RouteName
    {
        get
        {
            return routeName;
        }
        set
        {
            if (!string.IsNullOrEmpty(value) && value != routeName)
            {
                NavigateToRoute(value);
            }
        }
    }

    public string Url
    {
        get
        {
            return GetUrl();
        }
        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                _webviewManager?.Navigate(value);
            }
        }
    }

    private bool isPageLoaded;
    public bool IsPageLoaded
    {
        get
        {
            return isPageLoaded;
        }
        set
        {
            isPageLoaded = value;
            ((DotvvmWebView)VirtualView).IsPageLoaded = value;
        }
    }

    partial void StartWebViewCoreIfPossible();

    private partial string GetUrl();

    /// <summary>
    /// This field is part of MAUI infrastructure and is not intended for use by application code.
    /// </summary>
    public static readonly PropertyMapper<IDotvvmWebView, DotvvmWebViewHandler> DotvvmWebViewMapper = new(ViewMapper) {
        [nameof(IDotvvmWebView.ExternalNavigationStarting)] = MapNotifyExternalNavigationStarting,
        [nameof(IDotvvmWebView.PageNotificationReceived)] = MapPageNotificationReceived,
        [nameof(IDotvvmWebView.RouteName)] = MapRouteName,
        [nameof(IDotvvmWebView.Url)] = MapUrl
    };

    /// <summary>
    /// Initializes a new instance of <see cref="DotvvmWebViewHandler"/> with default mappings.
    /// </summary>
    public DotvvmWebViewHandler(WebViewMessageHandler messageHandler)
        : this(DotvvmWebViewMapper)
    {
        _messageHandler = messageHandler;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="DotvvmWebViewHandler"/> using the specified mappings.
    /// </summary>
    /// <param name="mapper">The property mappings.</param>
    public DotvvmWebViewHandler(PropertyMapper? mapper)
        : base(mapper ?? DotvvmWebViewMapper)
    {
    }


    /// <summary>
    /// Maps the <see cref="IDotvvmWebView.HostPage"/> property to the specified handler.
    /// </summary>
    /// <param name="handler">The <see cref="DotvvmWebViewHandler"/>.</param>
    /// <param name="webView">The <see cref="IDotvvmWebView"/>.</param>
    public static void MapRouteName(DotvvmWebViewHandler handler, IDotvvmWebView webView)
    {
        handler.RouteName = webView.RouteName;
        handler.StartWebViewCoreIfPossible();
    }


    /// <summary>
    /// Maps the <see cref="IDotvvmWebView.HostPage"/> property to the specified handler.
    /// </summary>
    /// <param name="handler">The <see cref="DotvvmWebViewHandler"/>.</param>
    /// <param name="webView">The <see cref="IDotvvmWebView"/>.</param>
    public static void MapUrl(DotvvmWebViewHandler handler, IDotvvmWebView webView)
    {
        handler.Url = webView.Url;
        handler.StartWebViewCoreIfPossible();
    }

    /// <summary>
    /// Maps the <see cref="DotvvmWebView.NotifyExternalNavigationStarting"/> property to the specified handler.
    /// </summary>
    /// <param name="handler">The <see cref="DotvvmWebViewHandler"/>.</param>
    /// <param name="webView">The <see cref="IDotvvmWebView"/>.</param>
    public static void MapNotifyExternalNavigationStarting(DotvvmWebViewHandler handler, IDotvvmWebView webView)
    {
        if (webView is DotvvmWebView dotvvmWebView)
        {
            handler.ExternalNavigationStarting = dotvvmWebView.NotifyExternalNavigationStarting;
        }
    }

    /// <summary>
    /// Maps the <see cref="DotvvmWebView.MapPageNotificationReceived"/> property to the specified handler.
    /// </summary>
    /// <param name="handler">The <see cref="DotvvmWebViewHandler"/>.</param>
    /// <param name="webView">The <see cref="IDotvvmWebView"/>.</param>
    public static void MapPageNotificationReceived(DotvvmWebViewHandler handler, IDotvvmWebView webView)
    {
        if (webView is DotvvmWebView dwv)
        {
            handler.PageNotificationReceived = dwv.NotifyPageNotificationReceived;
        }
    }

    protected void NavigateToRoute(string value)
    {
        // make sure DotVVM is initialized
        _ = Services.GetRequiredService<DotvvmWebRequestHandler>();

        var route = Services.GetRequiredService<DotvvmConfiguration>().RouteTable[value];
        var url = route.BuildUrl().TrimStart('~');

        routeName = value;
        Url = url;
    }

    internal Task<dynamic> GetViewModelSnapshot()
    {
        var message = _messageHandler.CreateCommandMessage("GetViewModelSnapshot");

        _webviewManager.SendMessage(_messageHandler.SerializeObject(message));
        return _messageHandler.WaitForMessage<dynamic>(message.MessageId);
    }

    internal Task<dynamic> PatchViewModel(dynamic patch)
    {
        var jsonPatch = _messageHandler.SerializeObject(patch, false);
        var message = _messageHandler.CreateCommandMessage("PatchViewModel", jsonPatch);

        _webviewManager.SendMessage(_messageHandler.SerializeObject(message));
        return _messageHandler.WaitForMessage<dynamic>(message.MessageId);
    }
}
