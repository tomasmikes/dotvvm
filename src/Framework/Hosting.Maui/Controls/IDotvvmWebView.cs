namespace DotVVM.Framework.Hosting.Maui.Controls;

/// <summary>
/// Defines a contract for a view that renders Blazor content.
/// </summary>
public interface IDotvvmWebView : IView
{
	
	/// <summary>
	/// Gets or sets the route on which the app should be navigated.
	/// </summary>
	string RouteName { get; set; }

	/// <summary>
	/// Gets or sets the URL on which the page should be navigated.
	/// </summary>
	string Url { get; set; }

    /// <summary>
    /// Indicates wheter initialization of page is completed.
    /// </summary>
    bool IsPageLoaded { get; }

    /// <summary>
    /// Allows customizing how external links are opened.
    /// Opens external links in the system browser by default.
    /// <see cref="DotvvmWebView.NotifyExternalNavigationStarting(ExternalLinkNavigationEventArgs)"/>
    /// </summary>
    event EventHandler<ExternalLinkNavigationEventArgs>? ExternalNavigationStarting;

    /// <summary>
    /// Occurs when the page tries to notify the host window using <see cref="DotvvmWebView.NotifyPageNotificationReceived(PageNotificationEventArgs)"/>.
    /// </summary>
    event EventHandler<PageNotificationEventArgs>? PageNotificationReceived;
}
