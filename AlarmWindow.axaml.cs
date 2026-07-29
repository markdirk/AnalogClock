using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;

namespace AnalogClock;

public partial class AlarmWindow : Window
{
    private ClockSettings _settings = new();
    private bool _updating;
    private CheckBox[] _dayChecks = Array.Empty<CheckBox>();

    public AlarmWindow()
    {
        InitializeComponent();
    }

    public AlarmWindow(ClockSettings settings) : this()
    {
        _settings = settings;
        SetupControls();
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

        HourUpDown.ValueChanged += EditControl_Changed;
        MinuteUpDown.ValueChanged += EditControl_Changed;
        DescriptionBox.TextChanged += EditControl_Changed;
        EnabledCheck.IsCheckedChanged += EditControl_Changed;
        foreach (var check in _dayChecks)
        {
            check.IsCheckedChanged += EditControl_Changed;
        }

        Closing += (_, _) => SettingsService.Save(_settings);
    }

    private void AddButton_Click(object? sender, RoutedEventArgs e)
    {
        var now = DateTime.Now;
        var alarm = new Alarm
        {
            Hour = now.Hour,
            Minute = now.Minute,
            Description = "Wecker"
        };
        _settings.Alarms.Add(alarm);
        AlarmList.SelectedItem = alarm;
        SettingsService.Save(_settings);
    }

    private void DeleteButton_Click(object? sender, RoutedEventArgs e)
    {
        if (AlarmList.SelectedItem is Alarm alarm)
        {
            _settings.Alarms.Remove(alarm);
            EditPanel.IsVisible = false;
            SettingsService.Save(_settings);
        }
    }

    private void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        if (AlarmList.SelectedItem is Alarm alarm)
        {
            UpdateAlarmFromControls(alarm);
            SettingsService.Save(_settings);
        }
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        _updating = true;
        try
        {
            if (AlarmList.SelectedItem is Alarm alarm)
            {
                HourUpDown.Value = alarm.Hour;
                MinuteUpDown.Value = alarm.Minute;
                DescriptionBox.Text = alarm.Description;
                EnabledCheck.IsChecked = alarm.Enabled;
                _dayChecks[0].IsChecked = alarm.Monday;
                _dayChecks[1].IsChecked = alarm.Tuesday;
                _dayChecks[2].IsChecked = alarm.Wednesday;
                _dayChecks[3].IsChecked = alarm.Thursday;
                _dayChecks[4].IsChecked = alarm.Friday;
                _dayChecks[5].IsChecked = alarm.Saturday;
                _dayChecks[6].IsChecked = alarm.Sunday;
                EditPanel.IsVisible = true;
            }
            else
            {
                EditPanel.IsVisible = false;
            }
        }
        finally
        {
            _updating = false;
        }
    }

    private void EditControl_Changed(object? sender, EventArgs e)
    {
        if (_updating || AlarmList.SelectedItem is not Alarm alarm)
        {
            return;
        }

        UpdateAlarmFromControls(alarm);
        SettingsService.Save(_settings);
    }

    private void UpdateAlarmFromControls(Alarm alarm)
    {
        alarm.Hour = (int)(HourUpDown.Value ?? 0);
        alarm.Minute = (int)(MinuteUpDown.Value ?? 0);
        alarm.Description = DescriptionBox.Text ?? string.Empty;
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
