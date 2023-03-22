using CutilloRigby.DesireRc.Device;
using CutilloRigby.Input.Gamepad;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CutilloRigby.DesireRc.Gamepad;

internal sealed class GamepadAvailabilityMonitor : IHostedService
{
    private readonly IGamepadAvailable _gamepadAvailable;
    private readonly StatusLed _statusLed;
    
    public GamepadAvailabilityMonitor(IGamepadAvailable gamepadAvailable, StatusLed statusLed, 
        ILogger<GamepadAvailabilityMonitor> logger)
    {
        _gamepadAvailable = gamepadAvailable ?? throw new ArgumentNullException(nameof(gamepadAvailable));
        _statusLed = statusLed ?? throw new ArgumentNullException(nameof(statusLed));
        SetLogHandlers(logger ?? throw new ArgumentNullException(nameof(logger)));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _gamepadAvailable.AvailableChanged += (s, e) => 
        {
            _statusLed.SetRedLed(!e.Value);
            setInforation_Availability(e.Value);
        };
        _statusLed.SetRedLed(!_gamepadAvailable.IsAvailable);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void SetLogHandlers(ILogger logger)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            setInforation_Availability = (value) =>
                logger.LogInformation("Gamepad Availability Changed: {value}", value);
        }
    }

    private Action<bool> setInforation_Availability = (value) => { };
}
