using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Mav.Platform;
using Mav.Primitives;

namespace Mav.Controls;

/// <summary>Toolbar action of a <see cref="MavContentPage"/>, shown in its app bar.</summary>
public sealed class MavPageAction
{
    public string Icon { get; init; } = string.Empty;
    public string? Tooltip { get; init; }
    public Action? OnClick { get; init; }
}
