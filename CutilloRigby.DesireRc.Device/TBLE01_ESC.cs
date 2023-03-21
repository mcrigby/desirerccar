using CutilloRigby.Input.Gamepad;
using CutilloRigby.Output.Servo;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CutilloRigby.DesireRc.Device;

public sealed class TBLE01_ESC : IHostedService
{
    private const byte _channel = 0;
    private const byte _forwardTrigger = 4;
    private const byte _reverseTrigger = 5;
    private const byte _xButton = 3;

    private readonly IGamepadState _gamepadState;
    private readonly IGamepadInputChanged _gamepadInputChanged;
    private readonly IServoState _servoState;
    private readonly StatusLed _statusLed;
    
    private DrivingMode _drivingMode = DrivingMode.ForwardOnly;

    public TBLE01_ESC(IGamepadState gamepadState, IGamepadInputChanged gamepadInputChanged, IServoState servoState, 
        StatusLed statusLed, ILogger<TBLE01_ESC> logger)
    {
        _gamepadState = gamepadState ?? throw new ArgumentNullException(nameof(gamepadState));
        _gamepadInputChanged = gamepadInputChanged ?? throw new ArgumentNullException(nameof(gamepadInputChanged));
        _servoState = servoState ?? throw new ArgumentNullException(nameof(servoState));
        _statusLed = statusLed ?? throw new ArgumentNullException(nameof(statusLed));
        SetLogHandlers(logger ?? throw new ArgumentNullException(nameof(logger)));
    }

    public DrivingMode DrivingMode
    {
        get => _drivingMode;
        private set
        {
            _drivingMode = value;
            _statusLed.SetGreenLed(_drivingMode == DrivingMode.Braking);
            setInformation_Braking(_drivingMode);
        }
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
        if (!(eventArgs.Address == _forwardTrigger || eventArgs.Address == _reverseTrigger))
            return;

        byte last = _servoState.GetChannel(_channel);
        byte current = CurrentDrive();
        
        if (last == current || _drivingMode == DrivingMode.Braking)
            return;

        _statusLed.SetBlueLed(true);

        if ((current <= TBLE01_Deadband_Lower && current >= TBLE01_Minimum) 
            && _drivingMode == DrivingMode.ForwardOnly)
        {
            DrivingMode = DrivingMode.Braking;
            
            _servoState.SetChannel(_channel, TBLE01_Minimum);
            Thread.Sleep(TBLE01_BrakeDuration);

            _servoState.SetChannel(_channel, TBLE01_Neutral);
            Thread.Sleep(TBLE01_BrakeWait);

            DrivingMode = DrivingMode.ReverseEnabled;
            current = CurrentDrive();
        }
        else if (current >= TBLE01_Deadband_Upper && current <= TBLE01_Maximum)
            DrivingMode = DrivingMode.ForwardOnly;

        _servoState.SetChannel(_channel, current);
        setInformation_Value(current);

        last = current;

        _statusLed.SetBlueLed(false);
    }

    private void Gamepad_ButtonChanged(object? sender, GamepadButtonInputEventArgs eventArgs)
    {
        if (eventArgs.Address != _xButton)
            return;

        _statusLed.SetBlueLed(true);

        if (!eventArgs.Value && DrivingMode == DrivingMode.Braking)
        {
            _servoState.SetChannel(_channel, TBLE01_Neutral);
            Thread.Sleep(TBLE01_BrakeWait);
            DrivingMode = DrivingMode.ReverseEnabled;
            var currentDrive= CurrentDrive();
            _servoState.SetChannel(_channel, currentDrive);
            setInformation_Value(currentDrive);
        }
        else if (eventArgs.Value)
        {
            DrivingMode = DrivingMode.Braking;

            if(CurrentDrive() > TBLE01_Deadband_Upper)
                _servoState.SetChannel(_channel, TBLE01_Minimum);
            else
                _servoState.SetChannel(_channel, TBLE01_Neutral);
        }

        _statusLed.SetBlueLed(false);
    }

    private byte CurrentDrive()
    {   
        short forwardTrigger = _gamepadState.GetAxis(_forwardTrigger);
        short reverseTrigger = _gamepadState.GetAxis(_reverseTrigger);
        return (byte)((forwardTrigger - reverseTrigger) >> 9);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (!_servoState.HasChannel(_channel))
            return Task.CompletedTask;

        _gamepadInputChanged.AxisChanged -= Gamepad_AxisChanged;

        _servoState.SetChannel(_channel, TBLE01_Neutral);
        setInformation_Value(TBLE01_Neutral);

        return Task.CompletedTask;
    }

    private void SetLogHandlers(ILogger logger)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            setInformation_Value = (value) =>
                logger.LogInformation("TBLE01 ESC ({channel}) set to {value}.",
                    _channel, value);
            setInformation_Braking = (state) => 
                logger.LogInformation("TBLE01 ESC ({channel}) driving mode changed to {value}",
                    _channel, state);
        }
    }

    private Action<byte> setInformation_Value = (value) => { };
    private Action<DrivingMode> setInformation_Braking = (state) => { };

    public const byte TBLE01_Minimum = 128; // 4% of -128 as unsigned byte
    public const byte TBLE01_Deadband_Lower = 251; // 4% of -128 as unsigned byte
    public const byte TBLE01_Neutral = 0;
    public const byte TBLE01_Deadband_Upper = 5; // 4% of 128
    public const byte TBLE01_Maximum = 127; // 4% of -128 as unsigned byte
    public const ushort TBLE01_BrakeDuration = 200;
    public const ushort TBLE01_BrakeWait = 800;
}
