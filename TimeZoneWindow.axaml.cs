using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;

namespace AnalogClock;

public partial class TimeZoneWindow : Window
{
    private ClockSettings _settings = new();
    private List<TimeZoneItem> _zones = new();

    public TimeZoneWindow()
    {
        InitializeComponent();
    }

    public TimeZoneWindow(ClockSettings settings) : this()
    {
        _settings = settings;
        SetupControls();
    }

    private void SetupControls()
    {
        _zones = TimeZoneHelper.GetTimeZones();
        ZoneList.ItemsSource = _zones;
        ZoneList.DisplayMemberBinding = new Binding("Display");

        var current = _zones.FirstOrDefault(z => z.Id == _settings.TimeZoneId);
        ZoneList.SelectedItem = current ?? _zones.FirstOrDefault();

        ApplyButton.Click += ApplyButton_Click;
        CancelButton.Click += CancelButton_Click;
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
        var w = (int)(Width * scale);
        var h = (int)(Height * scale);
        const int margin = 10;

        var screens = owner.Screens;
        var screen = screens.ScreenFromWindow(owner) ?? screens.Primary;
        if (screen is null)
        {
            return;
        }

        var work = screen.WorkingArea;

        int xRight = ownerPos.X + ownerSize.Width + margin;
        int xLeft = ownerPos.X - w - margin;
        int y = ownerPos.Y;

        bool rightFits = xRight >= work.X && xRight + w <= work.X + work.Width &&
                         y >= work.Y && y + h <= work.Y + work.Height;
        bool leftFits = xLeft >= work.X && xLeft + w <= work.X + work.Width &&
                        y >= work.Y && y + h <= work.Y + work.Height;

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
            if (x + w > work.X + work.Width)
            {
                x = work.X + work.Width - w;
            }

            if (x < work.X)
            {
                x = work.X;
            }

            if (y + h > work.Y + work.Height)
            {
                y = work.Y + work.Height - h;
            }

            if (y < work.Y)
            {
                y = work.Y;
            }
        }

        Position = new PixelPoint(x, y);
    }

    private void ApplyButton_Click(object? sender, RoutedEventArgs e)
    {
        if (ZoneList.SelectedItem is TimeZoneItem item)
        {
            _settings.TimeZoneId = item.Id;
        }

        Close();
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
