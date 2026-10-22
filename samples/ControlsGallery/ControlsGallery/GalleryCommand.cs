using System.Windows.Input;

namespace Mav.ControlsGallery;

/// <summary>
/// Minimal <see cref="ICommand"/> used by the AXAML pages. Commands are declared as static
/// fields and bound with <c>Command="{x:Static ...}"</c>, so pages need no event handlers.
/// </summary>
public sealed class GalleryCommand : ICommand
{
    private readonly Action<object?> _execute;

    public GalleryCommand(Action execute) => _execute = _ => execute();

    public GalleryCommand(Action<object?> execute) => _execute = execute;

    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => _execute(parameter);
}
