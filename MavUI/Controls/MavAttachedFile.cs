using System.Collections;
using System.Diagnostics;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Platform.Storage;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>File attached to a chat or form: name, size and an optional local path.</summary>
public sealed record MavAttachedFile(string Name, long SizeBytes, string? Path = null)
{
    public string FormattedSize =>
        SizeBytes >= 1024 * 1024
            ? $"{SizeBytes / (1024.0 * 1024.0):0.0} MB"
            : (SizeBytes >= 1024 ? $"{SizeBytes / 1024.0:0.0} KB" : $"{SizeBytes} B");
}
