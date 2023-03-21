using CutilloRigby.Output.Servo;
using System.Device.Pwm;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CutilloRigby.DesireRc.Device;

public sealed class Steering_Servo : IHostedService
{
    private const byte _channel = 1;

    private readonly IServoState _servoSettings;
    private readonly IServoMap _servoMap;
    
    private readonly PwmChannel _steeringPwm;
    
    public Steering_Servo(IServoState servoSettings, IServoMap servoMap, ILogger<Steering_Servo> logger)
    {
        _servoSettings = servoSettings ?? throw new ArgumentNullException(nameof(servoSettings));
        _servoMap = servoMap ?? throw new ArgumentNullException(nameof(servoMap));
        SetLogHandlers(logger ?? throw new ArgumentNullException(nameof(logger)));

        _steeringPwm = PwmChannel.Create(0, _channel, 50, _servoMap[0]);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!_servoSettings.HasChannel(_channel))
            return;// Task.CompletedTask;

        byte lastSteering = 127;
        float dutyCycle = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            byte currentSteering = _servoSettings.GetChannel(_channel);
            if (lastSteering != currentSteering)
            {
                dutyCycle = _servoMap[currentSteering];
                _steeringPwm.DutyCycle = dutyCycle;

                setInformation_Value(currentSteering, dutyCycle);
                lastSteering = currentSteering;
            }

            await Task.Delay(1);
        }

        _steeringPwm.DutyCycle = _servoMap[0];

        return; // Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void SetLogHandlers(ILogger logger)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            setInformation_Value = (value, dutyCycle) =>
                logger.LogInformation("Steering Servo ({channel}) set to {value} (Duty Cycle: {dutyCycle:n3})", 
                    _channel, value, dutyCycle);
        }
    }

    private Action<byte, float> setInformation_Value = (value, dutyCycle) => { };
}
