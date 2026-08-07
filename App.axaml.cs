using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;

namespace AnalogClock;

public partial class App : Application
{
    private static Window? _mainWindow;
    private static TrayIcon? _trayIcon;
    private static bool _starting;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            _starting = true;
            var settings = SettingsService.Load();

            if (!settings.IsLicensed)
            {
                var licenseWindow = new LicenseWindow(settings);
                licenseWindow.Closed += (_, _) =>
                {
                    if (settings.IsLicensed)
                    {
                        SettingsService.Save(settings);
                        StartMainWindow();
                    }
                    else
                    {
                        Shutdown(desktop);
                    }
                };
                licenseWindow.Show();
                licenseWindow.Activate();
            }
            else
            {
                StartMainWindow();
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void StartMainWindow()
    {
        if (!_starting)
        {
            return;
        }

        _starting = false;
        var settings = SettingsService.Load();

        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        _mainWindow = new MainWindow();
        desktop.MainWindow = _mainWindow;

        CreateTrayIcon();
        SetClockVisible(settings.ClockVisible, save: false);
    }

    public static void SetClockVisible(bool visible, bool save = true)
    {
        if (_mainWindow is null)
        {
            return;
        }

        if (visible)
        {
            _mainWindow.Show();
            _mainWindow.Activate();
            _mainWindow.Topmost = false;
            _mainWindow.Topmost = true;
            if (_trayIcon is not null)
            {
                _trayIcon.IsVisible = false;
            }
        }
        else
        {
            _mainWindow.Hide();
            if (_trayIcon is not null)
            {
                _trayIcon.IsVisible = true;
            }
        }

        if (save)
        {
            var settings = SettingsService.Load();
            settings.ClockVisible = visible;
            SettingsService.Save(settings);
        }
    }

    private void CreateTrayIcon()
    {
        Stream? iconStream = null;
        try
        {
            iconStream = AssetLoader.Open(new Uri("avares://AnalogClock/Assets/clock-icon.png"));
        }
        catch
        {
            // ignore missing icon
        }

        WindowIcon? icon = null;
        if (iconStream is not null)
        {
            try
            {
                icon = new WindowIcon(iconStream);
            }
            catch
            {
                // ignore
            }
        }

        var menu = new NativeMenu();
        var showItem = new NativeMenuItem { Header = "Uhr anzeigen" };
        showItem.Click += (_, _) => SetClockVisible(true);
        menu.Items.Add(showItem);

        var licenseItem = new NativeMenuItem { Header = "Lizenz" };
        licenseItem.Click += (_, _) => OpenLicenseWindow();
        menu.Items.Add(licenseItem);

        var infoItem = new NativeMenuItem { Header = "Info" };
        infoItem.Click += (_, _) => OpenInfoWindow();
        menu.Items.Add(infoItem);

        menu.Items.Add(new NativeMenuItemSeparator());

        var exitItem = new NativeMenuItem { Header = "Beenden" };
        exitItem.Click += (_, _) => ShutdownApplication();
        menu.Items.Add(exitItem);

        _trayIcon = new TrayIcon
        {
            Icon = icon,
            ToolTipText = "AnalogClock",
            Menu = menu,
            IsVisible = false
        };

        TrayIcon.SetIcons(this, new TrayIcons { _trayIcon });
    }

    private void OpenLicenseWindow()
    {
        var settings = SettingsService.Load();
        var window = new LicenseWindow(settings);
        window.Closed += (_, _) =>
        {
            if (settings.IsLicensed)
            {
                SettingsService.Save(settings);
                StartMainWindow();
            }
        };
        window.Show();
        window.Activate();
    }

    private void OpenInfoWindow()
    {
        var window = new InfoWindow();
        window.Show();
        window.Activate();
    }

    private static void ShutdownApplication()
    {
        if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.Shutdown();
        }
        else
        {
            Environment.Exit(0);
        }
    }

    private static void Shutdown(IClassicDesktopStyleApplicationLifetime lifetime)
    {
        lifetime.Shutdown();
    }
}
