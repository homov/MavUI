using System.Collections;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>Status of a <see cref="MavTimelineItem"/>, which selects its marker color.</summary>
public enum MavTimelineStatus
{
    Completed,
    Active,
    Pending,
    Warning,
    Error,
}
