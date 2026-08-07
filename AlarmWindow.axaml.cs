using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;

namespace AnalogClock;

public partial class AlarmWindow : Window
{
    private ClockSettings _settings = new();
    private Alarm? _currentAlarm;
    private bool _isNew;
    private RecurrenceRule? _selectedRule;

    private readonly List<CheckBox> _weeklyChecks = new();
    private readonly List<CheckBox> _weekdayChecks = new();
    private readonly List<CheckBox> _intervalWeekdayChecks = new();
    private DatePicker? _startDatePicker;
    private DatePicker? _specificDatePicker;
    private ListBox? _specificDatesList;
    private ComboBox? _weekdayCombo;
    private ComboBox? _ordinalCombo;
    private CheckBox? _beforeLastCheck;
    private TextBox? _monthDaysBox;
    private TextBox? _intervalValueBox;
    private ComboBox? _intervalUnitCombo;

    private class ModeOption
    {
        public string Display { get; set; } = string.Empty;
        public AlarmMode Mode { get; set; }
        public override string ToString() => Display;
    }

    private static readonly ModeOption[] ModeOptions = new[]
    {
        new ModeOption { Display = "Standard (Ton + blinkend)", Mode = AlarmMode.Default },
        new ModeOption { Display = "Still (nur blinkendes Panel)", Mode = AlarmMode.Visual },
        new ModeOption { Display = "Akustisch (Ton, kein Blinken)", Mode = AlarmMode.AcousticNoBlink },
        new ModeOption { Display = "Hintergrund (kein Panel)", Mode = AlarmMode.Background }
    };

    private class TypeOption
    {
        public string Display { get; set; } = string.Empty;
        public RecurrenceType Type { get; set; }
        public override string ToString() => Display;
    }

    private static readonly TypeOption[] TypeOptions = new[]
    {
        new TypeOption { Display = "Wochentage", Type = RecurrenceType.Weekly },
        new TypeOption { Display = "Konkrete Termine", Type = RecurrenceType.SpecificDates },
        new TypeOption { Display = "Tage im Monat", Type = RecurrenceType.MonthlyDay },
        new TypeOption { Display = "Wochentag im Monat", Type = RecurrenceType.MonthlyWeekday },
        new TypeOption { Display = "Intervall", Type = RecurrenceType.Interval }
    };

    private class WeekdayOption
    {
        public string Display { get; set; } = string.Empty;
        public DayOfWeek Day { get; set; }
        public override string ToString() => Display;
    }

    private static readonly WeekdayOption[] WeekdayOptions = new[]
    {
        new WeekdayOption { Display = "Montag", Day = DayOfWeek.Monday },
        new WeekdayOption { Display = "Dienstag", Day = DayOfWeek.Tuesday },
        new WeekdayOption { Display = "Mittwoch", Day = DayOfWeek.Wednesday },
        new WeekdayOption { Display = "Donnerstag", Day = DayOfWeek.Thursday },
        new WeekdayOption { Display = "Freitag", Day = DayOfWeek.Friday },
        new WeekdayOption { Display = "Samstag", Day = DayOfWeek.Saturday },
        new WeekdayOption { Display = "Sonntag", Day = DayOfWeek.Sunday }
    };

    private class OrdinalOption
    {
        public string Display { get; set; } = string.Empty;
        public int Ordinal { get; set; }
        public override string ToString() => Display;
    }

    private static readonly OrdinalOption[] OrdinalOptions = new[]
    {
        new OrdinalOption { Display = "1.", Ordinal = 1 },
        new OrdinalOption { Display = "2.", Ordinal = 2 },
        new OrdinalOption { Display = "3.", Ordinal = 3 },
        new OrdinalOption { Display = "4.", Ordinal = 4 },
        new OrdinalOption { Display = "5.", Ordinal = 5 },
        new OrdinalOption { Display = "letzter", Ordinal = -1 },
        new OrdinalOption { Display = "vorletzter", Ordinal = -2 },
        new OrdinalOption { Display = "drittletzter", Ordinal = -3 },
        new OrdinalOption { Display = "viertletzter", Ordinal = -4 }
    };

    private class UnitOption
    {
        public string Display { get; set; } = string.Empty;
        public RecurrenceUnit Unit { get; set; }
        public override string ToString() => Display;
    }

    private static readonly UnitOption[] UnitOptions = new[]
    {
        new UnitOption { Display = "Tage", Unit = RecurrenceUnit.Days },
        new UnitOption { Display = "Wochen", Unit = RecurrenceUnit.Weeks },
        new UnitOption { Display = "Monate", Unit = RecurrenceUnit.Months },
        new UnitOption { Display = "Jahre", Unit = RecurrenceUnit.Years }
    };

