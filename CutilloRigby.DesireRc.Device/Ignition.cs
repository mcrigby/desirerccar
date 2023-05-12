using System.Device.Gpio;
using CutilloRigby.Input.Gamepad;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CutilloRigby.DesireRc.Device;

public sealed class Ignition : IHostedService
{
    private const int IgnitionHoldTime = 3000;
    private const int ExtinguishIdleTime = 5000;

    private readonly GpioController _gpioController;
    private readonly IGamepadState _gamepadState;
    private readonly IGamepadInputChanged _gamepadInputChanged;    
    private readonly StatusLed _statusLed;
    
    private readonly Timer _ignitionTimer;
    private readonly Timer _idleTimer;

    private const int relayEnablePin = 25;
    private const byte _ignitionGamepadButton = 9; // Right Trigger

    public Ignition(GpioController gpioController, IGamepadState gamepadState, IGamepadInputChanged gamepadInputChanged, 
        IGamepadAvailable gamepadAvailable, StatusLed statusLed, ILogger<Ignition> logger)
    {
        _gpioController = gpioController ?? throw new ArgumentNullException(nameof(gpioController));
        _gamepadInputChanged = gamepadInputChanged ?? throw new ArgumentNullException(nameof(gamepadInputChanged));
        _gamepadState = gamepadState ?? throw new ArgumentNullException(nameof(gamepadState));
        _statusLed = statusLed ?? throw new ArgumentNullException(nameof(statusLed));
        SetLoggers(logger ?? throw new ArgumentNullException(nameof(logger)));

        _gpioController.OpenPin(relayEnablePin, PinMode.Output, PinValue.Low);

        if (gamepadAvailable == null) 
            throw new ArgumentNullException(nameof(gamepadAvailable));
        
        gamepadAvailable.AvailableChanged += (s, e) => {
            if (!e.Value)
                SetIgnition(false);
        };

        _ignitionTimer = new Timer(IgnitionTimer_Callback, null, Timeout.Infinite, Timeout.Infinite);
        _idleTimer = new Timer(IdleTimer_Callback, null, Timeout.Infinite, Timeout.Infinite);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Gamepad_ButtonChanged = ButtonChangedWhenIgnitionOff;
        _gamepadInputChanged.ButtonChanged += Gamepad_ButtonChanged;

        return Task.CompletedTask;
    }

    private EventHandler<GamepadButtonInputEventArgs> Gamepad_ButtonChanged = (s, e) => { };

    private void ButtonChangedWhenIgnitionOff(object? sender, GamepadButtonInputEventArgs eventArgs)
    {
        if (eventArgs.Address != _ignitionGamepadButton)
            return;

        _statusLed.SetBlueLed(true);

        if (eventArgs.Value)
            _ignitionTimer.Change(IgnitionHoldTime, Timeout.Infinite);
        else
            _ignitionTimer.Change(Timeout.Infinite, Timeout.Infinite);

        _statusLed.SetBlueLed(false);
    }

    private void ButtonChangedWhenIgnitionOn(object? sender, GamepadButtonInputEventArgs eventArgs)
    {
        _idleTimer.Change(ExtinguishIdleTime, Timeout.Infinite);
    }
    private void Gamepad_AxisChanged(object? sender, GamepadAxisInputEventArgs eventArgs)
    {
        _idleTimer.Change(ExtinguishIdleTime, Timeout.Infinite);
    }

    private void IgnitionTimer_Callback(object state)
    {
        SetIgnition(true);
    }
    private void IdleTimer_Callback(object state)
    {
        SetIgnition(false);
    }

    public bool IgnitionOn { get; private set; }

    public void SetIgnition(bool value)
    {
        if (IgnitionOn == value)
            return;
            
        if (value)
        {
            Gamepad_ButtonChanged = ButtonChangedWhenIgnitionOn;
            _gamepadInputChanged.AxisChanged += Gamepad_AxisChanged;
            _idleTimer.Change(ExtinguishIdleTime, Timeout.Infinite);
        }
        else
        {
            Gamepad_ButtonChanged = ButtonChangedWhenIgnitionOff;
            _gamepadInputChanged.AxisChanged -= Gamepad_AxisChanged;
            _idleTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        _gpioController.Write(relayEnablePin, value ? PinValue.High : PinValue.Low);
        information_SetIgnition(value);

        IgnitionOn = value;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        SetIgnition(false);
        _gamepadInputChanged.ButtonChanged -= Gamepad_ButtonChanged;

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _gpioController.ClosePin(relayEnablePin);
    }

    private void SetLoggers(ILogger logger)
    {
        if (logger.IsEnabled(LogLevel.Information))
            information_SetIgnition = (value) =>
                logger.LogInformation("Ignition set to {value}", value);
    }

    Action<bool> information_SetIgnition = (value) => { };
}
