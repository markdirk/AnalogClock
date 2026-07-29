using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace AnalogClock;

public static class SettingsService
{
    private static readonly string SettingsFileName = "settings.json";

    public static string SettingsPath => Path.Combine(GetSettingsDirectory(), SettingsFileName);

    private static string GetSettingsDirectory()
    {
        var mainModule = Process.GetCurrentProcess().MainModule?.FileName;
        var fileName = Path.GetFileName(mainModule) ?? string.Empty;
        if (!string.IsNullOrEmpty(mainModule)
            && !fileName.Equals("dotnet", StringComparison.OrdinalIgnoreCase)
            && !fileName.Equals("dotnet.exe", StringComparison.OrdinalIgnoreCase))
        {
            return Path.GetDirectoryName(mainModule)!;
        }

        return AppContext.BaseDirectory;
    }

    public static ClockSettings Load()
    {
        try
        {
            var path = SettingsPath;
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var settings = JsonSerializer.Deserialize<ClockSettings>(json);
                if (settings is not null)
                {
                    return settings;
                }
            }
        }
        catch
        {
            // ignore and return defaults
        }

        return new ClockSettings();
    }

    public static void Save(ClockSettings settings)
    {
        try
        {
            var path = SettingsPath;
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch
        {
            // ignore write failures (e.g. protected directories)
        }
    }
}
