using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;

namespace AnalogClock;

public class ClockControl : Control
{
    private Window? _window;
    private AlarmWindow? _alarmWindow;
    private ThemeWindow? _themeWindow;
    private TimeZoneWindow? _timeZoneWindow;
    private InfoWindow? _infoWindow;
    private LicenseWindow? _licenseWindow;
    private DispatcherTimer? _timer;
    private ClockSettings _settings = new();

    private IBrush? _faceBrush;
    private IBrush? _borderBrush;
    private IBrush? _numberBrush;
    private IBrush? _hourHandBrush;
    private IBrush? _minuteHandBrush;
    private IBrush? _tickBrush;
    private IBrush? _gripBrush;
    private IBrush? _secondHandBrush;
    private IBrush? _dateBrush;
    private IBrush? _timeBrush;
    private bool _secondHandVisible;
    private bool _handsAboveInfo;
    private IBrush? _centerDotBorderBrush;
    private IBrush? _dateBoxBgBrush;
    private IBrush? _dateBoxBorderBrush;
    private IBrush? _timeBoxBgBrush;
    private IBrush? _timeBoxBorderBrush;
    private double _dateBoxXOffset;
    private double _dateBoxYOffset;
    private double _timeBoxXOffset;
    private double _timeBoxYOffset;
    private FontFamily _numberFont = FontFamily.Default;
    private FontFamily _dateFont = new("Segoe UI");
    private FontFamily _timeFont = new("Segoe UI");
    private double _numberFontScale = 1.0;
    private double _dateFontScale = 1.0;
    private double _timeFontScale = 1.0;

    private static readonly FontFamily CustomDigitalFont = new("avares://AnalogClock/Assets/Digital7Mono.ttf#Digital-7 Mono");

    private const double TaperRatio = 0.95;

    private bool _isMoving;
    private bool _isResizing;
    private PixelPoint _moveStartScreen;
    private PixelPoint _windowStartPosition;
    private Point _resizeStartPos;
    private Size _resizeStartSize;

    private int _lastAlarmHour = -1;
    private int _lastAlarmMinute = -1;
    private DateTime _lastAlarmDate = DateTime.MinValue;
    private readonly HashSet<Guid> _triggeredThisMinute = new();
    private List<TimeZoneItem> _timeZones = new();

    public ClockControl()
    {
        ClipToBounds = false;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _window = TopLevel.GetTopLevel(this) as Window;

        _settings = SettingsService.Load();
        _timeZones = TimeZoneHelper.GetTimeZones();
        EnsureTheme();
        ApplyTheme();



        if (_window is not null)
        {
            _window.Opened += OnWindowOpened;
            _window.Closing += OnWindowClosing;
        }

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _timer.Tick += (_, _) =>
        {
            InvalidateVisual();
            CheckAlarms();
        };
        _timer.Start();

        this.ContextMenu = BuildContextMenu();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        _timer?.Stop();
        _timer = null;

        if (_window is not null)
        {
            _window.Opened -= OnWindowOpened;
            _window.Closing -= OnWindowClosing;
        }

        _window = null;
        this.ContextMenu = null;
    }

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        if (_window is null)
        {
            return;
        }

        if (!double.IsNaN(_settings.Width) && _settings.Width >= 200)
        {
            _window.Width = _settings.Width;
        }

        if (!double.IsNaN(_settings.Height) && _settings.Height >= 200)
        {
            _window.Height = _settings.Height;
        }