    public AlarmWindow()
    {
        InitializeComponent();
    }

    public AlarmWindow(ClockSettings settings) : this()
    {
        _settings = settings;
        Width = 520;
        Height = 800;
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
        AlarmList.ItemsSource = _settings.Alarms;
        AlarmList.DisplayMemberBinding = new Binding("DisplayText");
        AlarmList.SelectionChanged += OnSelectionChanged;

        ModeCombo.ItemsSource = ModeOptions;

        RecurrenceTypeCombo.ItemsSource = TypeOptions;
        RecurrenceTypeCombo.DisplayMemberBinding = new Binding("Display");
        RecurrenceTypeCombo.SelectedIndex = 0;

        RecurrenceList.DisplayMemberBinding = new Binding("DisplayText");
        RecurrenceList.SelectionChanged += OnRecurrenceSelectionChanged;

        AddButton.Click += AddButton_Click;
        DeleteButton.Click += DeleteButton_Click;
        SaveButton.Click += SaveButton_Click;
        BrowseSoundButton.Click += BrowseSoundButton_Click;
        AddRuleButton.Click += AddRuleButton_Click;
        RemoveRuleButton.Click += RemoveRuleButton_Click;
        RecurrenceTypeCombo.SelectionChanged += OnRecurrenceTypeComboChanged;

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
        Console.WriteLine("AddButton_Click");
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
        EditPanel.InvalidateMeasure();
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
        ModeCombo.SelectedItem = ModeOptions.FirstOrDefault(m => m.Mode == alarm.Mode);
        SoundFileBox.Text = alarm.SoundFile;

        EnsureRecurrenceRules(alarm);
        foreach (var rule in alarm.RecurrenceRules)
        {
            rule.RefreshDisplay();
        }

        RecurrenceList.ItemsSource = alarm.RecurrenceRules;
        _selectedRule = alarm.RecurrenceRules.FirstOrDefault();
        RecurrenceList.SelectedItem = _selectedRule;
        BuildRuleEditor();
    }

    private static void EnsureRecurrenceRules(Alarm alarm)
    {
        if (alarm.RecurrenceRules.Count > 0)
        {
            return;
        }

        var days = new List<DayOfWeek>();
        if (alarm.Monday) days.Add(DayOfWeek.Monday);
        if (alarm.Tuesday) days.Add(DayOfWeek.Tuesday);
        if (alarm.Wednesday) days.Add(DayOfWeek.Wednesday);
        if (alarm.Thursday) days.Add(DayOfWeek.Thursday);
        if (alarm.Friday) days.Add(DayOfWeek.Friday);
        if (alarm.Saturday) days.Add(DayOfWeek.Saturday);
        if (alarm.Sunday) days.Add(DayOfWeek.Sunday);

        if (days.Count == 0)
        {
            days.Add(DateTime.Now.DayOfWeek);
        }

        alarm.RecurrenceRules.Add(new RecurrenceRule { Type = RecurrenceType.Weekly, DaysOfWeek = days });
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
        alarm.Mode = (ModeCombo.SelectedItem as ModeOption)?.Mode ?? AlarmMode.Default;
        alarm.SoundFile = SoundFileBox.Text ?? string.Empty;

        if (_selectedRule is not null)
        {
            SyncSelectedRule();
        }

        foreach (var rule in alarm.RecurrenceRules)
        {
            rule.RefreshDisplay();
        }
    }

