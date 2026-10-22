using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Mav.Platform;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>Which part of the time a <see cref="MavTimeDial"/> is currently selecting: hours or minutes.</summary>
public enum MavTimeDialMode
{
    Hour,
    Minute,
}
