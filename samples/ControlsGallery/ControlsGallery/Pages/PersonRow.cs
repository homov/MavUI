namespace Mav.ControlsGallery.Pages;

/// <summary>Plain data row for the table demo, declared in AXAML as an item of the table's ItemsSource.</summary>
public sealed class PersonRow
{
    public string Name { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public int Score { get; set; }
}
