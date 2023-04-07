using CutilloRigby.DesireRc.Device;
using CutilloRigby.Input.Gamepad;
using CutilloRigby.Output.Servo;
using Iot.Device.HMC6352;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CutilloRigby.DesireRc.Compass;

internal sealed class CompassCalibrationService : IHostedService
{
    private readonly HMC6352 _hmc6352;
    private readonly IServo _steering;
    private readonly IServo _tble01;
    private readonly IGamepadInputChanged _gamepadInputChanged;

    public CompassCalibrationService(HMC6352 hmc6352, IServo<Steering_Servo> steering, IServo<TBLE01_ESC> tble01,
        IGamepadInputChanged gamepadInputChanged, ILogger<CompassCalibrationService> logger)
    {
        _hmc6352 = hmc6352 ?? throw new ArgumentNullException(nameof(hmc6352));
        _steering = steering ?? throw new ArgumentNullException(nameof(steering));
        _tble01 = tble01 ?? throw new ArgumentNullException(nameof(tble01));
        _gamepadInputChanged = gamepadInputChanged ?? throw new ArgumentNullException(nameof(gamepadInputChanged));

        _gamepadInputChanged.ButtonChanged += _gamepadInputChanged_ButtonChanged;
        SetupLogging(logger);
    }

    private void _gamepadInputChanged_ButtonChanged(object? sender, GamepadButtonInputEventArgs eventArgs)
    {
        if (eventArgs.Address != 7)
            return;
        
        if (!eventArgs.Value)
        {
            calibrateCancellationToken?.Cancel();
            return;
        }

        calibrateCancellationToken = new CancellationTokenSource(180000);
        Calibrate(calibrateCancellationToken.Token).Forget();
    }

    private CancellationTokenSource? calibrateCancellationToken = null;

    private async Task Calibrate(CancellationToken cancellationToken)
    {
        const short minimumCalibrationTime = 6000;
        const short calibrationStepTime = 500;

        int calibrationTime = 0;

        try
        {
            beginCalibration();

            _steering.SetValue(23);
            _tble01.SetValue(1);

            _hmc6352.BeginCalibration();

            while(!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(calibrationStepTime);
                calibrationInProgress();
                calibrationTime += calibrationStepTime;
            }
        }
        finally
        {
            finalisingCalibration();

            _tble01.SetValue(0);
            _steering.SetValue(0);

            if (calibrationTime < minimumCalibrationTime)
                await Task.Delay (minimumCalibrationTime - calibrationTime);
            
            _hmc6352.EndCalibration();
            endCalibration();
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void SetupLogging(ILogger logger)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            beginCalibration = () =>
                logger.LogInformation("Beginning Calibration");
            calibrationInProgress = () =>
                logger.LogInformation("Calibration in Progress");
            finalisingCalibration = () =>
                logger.LogInformation("Finalising Calibration");
            endCalibration = () =>
                logger.LogInformation("Calibration Complete");
        }
    }

    Action beginCalibration = () => { };
    Action calibrationInProgress = () => { };
    Action finalisingCalibration= () => { };
    Action endCalibration = () => { };
}
