using DotVVM.Framework.Security;
using System.Security;

namespace DotVVM.Framework.Hosting.Maui.Services
{
    public class WebViewCsrfProtector : ICsrfProtector
    {
        public string GenerateToken(IDotvvmRequestContext context)
        {
            return "webviewToken";
        }

        public void VerifyToken(IDotvvmRequestContext context, string token)
        {
            if (token != "webviewToken")
            {
                throw new SecurityException("Invalid CSRF token!");
            }
        }
    }
}
