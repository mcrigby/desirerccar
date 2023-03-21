using CutilloRigby.Output.Servo;
using System.Device.Pwm;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CutilloRigby.DesireRc.Device;

public sealed class TBLE01_ESC : IHostedService
{
    private const byte _channel = 0;

    private readonly IServoState _servoSettings;
    private readonly IServoMap _servoMap;
    private readonly StatusLed _statusLed;
    
    private readonly PwmChannel _drivePwm;
    
    private DrivingMode _drivingMode = DrivingMode.ForwardOnly;

    public TBLE01_ESC(IServoState servoSettings, IServoMap servoMap, StatusLed statusLed, ILogger<TBLE01_ESC> logger)
    {
        _servoSettings = servoSettings ?? throw new ArgumentNullException(nameof(servoSettings));
        _servoMap = servoMap ?? throw new ArgumentNullException(nameof(servoMap));
        _statusLed = statusLed ?? throw new ArgumentNullException(nameof(statusLed));
        SetLogHandlers(logger ?? throw new ArgumentNullException(nameof(logger)));

        _drivePwm = PwmChannel.Create(0, _channel, 50, _servoMap[0]);
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
        return Task.CompletedTask;
    }

    private async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!_servoSettings.HasChannel(_channel))
            return;

        byte lastDrive = 127;
        float dutyCycle = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            byte currentDrive = _servoSettings.GetChannel(_channel);
            if (lastDrive != currentDrive && _drivingMode != DrivingMode.Braking)
            {
                if ((currentDrive <= TBLE01_Deadband_Lower && currentDrive >= 128) && _drivingMode == DrivingMode.ForwardOnly)
                {
                    DrivingMode = DrivingMode.Braking;
                    
                    _drivePwm.DutyCycle = _servoMap[128];
                    await Task.Delay(200);
        
                    _drivePwm.DutyCycle = _servoMap[0];
                    await Task.Delay(800);
        
                    DrivingMode = DrivingMode.ReverseEnabled;
                }
                else if (currentDrive >= TBLE01_Deadband_Upper && currentDrive < 128)
                    DrivingMode = DrivingMode.ForwardOnly;
        
                currentDrive = _servoSettings.GetChannel(_channel);

                dutyCycle = _servoMap[currentDrive];
                _drivePwm.DutyCycle = dutyCycle;

                setInformation_Value(currentDrive, dutyCycle);
                lastDrive = currentDrive;
            }

            await Task.Delay(1);
        }

        _drivePwm.DutyCycle = _servoMap[0];
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void SetLogHandlers(ILogger logger)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            setInformation_Value = (value, dutyCycle) =>
                logger.LogInformation("TBLE01 ESC ({channel}) set to {value} (Duty Cycle: {dutyCycle:n3})",
                    _channel, value, dutyCycle);
            setInformation_Braking = (state) => 
                logger.LogInformation("TBLE01 ESC ({channel}) driving mode changed to {value}",
                    _channel, state);
        }
    }

    private Action<byte, float> setInformation_Value = (value, dutyCycle) => { };
    private Action<DrivingMode> setInformation_Braking = (state) => { };
    public const byte TBLE01_Deadband_Lower = 251; // 4% of -128 as unsigned byte
    public const byte TBLE01_Deadband_Upper = 5; // 4% of 128
}
