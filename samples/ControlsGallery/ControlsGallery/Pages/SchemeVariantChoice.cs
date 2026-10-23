using Mav.Controls;
using Mav.Theming;

namespace Mav.ControlsGallery.Pages;

/// <summary>One palette choice: the variant, its localized label and the button style that marks the current variant.</summary>
public sealed record SchemeVariantChoice(MavSchemeVariant Variant, string Label, MavButtonVariant ButtonVariant);
