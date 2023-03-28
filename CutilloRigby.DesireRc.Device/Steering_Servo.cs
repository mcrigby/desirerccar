using CutilloRigby.Input.Gamepad;
using CutilloRigby.Output.Servo;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CutilloRigby.DesireRc.Device;

public sealed class Steering_Servo : IHostedService
{
    private const byte _leftRightStick = 0;

    private readonly IGamepadInputChanged _gamepadInputChanged;
    private readonly IServo _servo;
    private readonly StatusLed _statusLed;
        
    public Steering_Servo(IGamepadState gamepadState, IGamepadInputChanged gamepadInputChanged, 
        IServo<Steering_Servo> servo, StatusLed statusLed, ILogger<Steering_Servo> logger)
    {
        _gamepadInputChanged = gamepadInputChanged ?? throw new ArgumentNullException(nameof(gamepadInputChanged));
        _servo = servo ?? throw new ArgumentNullException(nameof(servo));
        _statusLed = statusLed ?? throw new ArgumentNullException(nameof(statusLed));
        SetLogHandlers(logger ?? throw new ArgumentNullException(nameof(logger)));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _gamepadInputChanged.AxisChanged += Gamepad_AxisChanged;
        _servo.Start();
        _servo.SetValue(SteeringServo_Neutral);
        setInformation_Value(SteeringServo_Neutral);

        return Task.CompletedTask;
    }

    private void Gamepad_AxisChanged(object? sender, GamepadAxisInputEventArgs eventArgs)
    {
        if (eventArgs.Address != _leftRightStick)
            return;

        byte last = _servo.Value;
        byte current = (byte)(eventArgs.Value >> 8);
        
        if (last == current)
            return;

        _statusLed.SetBlueLed(true);

        _servo.SetValue(current);
        setInformation_Value(current);

        last = current;

        _statusLed.SetBlueLed(false);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _gamepadInputChanged.AxisChanged -= Gamepad_AxisChanged;

        _servo.SetValue(SteeringServo_Neutral);
        setInformation_Value(SteeringServo_Neutral);
        _servo.Stop();

        return Task.CompletedTask;
    }

    private void SetLogHandlers(ILogger logger)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            setInformation_Value = (value) =>
                logger.LogInformation("Steering Servo ({channel}) set to {value})", 
                    _servo.Name, value);
        }
    }

    private Action<byte> setInformation_Value = (value) => { };

    public const byte SteeringServo_Minimum = 128;
    public const byte SteeringServo_Neutral = 0;
    public const byte SteeringServo_Maximum = 127; // 4% of -128 as unsigned byte
}
