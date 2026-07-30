using System;
using System.Collections.Generic;
using System.Globalization;
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
    private bool _secondHandVisible;
    private FontFamily _numberFont = FontFamily.Default;

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

    public ClockControl()
    {
        ClipToBounds = false;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _window = TopLevel.GetTopLevel(this) as Window;

        _settings = SettingsService.Load();
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

        var zeigerItem = new MenuItem { Header = "Zeiger" };
        zeigerItem.Items.Add(CreateMenuItem("Sekunden-Zeiger rot", () => SetSecondHand("Red")));
        zeigerItem.Items.Add(CreateMenuItem("Sekunden-Zeiger weiß", () => SetSecondHand("White")));
        zeigerItem.Items.Add(CreateMenuItem("Sekundenzeiger aus", () => SetSecondHand("Hidden")));
        menu.Items.Add(zeigerItem);

        menu.Items.Add(CreateMenuItem("Wecker", OpenAlarmWindow));

        var themeItem = new MenuItem { Header = "Theme" };
        var themesSub = new MenuItem { Header = "Laden" };
        themeItem.Items.Add(themesSub);
        themeItem.Items.Add(CreateMenuItem("Bearbeiten...", OpenThemeWindow));
        menu.Items.Add(themeItem);

        menu.Items.Add(CreateMenuItem("Beenden", ExitApplication));

        menu.Opening += (_, _) => RebuildThemeMenu(themesSub);

        return menu;
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
    }

    private ClockTheme CreateDefaultTheme()
    {
        return new ClockTheme();
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

        try
        {
            _numberFont = string.IsNullOrWhiteSpace(theme.FontName) ? FontFamily.Default : new FontFamily(theme.FontName);
        }
        catch
        {
            _numberFont = FontFamily.Default;
        }
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
        var now = DateTime.Now;

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

            if (!IsDayEnabled(alarm, now.DayOfWeek))
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
        var owner = _alarmWindow ?? _window;
        if (owner is null)
        {
            return;
        }

        var alert = new AlarmAlertWindow(alarm.Description);
        alert.Show(owner);
        alert.Activate();
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

        var now = DateTime.Now;
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

        context.DrawEllipse(_hourHandBrush ?? Brushes.White, null, new Point(cx, cy), hourWidth * 0.55, hourWidth * 0.55);

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
        var fontSize = radius * 0.15;
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
}
