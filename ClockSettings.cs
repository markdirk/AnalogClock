using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AnalogClock;

public class ClockSettings
{
    public double? Left { get; set; }
    public double? Top { get; set; }
    public double Width { get; set; } = 400;
    public double Height { get; set; } = 400;
    public string SecondHandState { get; set; } = "White";
    public ClockTheme? CurrentTheme { get; set; }
    public List<ClockTheme> Themes { get; set; } = new();
    public ObservableCollection<Alarm> Alarms { get; set; } = new();
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
        var days = new[] { Monday ? "Mo" : null, Tuesday ? "Di" : null, Wednesday ? "Mi" : null, Thursday ? "Do" : null, Friday ? "Fr" : null, Saturday ? "Sa" : null, Sunday ? "So" : null };
        var dayText = string.Join(", ", Array.FindAll(days, x => x is not null));
        if (string.IsNullOrEmpty(dayText)) dayText = "Einmal";
        return $"{Hour:D2}:{Minute:D2} {dayText} - {Description}".TrimEnd(' ', '-');
    }
}
