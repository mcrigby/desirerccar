using CutilloRigby.DesireRc.Device;
using CutilloRigby.Output.Servo;
using CutilloRigby.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CutilloRigby.DesireRc.Servo;

internal sealed class ServoStartup : IConfigureServices
{
    public void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
    {
        var servoConfigurationSection = configuration.GetSection("Servo");
        var servoConfigurationDictionary = servoConfigurationSection
            .Get<Dictionary<string, ServoConfiguration>>(options => options.ErrorOnUnknownConfiguration = true)
            .ToDictionary(x => x.Key, x => (IServoConfiguration)x.Value);

        serviceCollection.AddServoConfiguration(servoConfigurationDictionary);
        serviceCollection.AddServoMap(configure: factory => {
            factory.AddServoMap("CutilloRigby.DesireRc.Device.Steering_Servo", ServoMap.CustomServoMap(
                rangeStart: -128, dutyCycleMin: 0.056f, dutyCycleMax: 0.094f,
                name: "Steering Servo Map"));
            factory.AddServoMap("CutilloRigby.DesireRc.Device.TBLE01_ESC", new RemappableServoMap(new Dictionary<byte, IServoMap>{
                {0, ServoMap.CustomServoMap(rangeStart: -128, dutyCycleMin: 0.068f, dutyCycleMax: 0.082f, name: "TBLE01 Standard")},
                {1, ServoMap.SignedServoMap(name: "TBLE01 Boost")}
            }));
        });
        serviceCollection.AddServo<Steering_Servo>();
        serviceCollection.AddServo<TBLE01_ESC>();
    }
}
