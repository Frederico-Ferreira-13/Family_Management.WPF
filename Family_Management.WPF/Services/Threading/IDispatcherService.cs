namespace Family_Management.WPF.Services.Threading;

public interface IDispatcherService
{
    bool CheckAccess();

    void Invoke(Action action);

    T Invoke<T>(Func<T> function);

    Task InvokeAsync(Action action);

    Task InvokeAsync(Func<Task> function);

    Task<T> InvokeAsync<T>(Func<T> function);
}