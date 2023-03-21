using CutilloRigby.Output.Servo;

namespace CutilloRigby.DesireRc.Servo;

internal sealed class ServoConfiguration
{
    public byte Chip { get; set; }
    public string? Name { get; set; }
    public IDictionary<string, ServoOutput>? Channels { get; set; }
}
