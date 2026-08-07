using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AnalogClock.KeyGenerator;

public class LicenseRecord : INotifyPropertyChanged
{
    private string _key = string.Empty;
    private string _customerName = string.Empty;
    private string _invoiceNumber = string.Empty;
    private string _invoicePosition = string.Empty;
    private DateTime _createdAt;

    public string Key
    {
        get => _key;
        set => SetField(ref _key, value);
    }

    public DateTime CreatedAt
    {
        get => _createdAt;
        set => SetField(ref _createdAt, value);
    }

    public string CustomerName
    {
        get => _customerName;
        set => SetField(ref _customerName, value);
    }

    public string InvoiceNumber
    {
        get => _invoiceNumber;
        set => SetField(ref _invoiceNumber, value);
    }

    public string InvoicePosition
    {
        get => _invoicePosition;
        set => SetField(ref _invoicePosition, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (!Equals(field, value))
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
