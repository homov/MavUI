using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Convenience Material Design 3 Expressive Wavy Linear Progress Indicator.
/// </summary>
public class MavWavyProgressIndicator : MavLinearProgress
{
    /// <summary>Creates a wavy progress indicator with default settings.</summary>
    public MavWavyProgressIndicator() : this(0.65, false, 3.0, 24.0)
    {
    }

    /// <summary>Creates a wavy progress indicator with the given value, indeterminate mode, amplitude and wavelength.</summary>
    public MavWavyProgressIndicator(
        double value = 0.65,
        bool indeterminate = false,
        double amplitude = 3.0,
        double wavelength = 24.0)
        : base(value, indeterminate, wavy: true)
    {
        WaveAmplitude = amplitude;
        WaveLength = wavelength;
    }
}
