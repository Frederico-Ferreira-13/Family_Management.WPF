using System.Windows.Input;

namespace Family_Management.WPF.Commands;

public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<object?, Task> _execute;
    private readonly Func<object?, bool>? _canExecute;

    private bool _isExecuting;

    public AsyncRelayCommand(
        Func<object?, Task> execute,
        Func<object?, bool>? canExecute = null)
    {
        _execute = execute
            ?? throw new ArgumentNullException(nameof(execute));

        _canExecute = canExecute;
    }

    public AsyncRelayCommand(
        Func<Task> execute,
        Func<bool>? canExecute = null)
        : this(
            _ => execute(),
            canExecute is null
                ? null
                : _ => canExecute())
    {
        ArgumentNullException.ThrowIfNull(execute);
    }

    public event EventHandler? CanExecuteChanged;

    public bool IsExecuting => _isExecuting;

    public bool CanExecute(object? parameter)
    {
        return !_isExecuting
               && (_canExecute?.Invoke(parameter) ?? true);
    }

    public async void Execute(object? parameter)
    {
        await ExecuteAsync(parameter);
    }

    public async Task ExecuteAsync(object? parameter = null)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        try
        {
            _isExecuting = true;

            RaiseCanExecuteChanged();

            await _execute(parameter);
        }
        finally
        {
            _isExecuting = false;

            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(
            this,
            EventArgs.Empty);

        CommandManager.InvalidateRequerySuggested();
    }
}

public sealed class AsyncRelayCommand<T> : ICommand
{
    private readonly Func<T?, Task> _execute;
    private readonly Func<T?, bool>? _canExecute;

    private bool _isExecuting;

    public AsyncRelayCommand(
        Func<T?, Task> execute,
        Func<T?, bool>? canExecute = null)
    {
        _execute = execute
            ?? throw new ArgumentNullException(nameof(execute));

        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool IsExecuting => _isExecuting;

    public bool CanExecute(object? parameter)
    {
        if (_isExecuting)
        {
            return false;
        }

        if (!TryGetParameter(parameter, out var value))
        {
            return false;
        }

        return _canExecute?.Invoke(value) ?? true;
    }

    public async void Execute(object? parameter)
    {
        if (!TryGetParameter(parameter, out var value))
        {
            return;
        }

        await ExecuteAsync(value);
    }

    public async Task ExecuteAsync(T? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        try
        {
            _isExecuting = true;

            RaiseCanExecuteChanged();

            await _execute(parameter);
        }
        finally
        {
            _isExecuting = false;

            RaiseCanExecuteChanged();
        }
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