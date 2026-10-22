using System.Collections;
using System.Collections.Specialized;
using System.Windows.Input;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Section subheader label for <see cref="MavList"/>.
/// </summary>
public class MavListSubheader : MavContentView
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MavListSubheader, string>(nameof(Text), string.Empty);

    private readonly TextBlock _label;

    static MavListSubheader()
    {
        TextProperty.Changed.AddClassHandler<MavListSubheader>((s, e) => s._label.Text = e.GetNewValue<string>());
    }

    /// <summary>Creates an empty subheader.</summary>
    public MavListSubheader() : this(string.Empty)
    {
    }

    /// <summary>Creates a subheader with the given text.</summary>
    public MavListSubheader(string text)
    {
        Padding = new Thickness(16, 12, 16, 4);
        _label = new TextBlock { Text = text };
        Content = _label;
        Text = text;

        MavTheme.Observe(this, (self, theme) =>
        {
            self._label.Apply(MavTypeRole.LabelLarge, theme.Typography);
            self._label.Foreground = MavTheme.BrushOf(theme.Color.Primary);
        });
    }

    [Content]
    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
}
