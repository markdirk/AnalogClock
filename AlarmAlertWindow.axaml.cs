using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;

namespace AnalogClock;

public partial class AlarmAlertWindow : Window
{
    private DispatcherTimer? _blinkTimer;
    private bool _red = true;
    private bool _stopSound;

    public AlarmAlertWindow()
    {
        InitializeComponent();
    }

    public AlarmAlertWindow(string description) : this()
    {
        DescriptionText!.Text = description;

        AddHandler(InputElement.PointerPressedEvent, (_, e) =>
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                e.Handled = true;
                Close();
            }
        }, RoutingStrategies.Tunnel, handledEventsToo: true);

        Closed += (_, _) =>
        {
            _stopSound = true;
            _blinkTimer?.Stop();
        };

        _blinkTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _blinkTimer.Tick += (_, _) =>
        {
            _red = !_red;
            Background = new SolidColorBrush(_red ? Color.Parse("#FF800020") : Color.Parse("#FF2D2D2D"));
        };
        _blinkTimer.Start();

        Task.Run(() => PlayBeepLoop());
    }

    private void PlayBeepLoop()
    {
        while (!_stopSound)
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    Console.Beep(880, 300);
                }
                Thread.Sleep(700);
            }
            catch
            {
                Thread.Sleep(1000);
            }
        }
    }
}
