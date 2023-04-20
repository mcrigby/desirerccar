using CutilloRigby.Device.HMC6352;
using CutilloRigby.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CutilloRigby.DesireRc.Compass;

internal sealed class CompassStartup : IConfigureServices
{
    public void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
    {
        var compassSection = configuration.GetSection("Compass");
        var compassConfiguration = compassSection.Get<CompassConfiguration>(options => options.ErrorOnUnknownConfiguration = true);

        if (!Enum.TryParse<OperationalMode>(compassConfiguration.OperationalMode, out var operationalModeValue))
            operationalModeValue = OperationalMode.Standby;
        if (!Enum.TryParse<ContinuousModeMeasurementRate>(compassConfiguration.MeasurementRate, out var measurementRate))
            measurementRate = ContinuousModeMeasurementRate._1Hz;

        serviceCollection.AddSingleton<HMC6352>(factory => {
            var hmc6352 = HMC6352.Create(compassConfiguration.I2CAddress, compassConfiguration.BusId);

            var operationalMode = hmc6352.GetOperationalMode();
            operationalMode.Mode = operationalModeValue;
            operationalMode.PeriodicSetReset = compassConfiguration.Periodic_Reset;
            operationalMode.Rate = measurementRate;
            hmc6352.SetOperationalMode(operationalMode);

            return hmc6352;
        });

        if (compassConfiguration.EnableCalibrationService)
            serviceCollection.AddHostedService<CompassCalibrationService>();
    }
}