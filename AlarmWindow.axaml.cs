using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace AnalogClock;

public partial class AlarmWindow : Window
{
    private ClockSettings _settings = new();
    private Alarm? _currentAlarm;
    private bool _isNew;
    private CheckBox[] _dayChecks = Array.Empty<CheckBox>();

    public AlarmWindow()
    {
        InitializeComponent();
    }

    public AlarmWindow(ClockSettings settings) : this()
    {
        _settings = settings;
        Width = 520;
        Height = 640;
        SetupControls();
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
        var alarmWidth = (int)(Width * scale);
        var alarmHeight = (int)(Height * scale);
        const int margin = 10;

        var screens = owner.Screens;
        var screen = screens.ScreenFromWindow(owner) ?? screens.Primary;
        if (screen is null)
        {
            return;
        }

        var work = screen.WorkingArea;

        int xRight = ownerPos.X + ownerSize.Width + margin;
        int xLeft = ownerPos.X - alarmWidth - margin;
        int y = ownerPos.Y;

        bool rightFits = xRight >= work.X && xRight + alarmWidth <= work.X + work.Width &&
                         y >= work.Y && y + alarmHeight <= work.Y + work.Height;
        bool leftFits = xLeft >= work.X && xLeft + alarmWidth <= work.X + work.Width &&
                        y >= work.Y && y + alarmHeight <= work.Y + work.Height;

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
            if (x + alarmWidth > work.X + work.Width)
            {
                x = work.X + work.Width - alarmWidth;
            }

            if (x < work.X)
            {
                x = work.X;
            }

            if (y + alarmHeight > work.Y + work.Height)
            {
                y = work.Y + work.Height - alarmHeight;
            }

            if (y < work.Y)
            {
                y = work.Y;
            }
        }

        Position = new PixelPoint(x, y);
    }

    private void SetupControls()
    {
        _dayChecks = new[] { MoCheck, DiCheck, MiCheck, DoCheck, FrCheck, SaCheck, SoCheck };

        AlarmList.ItemsSource = _settings.Alarms;
        AlarmList.DisplayMemberBinding = new Binding("DisplayText");
        AlarmList.SelectionChanged += OnSelectionChanged;

        AddButton.Click += AddButton_Click;
        DeleteButton.Click += DeleteButton_Click;
        SaveButton.Click += SaveButton_Click;

        HourTensSpinner.ValueChanged += (_, _) => OnDigitChanged();
        HourOnesSpinner.ValueChanged += (_, _) => OnDigitChanged();
        MinuteTensSpinner.ValueChanged += (_, _) => OnDigitChanged();
        MinuteOnesSpinner.ValueChanged += (_, _) => OnDigitChanged();

        Activated += OnActivated;
        Closing += (_, _) => SettingsService.Save(_settings);
    }

    private void OnActivated(object? sender, EventArgs e)
    {
        if (EditPanel.IsVisible)
        {
            DescriptionBox?.Focus();
            DescriptionBox?.SelectAll();
        }
        else
        {
            this.Focus();
        }
    }

    private void AddButton_Click(object? sender, RoutedEventArgs e)
    {
        var future = DateTime.Now.AddMinutes(1);

        AlarmList.SelectedItem = null;

        _currentAlarm = new Alarm
        {
            Hour = future.Hour,
            Minute = future.Minute,
            Description = "Wecker",
            Enabled = false,
            Command = string.Empty,
            Arguments = string.Empty
        };
        _isNew = true;

        LoadControls(_currentAlarm);
        EnabledCheck.IsChecked = true;
        EditPanel.IsVisible = true;
        DescriptionBox?.Focus();
        DescriptionBox?.SelectAll();
    }

    private void DeleteButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_currentAlarm is null)
        {
            return;
        }

        if (!_isNew)
        {
            _settings.Alarms.Remove(_currentAlarm);
            AlarmList.SelectedItem = null;
        }

        _currentAlarm = null;
        _isNew = false;
        EditPanel.IsVisible = false;
        SettingsService.Save(_settings);
    }

    private void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_currentAlarm is null)
        {
            return;
        }

        UpdateAlarmFromControls(_currentAlarm);
        _currentAlarm.RefreshDisplay();
        if (_isNew)
        {
            _settings.Alarms.Add(_currentAlarm);
            _isNew = false;
            AlarmList.SelectedItem = _currentAlarm;
        }

        SettingsService.Save(_settings);
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (AlarmList.SelectedItem is Alarm alarm)
        {
            _currentAlarm = alarm;
            _isNew = false;
            LoadControls(alarm);
            EditPanel.IsVisible = true;
            DescriptionBox?.Focus();
            DescriptionBox?.SelectAll();
        }
        else
        {
            _currentAlarm = null;
            _isNew = false;
            EditPanel.IsVisible = false;
        }
    }

    private void LoadControls(Alarm alarm)
    {
        HourTensSpinner.Value = alarm.Hour / 10;
        HourOnesSpinner.Value = alarm.Hour % 10;
        MinuteTensSpinner.Value = alarm.Minute / 10;
        MinuteOnesSpinner.Value = alarm.Minute % 10;
        UpdateLimits();
        DescriptionBox.Text = alarm.Description;
        CommandBox.Text = alarm.Command;
        ArgumentsBox.Text = alarm.Arguments;
        EnabledCheck.IsChecked = alarm.Enabled;
        _dayChecks[0].IsChecked = alarm.Monday;
        _dayChecks[1].IsChecked = alarm.Tuesday;
        _dayChecks[2].IsChecked = alarm.Wednesday;
        _dayChecks[3].IsChecked = alarm.Thursday;
        _dayChecks[4].IsChecked = alarm.Friday;
        _dayChecks[5].IsChecked = alarm.Saturday;
        _dayChecks[6].IsChecked = alarm.Sunday;
    }

    private void OnDigitChanged()
    {
        UpdateLimits();
    }

    private void UpdateLimits()
    {
        HourTensSpinner.Maximum = 2;
        HourOnesSpinner.Maximum = HourTensSpinner.Value == 2 ? 3 : 9;
        MinuteTensSpinner.Maximum = 5;
        MinuteOnesSpinner.Maximum = 9;
    }

    private void UpdateAlarmFromControls(Alarm alarm)
    {
        alarm.Hour = HourTensSpinner.Value * 10 + HourOnesSpinner.Value;
        alarm.Minute = MinuteTensSpinner.Value * 10 + MinuteOnesSpinner.Value;
        alarm.Description = DescriptionBox.Text ?? string.Empty;
        alarm.Command = CommandBox.Text ?? string.Empty;
        alarm.Arguments = ArgumentsBox.Text ?? string.Empty;
        alarm.Enabled = EnabledCheck.IsChecked ?? false;
        alarm.Monday = _dayChecks[0].IsChecked ?? false;
        alarm.Tuesday = _dayChecks[1].IsChecked ?? false;
        alarm.Wednesday = _dayChecks[2].IsChecked ?? false;
        alarm.Thursday = _dayChecks[3].IsChecked ?? false;
        alarm.Friday = _dayChecks[4].IsChecked ?? false;
        alarm.Saturday = _dayChecks[5].IsChecked ?? false;
        alarm.Sunday = _dayChecks[6].IsChecked ?? false;
    }
}
