using System.Device.Gpio;
using CutilloRigby.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CutilloRigby.DesireRc.Device;

internal sealed class DeviceStartup : IConfigureServices
{
    public void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection.AddSingleton<GpioController>();
        serviceCollection.AddSingleton<StatusLed>();
        serviceCollection.AddSingleton<OnBoardButton>();

        serviceCollection.AddHostedService<Steering_Servo>();
        serviceCollection.AddHostedService<TBLE01_ESC>();
    }
}
