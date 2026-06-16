using System.Windows.Input;

namespace Algomim.AutoCad.Mcp.UI;

internal sealed class RibbonCommandHandler : ICommand
{
    private readonly Action _action;

    public RibbonCommandHandler(Action action)
    {
        _action = action;
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => _action();

    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }
}
