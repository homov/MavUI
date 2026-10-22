using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Primitives;
using Mav.Platform;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Row presentation for <see cref="MavDataTable"/>.
/// </summary>
public enum MavDataTableLayout
{
    /// <summary>Cards on narrow hosts, classic table otherwise.</summary>
    Auto,
    /// <summary>Always render the classic header + rows table.</summary>
    Table,
    /// <summary>Always render one card per row (phone layout).</summary>
    Cards,
}
