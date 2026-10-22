using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Primitives;
using Mav.Theming;
using System.Collections;
using System.Globalization;
using Avalonia.Collections;
using Mav.Platform;

namespace Mav.Controls;

/// <summary>Presence status shown as a badge on a <see cref="MavAvatar"/>.</summary>
public enum MavAvatarStatus
{
    None,
    Online,
    Busy,
    Away,
    Offline,
}
