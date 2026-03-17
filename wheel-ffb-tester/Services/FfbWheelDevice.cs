using SharpDX.DirectInput;
using FFBWheelTester.Models;

namespace FFBWheelTester.Services;

/// <summary>
/// Wraps a single DirectInput force-feedback joystick device and exposes
/// simple test-force commands (constant left/right, spring center, stop).
/// All methods are safe to call from the UI thread — DirectInput calls are
/// short and do not block.
/// </summary>
public sealed class FfbWheelDevice : IDisposable
{
    private readonly Joystick _joystick;
    private Effect? _currentEffect;
    private bool _disposed;

    // DirectInput constants
    // -1 == 0xFFFFFFFF (INFINITE) and 0xFFFFFFFF (DIEB_NOTRIGGER) when cast to a signed int32
    private const int InfiniteDuration = unchecked((int)0xFFFFFFFF);   // INFINITE
    private const int NoTrigger        = unchecked((int)0xFFFFFFFF);   // DIEB_NOTRIGGER

    public string DeviceName { get; }
    public bool IsDisposed => _disposed;

    public FfbWheelDevice(DirectInput di, DeviceInstance instance, IntPtr windowHandle)
    {
        DeviceName = instance.ProductName;
        _joystick  = new Joystick(di, instance.InstanceGuid);
        _joystick.SetCooperativeLevel(windowHandle,
            CooperativeLevel.Background | CooperativeLevel.NonExclusive);
        _joystick.Acquire();
    }

    // ── Force commands ───────────────────────────────────────────────────────

    /// <summary>Apply constant force to the left at <paramref name="strengthPercent"/> (0–100).</summary>
    public void ForceLeft(int strengthPercent)  => ApplyConstant(-Clamp(strengthPercent));

    /// <summary>Apply constant force to the right at <paramref name="strengthPercent"/> (0–100).</summary>
    public void ForceRight(int strengthPercent) => ApplyConstant(+Clamp(strengthPercent));

    /// <summary>Activate spring-to-center effect.</summary>
    public void ForceCenter(int strengthPercent) => ApplySpring(Clamp(strengthPercent));

    /// <summary>Remove all force output immediately.</summary>
    public void ForceStop() => StopAndReleaseEffect();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static int Clamp(int pct) => Math.Clamp(pct, 0, 100);

    private void ApplyConstant(int magnitude)
    {
        StopAndReleaseEffect();

        // DirectInput magnitude range: −10000 … +10000
        int mag = magnitude * 100;

        var parameters = new EffectParameters
        {
            Duration              = InfiniteDuration,
            SamplePeriod          = 0,
            Gain                  = 10_000,
            TriggerButton         = NoTrigger,
            TriggerRepeatInterval = InfiniteDuration,
            Flags                 = EffectFlags.Cartesian | EffectFlags.ObjectOffsets,
        };
        parameters.SetAxes([0], [0]);
        parameters.Parameters = new ConstantForce { Magnitude = mag };

        try
        {
            _currentEffect = new Effect(_joystick, EffectGuid.ConstantForce, parameters);
            _currentEffect.Start(1, EffectPlayFlags.None);
        }
        catch
        {
            _currentEffect?.Dispose();
            _currentEffect = null;
        }
    }

    private void ApplySpring(int strengthPercent)
    {
        StopAndReleaseEffect();

        int mag = strengthPercent * 100;

        var condition = new Condition
        {
            PositiveSaturation  = mag,
            NegativeSaturation  = mag,
            PositiveCoefficient = mag,
            NegativeCoefficient = mag,
            DeadBand            = 0,
            Offset              = 0,
        };

        var parameters = new EffectParameters
        {
            Duration              = InfiniteDuration,
            SamplePeriod          = 0,
            Gain                  = 10_000,
            TriggerButton         = NoTrigger,
            TriggerRepeatInterval = InfiniteDuration,
            Flags                 = EffectFlags.Cartesian | EffectFlags.ObjectOffsets,
        };
        parameters.SetAxes([0], [0]);
        parameters.Parameters = new ConditionSet { Conditions = [condition] };

        try
        {
            _currentEffect = new Effect(_joystick, EffectGuid.Spring, parameters);
            _currentEffect.Start(1, EffectPlayFlags.None);
        }
        catch
        {
            _currentEffect?.Dispose();
            _currentEffect = null;
        }
    }

    private void StopAndReleaseEffect()
    {
        if (_currentEffect is not null)
        {
            try { _currentEffect.Stop(); } catch { }
            _currentEffect.Dispose();
            _currentEffect = null;
        }
    }

    // ── IDisposable ──────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopAndReleaseEffect();
        try { _joystick.Unacquire(); } catch { }
        _joystick.Dispose();
    }
}