        if (_settings.Left.HasValue && _settings.Top.HasValue)
        {
            _window.Position = new PixelPoint((int)_settings.Left.Value, (int)_settings.Top.Value);
        }
    }

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_window is null)
        {
            return;
        }

        _settings.Width = _window.Width;
        _settings.Height = _window.Height;
        _settings.Left = _window.Position.X;
        _settings.Top = _window.Position.Y;
        SettingsService.Save(_settings);
    }

    private ContextMenu BuildContextMenu()
    {
        var menu = new ContextMenu();

        var showSecondHandText = new TextBlock();
        var showSecondHandItem = new MenuItem { Header = showSecondHandText };
        showSecondHandItem.Click += (_, _) =>
        {
            if (_settings.CurrentTheme is null)
            {
                return;
            }

            _settings.CurrentTheme.SecondHandVisible = !_settings.CurrentTheme.SecondHandVisible;
            _settings.SecondHandState = _settings.CurrentTheme.SecondHandVisible ? "Red" : "Hidden";
            EnsureTheme();
            ApplyThemeAndSave();
        };

        var handsAboveText = new TextBlock();
        var handsAboveItem = new MenuItem { Header = handsAboveText };
        handsAboveItem.Click += (_, _) =>
        {
            if (_settings.CurrentTheme is null)
            {
                return;
            }

            _settings.CurrentTheme.HandsAboveInfo = !_settings.CurrentTheme.HandsAboveInfo;
            ApplyThemeAndSave();
        };

        var zeigerItem = new MenuItem { Header = "Zeiger" };
        zeigerItem.Items.Add(CreateMenuItem("Sekunden-Zeiger rot", () => SetSecondHand("Red")));
        zeigerItem.Items.Add(CreateMenuItem("Sekunden-Zeiger weiß", () => SetSecondHand("White")));
        zeigerItem.Items.Add(CreateMenuItem("Sekundenzeiger aus", () => SetSecondHand("Hidden")));
        zeigerItem.Items.Add(new Separator());
        zeigerItem.Items.Add(showSecondHandItem);
        zeigerItem.Items.Add(handsAboveItem);
        menu.Items.Add(zeigerItem);

        menu.Items.Add(CreateMenuItem("Wecker", OpenAlarmWindow));

        menu.Items.Add(CreateMenuItem("Zeitzone", OpenTimeZoneWindow));

        var themeItem = new MenuItem { Header = "Theme" };
        var themesSub = new MenuItem { Header = "Laden" };
        themeItem.Items.Add(themesSub);
        themeItem.Items.Add(CreateMenuItem("Bearbeiten...", OpenThemeWindow));
        menu.Items.Add(themeItem);

        menu.Items.Add(CreateMenuItem("Uhr ausblenden", HideClockWindow));

        menu.Items.Add(CreateMenuItem("Info", OpenInfoWindow));
        menu.Items.Add(CreateMenuItem("Lizenz", OpenLicenseWindow));

        menu.Items.Add(CreateMenuItem("Beenden", ExitApplication));

        menu.Opening += (_, _) =>
        {
            RebuildThemeMenu(themesSub);
            showSecondHandText.Text = ToggleText("Sekundenzeiger anzeigen", _settings.CurrentTheme?.SecondHandVisible ?? true);
            handsAboveText.Text = ToggleText("Zeiger über Datum/Zeit", _settings.CurrentTheme?.HandsAboveInfo ?? false);
        };

        return menu;
    }

    private static string ToggleText(string label, bool isChecked)
    {
        return isChecked ? $"[x] {label}" : $"[ ] {label}";
    }

    private MenuItem CreateMenuItem(string header, Action action)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) =>
        {
            this.ContextMenu?.Close();
            action();
        };
        return item;
    }

    private void RebuildThemeMenu(MenuItem themesSub)
    {
        themesSub.Items.Clear();
        foreach (var theme in _settings.Themes)
        {
            var name = theme.Name;
            themesSub.Items.Add(CreateMenuItem(name, () => ApplyThemeByName(name)));
        }

        if (themesSub.Items.Count == 0)
        {
            themesSub.Items.Add(new MenuItem { Header = "(keine)", IsEnabled = false });
        }
    }

    private void ApplyThemeByName(string name)
    {
        var theme = _settings.Themes.Find(t => t.Name == name);
        if (theme is null)
        {
            return;
        }

        _settings.CurrentTheme = theme;
        ApplyTheme();
        SettingsService.Save(_settings);
        InvalidateVisual();
    }

    private void EnsureTheme()
    {
        _settings.CurrentTheme ??= CreateDefaultTheme();
        _settings.CurrentTheme.SecondHandVisible = _settings.SecondHandState != "Hidden";
        _settings.CurrentTheme.SecondHandColor = _settings.SecondHandState == "Red" ? "#FF800020" : "#FFFFFFFF";

        if (_settings.Themes.Count == 0)
        {
            _settings.Themes.Add(_settings.CurrentTheme.Clone());
        }

        if (_settings.Themes.Find(t => t.Name == "Icon") is null)
        {
            _settings.Themes.Add(CreateIconTheme());
        }
    }

    private ClockTheme CreateDefaultTheme()
    {
        return new ClockTheme();
    }

    private ClockTheme CreateIconTheme()
    {
        return new ClockTheme
        {
            Name = "Icon",
            FaceColor = "#FFFFFFFF",
            BorderColor = "#FF000000",
            NumberColor = "#FF000000",
            HourHandColor = "#FF000000",
            MinuteHandColor = "#FF000000",
            SecondHandColor = "#FF000000",
            TickColor = "#FF000000",
            GripColor = "#FF808080",
            FontName = "Segoe UI",
            NumberFontScale = 1.0,
            DateColor = "#FF000000",
            DateFontName = "Segoe UI",
            DateFontScale = 1.0,
            TimeColor = "#FF000000",
            TimeFontName = "Segoe UI",
            TimeFontScale = 1.0,
            SecondHandVisible = true,
            HandsAboveInfo = true,
            CenterDotBorderColor = "#FF000000",
            DateBoxBackgroundColor = "#FFFFFFFF",
            DateBoxBorderColor = "#FF000000",
            DateBoxXOffset = 0.0,
            DateBoxYOffset = 0.0,
            TimeBoxBackgroundColor = "#FFFFFFFF",
            TimeBoxBorderColor = "#FF000000",
            TimeBoxXOffset = 0.0,
            TimeBoxYOffset = 0.0
        };
    }

    private void ApplyTheme()
    {
        var theme = _settings.CurrentTheme ?? CreateDefaultTheme();
        _faceBrush = ParseBrush(theme.FaceColor);
        _borderBrush = ParseBrush(theme.BorderColor);
        _numberBrush = ParseBrush(theme.NumberColor);
        _hourHandBrush = ParseBrush(theme.HourHandColor);
        _minuteHandBrush = ParseBrush(theme.MinuteHandColor);
        _tickBrush = ParseBrush(theme.TickColor);
        _gripBrush = ParseBrush(theme.GripColor);
        _secondHandVisible = theme.SecondHandVisible;
        _secondHandBrush = ParseBrush(theme.SecondHandColor);
        _dateBrush = ParseBrush(theme.DateColor);
        _timeBrush = ParseBrush(theme.TimeColor);

        _numberFont = ParseFont(theme.FontName);
        _dateFont = ParseFont(theme.DateFontName);
        _timeFont = ParseFont(theme.TimeFontName);
        _numberFontScale = theme.NumberFontScale;
        _dateFontScale = theme.DateFontScale;
        _timeFontScale = theme.TimeFontScale;

        _handsAboveInfo = theme.HandsAboveInfo;
        _centerDotBorderBrush = ParseBrush(theme.CenterDotBorderColor);
        _dateBoxBgBrush = ParseBrush(theme.DateBoxBackgroundColor);
        _dateBoxBorderBrush = ParseBrush(theme.DateBoxBorderColor);
        _timeBoxBgBrush = ParseBrush(theme.TimeBoxBackgroundColor);
        _timeBoxBorderBrush = ParseBrush(theme.TimeBoxBorderColor);
        _dateBoxXOffset = theme.DateBoxXOffset;
        _dateBoxYOffset = theme.DateBoxYOffset;
        _timeBoxXOffset = theme.TimeBoxXOffset;
        _timeBoxYOffset = theme.TimeBoxYOffset;
    }

    private static FontFamily ParseFont(string? name)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            var trimmed = name.Trim();
            if (string.Equals(trimmed, "Digital-7 Mono", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(trimmed, "Digital7Mono", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(trimmed, "Digital 7 Mono", StringComparison.OrdinalIgnoreCase))
            {
                return CustomDigitalFont;
            }

            try
            {
                return new FontFamily(trimmed);
            }
            catch
            {
                // fall through
            }
        }

        return FontFamily.Default;
    }

    private static IBrush ParseBrush(string? color)
    {
        if (!string.IsNullOrWhiteSpace(color) && Color.TryParse(color, out var c))
        {
            return new SolidColorBrush(c);
        }

        return Brushes.White;
    }

    private static Color DarkenColor(Color color, double factor)
    {
        factor = Math.Clamp(factor, 0, 1);
        return Color.FromArgb(color.A,
            (byte)(color.R * factor),
            (byte)(color.G * factor),
            (byte)(color.B * factor));
    }

    private static Color LightenColor(Color color, double factor)
    {
        factor = Math.Clamp(factor, 0, 1);
        return Color.FromArgb(color.A,
            (byte)(color.R + (255 - color.R) * factor),
            (byte)(color.G + (255 - color.G) * factor),
            (byte)(color.B + (255 - color.B) * factor));
    }

    private void SetSecondHand(string state)
    {
        _settings.SecondHandState = state;
        EnsureTheme();
        _settings.CurrentTheme!.SecondHandVisible = state != "Hidden";
        _settings.CurrentTheme.SecondHandColor = state == "Red" ? "#FF800020" : "#FFFFFFFF";
        ApplyTheme();
        SettingsService.Save(_settings);
        InvalidateVisual();
    }

    private void OpenAlarmWindow()
    {
        if (_window is null)
        {
            return;
        }

        if (_alarmWindow is not null)
        {
            _alarmWindow.Activate();
            return;
        }

        _alarmWindow = new AlarmWindow(_settings);
        _alarmWindow.Closed += (_, _) => _alarmWindow = null;
        _alarmWindow.WindowStartupLocation = WindowStartupLocation.Manual;
        _alarmWindow.PositionNextTo(_window);
        _alarmWindow.Show();
        _alarmWindow.Activate();
        _alarmWindow.Topmost = false;
        _alarmWindow.Topmost = true;
    }

    private void OpenThemeWindow()
    {
        if (_window is null)
        {
            return;
        }

        if (_themeWindow is not null)
        {
            _themeWindow.Activate();
            return;
        }

        _themeWindow = new ThemeWindow(_settings, ApplyThemeAndSave);
        _themeWindow.Closed += (_, _) => _themeWindow = null;
        _themeWindow.WindowStartupLocation = WindowStartupLocation.Manual;
        _themeWindow.PositionNextTo(_window);
        _themeWindow.Show();
        _themeWindow.Activate();
        _themeWindow.Topmost = false;
        _themeWindow.Topmost = true;
    }

    private void ApplyThemeAndSave()
    {
        ApplyTheme();
        SettingsService.Save(_settings);
        InvalidateVisual();
    }

    private void OpenTimeZoneWindow()
    {
        if (_window is null)
        {
            return;
        }

        if (_timeZoneWindow is not null)
        {
            _timeZoneWindow.Activate();
            return;
        }

        _timeZoneWindow = new TimeZoneWindow(_settings);
        _timeZoneWindow.Closed += (_, _) =>
        {
            _timeZoneWindow = null;
            _timeZones = TimeZoneHelper.GetTimeZones();
            SettingsService.Save(_settings);
            InvalidateVisual();
        };
        _timeZoneWindow.WindowStartupLocation = WindowStartupLocation.Manual;
        _timeZoneWindow.PositionNextTo(_window);
        _timeZoneWindow.Show();
        _timeZoneWindow.Activate();
        _timeZoneWindow.Topmost = false;
        _timeZoneWindow.Topmost = true;
    }

    private void HideClockWindow()
    {
        _settings.ClockVisible = false;
        SettingsService.Save(_settings);
        App.SetClockVisible(false);
    }

    private void OpenInfoWindow()
    {
        if (_infoWindow is not null)
        {
            _infoWindow.Activate();
            return;
        }

        _infoWindow = new InfoWindow();
        _infoWindow.Closed += (_, _) => _infoWindow = null;
        _infoWindow.Show();
        _infoWindow.Activate();
    }

    private void OpenLicenseWindow()
    {
        if (_licenseWindow is not null)
        {
            _licenseWindow.Activate();
            return;
        }

        _licenseWindow = new LicenseWindow(_settings);
        _licenseWindow.Closed += (_, _) =>
        {
            _licenseWindow = null;
            if (_settings.IsLicensed)
            {
                SettingsService.Save(_settings);
            }
        };
        _licenseWindow.Show();
        _licenseWindow.Activate();
    }

    private void ExitApplication()
    {
        if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.Shutdown();
        }
        else
        {
            Environment.Exit(0);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (_window is null)
        {
            return;
        }

        var pos = e.GetPosition(this);
        var (cx, cy, radius) = GetClockMetrics();
        var resizeCenter = GetResizeGripCenter(cx, cy, radius);
        var inResize = Distance(pos, resizeCenter) <= radius * 0.12;

        var props = e.GetCurrentPoint(this).Properties;

        if (props.IsRightButtonPressed)
        {
            return;
        }

        if (props.IsLeftButtonPressed)
        {
            e.Pointer.Capture(this);

            if (inResize)
            {
                _isResizing = true;
                _resizeStartPos = pos;
                _resizeStartSize = new Size(_window.Width, _window.Height);
            }
            else
            {
                _isMoving = true;
                _moveStartScreen = _window.PointToScreen(pos);
                _windowStartPosition = _window.Position;
            }

            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (_window is null)
        {
            return;
        }

        var pos = e.GetPosition(this);

        if (_isMoving && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var currentScreen = _window.PointToScreen(pos);
            var delta = new PixelPoint(currentScreen.X - _moveStartScreen.X, currentScreen.Y - _moveStartScreen.Y);
            _window.Position = new PixelPoint(_windowStartPosition.X + delta.X, _windowStartPosition.Y + delta.Y);
        }
        else if (_isResizing && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var delta = pos - _resizeStartPos;
            var scale = _window.RenderScaling;
            var newWidth = Math.Max(200, _resizeStartSize.Width + delta.X / scale);
            var newHeight = Math.Max(200, _resizeStartSize.Height + delta.Y / scale);
            _window.Width = newWidth;
            _window.Height = newHeight;
        }
        else
        {
            var (cx, cy, radius) = GetClockMetrics();
            var resizeCenter = GetResizeGripCenter(cx, cy, radius);
            var inResize = Distance(pos, resizeCenter) <= radius * 0.12;
            Cursor = inResize ? new Cursor(StandardCursorType.BottomRightCorner) : new Cursor(StandardCursorType.Arrow);
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        _isMoving = false;
        _isResizing = false;
        e.Pointer.Capture(null);
    }

    private void CheckAlarms()
    {
        var now = GetClockTime();

        if (now.Date != _lastAlarmDate.Date || now.Hour != _lastAlarmHour || now.Minute != _lastAlarmMinute)
        {
            _triggeredThisMinute.Clear();
            _lastAlarmDate = now;
            _lastAlarmHour = now.Hour;
            _lastAlarmMinute = now.Minute;
        }

        foreach (var alarm in _settings.Alarms)
        {
            if (!alarm.Enabled)
            {
                continue;
            }

            if (alarm.Hour != now.Hour || alarm.Minute != now.Minute)
            {
                continue;
            }

            if (!IsAlarmDay(alarm, now))
            {
                continue;
            }

            if (_triggeredThisMinute.Contains(alarm.Id))
            {
                continue;
            }

            _triggeredThisMinute.Add(alarm.Id);
            TriggerAlarm(alarm);
        }
    }

    private static bool IsAlarmDay(Alarm alarm, DateTime date)
    {
        if (alarm.RecurrenceRules.Count > 0)
        {
            return alarm.RecurrenceRules.Any(r => r.Matches(date.Date));
        }

        return IsDayEnabled(alarm, date.DayOfWeek);
    }

    private static bool IsDayEnabled(Alarm alarm, DayOfWeek day)
    {
        return day switch
        {
            DayOfWeek.Monday => alarm.Monday,
            DayOfWeek.Tuesday => alarm.Tuesday,
            DayOfWeek.Wednesday => alarm.Wednesday,
            DayOfWeek.Thursday => alarm.Thursday,
            DayOfWeek.Friday => alarm.Friday,
            DayOfWeek.Saturday => alarm.Saturday,
            DayOfWeek.Sunday => alarm.Sunday,
            _ => false
        };
    }

    private void TriggerAlarm(Alarm alarm)
    {
        if (!string.IsNullOrWhiteSpace(alarm.Command))
        {
            try
            {
                Process.Start(new ProcessStartInfo(alarm.Command, alarm.Arguments)
                {
                    UseShellExecute = true,
                    CreateNoWindow = false
                });
            }
            catch
            {
                // ignore command failures
            }
        }

        var soundPath = ResolveSoundPath(alarm.SoundFile);
        var shouldPlaySound = alarm.Mode != AlarmMode.Visual;
        var shouldShowPanel = alarm.Mode != AlarmMode.Background;
        var shouldBlink = alarm.Mode == AlarmMode.Default || alarm.Mode == AlarmMode.Visual;

        CancellationTokenSource? soundCts = null;
        if (shouldPlaySound)
        {
            if (shouldShowPanel)
            {
                soundCts = new CancellationTokenSource();
                AudioPlayer.PlayAsync(soundPath, soundCts.Token, loop: true);
            }
            else
            {
                _ = AudioPlayer.PlayAsync(soundPath, CancellationToken.None, loop: false);
            }
        }

        if (!shouldShowPanel)
        {
            return;
        }

        var owner = _alarmWindow ?? _window;
        if (owner is null)
        {
            soundCts?.Cancel();
            soundCts?.Dispose();
            return;
        }

        var now = GetClockTime();
        AlarmAlertWindow.ShowAlert(owner, _settings.CurrentTheme, alarm.Description, now, shouldBlink, soundCts);
    }

    private string? ResolveSoundPath(string? soundFile)
    {
        if (string.IsNullOrWhiteSpace(soundFile))
        {
            return null;
        }

        if (File.Exists(soundFile))
        {
            return soundFile;
        }

        var baseDir = _settings.GetBaseDirectory();
        var combined = Path.Combine(baseDir, soundFile);
        return File.Exists(combined) ? combined : null;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var w = Bounds.Width;
        var h = Bounds.Height;

        if (w <= 0 || h <= 0)
        {
            return;
        }

        var cx = w / 2.0;
        var cy = h / 2.0;
        var minHalf = Math.Min(w, h) / 2.0;
        var radius = minHalf * 0.80;
        var hourWidth = radius * 0.08;
        var borderWidth = radius * 0.10;

        var shadowSpread = radius * 0.20;
        const int shadowSteps = 25;
        const double maxLayerAlpha = 10.0;
        var shadowInner = radius + borderWidth;

        for (int i = 0; i < shadowSteps; i++)
        {
            var t = i / (double)(shadowSteps - 1);
            var r = shadowInner + t * shadowSpread;
            var a = (byte)(maxLayerAlpha * (1.0 - t));
            context.DrawEllipse(new SolidColorBrush(Color.FromArgb(a, 0, 0, 0)), null, new Point(cx, cy), r, r);
        }

        context.DrawEllipse(_faceBrush ?? new SolidColorBrush(Color.Parse("#FF2D2D2D")), null, new Point(cx, cy), radius, radius);

        DrawBorder(context, new Point(cx, cy), radius, borderWidth);

        DrawTicks(context, cx, cy, radius);

        var now = GetClockTime();
        var hour = now.Hour % 12;
        var minute = now.Minute;
        var second = now.Second;
        var millisecond = now.Millisecond;

        var totalSeconds = second + millisecond / 1000.0;
        var totalMinutes = minute + totalSeconds / 60.0;
        var totalHours = hour + totalMinutes / 60.0;

        var hourAngle = totalHours * Math.PI / 6.0;
        var minuteAngle = totalMinutes * Math.PI / 30.0;

        var hourLength = radius * 0.58;
        var minuteLength = radius * 0.78;
        var minuteWidth = hourWidth / 1.5;

        DrawNumbers(context, cx, cy, radius, now.Hour);

        var city = GetSelectedCity();

        if (_handsAboveInfo)
        {
            DrawTime(context, cx, cy, radius, now, city);
            DrawDate(context, cx, cy, radius, now);
            DrawHand(context, cx, cy, hourAngle, hourLength, hourWidth, _hourHandBrush ?? Brushes.White);
            DrawHand(context, cx, cy, minuteAngle, minuteLength, minuteWidth, _minuteHandBrush ?? Brushes.White);

            if (_secondHandVisible && _secondHandBrush is not null)
            {
                var secondAngle = totalSeconds * Math.PI / 30.0;
                var secondLength = radius * 0.92;
                var secondTail = radius * 0.15;
                var secondWidth = radius * 0.008;
                var secondPen = new Pen(_secondHandBrush, secondWidth) { LineCap = PenLineCap.Round };
                var secondStart = new Point(cx - secondTail * Math.Sin(secondAngle), cy + secondTail * Math.Cos(secondAngle));
                var secondEnd = new Point(cx + secondLength * Math.Sin(secondAngle), cy - secondLength * Math.Cos(secondAngle));
                context.DrawLine(secondPen, secondStart, secondEnd);
            }

            DrawCenterDot(context, cx, cy, hourWidth);
        }
        else
        {
            DrawHand(context, cx, cy, hourAngle, hourLength, hourWidth, _hourHandBrush ?? Brushes.White);
            DrawHand(context, cx, cy, minuteAngle, minuteLength, minuteWidth, _minuteHandBrush ?? Brushes.White);

            if (_secondHandVisible && _secondHandBrush is not null)
            {
                var secondAngle = totalSeconds * Math.PI / 30.0;
                var secondLength = radius * 0.92;
                var secondTail = radius * 0.15;
                var secondWidth = radius * 0.008;
                var secondPen = new Pen(_secondHandBrush, secondWidth) { LineCap = PenLineCap.Round };
                var secondStart = new Point(cx - secondTail * Math.Sin(secondAngle), cy + secondTail * Math.Cos(secondAngle));
                var secondEnd = new Point(cx + secondLength * Math.Sin(secondAngle), cy - secondLength * Math.Cos(secondAngle));
                context.DrawLine(secondPen, secondStart, secondEnd);
            }

            DrawCenterDot(context, cx, cy, hourWidth);

            DrawTime(context, cx, cy, radius, now, city);
            DrawDate(context, cx, cy, radius, now);
        }

        DrawResizeGrip(context, cx, cy, radius);
    }

    private void DrawBorder(DrawingContext context, Point center, double radius, double borderWidth)
    {
        var outer = radius + borderWidth;
        var rimCenter = radius + borderWidth / 2.0;
        var baseColor = (_borderBrush as SolidColorBrush)?.Color ?? Colors.Black;
        var shadowColor = DarkenColor(baseColor, 0.45);
        var lightColor = LightenColor(baseColor, 0.65);

        var rimBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0.0, 0.0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1.0, 1.0, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(lightColor, 0.0),
                new GradientStop(baseColor, 0.40),
                new GradientStop(shadowColor, 1.0)
            }
        };

        context.DrawEllipse(null, new Pen(rimBrush, borderWidth) { LineCap = PenLineCap.Round }, center, rimCenter, rimCenter);

        var innerLineColor = Color.FromArgb((byte)(shadowColor.A / 2 + 70), shadowColor.R, shadowColor.G, shadowColor.B);
        context.DrawEllipse(null, new Pen(new SolidColorBrush(innerLineColor), borderWidth * 0.22) { LineCap = PenLineCap.Round }, center, radius + borderWidth * 0.12, radius + borderWidth * 0.12);
    }

    private (double cx, double cy, double radius) GetClockMetrics()
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        var cx = w / 2.0;
        var cy = h / 2.0;
        var radius = Math.Min(w, h) / 2.0 * 0.80;
        return (cx, cy, radius);
    }

    private Point GetResizeGripCenter(double cx, double cy, double radius)
    {
        const double angle = 3.0 * Math.PI / 4.0;
        return new Point(cx + radius * 0.82 * Math.Sin(angle), cy - radius * 0.82 * Math.Cos(angle));
    }

    private void DrawResizeGrip(DrawingContext context, double cx, double cy, double radius)
    {
        var center = GetResizeGripCenter(cx, cy, radius);
        var gripRadius = radius * 0.06;

        context.DrawEllipse(_gripBrush ?? new SolidColorBrush(Color.Parse("#33FFFFFF")), null, center, gripRadius, gripRadius);

        var pen = new Pen(_numberBrush ?? Brushes.White, radius * 0.0075)
        {
            LineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round
        };

        for (int i = -1; i <= 1; i++)
        {
            var offset = i * radius * 0.015;
            var start = new Point(center.X + offset - radius * 0.01875, center.Y + offset + radius * 0.01875);
            var end = new Point(center.X + offset + radius * 0.01875, center.Y + offset - radius * 0.01875);
            context.DrawLine(pen, start, end);
        }
    }

    private void DrawTicks(DrawingContext context, double cx, double cy, double radius)
    {
        var tickColor = (_tickBrush as SolidColorBrush)?.Color ?? Colors.White;
        var minuteColor = Color.FromArgb((byte)(tickColor.A * 0.6), tickColor.R, tickColor.G, tickColor.B);
        var minuteTickPen = new Pen(new SolidColorBrush(minuteColor), radius * 0.003)
        {
            LineCap = PenLineCap.Round
        };
        var hourTickPen = new Pen(new SolidColorBrush(tickColor), radius * 0.006)
        {
            LineCap = PenLineCap.Round
        };

        for (int m = 0; m < 60; m++)
        {
            var isHour = m % 5 == 0;
            var innerR = radius * (isHour ? 0.86 : 0.91);
            var outerR = radius * 0.95;
            var angle = m * Math.PI / 30.0;
            var x1 = cx + innerR * Math.Sin(angle);
            var y1 = cy - innerR * Math.Cos(angle);
            var x2 = cx + outerR * Math.Sin(angle);
            var y2 = cy - outerR * Math.Cos(angle);
            context.DrawLine(isHour ? hourTickPen : minuteTickPen, new Point(x1, y1), new Point(x2, y2));
        }
    }

    private void DrawNumbers(DrawingContext context, double cx, double cy, double radius, int currentHour)
    {
        var numberRadius = radius * 0.73;
        var fontSize = radius * 0.15 * _numberFontScale;
        var typeface = new Typeface(_numberFont, FontStyle.Normal, FontWeight.Bold);
        var isAfternoon = currentHour > 12;

        for (int i = 1; i <= 12; i++)
        {
            var angle = i * Math.PI / 6.0;
            var x = cx + numberRadius * Math.Sin(angle);
            var y = cy - numberRadius * Math.Cos(angle);
            var value = isAfternoon ? i + 12 : i;
            var text = value.ToString(CultureInfo.InvariantCulture);
            var ft = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, fontSize, _numberBrush ?? Brushes.White);
            var origin = new Point(x - ft.Width / 2.0, y - ft.Height / 2.0);
            context.DrawText(ft, origin);
        }
    }

    private void DrawTime(DrawingContext context, double cx, double cy, double radius, DateTime now, string city)
    {
        var cityFontSize = radius * 0.06 * _timeFontScale;
        var cityTypeface = new Typeface(_timeFont, FontStyle.Normal, FontWeight.Bold);
        var cityFt = new FormattedText(city, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, cityTypeface, cityFontSize, _timeBrush ?? Brushes.White);

        var timeFontSize = radius * 0.10 * _timeFontScale;
        var timeTypeface = new Typeface(_timeFont, FontStyle.Normal, FontWeight.Bold);
        var text = now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        var timeFt = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, timeTypeface, timeFontSize, _timeBrush ?? Brushes.White);

        var spacing = radius * 0.005;
        var totalHeight = cityFt.Height + spacing + timeFt.Height;
        var maxWidth = Math.Max(cityFt.Width, timeFt.Width);

        var paddingX = radius * 0.04;
        var paddingY = radius * 0.015;
        var boxWidth = maxWidth + 2 * paddingX;
        var boxHeight = totalHeight + 2 * paddingY;
        var defaultX = cx - boxWidth / 2.0;
        var defaultY = cy - radius * 0.28 - boxHeight / 2.0;
        var boxX = defaultX + _timeBoxXOffset;
        var boxY = defaultY + _timeBoxYOffset;

        DrawRoundedRect(context, boxX, boxY, boxWidth, boxHeight, radius * 0.025, _timeBoxBgBrush, _timeBoxBorderBrush, radius * 0.003);

        var textX = boxX + paddingX;
        var textTop = boxY + paddingY;
        context.DrawText(cityFt, new Point(textX + (maxWidth - cityFt.Width) / 2.0, textTop));
        context.DrawText(timeFt, new Point(textX + (maxWidth - timeFt.Width) / 2.0, textTop + cityFt.Height + spacing));
    }

    private void DrawDate(DrawingContext context, double cx, double cy, double radius, DateTime now)
    {
        var daySize = radius * 0.10 * _dateFontScale;
        var dateSize = radius * 0.08 * _dateFontScale;
        var dayTypeface = new Typeface(_dateFont, FontStyle.Normal, FontWeight.Bold);
        var dateTypeface = new Typeface(_dateFont, FontStyle.Normal, FontWeight.Regular);

        var de = new CultureInfo("de-DE");
        var dayText = now.ToString("dddd", de);
        var dateText = now.ToString("dd.MM.yyyy", de);

        var dayFt = new FormattedText(dayText, de, FlowDirection.LeftToRight, dayTypeface, daySize, _dateBrush ?? Brushes.White);
        var dateFt = new FormattedText(dateText, de, FlowDirection.LeftToRight, dateTypeface, dateSize, _dateBrush ?? Brushes.White);

        var spacing = radius * 0.01;
        var totalHeight = dayFt.Height + spacing + dateFt.Height;
        var maxWidth = Math.Max(dayFt.Width, dateFt.Width);

        var paddingX = radius * 0.04;
        var paddingY = radius * 0.015;
        var boxWidth = maxWidth + 2 * paddingX;
        var boxHeight = totalHeight + 2 * paddingY;
        var defaultX = cx - boxWidth / 2.0;
        var defaultY = cy + radius * 0.40 - boxHeight / 2.0;
        var boxX = defaultX + _dateBoxXOffset;
        var boxY = defaultY + _dateBoxYOffset;

        DrawRoundedRect(context, boxX, boxY, boxWidth, boxHeight, radius * 0.025, _dateBoxBgBrush, _dateBoxBorderBrush, radius * 0.003);

        var textX = boxX + paddingX;
        var textTop = boxY + paddingY;
        context.DrawText(dayFt, new Point(textX + (maxWidth - dayFt.Width) / 2.0, textTop));
        context.DrawText(dateFt, new Point(textX + (maxWidth - dateFt.Width) / 2.0, textTop + dayFt.Height + spacing));
    }

    private void DrawCenterDot(DrawingContext context, double cx, double cy, double hourWidth)
    {
        var r = hourWidth * 0.55;
        var center = new Point(cx, cy);
        context.DrawEllipse(_hourHandBrush ?? Brushes.White, null, center, r, r);

        if (_centerDotBorderBrush is not null)
        {
            var pen = new Pen(_centerDotBorderBrush, r * 0.15) { LineCap = PenLineCap.Round };
            context.DrawEllipse(null, pen, center, r, r);
        }
    }

    private static void DrawRoundedRect(DrawingContext context, double x, double y, double width, double height, double radius, IBrush? fill, IBrush? stroke, double strokeThickness)
    {
        var geometry = CreateRoundedRectGeometry(new Rect(x, y, width, height), radius);

        if (fill is not null)
        {
            context.DrawGeometry(fill, null, geometry);
        }

        if (stroke is not null && strokeThickness > 0)
        {
            context.DrawGeometry(null, new Pen(stroke, strokeThickness), geometry);
        }
    }

    private static Geometry CreateRoundedRectGeometry(Rect rect, double r)
    {
        var geometry = new PathGeometry
        {
            Figures = new PathFigures()
        };
        var figure = new PathFigure
        {
            StartPoint = new Point(rect.X + r, rect.Y),
            IsClosed = true,
            Segments = new PathSegments()
        };

        figure.Segments.Add(new LineSegment { Point = new Point(rect.X + rect.Width - r, rect.Y) });
        figure.Segments.Add(new ArcSegment { Point = new Point(rect.X + rect.Width, rect.Y + r), Size = new Size(r, r), SweepDirection = SweepDirection.Clockwise, IsLargeArc = false });
        figure.Segments.Add(new LineSegment { Point = new Point(rect.X + rect.Width, rect.Y + rect.Height - r) });
        figure.Segments.Add(new ArcSegment { Point = new Point(rect.X + rect.Width - r, rect.Y + rect.Height), Size = new Size(r, r), SweepDirection = SweepDirection.Clockwise, IsLargeArc = false });
        figure.Segments.Add(new LineSegment { Point = new Point(rect.X + r, rect.Y + rect.Height) });
        figure.Segments.Add(new ArcSegment { Point = new Point(rect.X, rect.Y + rect.Height - r), Size = new Size(r, r), SweepDirection = SweepDirection.Clockwise, IsLargeArc = false });
        figure.Segments.Add(new LineSegment { Point = new Point(rect.X, rect.Y + r) });
        figure.Segments.Add(new ArcSegment { Point = new Point(rect.X + r, rect.Y), Size = new Size(r, r), SweepDirection = SweepDirection.Clockwise, IsLargeArc = false });

        geometry.Figures.Add(figure);
        return geometry;
    }

    private void DrawHand(DrawingContext context, double cx, double cy, double angle, double length, double width, IBrush brush)
    {
        var tipStart = length * TaperRatio;
        var halfWidth = width / 2.0;

        var localPoints = new[]
        {
            new Point(-halfWidth, 0),
            new Point(-halfWidth, -tipStart),
            new Point(0, -length),
            new Point(halfWidth, -tipStart),
            new Point(halfWidth, 0)
        };

        var geo = new StreamGeometry();
        using (var gc = geo.Open())
        {
            gc.BeginFigure(TransformPoint(localPoints[0], cx, cy, angle), true);

            for (int i = 1; i < localPoints.Length; i++)
            {
                gc.LineTo(TransformPoint(localPoints[i], cx, cy, angle));
            }

            gc.EndFigure(true);
        }

        context.DrawGeometry(brush, null, geo);
    }

    private Point TransformPoint(Point p, double cx, double cy, double angle)
    {
        var cos = Math.Cos(angle);
        var sin = Math.Sin(angle);
        var x = cx + p.X * cos - p.Y * sin;
        var y = cy + p.X * sin + p.Y * cos;
        return new Point(x, y);
    }

    private static double Distance(Point a, Point b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private DateTime GetClockTime()
    {
        var now = DateTime.Now;
        return TimeZoneHelper.ConvertToTimeZone(now, _settings.TimeZoneId);
    }

    private string GetSelectedCity()
    {
        if (_timeZones.Count == 0)
        {
            _timeZones = TimeZoneHelper.GetTimeZones();
        }

        if (string.IsNullOrWhiteSpace(_settings.TimeZoneId))
        {
            return "Berlin";
        }

        var item = TimeZoneHelper.FindItem(_settings.TimeZoneId, _timeZones);
        if (item is not null)
        {
            return item.City;
        }

        var tz = TimeZoneHelper.FindTimeZone(_settings.TimeZoneId);
        return tz is not null ? TimeZoneHelper.GetCity(tz) : "Berlin";
    }
}
