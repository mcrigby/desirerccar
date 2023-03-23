using CutilloRigby.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CutilloRigby.DesireRc.Gamepad;

internal sealed class GamepadStartup : IConfigureServices
{
    public void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
    {
        var gamepadSettingsSection = configuration.GetSection("GamepadSettings");
        var gamepadSettingsConfiguration = gamepadSettingsSection.Get<GamepadConfiguration>(options => options.ErrorOnUnknownConfiguration = true);

        serviceCollection.AddGamepadState(gamepadSettingsConfiguration.Name, gamepadSettingsConfiguration.DeviceFile,
            gamepadSettingsConfiguration.Axes, gamepadSettingsConfiguration.Buttons);
            
        serviceCollection.AddGamepadController();

        serviceCollection.AddHostedService<GamepadAvailabilityMonitor>();
    }
}