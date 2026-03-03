using System.IO.Compression;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace BloodshedModToolkitInstaller;

public record InstallOptions
{
    public bool InstallBepInEx { get; init; }
    public bool InstallModDll  { get; init; }
}

public class InstallWorker
{
    // BepInEx 6.0.0-be.754 (IL2CPP win-x64)
    private const string BepInExUrl =
        "https://builds.bepinex.dev/projects/bepinex_be/754/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.754%2Bc038613.zip";

    private static readonly HttpClient Http = new(new HttpClientHandler
    {
        AllowAutoRedirect = true,
        MaxAutomaticRedirections = 5,
    })
    {
        Timeout = TimeSpan.FromMinutes(10),
    };

    // -------------------------------------------------------------------------
    // Path detection
    // -------------------------------------------------------------------------

    /// <summary>
    /// Attempts to auto-detect the Bloodshed installation directory via the Steam registry.
    /// Returns null if not found.
    /// </summary>
    public static string? FindSteamGamePath()
    {
        try
        {
            // Read Steam install path from registry
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam");
            string? steamPath = key?.GetValue("SteamPath") as string;
            if (steamPath == null) return null;

            // Normalize separators
            steamPath = steamPath.Replace('/', Path.DirectorySeparatorChar);

            // Parse libraryfolders.vdf to enumerate all Steam library roots
            var libraryRoots = new List<string> { steamPath };
            string vdfPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
            if (File.Exists(vdfPath))
            {
                string vdf = File.ReadAllText(vdfPath);
                var matches = Regex.Matches(vdf, "\"path\"\\s+\"([^\"]+)\"");
                foreach (Match m in matches)
                {
                    string libPath = m.Groups[1].Value.Replace("\\\\", "\\");
                    libraryRoots.Add(libPath);
                }
            }

            foreach (string root in libraryRoots)
            {
                string candidate = Path.Combine(root, "steamapps", "common", "Bloodshed");
                if (ValidateGamePath(candidate))
                    return candidate;
            }
        }
        catch { /* best-effort */ }

        // Fallback default paths
        string[] defaults =
        {
            @"C:\Program Files (x86)\Steam\steamapps\common\Bloodshed",
            @"C:\Program Files\Steam\steamapps\common\Bloodshed",
        };
        foreach (string d in defaults)
            if (ValidateGamePath(d)) return d;

        return null;
    }

    public static bool ValidateGamePath(string path) =>
        File.Exists(Path.Combine(path, "Bloodshed.exe"));

    public static bool IsBepInExInstalled(string gamePath) =>
        Directory.Exists(Path.Combine(gamePath, "BepInEx", "core"));

    // -------------------------------------------------------------------------
    // Main install orchestration
    // -------------------------------------------------------------------------

    public async Task InstallAsync(
        string gamePath,
        InstallOptions options,
        IProgress<(int pct, string msg)> progress)
    {
        if (!ValidateGamePath(gamePath))
            throw new InvalidOperationException($"Bloodshed.exe not found in: {gamePath}");

        string? tempZip = null;

        try
        {
            // ------------------------------------------------------------------
            // Phase 1: BepInEx (0 → 80 %)
            // ------------------------------------------------------------------
            if (options.InstallBepInEx)
            {
                if (IsBepInExInstalled(gamePath))
                {
                    progress.Report((80, "BepInEx already installed — skipping download."));
                }
                else
                {
                    // Download: 0 → 60 %
                    tempZip = Path.Combine(Path.GetTempPath(), $"bepinex_{Guid.NewGuid():N}.zip");
                    await DownloadBepInExAsync(tempZip, pct =>
                    {
                        int mapped = (int)(pct * 0.60);
                        progress.Report((mapped, $"Downloading BepInEx... ({pct}%)"));
                    });

                    // Extract: 60 → 80 %
                    progress.Report((60, "Extracting BepInEx..."));
                    await ExtractBepInExAsync(tempZip, gamePath, pct =>
                    {
                        int mapped = 60 + (int)(pct * 0.20);
                        progress.Report((mapped, $"Extracting BepInEx... ({pct}%)"));
                    });
                }
            }
            else
            {
                progress.Report((80, "Skipping BepInEx (unchecked)."));
            }

            // ------------------------------------------------------------------
            // Phase 2: Mod DLL (80 → 100 %)
            // ------------------------------------------------------------------
            if (options.InstallModDll)
            {
                progress.Report((85, "Installing BloodshedModToolkit.dll..."));
                InstallModDll(gamePath);
                progress.Report((100, "Mod DLL installed."));
            }
            else
            {
                progress.Report((100, "Skipping mod DLL (unchecked)."));
            }
        }
        finally
        {
            if (tempZip != null && File.Exists(tempZip))
            {
                try { File.Delete(tempZip); } catch { /* ignore */ }
            }
        }
    }

    // -------------------------------------------------------------------------
    // BepInEx download
    // -------------------------------------------------------------------------

    private static async Task DownloadBepInExAsync(
        string destZipPath,
        Action<int> reportPct)
    {
        using var response = await Http.GetAsync(BepInExUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        long? totalBytes = response.Content.Headers.ContentLength;
        await using var src  = await response.Content.ReadAsStreamAsync();
        await using var dest = File.Create(destZipPath);

        const int bufSize = 8192;
        var buf = new byte[bufSize];
        long read = 0;
        int bytesRead;

        while ((bytesRead = await src.ReadAsync(buf)) > 0)
        {
            await dest.WriteAsync(buf.AsMemory(0, bytesRead));
            read += bytesRead;
            if (totalBytes.HasValue && totalBytes.Value > 0)
            {
                int pct = (int)(read * 100 / totalBytes.Value);
                reportPct(Math.Clamp(pct, 0, 99));
            }
        }

        reportPct(100);
    }

    // -------------------------------------------------------------------------
    // BepInEx extraction
    // -------------------------------------------------------------------------

    private static async Task ExtractBepInExAsync(
        string zipPath,
        string gamePath,
        Action<int> reportPct)
    {
        await Task.Run(() =>
        {
            using var archive = ZipFile.OpenRead(zipPath);
            int total   = archive.Entries.Count;
            int current = 0;

            foreach (var entry in archive.Entries)
            {
                current++;
                // Skip directory entries (empty names end with '/')
                if (string.IsNullOrEmpty(entry.Name)) continue;

                string destPath = Path.Combine(gamePath, entry.FullName.Replace('/', Path.DirectorySeparatorChar));
                string? destDir = Path.GetDirectoryName(destPath);
                if (destDir != null) Directory.CreateDirectory(destDir);

                entry.ExtractToFile(destPath, overwrite: true);

                int pct = (int)((double)current / total * 100);
                reportPct(Math.Clamp(pct, 0, 100));
            }
        });
    }

    // -------------------------------------------------------------------------
    // Mod DLL installation from embedded resource
    // -------------------------------------------------------------------------

    private static void InstallModDll(string gamePath)
    {
        string pluginsDir = Path.Combine(gamePath, "BepInEx", "plugins");
        Directory.CreateDirectory(pluginsDir);

        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("BloodshedModToolkit.dll")
            ?? throw new InvalidOperationException(
                "Embedded resource 'BloodshedModToolkit.dll' not found. " +
                "Make sure the mod DLL was built before publishing the installer.");

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        byte[] bytes = ms.ToArray();

        string destPath = Path.Combine(pluginsDir, "BloodshedModToolkit.dll");
        File.WriteAllBytes(destPath, bytes);
    }
}
