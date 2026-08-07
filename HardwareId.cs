using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace AnalogClock;

public static class HardwareId
{
    public static string GetHardwareId()
    {
        try
        {
            var components = new[]
            {
                GetWmiValue("SELECT ProcessorId FROM Win32_Processor", "ProcessorId"),
                GetWmiValue("SELECT SerialNumber FROM Win32_BaseBoard", "SerialNumber"),
                GetWmiValue("SELECT SerialNumber FROM Win32_BIOS", "SerialNumber"),
                GetWmiValue("SELECT UUID FROM Win32_ComputerSystemProduct", "UUID"),
            };

            var combined = string.Join("|", components);
            if (!string.IsNullOrWhiteSpace(combined) && combined != "|||")
            {
                return ComputeHash(combined);
            }
        }
        catch
        {
            // fall through to fallback
        }

        return ComputeHash(FallbackHardwareId());
    }

    private static string GetWmiValue(string query, string property)
    {
        if (!OperatingSystem.IsWindows())
        {
            return string.Empty;
        }

        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(query);
            foreach (var obj in searcher.Get().Cast<System.Management.ManagementObject>())
            {
                var value = obj[property]?.ToString() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(value) && !value.Equals("To be filled by O.E.M.", StringComparison.OrdinalIgnoreCase))
                {
                    return value.Trim();
                }
            }
        }
        catch
        {
            // ignore WMI failures
        }

        return string.Empty;
    }

    private static string FallbackHardwareId()
    {
        var parts = new[]
        {
            Environment.MachineName,
            Environment.UserName,
            Environment.OSVersion.Platform.ToString(),
            Environment.ProcessorCount.ToString()
        };
        return string.Join("|", parts);
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes[..16]);
    }
}
