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
        return Task.CompletedTask;
    }

    private void Gamepad_AxisChanged(object? sender, GamepadAxisInputEventArgs eventArgs)
    {
        if (eventArgs.Address != _leftRightStick)
            return;

        byte last = _servo.Value;
        byte current = (byte)(eventArgs.Value >> 8);
        
        if (96 < current && current <= 127)
            current = 96;
        if (128 <= current && current < 150)
            current = 150;
        
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
        _servo.SetValue(0);

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
}
