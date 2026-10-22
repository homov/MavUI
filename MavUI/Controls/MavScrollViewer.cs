using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Layout;
using Avalonia.Media;

namespace Mav.Controls;

/// <summary>
/// Material host for scrolling content. It provides its own template so the library
/// does not depend on an Avalonia control theme for <see cref="ScrollViewer"/>.
/// Scrollbars are intentionally omitted; touch, wheel and keyboard scrolling are
/// handled by <see cref="ScrollContentPresenter"/> and the gesture recognizer.
/// </summary>
public class MavScrollViewer : ScrollViewer
{
    private static readonly IControlTemplate DefaultTemplate =
        new FuncControlTemplate<ScrollViewer>((viewer, scope) =>
        {
            var presenter = new ScrollContentPresenter
            {
                Name = "PART_ContentPresenter",
                [!PaddingProperty] = new TemplateBinding(PaddingProperty),
                [!BackgroundProperty] = new TemplateBinding(BackgroundProperty),
                [!ScrollContentPresenter.HorizontalSnapPointsTypeProperty] =
                    new TemplateBinding(HorizontalSnapPointsTypeProperty),
                [!ScrollContentPresenter.VerticalSnapPointsTypeProperty] =
                    new TemplateBinding(VerticalSnapPointsTypeProperty),
                [!ScrollContentPresenter.HorizontalSnapPointsAlignmentProperty] =
                    new TemplateBinding(HorizontalSnapPointsAlignmentProperty),
                [!ScrollContentPresenter.VerticalSnapPointsAlignmentProperty] =
                    new TemplateBinding(VerticalSnapPointsAlignmentProperty),
            };

            var gesture = new ScrollGestureRecognizer();
            gesture.Bind(
                ScrollGestureRecognizer.CanHorizontallyScrollProperty,
                presenter.GetObservable(ScrollContentPresenter.CanHorizontallyScrollProperty));
            gesture.Bind(
                ScrollGestureRecognizer.CanVerticallyScrollProperty,
                presenter.GetObservable(ScrollContentPresenter.CanVerticallyScrollProperty));
            gesture.Bind(
                ScrollGestureRecognizer.IsScrollInertiaEnabledProperty,
                viewer.GetObservable(IsScrollInertiaEnabledProperty));
            gesture.Bind(
                ScrollGestureRecognizer.OffsetProperty,
                presenter.GetBindingObservable(ScrollContentPresenter.OffsetProperty, v => (Vector?)v));
            gesture.Bind(
                ScrollGestureRecognizer.ExtentProperty,
                presenter.GetBindingObservable(ScrollContentPresenter.ExtentProperty, v => (Size?)v));
            gesture.Bind(
                ScrollGestureRecognizer.ViewportProperty,
                presenter.GetBindingObservable(ScrollContentPresenter.ViewportProperty, v => (Size?)v));

            presenter.GestureRecognizers.Add(gesture);
            scope.Register("PART_ContentPresenter", presenter);
            return presenter;
        });

    /// <summary>Creates a scroll viewer with the library defaults.</summary>
    public MavScrollViewer()
    {
        Background = Brushes.Transparent;
        Template = DefaultTemplate;
    }
}
