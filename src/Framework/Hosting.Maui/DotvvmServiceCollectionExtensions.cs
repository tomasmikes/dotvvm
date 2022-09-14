using System;
using Microsoft.Maui.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using DotVVM.Framework.Hosting.Maui.Controls;
using DotVVM.Framework.Configuration;
using Microsoft.AspNetCore.Hosting.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using DotVVM.Framework.Hosting.Maui.Services;
using DotVVM.Framework.Controls;
using DotVVM.Framework.ResourceManagement;
using Microsoft.Extensions.FileProviders;
using DotVVM.Framework.Security;

namespace DotVVM.Framework.Hosting.Maui
{
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
            Action<DotvvmConfiguration> configure = null,
            Action<IApplicationBuilder> configureBeforeDotvvm = null,
            Action<IApplicationBuilder> configureAfterDotvvm = null)
            where TDotvvmStartup : IDotvvmStartup, IDotvvmServiceConfigurator, new()
        {
            return builder.AddMauiDotvvmWebView<TDotvvmStartup, TDotvvmStartup>(applicationPath, webRootPath, debug, configure, configureBeforeDotvvm, configureAfterDotvvm);
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
			Action<DotvvmConfiguration> configure = null,
            Action<IApplicationBuilder> configureBeforeDotvvm = null,
            Action<IApplicationBuilder> configureAfterDotvvm = null)
            where TDotvvmStartup : IDotvvmStartup, new()
			where TDotvvmServiceConfigurator : IDotvvmServiceConfigurator, new()
        {
            builder.ConfigureMauiHandlers(handlers => handlers.AddHandler<IDotvvmWebView, DotvvmWebViewHandler>());

            builder.Services.AddDotVVM<TDotvvmServiceConfigurator>();
            builder.Services.AddSingleton<ICsrfProtector, WebViewCsrfProtector>();

            var environment = RegisterEnvironment(builder, applicationPath, webRootPath, debug);
            
            builder.Services.AddSingleton<RequestDelegate>(provider => GetRequestDelegate<TDotvvmStartup>(provider, environment, debug, configure, configureBeforeDotvvm, configureAfterDotvvm));
            builder.Services.AddSingleton<DotvvmWebRequestHandler>();
            builder.Services.AddSingleton<WebViewMessageHandler>();

            return builder;
        }

        public static RequestDelegate GetRequestDelegate<TDotvvmStartup>(
            IServiceProvider provider,
            DotvvmWebHostEnvironment environment,
            bool debug,
            Action<DotvvmConfiguration> configure = null,
            Action<IApplicationBuilder> configureBeforeDotvvm = null,
            Action<IApplicationBuilder> configureAfterDotvvm = null)
            where TDotvvmStartup : IDotvvmStartup, new()
        {
            var factory = new ApplicationBuilderFactory(provider);
            
            var appBuilder = factory.CreateBuilder(new FeatureCollection());

            configureBeforeDotvvm?.Invoke(appBuilder);
            appBuilder.UseDotVVM<TDotvvmStartup>(environment.ContentRootPath, debug, config => 
            {
                ConfigureDotvvm(config);
                configure?.Invoke(config);
            });
            appBuilder.UseStaticFiles(new StaticFileOptions()
            {
                FileProvider = new PhysicalFileProvider(environment.WebRootPath)
            });
            configureAfterDotvvm?.Invoke(appBuilder);

            return appBuilder.Build();
        }

        private static void ConfigureDotvvm(DotvvmConfiguration config)
        {
            config.Resources.DefaultResourceProcessors.Add(new WebViewResourceProcessor(config));
        }

        private static DotvvmWebHostEnvironment RegisterEnvironment(MauiAppBuilder builder, string applicationPath, string? webRootPath, bool debug)
        {
            var environment = new DotvvmWebHostEnvironment()
            {
                EnvironmentName = debug ? "Development" : "Production",
                ApplicationName = System.Reflection.Assembly.GetEntryAssembly().GetName().Name,
                ContentRootPath = applicationPath,
                WebRootPath = webRootPath ?? Path.Combine(applicationPath, "wwwroot")
            };
            environment.WebRootFileProvider = new PhysicalFileProvider(environment.WebRootPath);

            builder.Services.AddSingleton<IWebHostEnvironment>(environment);
            return environment;
        }

    }
}
