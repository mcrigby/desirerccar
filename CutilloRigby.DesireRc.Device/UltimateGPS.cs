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

    public UltimateGPS(IGPS<UltimateGPS> gps, IGPSStatus gpsStatus, IGPSChanged gpsChanged, ILogger<UltimateGPS> logger)
    {
        _gps = gps ?? throw new ArgumentNullException(nameof(gps));
        _gpsStatus = gpsStatus ?? throw new ArgumentNullException(nameof(gpsStatus));
        _gpsChanged = gpsChanged ?? throw new ArgumentNullException(nameof(gpsChanged));

        _gpsChanged.Changed += (s, e) => currentPosition(e.Location, e.Bearing, e.SpeedOverGround);

        SetupLogHandlers(logger);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _gps.Start();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _gps.Stop();

        return Task.CompletedTask;
    }

    public IGPSStatus Status => _gpsStatus;

    private void SetupLogHandlers(ILogger logger)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            currentPosition = (gp, a, s) =>
                logger.LogInformation("GPS Position: {latitude}, {longitude}. {speed:n2}mph bearing {bearing:n2} degrees.",
                    gp?.LatitudeDMS(), gp?.LongitudeDMS(), s.MilesPerHour, a.Degrees);
        }
    }

    Action<IGeographicPosition?, Angle, Speed> currentPosition = (gp, a, s) => { };
}