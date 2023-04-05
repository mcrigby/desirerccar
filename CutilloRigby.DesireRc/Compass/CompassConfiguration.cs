namespace CutilloRigby.DesireRc.Compass;

internal sealed class CompassConfiguration
{
    public byte BusId { get; set; }
    public byte I2CAddress { get; set; }
    public string? OperationalMode { get; set; }
    public bool Periodic_Reset { get; set; }
    public string? MeasurementRate { get; set; }
    public bool EnableCalibrationService { get; set; }
}