using System;

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
	/// Allows customizing how external links are opened.
	/// Opens external links in the system browser by default.
	/// </summary>
	event EventHandler<ExternalLinkNavigationEventArgs>? ExternalNavigationStarting;
}
