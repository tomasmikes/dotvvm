namespace DotVVM.Framework.Hosting.Maui.Services
{
    public class DotvvmWebRequestHandler
    {
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly DotvvmMiddleware dotvvmMiddleware;

        public DotvvmWebRequestHandler(IServiceScopeFactory serviceScopeFactory, DotvvmMiddleware dotvvmMiddleware)
        {
            this.serviceScopeFactory = serviceScopeFactory;
            this.dotvvmMiddleware = dotvvmMiddleware;
        }

        public async Task<DotvvmResponse> ProcessRequest(Uri requestUri, string method, IEnumerable<KeyValuePair<string, string>> headers, Stream contentStream)
        {
			using var scope = serviceScopeFactory.CreateScope();
			
            var request = new DotvvmRequest(requestUri, method, headers, contentStream);
			var context = await dotvvmMiddleware.Invoke(request, scope);

			return new DotvvmResponse(
				context.Response.StatusCode,
                context.Response.Headers.SelectMany(h => h.Value.Select(v => new KeyValuePair<string, string>(h.Key, v))),
				(MemoryStream)context.Response.Body);
		}

    }

    public record DotvvmResponse(int StatusCode, IEnumerable<KeyValuePair<string, string>> Headers, MemoryStream Content);

    public record DotvvmRequest(Uri RequestUri,
        string Method,
        IEnumerable<KeyValuePair<string, string>> Headers,
        Stream ContentStream);
}
