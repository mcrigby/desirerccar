using System.Device.Gpio;
using Microsoft.Extensions.Logging;

namespace CutilloRigby.DesireRc.Device;

public sealed class StatusLed : IDisposable
{
    private readonly GpioController _gpioController;
    
    private const int redLed = 22;
    private const int greenLed = 23;
    private const int blueLed = 24;

    public StatusLed(GpioController gpioController, ILogger<StatusLed> logger)
    {
        _gpioController = gpioController ?? throw new ArgumentNullException(nameof(gpioController));
        SetLoggers(logger ?? throw new ArgumentNullException(nameof(logger)));

        _gpioController.OpenPin(redLed, PinMode.Output, PinValue.Low);
        _gpioController.OpenPin(greenLed, PinMode.Output, PinValue.Low);
        _gpioController.OpenPin(blueLed, PinMode.Output, PinValue.Low);
    }

    public void SetRedLed(bool value)
    {
        _gpioController.Write(redLed, value ? PinValue.High : PinValue.Low);
        information_SetValue("Red", value);
    }

    public void SetGreenLed(bool value)
    {
        _gpioController.Write(greenLed, value ? PinValue.High : PinValue.Low);
        information_SetValue("Green", value);
    }

    public void SetBlueLed(bool value)
    {
        _gpioController.Write(blueLed, value ? PinValue.High : PinValue.Low);
        information_SetValue("Blue", value);
    }

    public void Dispose()
    {
        _gpioController.ClosePin(redLed);
        _gpioController.ClosePin(greenLed);
        _gpioController.ClosePin(blueLed);
    }

    private void SetLoggers(ILogger logger)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            information_SetValue = (colour, value) =>
                logger.LogInformation("{colour} LED is set to {value}", colour, value);
        }
    }
    
    Action<string, bool> information_SetValue = (colour, value) => { };
}
