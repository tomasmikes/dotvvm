// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace DotVVM.Framework.Hosting.Maui.Services;

/// <summary>
/// Manages activities within a web view that DotVVM pages. Platform authors
/// should subclass this to wire up the abstract and protected methods to the APIs of
/// the platform's web view.
/// </summary>
public abstract class WebViewManager : IAsyncDisposable
{
    private readonly WebViewMessageHandler _messageHandler;
    private readonly IDispatcher _dispatcher;
    private readonly Uri _appBaseUri;

    // Each time a web page connects, we establish a new per-page context
    private bool _disposed;

    /// <summary>
    /// Constructs an instance of <see cref="WebViewManager"/>.
    /// </summary>
    /// <param name="dispatcher">A <see cref="Dispatcher"/> instance that can marshal calls to the required thread or sync context.</param>
    /// <param name="appBaseUri">The base URI for the application. Since this is a webview, the base URI is typically on a private origin such as http://0.0.0.0/ or app://example/</param>
    public WebViewManager(WebViewMessageHandler messageHandler, IDispatcher dispatcher, Uri appBaseUri)
    {
        _messageHandler = messageHandler;
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _appBaseUri = EnsureTrailingSlash(appBaseUri ?? throw new ArgumentNullException(nameof(appBaseUri)));
    }

    /// <summary>
    /// Gets the <see cref="Dispatcher"/> used by this <see cref="WebViewManager"/> instance.
    /// </summary>
    public IDispatcher Dispatcher => _dispatcher;

    /// <summary>
    /// Instructs the web view to navigate to the specified location, bypassing any
    /// client-side routing.
    /// </summary>
    /// <param name="url">The URL, which may be absolute or relative to the application root.</param>
    public void Navigate(string url)
        => NavigateCore(new Uri(_appBaseUri, url));

    /// <summary>
    /// Instructs the web view to navigate to the specified location, bypassing any
    /// client-side routing.
    /// </summary>
    /// <param name="absoluteUri">The absolute URI.</param>
    protected abstract void NavigateCore(Uri absoluteUri);

    /// <summary>
    /// Sends a message to JavaScript code running in the attached web view. This must
    /// be forwarded to the DotVVM JavaScript code.
    /// </summary>
    /// <param name="message">The message.</param>
    public abstract void SendMessage(string message);

    /// <summary>
    /// Notifies the <see cref="WebViewManager"/> about a message from JavaScript running within the web view.
    /// </summary>
    /// <param name="sourceUri">The source URI for the message.</param>
    /// <param name="message">The message.</param>
    protected void OnMessageReceived(Uri sourceUri, string message)
    {
        if (!_appBaseUri.IsBaseOf(sourceUri))
        {
            // It's important that we ignore messages from other origins, otherwise if the webview
            // navigates to a remote location, it could send commands that execute locally
            return;
        }

        _ = _dispatcher.DispatchAsync(async () =>
        {
            var response = await _messageHandler.ProcessRequestOrResponse(message);
            if (response != null)
            {
                SendMessage(response);
            }
        });
    }


    private static Uri EnsureTrailingSlash(Uri uri)
        => uri.AbsoluteUri.EndsWith('/') ? uri : new Uri(uri.AbsoluteUri + '/');

    /// <summary>
    /// Disposes the current <see cref="WebViewManager"/> instance.
    /// </summary>
    protected virtual ValueTask DisposeAsyncCore()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        // Do not change this code. Put cleanup code in 'DisposeAsync(bool disposing)' method
        GC.SuppressFinalize(this);
        await DisposeAsyncCore();
    }
}
