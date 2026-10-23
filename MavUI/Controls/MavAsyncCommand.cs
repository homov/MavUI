using System.Windows.Input;

namespace Mav.Controls;

/// <summary>
/// An <see cref="ICommand"/> whose work is a <see cref="Task"/>. Controls that understand it, such as
/// <see cref="MavRefreshIndicator"/>, await the task and keep showing their busy state until it completes.
/// Usable from AXAML, because the task factory is passed once at construction.
/// </summary>
public sealed class MavAsyncCommand : ICommand
{
    private readonly Func<object?, Task> _execute;

    public MavAsyncCommand(Func<Task> execute) => _execute = _ => execute();

    public MavAsyncCommand(Func<object?, Task> execute) => _execute = execute;

    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }

    public bool CanExecute(object? parameter) => true;

    /// <summary>Runs the command without waiting; use <see cref="ExecuteAsync"/> to observe completion.</summary>
    public void Execute(object? parameter) => _ = ExecuteAsync(parameter);

    /// <summary>Runs the command and returns the task that completes with it.</summary>
    public Task ExecuteAsync(object? parameter) => _execute(parameter);
}
