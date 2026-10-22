using System.Globalization;
using System.Windows.Input;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>Checked state of a <see cref="MavCheckBox"/>: unchecked, checked or indeterminate.</summary>
public enum MavCheckState
{
    Unchecked,
    Checked,
    Indeterminate,
}
