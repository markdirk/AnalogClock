using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

namespace AnalogClock;

public enum RecurrenceType
{
    Weekly,
    SpecificDates,
    MonthlyDay,
    MonthlyWeekday,
    Interval
}

public enum RecurrenceUnit
{
    Days,
    Weeks
}

public class RecurrenceRule : INotifyPropertyChanged
{
    public RecurrenceType Type { get; set; } = RecurrenceType.Weekly;

    public List<DayOfWeek> DaysOfWeek { get; set; } = new();

    public List<DateTime> Dates { get; set; } = new();

    public List<int> MonthDays { get; set; } = new();

    public DayOfWeek? MonthWeekday { get; set; }

    public int MonthWeekdayOrdinal { get; set; } = 1;

    public bool MonthWeekdayBeforeLastDay { get; set; }

    public int IntervalValue { get; set; } = 1;

    public RecurrenceUnit IntervalUnit { get; set; } = RecurrenceUnit.Days;

    public DateTime? IntervalStart { get; set; }

    public bool Matches(DateTime date)
    {
        return Type switch
        {
            RecurrenceType.Weekly => DaysOfWeek.Contains(date.DayOfWeek),
            RecurrenceType.SpecificDates => Dates.Any(d => d.Date == date.Date),
            RecurrenceType.MonthlyDay => MatchesMonthlyDay(date),
            RecurrenceType.MonthlyWeekday => MatchesMonthlyWeekday(date),
            RecurrenceType.Interval => MatchesInterval(date),
            _ => false
        };
    }

    private bool MatchesMonthlyDay(DateTime date)
    {
        if (MonthDays.Count == 0)
        {
            return false;
        }

        var daysInMonth = DateTime.DaysInMonth(date.Year, date.Month);
        foreach (var day in MonthDays)
        {
            if (day > 0 && date.Day == day)
            {
                return true;
            }

            if (day < 0)
            {
                var fromEnd = daysInMonth + day + 1;
                if (fromEnd >= 1 && fromEnd <= daysInMonth && date.Day == fromEnd)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool MatchesMonthlyWeekday(DateTime date)
    {
        if (MonthWeekday is null)
        {
            return false;
        }

        var daysInMonth = DateTime.DaysInMonth(date.Year, date.Month);
        var reference = new DateTime(date.Year, date.Month, daysInMonth);
        if (MonthWeekdayBeforeLastDay)
        {
            reference = reference.AddDays(-1);
        }

        if (MonthWeekdayOrdinal > 0)
        {
            var target = GetNthWeekdayOfMonth(date.Year, date.Month, MonthWeekday.Value, MonthWeekdayOrdinal);
            return target.HasValue && target.Value.Day == date.Day;
        }

        var ordinal = Math.Abs(MonthWeekdayOrdinal);
        var count = 0;
        for (int i = 0; i < 31; i++)
        {
            var d = reference.AddDays(-i);
            if (d.Month != date.Month)
            {
                break;
            }

            if (d.DayOfWeek == MonthWeekday)
            {
                count++;
                if (count == ordinal && d.Day == date.Day)
                {
                    return true;
                }

                if (count == ordinal)
                {
                    return false;
                }
            }
        }

        return false;
    }

    private static DateTime? GetNthWeekdayOfMonth(int year, int month, DayOfWeek weekday, int n)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var count = 0;
        for (int day = 1; day <= daysInMonth; day++)
        {
            var d = new DateTime(year, month, day);
            if (d.DayOfWeek == weekday)
            {
                count++;
                if (count == n)
                {
                    return d;
                }
            }
        }

        return null;
    }

    private bool MatchesInterval(DateTime date)
    {
        if (IntervalValue <= 0)
        {
            return false;
        }

        var start = IntervalStart?.Date ?? Dates.FirstOrDefault().Date;
        if (start == default)
        {
            start = date.Date;
        }

        if (date.Date < start)
        {
            return false;
        }

        var diff = (date.Date - start).Days;
        if (diff < 0)
        {
            return false;
        }

        if (IntervalUnit == RecurrenceUnit.Weeks)
        {
            var weeks = diff / 7;
            if (weeks % IntervalValue != 0)
            {
                return false;
            }

            if (DaysOfWeek.Count > 0 && !DaysOfWeek.Contains(date.DayOfWeek))
            {
                return false;
            }

            return true;
        }

        return diff % IntervalValue == 0;
    }

    private string _displayText = string.Empty;

    public string DisplayText
    {
        get => _displayText;
        private set
        {
            if (_displayText != value)
            {
                _displayText = value;
                OnPropertyChanged();
            }
        }
    }

    public void RefreshDisplay()
    {
        DisplayText = GetDisplayText();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private string GetDisplayText()
    {
        return Type switch
        {
            RecurrenceType.Weekly => $"Wochentage: {(DaysOfWeek.Count == 0 ? "keine" : string.Join(", ", DaysOfWeek.Select(GermanDay)))}",
            RecurrenceType.SpecificDates => $"Termine: {(Dates.Count == 0 ? "keine" : string.Join(", ", Dates.Select(d => d.ToString("dd.MM.yyyy"))))}",
            RecurrenceType.MonthlyDay => $"Monatstage: {(MonthDays.Count == 0 ? "keine" : string.Join(", ", MonthDays))}",
            RecurrenceType.MonthlyWeekday => $"{GermanOrdinal(MonthWeekdayOrdinal)} {GermanDay(MonthWeekday ?? DayOfWeek.Monday)}{(MonthWeekdayBeforeLastDay ? " vor Monatsletztem" : "")}",
            RecurrenceType.Interval => $"Alle {IntervalValue} {GermanUnit(IntervalUnit)}{(DaysOfWeek.Count > 0 ? $" am {string.Join(", ", DaysOfWeek.Select(GermanDay))}" : "")}",
            _ => string.Empty
        };
    }

    private static string GermanDay(DayOfWeek day)
    {
        return day switch
        {
            DayOfWeek.Monday => "Mo",
            DayOfWeek.Tuesday => "Di",
            DayOfWeek.Wednesday => "Mi",
            DayOfWeek.Thursday => "Do",
            DayOfWeek.Friday => "Fr",
            DayOfWeek.Saturday => "Sa",
            DayOfWeek.Sunday => "So",
            _ => day.ToString()
        };
    }

    private static string GermanOrdinal(int ordinal)
    {
        return ordinal switch
        {
            1 => "Jeder 1.",
            2 => "Jeder 2.",
            3 => "Jeder 3.",
            4 => "Jeder 4.",
            5 => "Jeder 5.",
            -1 => "Jeder letzte",
            -2 => "Jeder vorletzte",
            -3 => "Jeder drittletzte",
            -4 => "Jeder viertletzte",
            _ => $"Jeder {ordinal}."
        };
    }

    private static string GermanUnit(RecurrenceUnit unit)
    {
        return unit switch
        {
            RecurrenceUnit.Days => "Tage",
            RecurrenceUnit.Weeks => "Wochen",
            _ => unit.ToString()
        };
    }
}
