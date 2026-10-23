using Avalonia.Controls;
using Mav.Controls;

namespace Hc.Pages.Groups;

/// <summary>The schedule tab of a group: the schedule editor and its save button.</summary>
public partial class GroupScheduleTab : UserControl
{
    public GroupScheduleTab() => InitializeComponent();

    public MavButton SaveButton => Save;

    /// <summary>The schedule being edited. The page reads it to save.</summary>
    public ScheduleDraft Draft => Editor.Draft;

    public void Show(ScheduleDraft draft) => Editor.Show(draft);
}
