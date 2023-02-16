using DotVVM.Framework.Configuration;
using DotVVM.Framework.Diagnostics;
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
        public static IServiceCollection AddDotVVM<TServiceConfigurator>(this IServiceCollection services)
            where TServiceConfigurator : IDotvvmServiceConfigurator, new()
        {
            var configurator = new TServiceConfigurator();
            return services.AddDotVVM(configurator);
        }

        /// <summary>
        /// Adds DotVVM services with authorization and data protection to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <param name="configurator">The <see cref="IDotvvmServiceConfigurator"/> instance.</param>
        public static IServiceCollection AddDotVVM(this IServiceCollection services, IDotvvmServiceConfigurator configurator)
        {
            AddDotVVMServices(services);

            var dotvvmServices = new DotvvmServiceCollection(services);

            startupTracer.TraceEvent(StartupTracingConstants.DotvvmConfigurationUserServicesRegistrationStarted);
            configurator.ConfigureServices(dotvvmServices);
            startupTracer.TraceEvent(StartupTracingConstants.DotvvmConfigurationUserServicesRegistrationFinished);

            return services;
        }

        /// <summary>
        /// Adds DotVVM services with authorization and data protection to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        // ReSharper disable once InconsistentNaming
        public static IServiceCollection AddDotVVM(this IServiceCollection services)
        {
            AddDotVVMServices(services);
            return services;
        }

        // ReSharper disable once InconsistentNaming
        private static void AddDotVVMServices(IServiceCollection services)
        {
            startupTracer.TraceEvent(StartupTracingConstants.AddDotvvmStarted);

            var addAuthorizationMethod =
                Type.GetType("Microsoft.Extensions.DependencyInjection.AuthorizationServiceCollectionExtensions, Microsoft.AspNetCore.Authorization", throwOnError: false)
                    ?.GetMethod("AddAuthorization", new[] { typeof(IServiceCollection) })
                ?? Type.GetType("Microsoft.Extensions.DependencyInjection.PolicyServiceCollectionExtensions, Microsoft.AspNetCore.Authorization.Policy", throwOnError: false)
                    ?.GetMethod("AddAuthorization", new[] { typeof(IServiceCollection) })
                ?? throw new InvalidOperationException("Unable to find ASP.NET Core AddAuthorization method. You are probably using an incompatible version of ASP.NET Core.");
            addAuthorizationMethod.Invoke(null, new object[] { services });

            Microsoft.Extensions.DependencyInjection.DotvvmServiceCollectionExtensions.RegisterDotVVMServices(services);

            services.TryAddSingleton<IViewModelProtector, WebViewViewModelProtector>();
            services.TryAddSingleton<IEnvironmentNameProvider, WebViewEnvironmentNameProvider>();
            services.TryAddSingleton<IRequestCancellationTokenProvider, RequestCancellationTokenProvider>();
            services.TryAddScoped<DotvvmRequestContextStorage>(_ => new DotvvmRequestContextStorage());
            services.TryAddScoped<IDotvvmRequestContext>(s => s.GetRequiredService<DotvvmRequestContextStorage>().Context);

            services.TryAddSingleton<IStartupTracer>(startupTracer);
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
