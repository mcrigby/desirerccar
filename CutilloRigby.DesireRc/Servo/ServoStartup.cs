using CutilloRigby.Output.Servo;
using CutilloRigby.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CutilloRigby.DesireRc.Servo;

internal sealed class ServoStartup : IConfigureServices
{
    public void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
    {
        var servoSettingsSection = configuration.GetSection("ServoSettings");
        var servoSettingsConfiguration = servoSettingsSection.Get<ServoConfiguration>(options => options.ErrorOnUnknownConfiguration = true);

        serviceCollection.AddSingleton<IServoMap>(Harness.ServoMap.SignedServoMap());
        serviceCollection.AddServoState(servoSettingsConfiguration.Chip, servoSettingsConfiguration.Name,
            servoSettingsConfiguration.Channels);
        serviceCollection.AddServoControllers();
    }
}
