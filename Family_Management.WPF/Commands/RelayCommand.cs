using System.Windows.Input;

namespace Family_Management.WPF.Commands;

public sealed class RelayCommand : IRelayCommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(
        Action<object?> execute,
        Func<object?, bool>? canExecute = null)
    {
        _execute = execute
            ?? throw new ArgumentNullException(nameof(execute));

        _canExecute = canExecute;
    }

    public RelayCommand(
        Action execute,
        Func<bool>? canExecute = null)
        : this(
            _ =>
            {
                execute();
            },
            canExecute is null
                ? null
                : _ => canExecute())
    {
        ArgumentNullException.ThrowIfNull(execute);
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        return _canExecute?.Invoke(parameter) ?? true;
    }

    public void Execute(object? parameter)
    {
        _execute(parameter);
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(
            this,
            EventArgs.Empty);

        CommandManager.InvalidateRequerySuggested();
    }
}

public sealed class RelayCommand<T> : IRelayCommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public RelayCommand(
        Action<T?> execute,
        Func<T?, bool>? canExecute = null)
    {
        _execute = execute
            ?? throw new ArgumentNullException(nameof(execute));

        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        if (!TryGetParameter(parameter, out var value))
        {
            return false;
        }

        return _canExecute?.Invoke(value) ?? true;
    }

    public void Execute(object? parameter)
    {
        if (!TryGetParameter(parameter, out var value))
        {
            return;
        }

        _execute(value);
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(
            this,
            EventArgs.Empty);

        CommandManager.InvalidateRequerySuggested();
    }

    private static bool TryGetParameter(
        object? parameter,
        out T? value)
    {
        if (parameter is T typedValue)
        {
            value = typedValue;
            return true;
        }

        if (parameter is null &&
            default(T) is null)
        {
            value = default;
            return true;
        }

        value = default;
        return false;
    }
}