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
            var steeringServoMap = ServoMap.StandardServoMap(
                rangeMin: -128, rangeMax: 127, dutyCycleMin: 0.056f, dutyCycleNeutral: 0.075f,
                dutyCycleMax: 0.094f, name: "Steering Servo Map");

            factory.AddServoMap<Steering_Servo>(
                new RemappableServoMap(new Dictionary<byte, IServoMap>{
                    {0, steeringServoMap},
                    {1, steeringServoMap.Reverse()}
                }));

            factory.AddServoMap<TBLE01_ESC>(new RemappableServoMap(new Dictionary<byte, IServoMap>{
                {0, ServoMap.SplitDualRangeServoMap(
                    rangeMin: -128, rangeNeutral: 0, rangeMax: 127, dutyCycleNeutral: 0.075f,
                    lowRangeDutyCycleMin: 0.05f, lowRangeDutyCycleMax: 0.06f, 
                    highRangeDutyCycleMin: 0.08f, highRangeDutyCycleMax: 0.085f,
                    name: "TBLE01 Standard")},
                {1, ServoMap.SplitDualRangeServoMap(
                    rangeMin: -128, rangeNeutral: 0, rangeMax: 127, dutyCycleNeutral: 0.075f,
                    lowRangeDutyCycleMin: 0.05f, lowRangeDutyCycleMax: 0.06f, 
                    highRangeDutyCycleMin: 0.08f, highRangeDutyCycleMax: 0.095f,
                    name: "TBLE01 Boost")}
            }));
        });
        serviceCollection.AddServo<Steering_Servo>();
        serviceCollection.AddServo<TBLE01_ESC>();
    }
}
