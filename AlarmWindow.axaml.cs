using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
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

        Closing += (_, _) => SettingsService.Save(_settings);
    }

    private void AddButton_Click(object? sender, RoutedEventArgs e)
    {
        var future = DateTime.Now.AddMinutes(1);
        _currentAlarm = new Alarm
        {
            Hour = future.Hour,
            Minute = future.Minute,
            Description = "Wecker",
            Enabled = false
        };
        _isNew = true;

        AlarmList.SelectedItem = null;
        LoadControls(_currentAlarm);
        EnabledCheck.IsChecked = true;
        EditPanel.IsVisible = true;
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
