using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace AnalogClock;

public partial class ThemeWindow : Window
{
    private ClockSettings _settings = new();
    private Action _onApply = () => { };

    public ThemeWindow()
    {
        InitializeComponent();
    }

    public ThemeWindow(ClockSettings settings, Action onApply) : this()
    {
        _settings = settings;
        _onApply = onApply;
        Setup();
    }

    private void Setup()
    {
        var fontNames = FontManager.Current.SystemFonts.Select(f => f.Name).OrderBy(n => n).ToList();
        NumberFontCombo.ItemsSource = fontNames;
        DateFontCombo.ItemsSource = fontNames;
        TimeFontCombo.ItemsSource = fontNames;

        ThemeCombo.ItemsSource = _settings.Themes;
        ThemeCombo.DisplayMemberBinding = new Binding("Name");

        if (_settings.CurrentTheme is not null)
        {
            ThemeCombo.SelectedItem = _settings.Themes.Find(t => t.Name == _settings.CurrentTheme.Name);
        }

        LoadTheme(_settings.CurrentTheme ?? new ClockTheme { Name = "Standard" });

        NumberFontCombo.SelectionChanged += (_, _) => PreviewTheme();
        DateFontCombo.SelectionChanged += (_, _) => PreviewTheme();
        TimeFontCombo.SelectionChanged += (_, _) => PreviewTheme();
        NumberFontScale.ValueChanged += (_, _) => PreviewTheme();
        DateFontScale.ValueChanged += (_, _) => PreviewTheme();
        TimeFontScale.ValueChanged += (_, _) => PreviewTheme();
        DateColorPicker.ColorChanged += (_, _) => PreviewTheme();
        TimeColorPicker.ColorChanged += (_, _) => PreviewTheme();

        ThemeCombo.SelectionChanged += (_, _) =>
        {
            if (ThemeCombo.SelectedItem is ClockTheme theme)
            {
                LoadTheme(theme);
            }
        };

        SaveButton.Click += SaveButton_Click;
        DeleteButton.Click += DeleteButton_Click;
        CloseButton.Click += (_, _) => Close();
    }

    private void LoadTheme(ClockTheme theme)
    {
        NameBox.Text = theme.Name;
        FaceColorPicker.Color = ParseColor(theme.FaceColor) ?? Colors.White;
        BorderColorPicker.Color = ParseColor(theme.BorderColor) ?? Colors.Black;
        NumberColorPicker.Color = ParseColor(theme.NumberColor) ?? Colors.White;
        HourHandColorPicker.Color = ParseColor(theme.HourHandColor) ?? Colors.White;
        MinuteHandColorPicker.Color = ParseColor(theme.MinuteHandColor) ?? Colors.White;
        SecondHandColorPicker.Color = ParseColor(theme.SecondHandColor) ?? Colors.White;
        TickColorPicker.Color = ParseColor(theme.TickColor) ?? Colors.White;
        GripColorPicker.Color = ParseColor(theme.GripColor) ?? Color.Parse("#33FFFFFF");
        DateColorPicker.Color = ParseColor(theme.DateColor) ?? Colors.White;
        TimeColorPicker.Color = ParseColor(theme.TimeColor) ?? Colors.White;
        SecondHandVisibleCheck.IsChecked = theme.SecondHandVisible;
        NumberFontCombo.SelectedItem = string.IsNullOrWhiteSpace(theme.FontName)
            ? FontManager.Current.DefaultFontFamily.Name
            : theme.FontName;
        DateFontCombo.SelectedItem = string.IsNullOrWhiteSpace(theme.DateFontName)
            ? FontManager.Current.DefaultFontFamily.Name
            : theme.DateFontName;
        TimeFontCombo.SelectedItem = string.IsNullOrWhiteSpace(theme.TimeFontName)
            ? FontManager.Current.DefaultFontFamily.Name
            : theme.TimeFontName;
        NumberFontScale.Value = (decimal)theme.NumberFontScale;
        DateFontScale.Value = (decimal)theme.DateFontScale;
        TimeFontScale.Value = (decimal)theme.TimeFontScale;
    }

