using DotVVM.Framework.Compilation;
using DotVVM.Framework.Compilation.ControlTree;
using DotVVM.Framework.Configuration;
using DotVVM.Framework.Diagnostics;
using DotVVM.Framework.Hosting.Maui.Services;
using DotVVM.Framework.Hosting.Middlewares;
using DotVVM.Framework.Routing;
using DotVVM.Framework.Runtime.Tracing;
using DotVVM.Framework.Security;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotVVM.Framework.Hosting.Maui
{
    public static class ServiceCollectionExtensions
    {
        private static readonly DiagnosticsStartupTracer startupTracer = new DiagnosticsStartupTracer();

        /// <summary>
        /// Adds DotVVM services with authorization and data protection to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        // ReSharper disable once InconsistentNaming
        public static IServiceCollection AddDotVVM<TServiceConfigurator, TDotvvmStartup>(this IServiceCollection services,
            string applicationRootPath,
            Action<DotvvmConfiguration> modifyConfiguration = null)
            where TServiceConfigurator : IDotvvmServiceConfigurator, new()
            where TDotvvmStartup : IDotvvmStartup, new()
        {
            var configurator = new TServiceConfigurator();
            var startup = new TDotvvmStartup();
            return services.AddDotVVM(configurator, startup, applicationRootPath, modifyConfiguration);
        }

        /// <summary>
        /// Adds DotVVM services with authorization and data protection to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <param name="configurator">The <see cref="IDotvvmServiceConfigurator"/> instance.</param>
        public static IServiceCollection AddDotVVM(this IServiceCollection services,
            IDotvvmServiceConfigurator configurator,
            IDotvvmStartup startup,
            string applicationRootPath,
            Action<DotvvmConfiguration> modifyConfiguration = null)
        {
            AddDotVVMServices(services);

            var dotvvmServices = new DotvvmServiceCollection(services);

            startupTracer.TraceEvent(StartupTracingConstants.DotvvmConfigurationUserServicesRegistrationStarted);
            configurator.ConfigureServices(dotvvmServices);
            startupTracer.TraceEvent(StartupTracingConstants.DotvvmConfigurationUserServicesRegistrationFinished);

            services.TryAddSingleton<DotvvmMiddleware>(provider => 
            {
                var config = provider.GetRequiredService<DotvvmConfiguration>();
                return CreateDotvvmMiddleware(provider, config, startup, applicationRootPath, modifyConfiguration);
            });

            return services;
        }

        // ReSharper disable once InconsistentNaming
        private static void AddDotVVMServices(IServiceCollection services)
        {
            startupTracer.TraceEvent(StartupTracingConstants.AddDotvvmStarted);

            Microsoft.Extensions.DependencyInjection.DotvvmServiceCollectionExtensions.RegisterDotVVMServices(services);

            services.AddSingleton<ICsrfProtector, WebViewCsrfProtector>();
            services.TryAddSingleton<IViewModelProtector, WebViewViewModelProtector>();
            services.TryAddSingleton<IDotvvmFileProvider, MauiDotvvmFileProvider>();
            services.TryAddSingleton<IEnvironmentNameProvider, WebViewEnvironmentNameProvider>();
            services.TryAddSingleton<IRequestCancellationTokenProvider, RequestCancellationTokenProvider>();
            services.TryAddScoped<DotvvmRequestContextStorage>(_ => new DotvvmRequestContextStorage());
            services.TryAddScoped<IDotvvmRequestContext>(s => s.GetRequiredService<DotvvmRequestContextStorage>().Context);

            services.TryAddSingleton<IStartupTracer>(startupTracer);
        }

        private static DotvvmMiddleware CreateDotvvmMiddleware(IServiceProvider provider,
            DotvvmConfiguration config,
            IDotvvmStartup startup,
            string applicationRootPath,
            Action<DotvvmConfiguration> modifyConfiguration)
        {
            config.ApplicationPhysicalPath = applicationRootPath;

            startupTracer.TraceEvent(StartupTracingConstants.DotvvmConfigurationUserConfigureStarted);
            startup.Configure(config, applicationRootPath);
            CopyViews(config.RouteTable);
            startupTracer.TraceEvent(StartupTracingConstants.DotvvmConfigurationUserConfigureFinished);

            modifyConfiguration?.Invoke(config);
            config.Diagnostics.Apply(config);
            config.Freeze();
            // warm up the resolver in the background
            Task.Run(() => provider.GetService(typeof(IControlResolver)));
            Task.Run(() => VisualStudioHelper.DumpConfiguration(config, config.ApplicationPhysicalPath));

            startupTracer.TraceEvent(StartupTracingConstants.UseDotvvmStarted);

            var middlewares = new List<IMiddleware>()
            {
                ActivatorUtilities.CreateInstance<DotvvmCsrfTokenMiddleware>(provider),
                ActivatorUtilities.CreateInstance<DotvvmLocalResourceMiddleware>(provider),
                DotvvmFileUploadMiddleware.TryCreate(provider),
                ActivatorUtilities.CreateInstance<DotvvmReturnedFileMiddleware>(provider),
                ActivatorUtilities.CreateInstance<DotvvmRoutingMiddleware>(provider),
                ActivatorUtilities.CreateInstance<WebViewFileSystemMiddleware>(provider)
            }.Where(x => x != null)
            .ToList();

            var middleware = new DotvvmMiddleware(config, middlewares, config.Debug);

            startupTracer.TraceEvent(StartupTracingConstants.UseDotvvmFinished);

            var compilationConfiguration = config.Markup.ViewCompilation;
            compilationConfiguration.HandleViewCompilation(config, startupTracer);

            if (config.ServiceProvider.GetService<IDiagnosticsInformationSender>() is IDiagnosticsInformationSender sender)
            {
                startupTracer.NotifyStartupCompleted(sender);
            }

            return middleware;
            
            void CopyViews(DotvvmRouteTable dotvvmRouteTable)
            {
                var fileProvider = config.ServiceProvider.GetService<IDotvvmFileProvider>();

                var viewPaths = dotvvmRouteTable
                    .Select(x => x.VirtualPath)
                    .ToList();

                var controlViewPaths = config.Markup.Controls
                    .Where(x => x.Src != null)
                    .Select(x => x.Src)
                    .ToList();

                foreach (var viewPath in viewPaths.Concat(controlViewPaths))
                {
                    // TODO: do this better way?
                    using var manualResetEventSlim = new ManualResetEventSlim();
                    Task.Run(async () => {
                        await fileProvider.CopyFileToAppDataAsync(viewPath);
                        manualResetEventSlim.Set();
                    });
                    manualResetEventSlim.Wait();
                }
            }
        }
    }

    public class RequestCancellationTokenProvider : IRequestCancellationTokenProvider
    {
        public CancellationToken GetCancellationToken(IDotvvmRequestContext context)
            => CancellationToken.None;
    }

    public class WebViewEnvironmentNameProvider : IEnvironmentNameProvider
    {
        public string GetEnvironmentName(IDotvvmRequestContext context)
            => context.Configuration.Debug ? "Development" : "Production";
    }

    public class WebViewViewModelProtector : IViewModelProtector
    {
        public string Protect(string serializedData, IDotvvmRequestContext context) => serializedData;

        public byte[] Protect(byte[] plaintextData, params string[] purposes) => plaintextData;

        public string Unprotect(string protectedData, IDotvvmRequestContext context) => protectedData;

        public byte[] Unprotect(byte[] protectedData, params string[] purposes) => protectedData;
    }
}
