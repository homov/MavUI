using System.Windows.Input;
using Avalonia;
using Avalonia.Media;
using Mav;
using Mav.Controls;
using Mav.Theming;

namespace Mav.ControlsGallery.Pages;

public static class OverviewCommands
{
    public static readonly ICommand UseLight = new GalleryCommand(MavTheme.UseLight);
    public static readonly ICommand UseDark = new GalleryCommand(MavTheme.UseDark);
    public static readonly ICommand UseSystem = new GalleryCommand(MavTheme.UseSystemMode);

    public static readonly ICommand SetSeed = new GalleryCommand(p => MavTheme.Configure(seed: (Color)p!));

    public static readonly ICommand ContrastStandard = new GalleryCommand(() => MavTheme.Configure(contrast: MavContrast.Standard));
    public static readonly ICommand ContrastMedium = new GalleryCommand(() => MavTheme.Configure(contrast: MavContrast.Medium));
    public static readonly ICommand ContrastHigh = new GalleryCommand(() => MavTheme.Configure(contrast: MavContrast.High));

    public static readonly ICommand DensityComfortable = new GalleryCommand(() => MavTheme.Configure(density: MavDensity.Comfortable));
    public static readonly ICommand DensityCompact = new GalleryCommand(() => MavTheme.Configure(density: MavDensity.Compact));
    public static readonly ICommand DensityDense = new GalleryCommand(() => MavTheme.Configure(density: MavDensity.Dense));

    public static readonly ICommand MotionExpressive = new GalleryCommand(() => MavTheme.Configure(motion: MavMotionScheme.Expressive));
    public static readonly ICommand MotionStandard = new GalleryCommand(() => MavTheme.Configure(motion: MavMotionScheme.Standard));
    public static readonly ICommand MotionReduced = new GalleryCommand(() => MavTheme.Configure(motion: MavMotionScheme.Reduced));
}
