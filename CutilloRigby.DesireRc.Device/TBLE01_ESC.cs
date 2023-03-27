using CutilloRigby.Input.Gamepad;
using CutilloRigby.Output.Servo;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CutilloRigby.DesireRc.Device;

public sealed class TBLE01_ESC : IHostedService
{
    private const byte _aButton = 0;
    private const byte _xButton = 3;
    private const byte _forwardTrigger = 4;
    private const byte _reverseTrigger = 5;

    private readonly IGamepadState _gamepadState;
    private readonly IGamepadInputChanged _gamepadInputChanged;
    private readonly IServo _servo;
    private readonly IRemappableServoMap _servoMap;
    private readonly StatusLed _statusLed;
    
    private DrivingMode _drivingMode = DrivingMode.ForwardOnly;

    public TBLE01_ESC(IGamepadState gamepadState, IGamepadInputChanged gamepadInputChanged, IServo<TBLE01_ESC> servo, 
        IRemappableServoMap<TBLE01_ESC> servoMap, StatusLed statusLed, ILogger<TBLE01_ESC> logger)
    {
        _gamepadState = gamepadState ?? throw new ArgumentNullException(nameof(gamepadState));
        _gamepadInputChanged = gamepadInputChanged ?? throw new ArgumentNullException(nameof(gamepadInputChanged));
        _servo = servo ?? throw new ArgumentNullException(nameof(servo));
        _servoMap = servoMap ?? throw new ArgumentNullException(nameof(servoMap));
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
        _gamepadInputChanged.AxisChanged += Gamepad_AxisChanged;
        _gamepadInputChanged.ButtonChanged += Gamepad_ButtonChanged;

        return Task.CompletedTask;
    }

    private void Gamepad_AxisChanged(object? sender, GamepadAxisInputEventArgs eventArgs)
    {
        if (!(eventArgs.Address == _forwardTrigger || eventArgs.Address == _reverseTrigger))
            return;

        byte last = _servo.Value;
        byte current = CurrentDrive();
        
        if (last == current || _drivingMode == DrivingMode.Braking)
            return;

        _statusLed.SetBlueLed(true);

        if ((current <= TBLE01_Deadband_Lower && current >= TBLE01_Minimum) 
            && _drivingMode == DrivingMode.ForwardOnly)
        {
            DrivingMode = DrivingMode.Braking;
            
            _servo.SetValue(TBLE01_Minimum);
            Thread.Sleep(TBLE01_BrakeDuration);

            _servo.SetValue(TBLE01_Neutral);
            Thread.Sleep(TBLE01_BrakeWait);

            DrivingMode = DrivingMode.ReverseEnabled;
            current = CurrentDrive();
        }
        else if (current >= TBLE01_Deadband_Upper && current <= TBLE01_Maximum)
            DrivingMode = DrivingMode.ForwardOnly;

        _servo.SetValue(current);
        setInformation_Value(current);

        last = current;

        _statusLed.SetBlueLed(false);
    }

    private void Gamepad_ButtonChanged(object? sender, GamepadButtonInputEventArgs eventArgs)
    {
        _statusLed.SetBlueLed(true);

        if (eventArgs.Address == _aButton)
        {
            _servoMap.Remap((byte)(eventArgs.Value ? 1 : 0));
            var currentValue = _servo.Value;
            _servo.SetValue(0);
            _servo.SetValue(currentValue);
            setInformation_Map(_servoMap.Name);
        }

        if (eventArgs.Address != _xButton)
        {
            _statusLed.SetBlueLed(false);
            return;
        }

        if (!eventArgs.Value && DrivingMode == DrivingMode.Braking)
        {
            _servo.SetValue(TBLE01_Neutral);
            Thread.Sleep(TBLE01_BrakeWait);
            DrivingMode = DrivingMode.ReverseEnabled;
            var currentDrive = CurrentDrive();
            _servo.SetValue(currentDrive);
            setInformation_Value(currentDrive);
        }
        else if (eventArgs.Value)
        {
            DrivingMode = DrivingMode.Braking;

            if(CurrentDrive() > TBLE01_Deadband_Upper)
                _servo.SetValue(TBLE01_Minimum);
            else
                _servo.SetValue(TBLE01_Neutral);
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
        _gamepadInputChanged.AxisChanged -= Gamepad_AxisChanged;
        _gamepadInputChanged.ButtonChanged -= Gamepad_ButtonChanged;

        _servo.SetValue(TBLE01_Neutral);
        setInformation_Value(TBLE01_Neutral);

        return Task.CompletedTask;
    }

    private void SetLogHandlers(ILogger logger)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            setInformation_Value = (value) =>
                logger.LogInformation("TBLE01 ESC ({channel}) set to {value}.",
                    _servo.Name, value);
            setInformation_Braking = (state) => 
                logger.LogInformation("TBLE01 ESC ({channel}) driving mode changed to {value}",
                    _servo.Name, state);
            setInformation_Map = (mapName) =>
                logger.LogInformation("TBLE01 ESC ({channel}) servo map changed to {mapName}",
                    _servo.Name, mapName);
        }
    }

    private Action<byte> setInformation_Value = (value) => { };
    private Action<DrivingMode> setInformation_Braking = (state) => { };
    private Action<string> setInformation_Map = (mapName) => { };

    public const byte TBLE01_Minimum = 128; // 4% of -128 as unsigned byte
    public const byte TBLE01_Deadband_Lower = 251; // 4% of -128 as unsigned byte
    public const byte TBLE01_Neutral = 0;
    public const byte TBLE01_Deadband_Upper = 5; // 4% of 128
    public const byte TBLE01_Maximum = 127; // 4% of -128 as unsigned byte
    public const ushort TBLE01_BrakeDuration = 200;
    public const ushort TBLE01_BrakeWait = 800;
}