    private ClockTheme ThemeFromControls()
    {
        var name = NameBox.Text?.Trim() ?? "Theme";
        return new ClockTheme
        {
            Name = name,
            FaceColor = ColorToString(FaceColorPicker.Color),
            BorderColor = ColorToString(BorderColorPicker.Color),
            NumberColor = ColorToString(NumberColorPicker.Color),
            HourHandColor = ColorToString(HourHandColorPicker.Color),
            MinuteHandColor = ColorToString(MinuteHandColorPicker.Color),
            SecondHandColor = ColorToString(SecondHandColorPicker.Color),
            TickColor = ColorToString(TickColorPicker.Color),
            GripColor = ColorToString(GripColorPicker.Color),
            DateColor = ColorToString(DateColorPicker.Color),
            TimeColor = ColorToString(TimeColorPicker.Color),
            SecondHandVisible = SecondHandVisibleCheck.IsChecked ?? false,
            FontName = NumberFontCombo.SelectedItem?.ToString() ?? string.Empty,
            NumberFontScale = (double)(NumberFontScale.Value ?? 1.0m),
            DateFontName = DateFontCombo.SelectedItem?.ToString() ?? string.Empty,
            DateFontScale = (double)(DateFontScale.Value ?? 1.0m),
            TimeFontName = TimeFontCombo.SelectedItem?.ToString() ?? string.Empty,
            TimeFontScale = (double)(TimeFontScale.Value ?? 1.0m)
        };
    }

    private void PreviewTheme()
    {
        _settings.CurrentTheme = ThemeFromControls();
        _onApply();
    }

    private static Color? ParseColor(string color)
    {
        return Color.TryParse(color, out var c) ? c : null;
    }

    private static string ColorToString(Color color)
    {
        return color.ToString();
    }

    private void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        var theme = ThemeFromControls();

        var existing = _settings.Themes.Find(t => t.Name == theme.Name);
        if (existing is not null)
        {
            _settings.Themes.Remove(existing);
        }

        _settings.Themes.Add(theme);
        _settings.CurrentTheme = theme;
        _onApply();
        Close();
    }

    private void DeleteButton_Click(object? sender, RoutedEventArgs e)
    {
        var name = NameBox.Text?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        var existing = _settings.Themes.Find(t => t.Name == name);
        if (existing is not null)
        {
            _settings.Themes.Remove(existing);
        }

        if (_settings.CurrentTheme?.Name == name)
        {
            _settings.CurrentTheme = _settings.Themes.Count > 0 ? _settings.Themes[0] : new ClockTheme { Name = "Standard" };
            _onApply();
        }

        Close();
    }

    public void PositionNextTo(Window owner)
    {
        if (owner is null || !owner.IsVisible)
        {
            return;
        }

        var scale = owner.RenderScaling;
        var ownerPos = owner.Position;
        var ownerSize = new PixelSize((int)(owner.Width * scale), (int)(owner.Height * scale));
        var themeWidth = (int)(Width * scale);
        var themeHeight = (int)(Height * scale);
        const int margin = 10;

        var screens = owner.Screens;
        var screen = screens.ScreenFromWindow(owner) ?? screens.Primary;
        if (screen is null)
        {
            return;
        }

        var work = screen.WorkingArea;

        int xRight = ownerPos.X + ownerSize.Width + margin;
        int xLeft = ownerPos.X - themeWidth - margin;
        int y = ownerPos.Y;

        bool rightFits = xRight >= work.X && xRight + themeWidth <= work.X + work.Width &&
                         y >= work.Y && y + themeHeight <= work.Y + work.Height;
        bool leftFits = xLeft >= work.X && xLeft + themeWidth <= work.X + work.Width &&
                        y >= work.Y && y + themeHeight <= work.Y + work.Height;

        int x;
        if (rightFits)
        {
            x = xRight;
        }
        else if (leftFits)
        {
            x = xLeft;
        }
        else
        {
            x = xRight;
            if (x + themeWidth > work.X + work.Width)
            {
                x = work.X + work.Width - themeWidth;
            }

            if (x < work.X)
            {
                x = work.X;
            }

            if (y + themeHeight > work.Y + work.Height)
            {
                y = work.Y + work.Height - themeHeight;
            }

            if (y < work.Y)
            {
                y = work.Y;
            }
        }

        Position = new PixelPoint(x, y);
    }
}
