using CutilloRigby.Input.Gamepad;

namespace CutilloRigby.DesireRc.Gamepad;

public sealed class GamepadConfiguration
{
    public string? Name { get; set; }
    public string? DeviceFile { get; set; }

    public IDictionary<string, GamepadAxisInput>? Axes { get; set; }
    public IDictionary<string, GamepadButtonInput>? Buttons { get; set; }
}