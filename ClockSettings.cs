using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace AnalogClock;

public class ClockSettings
{
    public double? Left { get; set; }
    public double? Top { get; set; }
    public double Width { get; set; } = 400;
    public double Height { get; set; } = 400;
    public bool ClockVisible { get; set; } = true;
    public string SecondHandState { get; set; } = "Red";
    public string TimeZoneId { get; set; } = "W. Europe Standard Time";
    public bool IsLicensed { get; set; } = false;
    public string LicenseKeyEncrypted { get; set; } = string.Empty;
    [JsonIgnore]
    public string LicenseKey { get; set; } = string.Empty;
    public string ActivationTokenEncrypted { get; set; } = string.Empty;
    [JsonIgnore]
    public string ActivationToken { get; set; } = string.Empty;
    public ClockTheme CurrentTheme { get; set; } = new();
    public List<ClockTheme> Themes { get; set; } = new() { new() };
    public ObservableCollection<Alarm> Alarms { get; set; } = new();

    public string GetBaseDirectory()
    {
        return AppContext.BaseDirectory;
    }
}

public enum AlarmMode
{
    Default,
    Visual,
    AcousticNoBlink,
    Background
}

public class Alarm : INotifyPropertyChanged
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Description { get; set; } = string.Empty;
    public int Hour { get; set; }
    public int Minute { get; set; }
    public bool Monday { get; set; } = true;
    public bool Tuesday { get; set; } = true;
    public bool Wednesday { get; set; } = true;
    public bool Thursday { get; set; } = true;
    public bool Friday { get; set; } = true;
    public bool Saturday { get; set; } = true;
    public bool Sunday { get; set; } = true;
    public bool Enabled { get; set; } = true;
    public string Command { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public AlarmMode Mode { get; set; } = AlarmMode.Default;
    public string SoundFile { get; set; } = string.Empty;
    public List<RecurrenceRule> RecurrenceRules { get; set; } = new();

    public string DisplayText => ToString();

    public event PropertyChangedEventHandler? PropertyChanged;

    public void RefreshDisplay()
    {
        OnPropertyChanged(nameof(DisplayText));
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public override string ToString()
    {
        string recurrence;
        if (RecurrenceRules.Count > 0)
        {
            recurrence = string.Join("; ", RecurrenceRules.Select(r => r.DisplayText));
        }
        else
        {
            var days = new[] { Monday ? "Mo" : null, Tuesday ? "Di" : null, Wednesday ? "Mi" : null, Thursday ? "Do" : null, Friday ? "Fr" : null, Saturday ? "Sa" : null, Sunday ? "So" : null };
            recurrence = string.Join(", ", Array.FindAll(days, x => x is not null));
            if (string.IsNullOrEmpty(recurrence)) recurrence = "Einmal";
        }

        var mode = Mode switch
        {
            AlarmMode.Visual => " [Still]",
            AlarmMode.AcousticNoBlink => " [Akustisch]",
            AlarmMode.Background => " [Hintergrund]",
            _ => string.Empty
        };
        return $"{Hour:D2}:{Minute:D2} {recurrence}{mode} - {Description}".TrimEnd(' ', '-');
    }
}
