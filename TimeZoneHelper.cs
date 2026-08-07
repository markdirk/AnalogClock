using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AnalogClock;

public class TimeZoneItem
{
    public string Id { get; set; } = string.Empty;
    public string Display { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public TimeSpan Offset { get; set; }
}

public static class TimeZoneHelper
{
    private static readonly Dictionary<string, string> CityOverrides = new(StringComparer.OrdinalIgnoreCase)
    {
        ["W. Europe Standard Time"] = "Berlin",
        ["Central Europe Standard Time"] = "Wien",
        ["Romance Standard Time"] = "Paris",
        ["GMT Standard Time"] = "London",
        ["Eastern Standard Time"] = "Toronto, Ontario, Canada",
        ["Pacific Standard Time"] = "Los Angeles",
        ["Central Standard Time"] = "Mexico City",
        ["Mountain Standard Time"] = "Denver",
        ["Atlantic Standard Time"] = "Halifax",
        ["Alaskan Standard Time"] = "Anchorage",
        ["Hawaiian Standard Time"] = "Honolulu",
        ["Russia Time Zone 3"] = "Moskau",
        ["Arab Standard Time"] = "Dubai",
        ["India Standard Time"] = "Mumbai",
        ["China Standard Time"] = "Peking",
        ["Tokyo Standard Time"] = "Tokio",
        ["Korea Standard Time"] = "Seoul",
        ["AUS Eastern Standard Time"] = "Sydney",
        ["New Zealand Standard Time"] = "Wellington",
        ["South Africa Standard Time"] = "Johannesburg",
        ["E. South America Standard Time"] = "São Paulo",
        ["Argentina Standard Time"] = "Buenos Aires",
        ["Greenland Standard Time"] = "Nuuk",
        ["UTC"] = "UTC",
        ["Greenwich Standard Time"] = "Lissabon",
        ["Europe/Berlin"] = "Berlin",
        ["Europe/London"] = "London",
        ["Europe/Paris"] = "Paris",
        ["Europe/Vienna"] = "Wien",
        ["America/Toronto"] = "Toronto, Ontario, Canada",
        ["America/New_York"] = "New York",
        ["America/Los_Angeles"] = "Los Angeles",
        ["America/Chicago"] = "Chicago",
        ["America/Denver"] = "Denver",
        ["America/Anchorage"] = "Anchorage",
        ["America/Halifax"] = "Halifax",
        ["America/Mexico_City"] = "Mexico City",
        ["America/Sao_Paulo"] = "São Paulo",
        ["America/Argentina/Buenos_Aires"] = "Buenos Aires",
        ["America/Vancouver"] = "Vancouver",
        ["Asia/Dubai"] = "Dubai",
        ["Asia/Kolkata"] = "Mumbai",
        ["Asia/Shanghai"] = "Peking",
        ["Asia/Tokyo"] = "Tokio",
        ["Asia/Seoul"] = "Seoul",
        ["Asia/Manila"] = "Manila",
        ["Australia/Sydney"] = "Sydney",
        ["Pacific/Auckland"] = "Wellington",
        ["Africa/Johannesburg"] = "Johannesburg",
        ["Africa/Cairo"] = "Kairo",
        ["Europe/Moscow"] = "Moskau",
        ["Asia/Bangkok"] = "Bangkok",
        ["Asia/Singapore"] = "Singapur",
        ["Europe/Istanbul"] = "Istanbul",
        ["Pacific/Honolulu"] = "Honolulu"
    };

    private static readonly (string Id, string City)[] AdditionalZones =
    {
        ("Asia/Manila", "Manila"),
        ("America/Vancouver", "Vancouver")
    };

    public static List<TimeZoneItem> GetTimeZones()
    {
        var list = new List<TimeZoneItem>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var zones = TimeZoneInfo.GetSystemTimeZones();

        foreach (var tz in zones)
        {
            var item = CreateItem(tz);
            list.Add(item);
            seen.Add($"{item.Id}|{item.City}");
        }

        foreach (var (id, city) in AdditionalZones)
        {
            var tz = FindTimeZone(id) ?? FindConvertedTimeZone(id);
            if (tz is null)
            {
                continue;
            }

            var item = CreateItem(tz, city);
            var key = $"{item.Id}|{item.City}";
            if (!seen.Contains(key))
            {
                list.Add(item);
                seen.Add(key);
            }
        }

        return list.OrderBy(t => t.Offset).ThenBy(t => t.City).ToList();
    }

    private static TimeZoneItem CreateItem(TimeZoneInfo tz, string? cityOverride = null)
    {
        var city = cityOverride ?? GetCity(tz);
        var now = DateTime.Now;
        var offset = tz.GetUtcOffset(now);
        var totalMinutes = Math.Abs((int)offset.TotalMinutes);
        var sign = offset < TimeSpan.Zero ? "-" : "+";
        var offsetText = $"UTC{sign}{totalMinutes / 60:D2}:{totalMinutes % 60:D2}";
        var display = $"{city} · {offsetText}";

        return new TimeZoneItem
        {
            Id = tz.Id,
            Display = display,
            City = city,
            Offset = offset
        };
    }

    public static TimeZoneInfo? FindTimeZone(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch
        {
            return null;
        }
    }

    private static TimeZoneInfo? FindConvertedTimeZone(string id)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                if (TimeZoneInfo.TryConvertIanaIdToWindowsId(id, out var windowsId) && !string.IsNullOrWhiteSpace(windowsId))
                {
                    return FindTimeZone(windowsId);
                }
            }
            else
            {
                if (TimeZoneInfo.TryConvertWindowsIdToIanaId(id, out var ianaId) && !string.IsNullOrWhiteSpace(ianaId))
                {
                    return FindTimeZone(ianaId);
                }
            }
        }
        catch
        {
            // ignore conversion failures
        }

        return null;
    }

    public static TimeZoneItem? FindItem(string? id, List<TimeZoneItem>? cache = null)
    {
        return (cache ?? GetTimeZones()).FirstOrDefault(t => t.Id == id);
    }

    public static string GetCity(TimeZoneInfo tz)
    {
        if (CityOverrides.TryGetValue(tz.Id, out var city))
        {
            return city;
        }

        var display = tz.DisplayName;
        if (!string.IsNullOrWhiteSpace(display))
        {
            var closeIndex = display.IndexOf(')');
            if (closeIndex >= 0 && closeIndex + 1 < display.Length)
            {
                var after = display.Substring(closeIndex + 1).Trim();
                if (!string.IsNullOrWhiteSpace(after))
                {
                    var commaIndex = after.IndexOf(',');
                    var first = commaIndex >= 0 ? after.Substring(0, commaIndex).Trim() : after;
                    if (!string.IsNullOrWhiteSpace(first))
                    {
                        return first;
                    }
                }
            }
        }

        return tz.StandardName;
    }

    public static DateTime ConvertToTimeZone(DateTime local, string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return local;
        }

        var tz = FindTimeZone(timeZoneId) ?? FindConvertedTimeZone(timeZoneId);
        if (tz is null)
        {
            return local;
        }

        try
        {
            return TimeZoneInfo.ConvertTime(local, tz);
        }
        catch
        {
            return local;
        }
    }
}
