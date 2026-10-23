using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AMarkup.Demo;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private string _title = "AMarkup Demo";
    private string _userName = "Avalonia user";
    private int _counter;
    private bool _isEnabled = true;
    private string? _selectedItem = "Item 2";
    private double _sliderValue = 42;
    private DateTimeOffset _selectedDate = DateTimeOffset.Now;

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string UserName
    {
        get => _userName;
        set => SetProperty(ref _userName, value);
    }

    public int Counter
    {
        get => _counter;
        set => SetProperty(ref _counter, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public string? SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    public double SliderValue
    {
        get => _sliderValue;
        set => SetProperty(ref _sliderValue, value);
    }

    public DateTimeOffset SelectedDate
    {
        get => _selectedDate;
        set => SetProperty(ref _selectedDate, value);
    }

    public ObservableCollection<string> Items { get; } = new()
    {
        "Item 1", "Item 2", "Item 3", "Item 4", "Item 5"
    };

    public void IncrementCounter() => Counter++;

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
