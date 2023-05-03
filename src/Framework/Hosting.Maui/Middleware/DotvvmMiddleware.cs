using DotVVM.Framework.Configuration;
using DotVVM.Framework.Hosting.ErrorPages;
using DotVVM.Framework.Hosting.Maui.Services;
using DotVVM.Framework.Hosting.Middlewares;
using IDotvvmMiddleware = DotVVM.Framework.Hosting.Middlewares.IMiddleware;

namespace DotVVM.Framework.Hosting.Maui
{
    /// <summary>
    /// A middleware that handles DotVVM HTTP requests.
    /// </summary>
    public class DotvvmMiddleware : DotvvmMiddlewareBase
    {
        public readonly DotvvmConfiguration Configuration;
        private readonly IList<IDotvvmMiddleware> middlewares;
        private readonly bool useErrorPage;

        /// <summary>
        /// Initializes a new instance of the <see cref="DotvvmMiddleware" /> class.
        /// </summary>
        public DotvvmMiddleware(DotvvmConfiguration configuration, IList<IDotvvmMiddleware> middlewares, bool useErrorPage)
        {
            Configuration = configuration;
            this.middlewares = middlewares;
            this.useErrorPage = useErrorPage;
        }

        /// <summary>
        /// Process an individual request.
        /// </summary>
        public async Task<IHttpContext> Invoke(DotvvmRequest dotvvmRequest, IServiceScope scope)
        {
            // create the context
            var dotvvmContext = CreateDotvvmContext(dotvvmRequest, scope);
            dotvvmContext.Services.GetRequiredService<DotvvmRequestContextStorage>().Context = dotvvmContext;
            dotvvmContext.HttpContext.SetItem(HostingConstants.DotvvmRequestContextOwinKey, dotvvmContext);

            try
            {
                foreach (var middleware in middlewares)
                {
                    if (await middleware.Handle(dotvvmContext))
                    {
                        return dotvvmContext.HttpContext;
                    }
                }

                dotvvmContext.HttpContext.Response.StatusCode = 404;
                dotvvmContext.HttpContext.Response.Body.SetLength(0);
                dotvvmContext.HttpContext.Response.Write("Not found");
                return dotvvmContext.HttpContext;
            }
            catch (DotvvmInterruptRequestExecutionException)
            {
                return dotvvmContext.HttpContext;
            }
            catch (Exception ex) when (useErrorPage)
            {
                dotvvmContext.HttpContext.Response.StatusCode = 500;
                var dotvvmErrorPageRenderer = dotvvmContext.Services.GetRequiredService<DotvvmErrorPageRenderer>();
                await dotvvmErrorPageRenderer.RenderErrorResponse(dotvvmContext.HttpContext, ex);
                return dotvvmContext.HttpContext;
            }
        }

        public static IHttpContext ConvertHttpContext(DotvvmRequest dotvvmRequest)
        {
            return new DotvvmHttpContext(dotvvmRequest);
        }

        protected DotvvmRequestContext CreateDotvvmContext(DotvvmRequest dotvvmRequest, IServiceScope scope)
        {
            return new DotvvmRequestContext(
                ConvertHttpContext(dotvvmRequest),
                Configuration,
                scope.ServiceProvider
            );
        }
    }
}
