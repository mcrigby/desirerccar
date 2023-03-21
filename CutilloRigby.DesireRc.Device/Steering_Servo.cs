using CutilloRigby.Input.Gamepad;
using CutilloRigby.Output.Servo;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CutilloRigby.DesireRc.Device;

public sealed class Steering_Servo : IHostedService
{
    private const byte _channel = 1;
    private const byte _leftRightStick = 0;

    private readonly IGamepadInputChanged _gamepadInputChanged;
    private readonly IServoState _servoState;
    private readonly StatusLed _statusLed;
        
    public Steering_Servo(IGamepadState gamepadState, IGamepadInputChanged gamepadInputChanged, IServoState servoState, 
        StatusLed statusLed, ILogger<Steering_Servo> logger)
    {
        _gamepadInputChanged = gamepadInputChanged ?? throw new ArgumentNullException(nameof(gamepadInputChanged));
        _servoState = servoState ?? throw new ArgumentNullException(nameof(servoState));
        _statusLed = statusLed ?? throw new ArgumentNullException(nameof(statusLed));
        SetLogHandlers(logger ?? throw new ArgumentNullException(nameof(logger)));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_servoState.HasChannel(_channel))
            return Task.CompletedTask;

        _gamepadInputChanged.AxisChanged += Gamepad_AxisChanged;
        return Task.CompletedTask;
    }

    private void Gamepad_AxisChanged(object? sender, GamepadAxisInputEventArgs eventArgs)
    {
        if (eventArgs.Address != _leftRightStick)
            return;

        byte last = _servoState.GetChannel(_channel);
        byte current = (byte)(eventArgs.Value >> 8);
        
        if (last == current)
            return;

        _statusLed.SetBlueLed(true);

        _servoState.SetChannel(_channel, current);
        setInformation_Value(current);

        last = current;

        _statusLed.SetBlueLed(false);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void SetLogHandlers(ILogger logger)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            setInformation_Value = (value) =>
                logger.LogInformation("Steering Servo ({channel}) set to {value})", 
                    _channel, value);
        }
    }

    private Action<byte> setInformation_Value = (value) => { };
}
