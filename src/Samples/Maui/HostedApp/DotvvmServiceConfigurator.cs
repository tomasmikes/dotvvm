using DotVVM.Framework.Configuration;
using DotVVM.Samples.Common;

namespace DotVVM.Samples.BasicSamples.Maui.HostedApp
{
    public class DotvvmServiceConfigurator : IDotvvmServiceConfigurator
    {
        public void ConfigureServices(IDotvvmServiceCollection services)
        {
            CommonConfiguration.ConfigureServices(services);
            services.AddDefaultTempStorages("Temp");
        }
    }
}
