using System.Diagnostics;

namespace FFBWheelConfig.Services;

/// <summary>
/// Flashes the bundled EMC-compatible .hex to the wheel controller via avrdude.
/// Reports progress line-by-line through the Progress callback.
/// </summary>
public sealed class FirmwareFlasher
{
    /// <summary>
    /// Finds the latest pre-built hex in the versions/ directory next to the app,
    /// falling back to the firmware source tree if the versioned copy is missing.
    /// </summary>
    public static string? FindHexPath()
    {
        // Look relative to the running executable
        string exeDir = AppContext.BaseDirectory;

        // 1. Check for a bundled firmware/ subfolder (deployed beside the exe)
        string bundled = Path.Combine(exeDir, "firmware", "leonardo-wheel.ino.hex");
        if (File.Exists(bundled)) return bundled;

        // 2. Walk up from the exe and look for versions/<latest>/firmware/*.hex
        string? root = FindRepoRoot(exeDir);
        if (root != null)
        {
            string latestTxt = Path.Combine(root, "versions", "latest.txt");
            if (File.Exists(latestTxt))
            {
                string version = File.ReadAllText(latestTxt).Trim();
                string versionedHex = Path.Combine(
                    root, "versions", version, "firmware", "leonardo-wheel.ino.hex");
                if (File.Exists(versionedHex)) return versionedHex;
            }

            // 3. Firmware build output
            string buildHex = Path.Combine(
                root, "firmware", "leonardo-wheel", "build", "leonardo-wheel.ino.hex");
            if (File.Exists(buildHex)) return buildHex;
        }

        return null;
    }

    /// <summary>Walks up the directory tree looking for a versions/ folder (repo root signal).</summary>
    private static string? FindRepoRoot(string start)
    {
        string? dir = start;
        for (int depth = 0; depth < 8 && dir != null; depth++)
        {
            if (Directory.Exists(Path.Combine(dir, "versions"))) return dir;
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }

    /// <summary>
    /// Flashes <paramref name="hexPath"/> to <paramref name="portName"/> using avrdude.
    /// The Leonardo requires a 1200-baud touch first to enter the bootloader.
    /// </summary>
    public static async Task<(bool success, string log)> FlashAsync(
        string hexPath,
        string portName,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        var log = new System.Text.StringBuilder();

        void Emit(string line)
        {
            log.AppendLine(line);
            progress?.Report(line);
        }

        if (!File.Exists(hexPath))
        {
            Emit($"ERROR: hex file not found: {hexPath}");
            return (false, log.ToString());
        }

        Emit($"Hex: {hexPath}");
        Emit($"Port: {portName}");
        Emit("Sending 1200-baud bootloader touch…");

        try
        {
            SerialWheelClient.ResetToBootloader(portName);
        }
        catch (Exception ex)
        {
            Emit($"WARNING: bootloader touch failed ({ex.Message}) – continuing anyway");
        }

        Emit("Waiting for bootloader…");
        await Task.Delay(3000, ct);

        string args = $"-p atmega32u4 -c avr109 -P {portName} -b 57600 -D -U flash:w:\"{hexPath}\":i";
        Emit($"Running: avrdude {args}");

        var psi = new ProcessStartInfo("avrdude", args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        try
        {
            using var proc = Process.Start(psi)!;

            proc.OutputDataReceived += (_, e) => { if (e.Data != null) Emit(e.Data); };
            proc.ErrorDataReceived  += (_, e) => { if (e.Data != null) Emit(e.Data); };
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            await proc.WaitForExitAsync(ct);

            bool ok = proc.ExitCode == 0;
            Emit(ok ? "Flash complete." : $"avrdude exited with code {proc.ExitCode}");
            return (ok, log.ToString());
        }
        catch (Exception ex)
        {
            Emit($"ERROR: {ex.Message}");
            Emit("Is avrdude installed and on PATH?");
            return (false, log.ToString());
        }
    }
}