    private void AddRuleButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_currentAlarm is null)
        {
            return;
        }

        var type = (RecurrenceTypeCombo.SelectedItem as TypeOption)?.Type ?? RecurrenceType.Weekly;
        var rule = new RecurrenceRule { Type = type };

        if (type == RecurrenceType.Weekly && rule.DaysOfWeek.Count == 0)
        {
            rule.DaysOfWeek.Add(DateTime.Now.DayOfWeek);
        }

        if (type == RecurrenceType.MonthlyWeekday)
        {
            rule.MonthWeekday = DateTime.Now.DayOfWeek;
            rule.MonthWeekdayOrdinal = 1;
        }

        if (type == RecurrenceType.Interval)
        {
            rule.IntervalValue = 14;
            rule.IntervalUnit = RecurrenceUnit.Days;
            rule.IntervalStart = DateTime.Today;
        }

        rule.RefreshDisplay();
        _currentAlarm.RecurrenceRules.Add(rule);
        _selectedRule = rule;
        RecurrenceList.ItemsSource = _currentAlarm.RecurrenceRules;
        RecurrenceList.SelectedItem = rule;
        BuildRuleEditor();
    }

    private void RemoveRuleButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_currentAlarm is null || _selectedRule is null)
        {
            return;
        }

        _currentAlarm.RecurrenceRules.Remove(_selectedRule);
        _selectedRule = _currentAlarm.RecurrenceRules.FirstOrDefault();
        RecurrenceList.ItemsSource = _currentAlarm.RecurrenceRules;
        RecurrenceList.SelectedItem = _selectedRule;
        BuildRuleEditor();
    }

    private void OnRecurrenceSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var newRule = RecurrenceList.SelectedItem as RecurrenceRule;
        if (newRule == _selectedRule)
        {
            return;
        }

        if (_selectedRule is not null)
        {
            SyncSelectedRule();
        }

        _selectedRule = newRule;
        BuildRuleEditor();
    }

    private void OnRecurrenceTypeComboChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_selectedRule is null)
        {
            return;
        }

        var newType = (RecurrenceTypeCombo.SelectedItem as TypeOption)?.Type ?? _selectedRule.Type;
        if (newType == _selectedRule.Type)
        {
            return;
        }

        _selectedRule.Type = newType;
        _selectedRule.DaysOfWeek.Clear();
        _selectedRule.Dates.Clear();
        _selectedRule.MonthDays.Clear();
        _selectedRule.MonthWeekday = null;
        _selectedRule.MonthWeekdayOrdinal = 1;
        _selectedRule.MonthWeekdayBeforeLastDay = false;
        _selectedRule.IntervalValue = 1;
        _selectedRule.IntervalUnit = RecurrenceUnit.Days;
        _selectedRule.IntervalStart = null;

        if (newType == RecurrenceType.Weekly)
        {
            _selectedRule.DaysOfWeek.Add(DateTime.Now.DayOfWeek);
        }
        else if (newType == RecurrenceType.MonthlyWeekday)
        {
            _selectedRule.MonthWeekday = DateTime.Now.DayOfWeek;
        }
        else if (newType == RecurrenceType.Interval)
        {
            _selectedRule.IntervalValue = 14;
            _selectedRule.IntervalUnit = RecurrenceUnit.Days;
            _selectedRule.IntervalStart = DateTime.Today;
        }

        _selectedRule.RefreshDisplay();
        BuildRuleEditor();
    }

    private void SyncSelectedRule()
    {
        if (_selectedRule is null)
        {
            return;
        }

        switch (_selectedRule.Type)
        {
            case RecurrenceType.Weekly:
                _selectedRule.DaysOfWeek.Clear();
                for (int i = 0; i < _weeklyChecks.Count; i++)
                {
                    if (_weeklyChecks[i].IsChecked == true)
                    {
                        _selectedRule.DaysOfWeek.Add((DayOfWeek)i);
                    }
                }
                break;

            case RecurrenceType.MonthlyDay:
                _selectedRule.MonthDays.Clear();
                if (_monthDaysBox?.Text is { } text)
                {
                    foreach (var part in text.Split(',', ';'))
                    {
                        var trimmed = part.Trim();
                        if (int.TryParse(trimmed, out var day))
                        {
                            _selectedRule.MonthDays.Add(day);
                        }
                    }
                }
                break;

            case RecurrenceType.MonthlyWeekday:
                _selectedRule.MonthWeekday = (_weekdayCombo?.SelectedItem as WeekdayOption)?.Day;
                _selectedRule.MonthWeekdayOrdinal = (_ordinalCombo?.SelectedItem as OrdinalOption)?.Ordinal ?? 1;
                _selectedRule.MonthWeekdayBeforeLastDay = _beforeLastCheck?.IsChecked ?? false;
                break;

            case RecurrenceType.Interval:
                if (_intervalValueBox?.Text is { } valText && int.TryParse(valText, out var interval) && interval > 0)
                {
                    _selectedRule.IntervalValue = interval;
                }
                _selectedRule.IntervalUnit = (_intervalUnitCombo?.SelectedItem as UnitOption)?.Unit ?? RecurrenceUnit.Days;
                _selectedRule.IntervalStart = _startDatePicker?.SelectedDate?.DateTime;
                _selectedRule.DaysOfWeek.Clear();
                if (_selectedRule.IntervalUnit == RecurrenceUnit.Weeks)
                {
                    for (int i = 0; i < _intervalWeekdayChecks.Count; i++)
                    {
                        if (_intervalWeekdayChecks[i].IsChecked == true)
                        {
                            _selectedRule.DaysOfWeek.Add((DayOfWeek)i);
                        }
                    }
                }
                break;

            case RecurrenceType.SpecificDates:
                break;
        }

        _selectedRule.RefreshDisplay();
    }

    private void BuildRuleEditor()
    {
        RuleEditorPanel.Children.Clear();
        _weeklyChecks.Clear();
        _weekdayChecks.Clear();
        _intervalWeekdayChecks.Clear();
        _monthDaysBox = null;
        _weekdayCombo = null;
        _ordinalCombo = null;
        _beforeLastCheck = null;
        _intervalValueBox = null;
        _intervalUnitCombo = null;
        _startDatePicker = null;
        _specificDatePicker = null;
        _specificDatesList = null;

        if (_selectedRule is null)
        {
            return;
        }

        RecurrenceTypeCombo.SelectedItem = TypeOptions.FirstOrDefault(t => t.Type == _selectedRule.Type);

        switch (_selectedRule.Type)
        {
            case RecurrenceType.Weekly:
                BuildWeeklyEditor();
                break;
            case RecurrenceType.SpecificDates:
                BuildSpecificDatesEditor();
                break;
            case RecurrenceType.MonthlyDay:
                BuildMonthlyDayEditor();
                break;
            case RecurrenceType.MonthlyWeekday:
                BuildMonthlyWeekdayEditor();
                break;
            case RecurrenceType.Interval:
                BuildIntervalEditor();
                break;
        }
    }

    private void BuildWeeklyEditor()
    {
        if (_selectedRule is null)
        {
            return;
        }

        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        var days = new[] { "Mo", "Di", "Mi", "Do", "Fr", "Sa", "So" };
        for (int i = 0; i < days.Length; i++)
        {
            var day = (DayOfWeek)i;
            var check = new CheckBox
            {
                Content = days[i],
                Foreground = Brushes.White,
                Padding = new Thickness(2, 0, 0, 0)
            };
            _weeklyChecks.Add(check);
            panel.Children.Add(check);
            check.IsChecked = _selectedRule.DaysOfWeek.Contains(day);
            check.IsCheckedChanged += (_, _) =>
            {
                SyncSelectedRule();
                _selectedRule?.RefreshDisplay();
                if (_currentAlarm is not null)
                {
                    RecurrenceList.ItemsSource = _currentAlarm.RecurrenceRules;
                }
            };
        }

        RuleEditorPanel.Children.Add(panel);
    }

    private void BuildSpecificDatesEditor()
    {
        if (_selectedRule is null)
        {
            return;
        }

        _specificDatesList = new ListBox
        {
            Background = new SolidColorBrush(Color.Parse("#FF3D3D3D")),
            Foreground = Brushes.White,
            Height = 80
        };

        foreach (var date in _selectedRule.Dates)
        {
            _specificDatesList.Items.Add(date.ToString("dd.MM.yyyy"));
        }

        _specificDatePicker = new DatePicker
        {
            Width = 150,
            SelectedDate = new DateTimeOffset(DateTime.Today)
        };

        var addButton = new Button
        {
            Content = "Hinzufügen",
            Foreground = Brushes.White,
            Background = new SolidColorBrush(Color.Parse("#FF555555")),
            ClickMode = ClickMode.Press
        };
        addButton.Click += (_, _) =>
        {
            if (_selectedRule is null || _specificDatePicker?.SelectedDate is not { } selected)
            {
                return;
            }

            var date = selected.DateTime.Date;
            if (date < DateTime.Today)
            {
                return;
            }

            if (!_selectedRule.Dates.Any(d => d.Date == date))
            {
                _selectedRule.Dates.Add(date);
                _selectedRule.Dates.Sort();
                _selectedRule.RefreshDisplay();
                _specificDatesList?.Items.Clear();
                foreach (var d in _selectedRule.Dates)
                {
                    _specificDatesList?.Items.Add(d.ToString("dd.MM.yyyy"));
                }

                if (_currentAlarm is not null)
                {
                    RecurrenceList.ItemsSource = _currentAlarm.RecurrenceRules;
                    RecurrenceList.SelectedItem = _selectedRule;
                }
            }
        };

        var removeButton = new Button
        {
            Content = "Entfernen",
            Foreground = Brushes.White,
            Background = new SolidColorBrush(Color.Parse("#FF555555")),
            ClickMode = ClickMode.Press
        };
        removeButton.Click += (_, _) =>
        {
            if (_selectedRule is null || _specificDatesList?.SelectedItem is not string selectedText)
            {
                return;
            }

            if (DateTime.TryParseExact(selectedText, "dd.MM.yyyy", null, DateTimeStyles.None, out var date))
            {
                _selectedRule.Dates.RemoveAll(d => d.Date == date.Date);
                _selectedRule.RefreshDisplay();
                _specificDatesList.Items.Clear();
                foreach (var d in _selectedRule.Dates)
                {
                    _specificDatesList.Items.Add(d.ToString("dd.MM.yyyy"));
                }

                if (_currentAlarm is not null)
                {
                    RecurrenceList.ItemsSource = _currentAlarm.RecurrenceRules;
                    RecurrenceList.SelectedItem = _selectedRule;
                }
            }
        };

        var controls = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
        controls.Children.Add(_specificDatePicker!);
        controls.Children.Add(addButton);
        controls.Children.Add(removeButton);

        RuleEditorPanel.Children.Add(_specificDatesList);
        RuleEditorPanel.Children.Add(controls);
    }

    private void BuildMonthlyDayEditor()
    {
        if (_selectedRule is null)
        {
            return;
        }

        _monthDaysBox = new TextBox
        {
            Watermark = "z. B. 1, 15, -1 für letzten",
            Foreground = Brushes.White,
            Background = new SolidColorBrush(Color.Parse("#FF555555")),
            CaretBrush = Brushes.White,
            Text = string.Join(", ", _selectedRule.MonthDays)
        };
        _monthDaysBox.LostFocus += (_, _) =>
        {
            SyncSelectedRule();
            _selectedRule?.RefreshDisplay();
            if (_currentAlarm is not null)
            {
                RecurrenceList.ItemsSource = _currentAlarm.RecurrenceRules;
            }
        };

        var hint = new TextBlock
        {
            Text = "Positive Zahlen 1-31, -1 = letzter, -2 = vorletzter Tag",
            Foreground = Brushes.LightGray,
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap
        };

        RuleEditorPanel.Children.Add(_monthDaysBox);
        RuleEditorPanel.Children.Add(hint);
    }

    private void BuildMonthlyWeekdayEditor()
    {
        if (_selectedRule is null)
        {
            return;
        }

        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };

        _ordinalCombo = new ComboBox
        {
            Width = 105,
            Background = new SolidColorBrush(Color.Parse("#FF3D3D3D")),
            Foreground = Brushes.White,
            ItemsSource = OrdinalOptions
        };
        _ordinalCombo.DisplayMemberBinding = new Binding("Display");
        _ordinalCombo.SelectedItem = OrdinalOptions.FirstOrDefault(o => o.Ordinal == _selectedRule.MonthWeekdayOrdinal);

        _weekdayCombo = new ComboBox
        {
            Width = 115,
            Background = new SolidColorBrush(Color.Parse("#FF3D3D3D")),
            Foreground = Brushes.White,
            ItemsSource = WeekdayOptions
        };
        _weekdayCombo.DisplayMemberBinding = new Binding("Display");
        _weekdayCombo.SelectedItem = WeekdayOptions.FirstOrDefault(w => w.Day == _selectedRule.MonthWeekday);

        _beforeLastCheck = new CheckBox
        {
            Content = "vor Monatsletztem",
            Foreground = Brushes.White,
            IsChecked = _selectedRule.MonthWeekdayBeforeLastDay
        };

        var suppress = false;

        void UpdateBeforeLastCheck()
        {
            if (_beforeLastCheck is null || _ordinalCombo?.SelectedItem is not OrdinalOption option)
            {
                return;
            }

            suppress = true;
            if (option.Ordinal > 0)
            {
                _beforeLastCheck.IsEnabled = false;
                _beforeLastCheck.IsChecked = false;
            }
            else
            {
                _beforeLastCheck.IsEnabled = true;
            }
            suppress = false;
        }

        void OnChanged()
        {
            if (suppress)
            {
                return;
            }

            UpdateBeforeLastCheck();
            SyncSelectedRule();
            _selectedRule?.RefreshDisplay();
            if (_currentAlarm is not null)
            {
                RecurrenceList.ItemsSource = _currentAlarm.RecurrenceRules;
            }
        }

        _ordinalCombo.SelectionChanged += (_, _) => OnChanged();
        _weekdayCombo.SelectionChanged += (_, _) => OnChanged();
        _beforeLastCheck.IsCheckedChanged += (_, _) => OnChanged();

        UpdateBeforeLastCheck();

        panel.Children.Add(_ordinalCombo);
        panel.Children.Add(_weekdayCombo);
        panel.Children.Add(_beforeLastCheck);

        RuleEditorPanel.Children.Add(panel);
    }

    private void BuildIntervalEditor()
    {
        if (_selectedRule is null)
        {
            return;
        }

        _intervalValueBox = new TextBox
        {
            Watermark = "Anzahl",
            Foreground = Brushes.White,
            Background = new SolidColorBrush(Color.Parse("#FF555555")),
            CaretBrush = Brushes.White,
            Text = _selectedRule.IntervalValue.ToString(),
            Width = 60
        };

        _intervalUnitCombo = new ComboBox
        {
            Width = 100,
            MinWidth = 100,
            Background = new SolidColorBrush(Color.Parse("#FF3D3D3D")),
            Foreground = Brushes.White,
            ItemsSource = UnitOptions
        };
        _intervalUnitCombo.DisplayMemberBinding = new Binding("Display");
        _intervalUnitCombo.SelectedItem = UnitOptions.FirstOrDefault(u => u.Unit == _selectedRule.IntervalUnit);

        _startDatePicker = new DatePicker
        {
            Width = 150
        };
        if (_selectedRule.IntervalStart.HasValue)
        {
            _startDatePicker.SelectedDate = new DateTimeOffset(_selectedRule.IntervalStart.Value);
        }

        var weekdayPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, IsVisible = _selectedRule.IntervalUnit == RecurrenceUnit.Weeks };
        var days = new[] { "Mo", "Di", "Mi", "Do", "Fr", "Sa", "So" };
        for (int i = 0; i < days.Length; i++)
        {
            var day = (DayOfWeek)i;
            var check = new CheckBox
            {
                Content = days[i],
                Foreground = Brushes.White,
                Padding = new Thickness(2, 0, 0, 0),
                IsChecked = _selectedRule.DaysOfWeek.Contains(day)
            };
            _intervalWeekdayChecks.Add(check);
            weekdayPanel.Children.Add(check);
        }

        void OnChanged()
        {
            SyncSelectedRule();
            _selectedRule?.RefreshDisplay();
            if (_currentAlarm is not null)
            {
                RecurrenceList.ItemsSource = _currentAlarm.RecurrenceRules;
            }
        }

        _intervalValueBox.LostFocus += (_, _) =>
        {
            OnChanged();
            weekdayPanel.IsVisible = _selectedRule?.IntervalUnit == RecurrenceUnit.Weeks;
        };
        _intervalUnitCombo.SelectionChanged += (_, _) =>
        {
            OnChanged();
            weekdayPanel.IsVisible = _selectedRule?.IntervalUnit == RecurrenceUnit.Weeks;
        };
        _startDatePicker.SelectedDateChanged += (_, _) => OnChanged();
        foreach (var check in _intervalWeekdayChecks)
        {
            check.IsCheckedChanged += (_, _) => OnChanged();
        }

        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
        row.Children.Add(_intervalValueBox);
        row.Children.Add(_intervalUnitCombo);
        row.Children.Add(_startDatePicker);

        RuleEditorPanel.Children.Add(row);
        RuleEditorPanel.Children.Add(weekdayPanel);
    }

    private async void BrowseSoundButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_currentAlarm is null)
        {
            return;
        }

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Alarmton auswählen",
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Audiodateien")
                {
                    Patterns = new[] { "*.mp3", "*.wav", "*.ogg", "*.flac", "*.aac", "*.wma" }
                },
                new FilePickerFileType("Alle Dateien")
                {
                    Patterns = new[] { "*" }
                }
            }
        });

        if (files is null || files.Count == 0)
        {
            return;
        }

        var file = files[0];
        var soundsDir = Path.Combine(_settings.GetBaseDirectory(), "Sounds");
        Directory.CreateDirectory(soundsDir);

        var relativeTarget = $"Sounds/{file.Name}";
        var targetPath = Path.Combine(_settings.GetBaseDirectory(), relativeTarget);

        if (file.TryGetLocalPath() is { } localPath && !string.Equals(localPath, targetPath, StringComparison.OrdinalIgnoreCase))
        {
            await using var sourceStream = await file.OpenReadAsync();
            await using var targetStream = File.Create(targetPath);
            await sourceStream.CopyToAsync(targetStream);
        }

        SoundFileBox.Text = relativeTarget;
    }
}
