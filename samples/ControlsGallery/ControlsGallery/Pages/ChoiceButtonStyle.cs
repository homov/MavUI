using Mav.Controls;

namespace Mav.ControlsGallery.Pages;

/// <summary>Style of a choice button: the selected choice is filled, the others keep their base style.</summary>
internal static class ChoiceButtonStyle
{
    public static MavButtonVariant Of(bool selected, MavButtonVariant baseVariant) =>
        selected ? MavButtonVariant.Filled : baseVariant;
}
