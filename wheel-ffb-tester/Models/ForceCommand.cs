namespace FFBWheelTester.Models;

/// <summary>Represents a single force command to send to the wheel.</summary>
public sealed class ForceCommand
{
    public ForceDirection Direction { get; init; }

    /// <summary>Force magnitude 0–100 %.</summary>
    public int Strength { get; init; }

    public static ForceCommand Left(int pct)   => new() { Direction = ForceDirection.Left,    Strength = Math.Clamp(pct, 0, 100) };
    public static ForceCommand Right(int pct)  => new() { Direction = ForceDirection.Right,   Strength = Math.Clamp(pct, 0, 100) };
    public static ForceCommand Center()        => new() { Direction = ForceDirection.Center,  Strength = 100 };
    public static ForceCommand Stop()          => new() { Direction = ForceDirection.Stopped, Strength = 0 };

    public override string ToString() => $"{Direction} @ {Strength}%";
}
