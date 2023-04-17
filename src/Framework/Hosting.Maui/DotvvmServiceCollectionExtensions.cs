using DotVVM.Framework.Hosting.Maui.Controls;
using DotVVM.Framework.Configuration;
using DotVVM.Framework.Hosting.Maui.Services;

namespace DotVVM.Framework.Hosting.Maui;

public static class DotvvmServiceCollectionExtensions
{

    /// <summary>
    /// Configures <see cref="MauiAppBuilder"/> to add support for <see cref="DotvvmWebView"/>.
    /// </summary>
    /// <param name="builder">The <see cref="MauiAppBuilder"/>.</param>
    /// <returns>The <see cref="MauiAppBuilder"/>.</returns>
    public static MauiAppBuilder AddMauiDotvvmWebView<TDotvvmStartup>(
        this MauiAppBuilder builder,
        string applicationPath,
        string? webRootPath = null,
        bool debug = false,
        Action<DotvvmConfiguration> configure = null)
        where TDotvvmStartup : IDotvvmStartup, IDotvvmServiceConfigurator, new()
    {
        return builder.AddMauiDotvvmWebView<TDotvvmStartup, TDotvvmStartup>(applicationPath, webRootPath, debug,
            configure);
    }

    /// <summary>
    /// Configures <see cref="MauiAppBuilder"/> to add support for <see cref="DotvvmWebView"/>.
    /// </summary>
    /// <param name="builder">The <see cref="MauiAppBuilder"/>.</param>
    /// <returns>The <see cref="MauiAppBuilder"/>.</returns>
    public static MauiAppBuilder AddMauiDotvvmWebView<TDotvvmStartup, TDotvvmServiceConfigurator>(
        this MauiAppBuilder builder,
        string applicationPath,
        string? webRootPath = null,
        bool debug = false,
        Action<DotvvmConfiguration> configure = null)
        where TDotvvmStartup : IDotvvmStartup, new()
        where TDotvvmServiceConfigurator : IDotvvmServiceConfigurator, new()
    {
        builder.ConfigureMauiHandlers(handlers => handlers.AddHandler<IDotvvmWebView, DotvvmWebViewHandler>());

        builder.Services.AddDotVVM<TDotvvmServiceConfigurator, TDotvvmStartup>(applicationPath, config => {
            config.Debug = debug;
            ConfigureDotvvm(config);
        });

        builder.Services.AddSingleton<DotvvmWebRequestHandler>();
        builder.Services.AddSingleton<WebViewMessageHandler>();

        return builder;
    }

    private static void ConfigureDotvvm(DotvvmConfiguration config)
    {
        config.Resources.DefaultResourceProcessors.Add(new WebViewResourceProcessor(config));
    }
}
