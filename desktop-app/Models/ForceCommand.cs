namespace FFBWheelConfig.Models;

/// <summary>Represents a force command to send to the wheel during testing.</summary>
public sealed class ForceCommand
{
    public ForceDirection Direction { get; init; }

    /// <summary>Force magnitude as a percentage 0–100.</summary>
    public int Strength { get; init; }

    public static ForceCommand Left(int strength) =>
        new() { Direction = ForceDirection.Left, Strength = Clamp(strength) };

    public static ForceCommand Right(int strength) =>
        new() { Direction = ForceDirection.Right, Strength = Clamp(strength) };

    public static ForceCommand Center() =>
        new() { Direction = ForceDirection.Center, Strength = 0 };

    public static ForceCommand Stop() =>
        new() { Direction = ForceDirection.Stopped, Strength = 0 };

    private static int Clamp(int v) => v < 0 ? 0 : v > 100 ? 100 : v;
}
