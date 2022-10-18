﻿namespace DotVVM.Framework.Hosting.Maui.Controls;

/// <summary>
/// A <see cref="View"/> that can render DotVVM content.
/// </summary>
public class DotvvmWebView : View, IDotvvmWebView
{
    /// <summary>
    /// Initializes a new instance of <see cref="DotvvmWebView"/>.
    /// </summary>
    public DotvvmWebView()
    {
    }

    public string RouteName
    {
        get { return (string)GetValue(RouteNameProperty); }
        set { SetValue(RouteNameProperty, value); }
    }

    public static readonly BindableProperty RouteNameProperty =
        BindableProperty.Create(nameof(RouteName), typeof(string), typeof(DotvvmWebView), "", BindingMode.TwoWay);

    public string Url
    {
        get { return (string)GetValue(UrlProperty); }
        set { SetValue(UrlProperty, value); }
    }

    public static readonly BindableProperty UrlProperty =
        BindableProperty.Create(nameof(Url), typeof(string), typeof(DotvvmWebView), "", BindingMode.TwoWay);

    public bool IsPageLoaded
    {
        get { return (bool)GetValue(IsPageLoadedProperty); }
        internal set { SetValue(IsPageLoadedProperty, value); }
    }
    
    internal static readonly BindableProperty IsPageLoadedProperty =
        BindableProperty.Create(nameof(IsPageLoaded), typeof(bool), typeof(DotvvmWebView), false, BindingMode.OneWayToSource);

    /// <inheritdoc/>
    public event EventHandler<ExternalLinkNavigationEventArgs>? ExternalNavigationStarting;
    public event EventHandler<PageNotificationEventArgs>? PageNotificationReceived;

    internal void NotifyExternalNavigationStarting(ExternalLinkNavigationEventArgs args)
    {
        ExternalNavigationStarting?.Invoke(this, args);
    }

    internal void NotifyPageNotificationReceived(PageNotificationEventArgs args)
    {
        PageNotificationReceived?.Invoke(this, args);
    }

    public Task<dynamic> GetViewModelSnapshot()
    {
        return ((DotvvmWebViewHandler)Handler).GetViewModelSnapshot();
    }

    public Task PatchViewModel(dynamic patch)
    {
        return ((DotvvmWebViewHandler)Handler).PatchViewModel(patch);
    }
}
