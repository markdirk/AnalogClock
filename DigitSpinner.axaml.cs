using System;
using Avalonia;
using Avalonia.Controls;

namespace AnalogClock;

public partial class DigitSpinner : UserControl
{
    public static readonly DirectProperty<DigitSpinner, int> ValueProperty =
        AvaloniaProperty.RegisterDirect<DigitSpinner, int>(nameof(Value), o => o.Value, (o, v) => o.Value = v);

    public static readonly DirectProperty<DigitSpinner, int> MinimumProperty =
        AvaloniaProperty.RegisterDirect<DigitSpinner, int>(nameof(Minimum), o => o.Minimum, (o, v) => o.Minimum = v);

    public static readonly DirectProperty<DigitSpinner, int> MaximumProperty =
        AvaloniaProperty.RegisterDirect<DigitSpinner, int>(nameof(Maximum), o => o.Maximum, (o, v) => o.Maximum = v);

    private int _value;
    private int _minimum;
    private int _maximum = 9;

    public event EventHandler? ValueChanged;

    public DigitSpinner()
    {
        InitializeComponent();
        UpButton.PointerPressed += (_, _) => Increment();
        DownButton.PointerPressed += (_, _) => Decrement();
        UpdateText();
    }

    public int Value
    {
        get => _value;
        set
        {
            var newValue = Math.Clamp(value, _minimum, _maximum);
            if (_value != newValue)
            {
                _value = newValue;
                UpdateText();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public int Minimum
    {
        get => _minimum;
        set
        {
            if (_minimum != value)
            {
                _minimum = value;
                Value = _value;
            }
        }
    }

    public int Maximum
    {
        get => _maximum;
        set
        {
            if (_maximum != value)
            {
                _maximum = value;
                Value = _value;
            }
        }
    }

    private void Increment()
    {
        if (Value < Maximum)
        {
            Value++;
        }
    }

    private void Decrement()
    {
        if (Value > Minimum)
        {
            Value--;
        }
    }

    private void UpdateText()
    {
        DigitText!.Text = _value.ToString();
    }
}
