using System.Device.Gpio;
using CutilloRigby.Input.Gamepad;
using CutilloRigby.Input.GPS;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UnitsNet;
namespace CutilloRigby.DesireRc.Device;

public sealed class UltimateGPS : IHostedService
{
    private readonly IGPS _gps;
    private readonly IGPSStatus _gpsStatus;
    private readonly IGPSChanged _gpsChanged;
    private readonly IGamepadInputChanged _gamepadInputChanged;

    private readonly GpioController _gpioController;
    private readonly Timer _gpsFixTimer;
    private bool _enabled = false;

    private const int gpsEnabled = 17;
    private const int gpsFix = 27;
    private const int gpsFix_TimeOut = 2000;

    public UltimateGPS(IGPS<UltimateGPS> gps, IGPSStatus gpsStatus, IGPSChanged gpsChanged, 
        IGamepadInputChanged gamepadInputChanged, GpioController gpioController, ILogger<UltimateGPS> logger)
    {
        _gps = gps ?? throw new ArgumentNullException(nameof(gps));
        _gpsStatus = gpsStatus ?? throw new ArgumentNullException(nameof(gpsStatus));
        _gpsChanged = gpsChanged ?? throw new ArgumentNullException(nameof(gpsChanged));

        _gpsChanged.Changed += (s, e) => currentPosition(e.Location, e.Bearing, e.SpeedOverGround);

        _gpioController = gpioController ?? throw new ArgumentNullException(nameof(gpioController));
        _gpioController.OpenPin(gpsEnabled, PinMode.Output, PinValue.High);
        _gpioController.OpenPin(gpsFix, PinMode.Input);
        _gpioController.RegisterCallbackForPinValueChangedEvent(gpsFix, PinEventTypes.Rising, gpsFix_PinRising);

        _gpsFixTimer = new Timer(gpsFix_TimerCallback, null, Timeout.Infinite, Timeout.Infinite);
        Fix = false;

        _gamepadInputChanged = gamepadInputChanged ?? throw new ArgumentNullException(nameof(gamepadInputChanged));
        _gamepadInputChanged.ButtonChanged += _gamepadInputChanged_ButtonChanged;

        SetupLogHandlers(logger);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // SetEnabled(true); // Do Not Enable at Startup

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        SetEnabled(false);

        return Task.CompletedTask;
    }

    public IGPSStatus Status => _gpsStatus;

    public bool Fix { get; private set; }
    private void gpsFix_PinRising(object sender, PinValueChangedEventArgs eventArgs)
    {
        _gpsFixTimer.Change(gpsFix_TimeOut, Timeout.Infinite);
        Fix = false;
        noFix();
    }
    private void gpsFix_TimerCallback(object state)
    {
        Fix = true;
        hasFix();
    }

    public bool Enabled{
        get => _enabled;
        set => SetEnabled(value);
    }
    private void SetEnabled(bool enabled)
    {
        if (enabled == _enabled)
            return;

        if (enabled)
        {
            _gpioController.Write(gpsEnabled, PinValue.High);
            _gpsFixTimer.Change(gpsFix_TimeOut, Timeout.Infinite);
            Fix = false;
            _gps.Start();

            _enabled = true;
            setEnabled(_enabled);
        }
        else
        {
            _gps.Stop();
            _gpsFixTimer.Change(Timeout.Infinite, Timeout.Infinite);
            Fix = false;
            _gpioController.Write(gpsEnabled, PinValue.Low);

            _enabled = false;
            setEnabled(_enabled);
        }
    }

    private void _gamepadInputChanged_ButtonChanged(object? sender, GamepadButtonInputEventArgs eventArgs)
    {
        if (eventArgs.Address != 6)
            return;

        if (!eventArgs.Value)
            return;

        SetEnabled(!_enabled);
    }

    private void SetupLogHandlers(ILogger logger)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            currentPosition = (gp, a, s) =>
                logger.LogInformation("GPS Position: {latitude}, {longitude}. {speed:n2}mph bearing {bearing:n2} degrees.",
                    gp?.LatitudeDMS(), gp?.LongitudeDMS(), s.MilesPerHour, a.Degrees);
            setEnabled = (e) =>
                logger.LogInformation("Gps Enable has been set to {enabled}", e);
            noFix = () =>
                logger.LogInformation("Awaiting GPS Fix");
            hasFix = () =>
                logger.LogInformation("GPS Fix Aquired");
        }
    }

    Action<IGeographicPosition?, Angle, Speed> currentPosition = (gp, a, s) => { };
    Action<bool> setEnabled = (e) => { };
    Action noFix = () => { };
    Action hasFix = () => { };
}