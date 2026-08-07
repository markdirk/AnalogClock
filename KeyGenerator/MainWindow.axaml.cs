using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace AnalogClock.KeyGenerator;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<LicenseRecord> _records = new();
    private readonly ObservableCollection<LicenseRecord> _filtered = new();
    private readonly string _filePath;
    private readonly string _backupPath;

    public MainWindow()
    {
        InitializeComponent();

        _filePath = Path.Combine(AppContext.BaseDirectory, "keys.dat");
        _backupPath = _filePath + ".bak";

        DataContext = this;
        RecordsGrid.ItemsSource = _filtered;

        GenerateButton.Click += GenerateButton_Click;
        SaveButton.Click += SaveButton_Click;
        RestoreBackupButton.Click += RestoreBackupButton_Click;
        SearchBox.TextChanged += SearchBox_TextChanged;
        RecordsGrid.CellEditEnded += (_, _) => SaveRecords();
        Closing += (_, _) => SaveRecords();

        LoadRecords();
        ApplyFilter();
        UpdateStatus();
    }

    private void LoadRecords()
    {
        _records.Clear();

        if (!File.Exists(_filePath))
        {
            return;
        }

        var encrypted = File.ReadAllText(_filePath);
        var json = KeyStoreCrypto.Decrypt(encrypted);

        if (string.IsNullOrEmpty(json) && File.Exists(_backupPath))
        {
            encrypted = File.ReadAllText(_backupPath);
            json = KeyStoreCrypto.Decrypt(encrypted);
            if (!string.IsNullOrEmpty(json))
            {
                StatusText.Text = "Hauptdatei defekt, Backup wurde geladen.";
            }
        }

        if (string.IsNullOrEmpty(json))
        {
            return;
        }

        try
        {
            var records = JsonSerializer.Deserialize<LicenseRecord[]>(json);
            if (records != null)
            {
                foreach (var record in records.Distinct(new LicenseRecordKeyComparer()))
                {
                    _records.Add(record);
                }
            }
        }
        catch
        {
            StatusText.Text = "Fehler beim Laden der Schlüsselliste.";
        }
    }

    private void SaveRecords()
    {
        try
        {
            var json = JsonSerializer.Serialize(_records.ToArray());
            var encrypted = KeyStoreCrypto.Encrypt(json);

            if (File.Exists(_filePath))
            {
                if (File.Exists(_backupPath))
                {
                    File.Delete(_backupPath);
                }

                File.Copy(_filePath, _backupPath);
            }

            File.WriteAllText(_filePath, encrypted);
            StatusText.Text = $"{DateTime.Now:HH:mm:ss} – {_records.Count} Schlüssel gespeichert.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Fehler beim Speichern: {ex.Message}";
        }

        UpdateStatus();
    }

    private void UpdateStatus()
    {
        RecordCountText.Text = $"{_records.Count}\u00A0Schlüssel";
        if (string.IsNullOrEmpty(StatusText.Text) || StatusText.Text == "Bereit")
        {
            StatusText.Text = $"{DateTime.Now:HH:mm:ss} – {_records.Count} Schlüssel geladen.";
        }
    }

    private void GenerateButton_Click(object? sender, RoutedEventArgs e)
    {
        if (!int.TryParse(CountBox.Text, out var count) || count < 1)
        {
            count = 1;
        }

        const int maxAttempts = 1000;

        for (var i = 0; i < count; i++)
        {
            var key = string.Empty;
            var attempts = 0;

            do
            {
                key = LicenseKey.Generate();
                attempts++;
            }
            while (_records.Any(r => r.Key == key) && attempts < maxAttempts);

            var record = new LicenseRecord
            {
                Key = key,
                CreatedAt = DateTime.Now
            };

            _records.Add(record);
            if (MatchesFilter(record))
            {
                _filtered.Add(record);
            }
        }

        SaveRecords();
    }

    private void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        SaveRecords();
    }

    private async void RestoreBackupButton_Click(object? sender, RoutedEventArgs e)
    {
        if (!File.Exists(_backupPath))
        {
            StatusText.Text = "Kein Backup vorhanden.";
            return;
        }

        if (!await ConfirmAsync("Backup wiederherstellen? Die aktuelle Liste wird überschrieben."))
        {
            return;
        }

        try
        {
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }

            File.Copy(_backupPath, _filePath);
            LoadRecords();
            ApplyFilter();
            StatusText.Text = "Backup wiederhergestellt.";
            UpdateStatus();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Fehler beim Wiederherstellen: {ex.Message}";
        }
    }

    private async void CopyButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: LicenseRecord record })
        {
            return;
        }

        if (Clipboard is not null)
        {
            await Clipboard.SetTextAsync(record.Key);
            StatusText.Text = $"{DateTime.Now:HH:mm:ss} – Schlüssel in Zwischenablage kopiert.";
        }
    }

    private void SearchBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        _filtered.Clear();

        var search = SearchBox.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(search))
        {
            foreach (var record in _records)
            {
                _filtered.Add(record);
            }
            return;
        }

        var term = search.ToUpperInvariant();
        foreach (var record in _records.Where(MatchesFilter))
        {
            _filtered.Add(record);
        }
    }

    private bool MatchesFilter(LicenseRecord record)
    {
        var search = SearchBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(search))
        {
            return true;
        }

        var term = search.ToUpperInvariant();
        return record.Key.Contains(term, StringComparison.InvariantCultureIgnoreCase)
               || record.CustomerName.Contains(term, StringComparison.InvariantCultureIgnoreCase)
               || record.InvoiceNumber.Contains(term, StringComparison.InvariantCultureIgnoreCase)
               || record.InvoicePosition.Contains(term, StringComparison.InvariantCultureIgnoreCase);
    }

    private async Task<bool> ConfirmAsync(string message)
    {
        var panel = CreateConfirmPanel(message, out var yesButton, out var noButton);

        var dialog = new Window
        {
            Title = "Bestätigung",
            Width = 360,
            Height = 160,
            Background = this.Background,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = panel
        };

        yesButton!.Click += (_, _) => dialog.Close(true);
        noButton!.Click += (_, _) => dialog.Close(false);

        return await dialog.ShowDialog<bool>(this);
    }

    private static StackPanel CreateConfirmPanel(string message, out Button yesButton, out Button noButton)
    {
        var panel = new StackPanel
        {
            Margin = new Thickness(15),
            Spacing = 15
        };

        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.White
        });

        var buttons = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Spacing = 10
        };

        yesButton = new Button
        {
            Content = "Ja",
            Background = new SolidColorBrush(Color.Parse("#FF555555")),
            Foreground = Brushes.White
        };

        noButton = new Button
        {
            Content = "Nein",
            Background = new SolidColorBrush(Color.Parse("#FF555555")),
            Foreground = Brushes.White
        };

        buttons.Children.Add(yesButton);
        buttons.Children.Add(noButton);
        panel.Children.Add(buttons);

        return panel;
    }

    private class LicenseRecordKeyComparer : System.Collections.Generic.IEqualityComparer<LicenseRecord>
    {
        public bool Equals(LicenseRecord? x, LicenseRecord? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            return x.Key == y.Key;
        }

        public int GetHashCode(LicenseRecord obj)
        {
            return obj.Key.GetHashCode();
        }
    }
}
