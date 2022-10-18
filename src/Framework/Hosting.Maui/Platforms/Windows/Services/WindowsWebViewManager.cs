// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Web.WebView2.Core;
using WebView2Control = Microsoft.UI.Xaml.Controls.WebView2;
using Launcher = Windows.System.Launcher;
using DotVVM.Framework.Hosting.Maui.Controls;
using Windows.Storage.Streams;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;

namespace DotVVM.Framework.Hosting.Maui.Services;

/// <summary>
/// An implementation of <see cref="WebViewManager"/> that uses the Edge WebView2 browser control
/// to render web content.
/// </summary>
public class WindowsWebViewManager : WebViewManager
{
    // Using an IP address means that WebView2 doesn't wait for any DNS resolution,
    // making it substantially faster. Note that this isn't real HTTP traffic, since
    // we intercept all the requests within this origin.
    internal static readonly string AppHostAddress = "0.0.0.0";

    /// <summary>
    /// Gets the application's base URI. Defaults to <c>https://0.0.0.0/</c>
    /// </summary>
    protected static readonly string AppOrigin = $"https://{AppHostAddress}/";

    private readonly WebView2Control _webview;
    private readonly DotvvmWebRequestHandler _dotvvmWebRequestHandler;
    private readonly DotvvmWebViewHandler _dotvvmWebViewHandler;
    private readonly Task _webviewReadyTask;

    private protected CoreWebView2Environment? _coreWebView2Environment;

    /// <summary>
    /// Constructs an instance of <see cref="WindowsWebViewManager"/>.
    /// </summary>
    /// <param name="webview">A <see cref="WebView2Control"/> to access platform-specific WebView2 APIs.</param>
    /// <param name="dispatcher">A <see cref="Dispatcher"/> instance that can marshal calls to the required thread or sync context.</param>
    /// <param name="dotvvmWebRequestHandler">Provides static content to the webview.</param>
    /// <param name="dotvvmWebViewHandler">The <see cref="DotvvmWebViewHandler" />.</param>
    public WindowsWebViewManager(
        WebView2Control webview,
        WebViewMessageHandler messageHandler,
        IDispatcher dispatcher,
        DotvvmWebRequestHandler dotvvmWebRequestHandler,
        DotvvmWebViewHandler dotvvmWebViewHandler
    )
        : base(messageHandler, dispatcher, new Uri(AppOrigin))
    {
        _webview = webview ?? throw new ArgumentNullException(nameof(webview));
        _dotvvmWebRequestHandler = dotvvmWebRequestHandler;
        _dotvvmWebViewHandler = dotvvmWebViewHandler;

        // Unfortunately the CoreWebView2 can only be instantiated asynchronously.
        // We want the external API to behave as if initalization is synchronous,
        // so keep track of a task we can await during LoadUri.
        _webviewReadyTask = InitializeWebView2();
    }

    /// <inheritdoc />
    protected override void NavigateCore(Uri absoluteUri)
    {
        _ = Dispatcher.DispatchAsync(async () =>
        {
            await _webviewReadyTask;
            _webview.Source = absoluteUri;
        });
    }

    /// <inheritdoc />
    public override void SendMessage(string message)
        => _webview.CoreWebView2.PostWebMessageAsString(message);

    private async Task InitializeWebView2()
    {
        _coreWebView2Environment = await CoreWebView2Environment.CreateAsync()
            .AsTask()
            .ConfigureAwait(true);
        await _webview.EnsureCoreWebView2Async();

        ApplyDefaultWebViewSettings();

        _webview.CoreWebView2.AddWebResourceRequestedFilter($"{AppOrigin}*", CoreWebView2WebResourceContext.All);

        _webview.CoreWebView2.WebResourceRequested += async (s, eventArgs) =>
        {
            await HandleWebResourceRequest(eventArgs);
        };

        _webview.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
        _webview.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;

        // The code inside dotvvm.webview.js is meant to be agnostic to specific webview technologies,
        // so the following is an adaptor from dotvvm.webview.js conventions to WebView2 APIs
        await _webview.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
                window.external = {
                    sendMessage: message => {
                        window.chrome.webview.postMessage(message);
                    },
                    receiveMessage: callback => {
                        window.chrome.webview.addEventListener('message', e => callback(e.data));
                    }
                };
            ")
            .AsTask()
            .ConfigureAwait(true);

        _webview.CoreWebView2.WebMessageReceived += (s, e) => OnMessageReceived(new Uri(e.Source), e.WebMessageAsJson);
    }

    /// <summary>
    /// Handles outbound URL requests.
    /// </summary>
    /// <param name="eventArgs">The <see cref="CoreWebView2WebResourceRequestedEventArgs"/>.</param>
    protected virtual async Task HandleWebResourceRequest(CoreWebView2WebResourceRequestedEventArgs eventArgs)
    {
        // Get a deferral object so that WebView2 knows there's some async stuff going on. We call Complete() at the end of this method.
        using var deferral = eventArgs.GetDeferral();

        var requestUri = new Uri(eventArgs.Request.Uri);
        var response = await _dotvvmWebRequestHandler.ProcessRequest(requestUri, eventArgs.Request.Method,
            eventArgs.Request.Headers, eventArgs.Request.Content?.AsStreamForRead());

        using var ms = new InMemoryRandomAccessStream();
        await ms.WriteAsync(response.Content.GetWindowsRuntimeBuffer());

        eventArgs.Response = _coreWebView2Environment!.CreateWebResourceResponse(ms, response.StatusCode,
            ((HttpStatusCode)response.StatusCode).ToString(), GetHeaderString(response.Headers));

        // Notify WebView2 that the deferred (async) operation is complete and we set a response.
        deferral.Complete();
    }

    private void CoreWebView2_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs args)
    {
        _dotvvmWebViewHandler.IsPageLoaded = false;

        if (Uri.TryCreate(args.Uri, UriKind.RelativeOrAbsolute, out var uri) && uri.Host != AppHostAddress)
        {
            var callbackArgs = new ExternalLinkNavigationEventArgs(uri);
            _dotvvmWebViewHandler.ExternalNavigationStarting?.Invoke(callbackArgs);

            if (callbackArgs.ExternalLinkNavigationPolicy == ExternalLinkNavigationPolicy.OpenInExternalBrowser)
            {
                LaunchUriInExternalBrowser(uri);
            }

            args.Cancel = callbackArgs.ExternalLinkNavigationPolicy !=
                          ExternalLinkNavigationPolicy.InsecureOpenInWebView;
        }
    }

    private void CoreWebView2_NewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs args)
    {
        // Intercept _blank target <a> tags to always open in device browser.
        // The ExternalLinkCallback is not invoked.
        if (Uri.TryCreate(args.Uri, UriKind.RelativeOrAbsolute, out var uri))
        {
            LaunchUriInExternalBrowser(uri);
            args.Handled = true;
        }
    }

    private void LaunchUriInExternalBrowser(Uri uri)
    {
        _ = Launcher.LaunchUriAsync(uri);
    }

    private protected static string GetHeaderString(IEnumerable<KeyValuePair<string, string>> headers) =>
        string.Join(Environment.NewLine, headers.Select(kvp => $"{kvp.Key}: {kvp.Value}"));

    private void ApplyDefaultWebViewSettings()
    {
        _webview.CoreWebView2.Settings.AreDevToolsEnabled = true;

        // Desktop applications typically don't want the default web browser context menu
        _webview.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;

        // Desktop applications almost never want to show a URL preview when hovering over a link
        _webview.CoreWebView2.Settings.IsStatusBarEnabled = false;
    }
}
