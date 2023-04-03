using CutilloRigby.DesireRc.Device;
using CutilloRigby.Input.GPS;
using CutilloRigby.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CutilloRigby.DesireRc.GPS;

internal sealed class GPSStartup : IConfigureServices
{
    public void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
    {
        var gpsSection = configuration.GetSection("GPS");
        var gpsConfigurationDictionary = gpsSection
            .Get<Dictionary<string, GPSConfiguration>>(options => options.ErrorOnUnknownConfiguration = true)
            .ToDictionary(x => x.Key, x => (IGPSConfiguration)x.Value);

        serviceCollection.AddGPSConfiguration(gpsConfigurationDictionary);
        serviceCollection.AddGPS<UltimateGPS>();
    }
}