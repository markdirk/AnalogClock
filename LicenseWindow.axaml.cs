using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace AnalogClock;

public partial class LicenseWindow : Window
{
    private ClockSettings _settings = new();
    private TextBox[] _parts = Array.Empty<TextBox>();
    private string _hardwareId = string.Empty;

    public LicenseWindow()
    {
        InitializeComponent();
    }

    public LicenseWindow(ClockSettings settings) : this()
    {
        _settings = settings;
        SetupControls();
    }

    private void SetupControls()
    {
        _parts = new[] { Part1Box, Part2Box, Part3Box, Part4Box, Part5Box, Part6Box };
        _hardwareId = HardwareId.GetHardwareId();

        HwidText.Text = _hardwareId;

        var existing = _settings.LicenseKey?.Split('-');
        for (int i = 0; i < _parts.Length; i++)
        {
            var part = _parts[i];
            part.Text = existing is not null && i < existing.Length ? existing[i] : string.Empty;
            part.TextChanged += Part_TextChanged;
            part.AddHandler(InputElement.KeyDownEvent, Part_KeyDown, Avalonia.Interactivity.RoutingStrategies.Bubble, handledEventsToo: true);
        }

        if (_settings.IsLicensed)
        {
            StatusText!.Text = "Bereits aktiviert";
            StatusText.Foreground = Avalonia.Media.Brushes.LightGreen;
        }
    }

    private void Part_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox box)
        {
            return;
        }

        var upper = box.Text?.ToUpperInvariant() ?? string.Empty;
        if (upper != box.Text)
        {
            box.TextChanged -= Part_TextChanged;
            box.Text = upper;
            box.CaretIndex = upper.Length;
            box.TextChanged += Part_TextChanged;
        }
    }

    private void Part_KeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox box)
        {
            return;
        }

        var index = Array.IndexOf(_parts, box);
        if (e.Key == Key.Back && box.Text?.Length == 0 && index > 0)
        {
            _parts[index - 1].Focus();
            _parts[index - 1].CaretIndex = _parts[index - 1].Text?.Length ?? 0;
            e.Handled = true;
        }
        else if (e.Key == Key.V && e.KeyModifiers == KeyModifiers.Control)
        {
            e.Handled = true;
            _ = PasteFromClipboardAsync();
        }
    }

    public void ActivateButton_Click(object? sender, RoutedEventArgs e)
    {
        var key = string.Join("-", _parts.Select(p => p.Text ?? string.Empty));
        if (LicenseKey.Verify(key))
        {
            _settings.IsLicensed = true;
            _settings.LicenseKey = key;
            SettingsService.Save(_settings);
            Close();
        }
        else
        {
            if (StatusText is not null)
            {
                StatusText.Text = "Ungültiger Lizenzschlüssel.";
                StatusText.Foreground = Avalonia.Media.Brushes.Red;
            }
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.V && e.KeyModifiers == KeyModifiers.Control)
        {
            e.Handled = true;
            _ = PasteFromClipboardAsync();
        }
    }

    private async Task PasteFromClipboardAsync()
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null)
        {
            return;
        }

        var text = await clipboard.GetTextAsync();
        PasteLicense(text);
    }

    private void PasteLicense(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var key = text.Trim().Replace(" ", string.Empty);
        var match = Regex.Match(key, @"^[A-Za-z0-9]{4}-[A-Za-z0-9]{4}-[A-Za-z0-9]{4}-[A-Za-z0-9]{4}-[A-Za-z0-9]{4}-[A-Za-z0-9]{4,5}$");
        if (!match.Success)
        {
            if (StatusText is not null)
            {
                StatusText.Text = "Ungültiges Lizenzformat. Erwartet: XXXX-XXXX-XXXX-XXXX-XXXX-XXXXX";
                StatusText.Foreground = Brushes.Yellow;
            }
            return;
        }

        var parts = key.Split('-');
        for (int i = 0; i < _parts.Length && i < parts.Length; i++)
        {
            _parts[i].Text = parts[i].Trim().ToUpperInvariant();
        }
    }
}
