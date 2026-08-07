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
        ["Asia/Dubai"] = "Dubai",
        ["Asia/Kolkata"] = "Mumbai",
        ["Asia/Shanghai"] = "Peking",
        ["Asia/Tokyo"] = "Tokio",
        ["Asia/Seoul"] = "Seoul",
        ["Australia/Sydney"] = "Sydney",
        ["Pacific/Auckland"] = "Wellington",
        ["Africa/Johannesburg"] = "Johannesburg",
        ["Africa/Cairo"] = "Kairo",
        ["Europe/Moscow"] = "Moskau",
        ["Asia/Bangkok"] = "Bangkok",
        ["Asia/Singapore"] = "Singapur",
        ["America/Vancouver"] = "Vancouver",
        ["Europe/Istanbul"] = "Istanbul",
        ["Pacific/Honolulu"] = "Honolulu"
    };

    public static List<TimeZoneItem> GetTimeZones()
    {
        var list = new List<TimeZoneItem>();
        var zones = TimeZoneInfo.GetSystemTimeZones();

        foreach (var tz in zones)
        {
            var city = GetCity(tz);
            var now = DateTime.Now;
            var offset = tz.GetUtcOffset(now);
            var totalMinutes = Math.Abs((int)offset.TotalMinutes);
            var sign = offset < TimeSpan.Zero ? "-" : "+";
            var offsetText = $"UTC{sign}{totalMinutes / 60:D2}:{totalMinutes % 60:D2}";
            var display = $"{city} · {offsetText}";

            list.Add(new TimeZoneItem
            {
                Id = tz.Id,
                Display = display,
                City = city,
                Offset = offset
            });
        }

        return list.OrderBy(t => t.Offset).ThenBy(t => t.City).ToList();
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
        var tz = FindTimeZone(timeZoneId);
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
