using CutilloRigby.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CutilloRigby.DesireRc.Gamepad;

internal sealed class GamepadStartup : IConfigureServices
{
    public void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
    {
        var gamepadSection = configuration.GetSection("Gamepad");
        var gamepadConfiguration = gamepadSection.Get<GamepadConfiguration>(options => options.ErrorOnUnknownConfiguration = true);

        serviceCollection.AddGamepadState(gamepadConfiguration.Name, gamepadConfiguration.DeviceFile,
            gamepadConfiguration.Axes, gamepadConfiguration.Buttons);
            
        serviceCollection.AddGamepadController();

        serviceCollection.AddHostedService<GamepadAvailabilityMonitor>();
    }
}