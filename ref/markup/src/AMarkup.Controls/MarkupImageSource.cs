using System;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace AMarkup;

/// <summary>
/// Loads images from Avalonia resource URIs or local files.
/// </summary>
public static class MarkupImageSource
{
    public static IImage? Load(string source, string? assemblyName = null)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return null;
        }

        if (Uri.TryCreate(source, UriKind.Absolute, out Uri? uri))
        {
            return Load(uri);
        }

        if (string.IsNullOrWhiteSpace(assemblyName))
        {
            return null;
        }

        return Load(new Uri($"avares://{assemblyName}/{source.TrimStart('/')}"));
    }

    public static IImage? Load(Uri uri, string? assemblyName = null)
    {
        ArgumentNullException.ThrowIfNull(uri);

        if (!uri.IsAbsoluteUri && !string.IsNullOrWhiteSpace(assemblyName))
        {
            return Load(new Uri($"avares://{assemblyName}/{uri.OriginalString.TrimStart('/')}"));
        }

        return Load(uri);
    }

    public static IImage? Load(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        try
        {
            if (uri.IsFile)
            {
                return new Bitmap(uri.LocalPath);
            }

            if (!uri.Scheme.Equals("avares", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            using var stream = AssetLoader.Open(uri);
            return new Bitmap(stream);
        }
        catch
        {
            return null;
        }
    }
}
