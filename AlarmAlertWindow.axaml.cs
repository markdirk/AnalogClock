using System;
using System.Collections.Generic;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;

namespace AnalogClock;

public partial class AlarmAlertWindow : Window
{
    private static AlarmAlertWindow? _current;

    private DispatcherTimer? _blinkTimer;
    private bool _red = true;
    private readonly List<CancellationTokenSource> _soundTokens = new();
    private readonly Color _darkColor = Color.Parse("#FF2D2D2D");
    private readonly Color _redColor = Color.Parse("#FF800020");

    public AlarmAlertWindow()
    {
        InitializeComponent();
    }

    public AlarmAlertWindow(ClockTheme theme) : this()
    {
        var borderColor = TryParseColor(theme.BorderColor, Color.Parse("#FF050505"));
        if (RootBorder is not null)
        {
            RootBorder.BorderBrush = new SolidColorBrush(borderColor);
        }

        StopButton!.PointerPressed += (_, e) =>
        {
            e.Handled = true;
            Close();
        };

        RootBorder!.PointerPressed += (_, e) =>
        {
            if (e.Source is TextBlock)
            {
                return;
            }
            e.Handled = true;
            Close();
        };

        Closed += (_, _) =>
        {
            _blinkTimer?.Stop();
            foreach (var token in _soundTokens)
            {
                try
                {
                    token.Cancel();
                    token.Dispose();
                }
                catch
                {
                    // ignore
                }
            }
            _soundTokens.Clear();
            _current = null;
        };
    }

    public static void ShowAlert(Window? owner, ClockTheme theme, string description, DateTime triggerTime, bool blink, CancellationTokenSource? soundToken)
    {
        if (_current is null || !_current.IsVisible)
        {
            _current = new AlarmAlertWindow(theme);
            if (owner is not null && !owner.IsVisible)
            {
                owner = null;
            }

            if (owner is not null)
            {
                _current.Show(owner);
            }
            else
            {
                _current.Show();
            }

            _current.Activate();
        }

        _current!.AddAlarm(description, triggerTime, blink, soundToken);
    }

    private void AddAlarm(string description, DateTime triggerTime, bool blink, CancellationTokenSource? soundToken)
    {
        if (soundToken is not null)
        {
            _soundTokens.Add(soundToken);
        }

        var text = new TextBlock
        {
            Text = $"{triggerTime:HH:mm} {description}",
            FontSize = 24,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center
        };

        AlarmStack!.Children.Add(text);

        if (blink && _blinkTimer is null)
        {
            StartBlink();
        }

        Activate();
    }

    private void StartBlink()
    {
        RootBorder!.Background = new SolidColorBrush(_redColor);
        _red = true;
        _blinkTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _blinkTimer.Tick += (_, _) =>
        {
            _red = !_red;
            if (RootBorder is not null)
            {
                RootBorder.Background = new SolidColorBrush(_red ? _redColor : _darkColor);
            }
        };
        _blinkTimer.Start();
    }

    private static Color TryParseColor(string? value, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        try
        {
            return Color.Parse(value);
        }
        catch
        {
            return fallback;
        }
    }
}
