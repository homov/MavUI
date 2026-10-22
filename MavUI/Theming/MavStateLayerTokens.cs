using Avalonia.Media;

namespace Mav.Theming;

/// <summary>Opacity values for hover, focus, pressed and dragged state layers.</summary>
public sealed class MavStateLayerTokens
{
    public static MavStateLayerTokens Default { get; } = new();

    public float Hover => 0.08f;
    public float Focus => 0.10f;
    public float Pressed => 0.12f;
    public float Dragged => 0.16f;
    public float DisabledContainer => 0.12f;
    public float DisabledContent => 0.38f;
}
