using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Family_Management.WPF.Services.Threading;

public class WpfDispatcherService : IDispatcherService
{
    private readonly Dispatcher _dispatcher;

    public WpfDispatcherService()
    {
        _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
    }

    public bool CheckAccess() => _dispatcher.CheckAccess();

    public void Invoke(Action action)
    {
        if (CheckAccess())
            action();
        else
            _dispatcher.Invoke(action);
    }

    public T Invoke<T>(Func<T> function)
    {
        if (CheckAccess())
            return function();

        return _dispatcher.Invoke(function);
    }

    public async Task InvokeAsync(Action action)
    {
        await _dispatcher.InvokeAsync(action);
    }

    // NOVO: Essencial para métodos que já são Task (Async)
    public async Task InvokeAsync(Func<Task> function)
    {
        await _dispatcher.InvokeAsync(function).Task.Unwrap();
    }

    public async Task<T> InvokeAsync<T>(Func<T> function)
    {
        return await _dispatcher.InvokeAsync(function);
    }
}

