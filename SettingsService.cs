using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

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
                var settings = JsonSerializer.Deserialize<ClockSettings>(json) ?? new ClockSettings();

                if (!string.IsNullOrEmpty(settings.LicenseKeyEncrypted))
                {
                    settings.LicenseKey = LicenseCrypto.Decrypt(settings.LicenseKeyEncrypted) ?? string.Empty;
                }
                else
                {
                    var node = JsonNode.Parse(json);
                    if (node?["LicenseKey"]?.GetValue<string>() is { } legacyKey)
                    {
                        settings.LicenseKey = legacyKey;
                        settings.LicenseKeyEncrypted = LicenseCrypto.Encrypt(legacyKey);
                    }
                }

                var hardwareId = HardwareId.GetHardwareId();
                if (string.IsNullOrEmpty(settings.LicenseKey))
                {
                    settings.IsLicensed = false;
                }
                else if (settings.LicenseKey == LicenseKey.DeveloperKey)
                {
                    settings.IsLicensed = true;
                }
                else if (LicenseKey.Verify(settings.LicenseKey))
                {
                    if (!string.IsNullOrEmpty(settings.ActivationTokenEncrypted))
                    {
                        settings.ActivationToken = LicenseCrypto.Decrypt(settings.ActivationTokenEncrypted) ?? string.Empty;
                        settings.IsLicensed = LicenseKey.VerifyToken(settings.LicenseKey, hardwareId, settings.ActivationToken);
                    }
                    else
                    {
                        settings.IsLicensed = true;
                    }
                }
                else
                {
                    settings.IsLicensed = false;
                }

                return settings;
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

            settings.LicenseKeyEncrypted = LicenseCrypto.Encrypt(settings.LicenseKey);

            if (!string.IsNullOrEmpty(settings.LicenseKey) && settings.LicenseKey != LicenseKey.DeveloperKey)
            {
                var hardwareId = HardwareId.GetHardwareId();
                settings.ActivationToken = LicenseKey.GenerateToken(settings.LicenseKey, hardwareId);
                settings.ActivationTokenEncrypted = LicenseCrypto.Encrypt(settings.ActivationToken);
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
