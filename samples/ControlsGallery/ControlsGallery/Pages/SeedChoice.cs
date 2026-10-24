using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media;

namespace Mav.ControlsGallery.Pages;

/// <summary>
/// A seed color choice; <see cref="IsSelected"/> marks the seed the theme currently uses.
/// The instance is mutable and raises <see cref="PropertyChanged"/> so the radio row keeps its
/// controls (and their hover/focus) when the theme changes instead of being rebuilt.
/// </summary>
public sealed class SeedChoice(string name, Color color) : INotifyPropertyChanged
{
    private bool _isSelected;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Name { get; } = name;

    public Color Color { get; } = color;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
                return;

            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    /// <summary>Marks the choice without raising a change when nothing moved.</summary>
    public bool SetSelected(bool selected)
    {
        if (_isSelected == selected)
            return false;

        IsSelected = selected;
        return true;
    }
}
