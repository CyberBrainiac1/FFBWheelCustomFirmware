using System.Diagnostics;

namespace FFBWheelTester.Services;

/// <summary>
/// Locates the bundled EMC-compatible .hex file and invokes avrdude to flash it.
/// Progress lines are reported via the <see cref="Progress"/> callback.
/// </summary>
public sealed class FirmwareFlasher
{
    private const int BootloaderDelayMs = 3000;

    // Relative search paths from the executable directory
    private static readonly string[] HexRelativePaths =
    [
        Path.Combine("firmware", "emc_hex", "leonardo-wheel.ino.hex"),
        Path.Combine("..", "versions", "1.2.2", "firmware", "leonardo-wheel.ino.hex"),
        Path.Combine("versions", "1.2.2",  "firmware", "leonardo-wheel.ino.hex"),
        "leonardo-wheel.ino.hex",
    ];

    public event Action<string>? Progress;

    /// <summary>Finds the bundled .hex file; returns null if not found.</summary>
    public string? FindHex()
    {
        string baseDir = AppContext.BaseDirectory;

        foreach (string rel in HexRelativePaths)
        {
            string full = Path.GetFullPath(Path.Combine(baseDir, rel));
            if (File.Exists(full)) return full;
        }

        // Walk up from baseDir looking for a directory that contains "versions/1.2.2/firmware"
        // (handles dev layout where the exe is several levels below the repo root)
        DirectoryInfo? dir = new DirectoryInfo(baseDir);
        while (dir is not null)
        {
            string candidate = Path.Combine(dir.FullName, "versions", "1.2.2", "firmware", "leonardo-wheel.ino.hex");
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }

        return null;
    }

    /// <summary>
    /// Flashes <paramref name="hexPath"/> to the device on <paramref name="comPort"/> using avrdude.
    /// Returns true on success.
    /// </summary>
    public async Task<bool> FlashAsync(string hexPath, string comPort, CancellationToken ct = default)
    {
        if (!File.Exists(hexPath))
        {
            Report($"ERROR: Hex file not found: {hexPath}");
            return false;
        }

        // 1200-baud bootloader touch (Arduino Leonardo bootloader entry)
        Report($"Opening {comPort} at 1200 baud to trigger bootloader…");
        try
        {
            using var touch = new System.IO.Ports.SerialPort(comPort, 1200);
            touch.Open();
            await Task.Delay(200, ct);
            touch.Close();
        }
        catch (Exception ex)
        {
            Report($"WARNING: Bootloader touch failed ({ex.Message}). Continuing…");
        }

        Report($"Waiting {BootloaderDelayMs / 1000}s for bootloader…");
        await Task.Delay(BootloaderDelayMs, ct);

        // Build avrdude command
        string avrdude = "avrdude";
        string args    = $"-p atmega32u4 -c avr109 -P {comPort} -b 57600 -D -U flash:w:\"{hexPath}\":i";

        Report($"Running: {avrdude} {args}");
        Report("─────────────────────────────────");

        var psi = new ProcessStartInfo(avrdude, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true,
        };

        try
        {
            using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Could not start avrdude.");
            var output = await proc.StandardOutput.ReadToEndAsync(ct);
            var error  = await proc.StandardError.ReadToEndAsync(ct);

            await proc.WaitForExitAsync(ct);

            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                Report(line.TrimEnd());
            foreach (var line in error.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                Report(line.TrimEnd());

            bool success = proc.ExitCode == 0;
            Report("─────────────────────────────────");
            Report(success ? "✓ Flash complete!" : $"✗ avrdude exited with code {proc.ExitCode}");
            return success;
        }
        catch (Exception ex)
        {
            Report($"ERROR: {ex.Message}");
            Report("Make sure avrdude is installed and on PATH.");
            return false;
        }
    }

    private void Report(string line) => Progress?.Invoke(line);
}
