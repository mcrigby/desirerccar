using System.Device.Gpio;
using Microsoft.Extensions.Logging;

namespace CutilloRigby.DesireRc.Device;

public sealed class OnBoardButton : IDisposable
{
    private readonly GpioController _gpioController;
    private readonly StatusLed _statusLed;
    private readonly ILogger _logger;

    public event PinChangeEventHandler? ButtonChanged;

    private const int button = 20;

    public OnBoardButton(GpioController gpioController, StatusLed statusLed, ILogger<OnBoardButton> logger)
    {
        _gpioController = gpioController ?? throw new ArgumentNullException(nameof(gpioController));
        _statusLed = statusLed ?? throw new ArgumentNullException(nameof(statusLed));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _gpioController.OpenPin(button, PinMode.Input);
        _gpioController.RegisterCallbackForPinValueChangedEvent(button, PinEventTypes.Rising | PinEventTypes.Falling,
            ButtonCallback);
        
        SetLogHandlers();
    }

    public bool GetButton()
    {
        return _gpioController.Read(button) == PinValue.Low;
    }

    private void ButtonCallback(object sender, PinValueChangedEventArgs eventArgs)
    {
        if (eventArgs.ChangeType == PinEventTypes.Falling)
            _statusLed.SetGreenLed(true);
        else
            _statusLed.SetGreenLed(false);
        
        callbackInformation_Change(eventArgs.ChangeType);
        ButtonChanged?.Invoke(this, eventArgs);
    }

    public void Dispose()
    {
        _gpioController.UnregisterCallbackForPinValueChangedEvent(button, ButtonCallback);
        _gpioController.ClosePin(button);
    }

    private void SetLogHandlers()
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            callbackInformation_Change = (changeType) =>
                _logger.LogInformation("On Board Button Changed. Value {value}.",
                    changeType);
        }
    }
    private Action<PinEventTypes> callbackInformation_Change = (changeType) => { };
}
