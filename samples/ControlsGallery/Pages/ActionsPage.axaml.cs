using Avalonia.Controls;
using Mav;
using Mav.Controls;

namespace Mav.ControlsGallery.Pages;

public partial class ActionsPage : UserControl
{
    private int _clicks;

    public ActionsPage()
    {
        InitializeComponent();

        ClickButton.Clicked += (_, _) =>
        {
            _clicks++;
            ClickCounter.Text = MavLocalization.Format("Actions.Click.Counter.Value", "Clicks: {0}", _clicks);
        };

        LoadingButton.Clicked += async (_, _) =>
        {
            LoadingButton.IsLoading = true;
            await Task.Delay(1200);
            LoadingButton.IsLoading = false;
        };

        StandardGroup.AddButton(new MavButton("Day", MavButtonVariant.Outlined));
        StandardGroup.AddButton(new MavButton("Week", MavButtonVariant.Outlined));
        StandardGroup.AddButton(new MavButton("Month", MavButtonVariant.Outlined));

        ConnectedGroup.AddButton(new MavButton("Bold", MavButtonVariant.Tonal, MavIcons.FormatBold));
        ConnectedGroup.AddButton(new MavButton("Italic", MavButtonVariant.Tonal, MavIcons.FormatItalic));
        ConnectedGroup.AddButton(new MavButton("Underline", MavButtonVariant.Tonal, MavIcons.FormatUnderlined));

        SplitButton.Menu = new MavMenu()
            .AddItem("Save as draft", MavIcons.Description, onSelected: () => { })
            .AddItem("Save and close", MavIcons.Close, onSelected: () => { });

        FabMenu.AddItem("New document", MavIcons.NoteAdd, () => { });
        FabMenu.AddItem("Upload", MavIcons.Upload, () => { });
        FabMenu.AddItem("Scan", MavIcons.DocumentScanner, () => { });
    }
}
