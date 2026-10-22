using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Mav.Platform;
using Mav.Primitives;

namespace Mav.Controls;

/// <summary>Event data for a page change in a <see cref="MavNavigationPage"/>: the page and the kind of navigation.</summary>
public sealed class MavPageChangedEventArgs : EventArgs
{
    /// <summary>Creates the event data for a page change, with the old and the new page.</summary>
    public MavPageChangedEventArgs(
        MavContentPage? oldPage,
        MavContentPage newPage,
        MavPageNavigationType navigationType)
    {
        OldPage = oldPage;
        NewPage = newPage;
        NavigationType = navigationType;
    }

    public MavContentPage? OldPage { get; }
    public MavContentPage NewPage { get; }
    public MavPageNavigationType NavigationType { get; }
}
