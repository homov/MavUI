using Avalonia;
using Avalonia.Media;

namespace Mav.ControlsGallery.Pages;

/// <summary>A seed color choice; IsSelected marks the seed the theme currently uses.</summary>
public sealed record SeedChoice(string Name, Color Color, bool IsSelected);
