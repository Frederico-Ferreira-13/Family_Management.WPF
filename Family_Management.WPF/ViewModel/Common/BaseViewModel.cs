using System.ComponentModel;
using System.Runtime.CompilerServices;
using Family_Management.WPF.Services.Dialogs;
using Family_Management.WPF.Services.Navigation;

namespace Family_Management.WPF.ViewModel.Common;

public abstract class BaseViewModel :
    INotifyPropertyChanged,
    IAsyncLoadable
{
    private bool _isBusy;
    private bool _isInitialized;
    private string _title = string.Empty;

    protected BaseViewModel(
        INavigationService navigationService,
        INotificationService notificationService)
    {
        NavigationService = navigationService
            ?? throw new ArgumentNullException(nameof(navigationService));

        NotificationService = notificationService
            ?? throw new ArgumentNullException(nameof(notificationService));
    }

    protected INavigationService NavigationService { get; }

    protected INotificationService NotificationService { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsBusy
    {
        get => _isBusy;
        protected set => SetProperty(ref _isBusy, value);
    }

    public bool IsInitialized
    {
        get => _isInitialized;
        protected set => SetProperty(ref _isInitialized, value);
    }

    public string Title
    {
        get => _title;
        protected set => SetProperty(ref _title, value);
    }

    public virtual Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!IsInitialized)
        {
            IsInitialized = true;
        }

        return Task.CompletedTask;
    }

    protected bool SetProperty<T>(
        ref T field,
        T value,
        Action? onChanged = null,
        [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;

        OnPropertyChanged(propertyName);

        onChanged?.Invoke();

        return true;
    }

    protected void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }

    protected async Task RunSafeAsync(
        Func<Task> action,
        string? successMessage = null,
        bool showErrorNotification = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (IsBusy)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        IsBusy = true;

        try
        {
            await action();

            if (!string.IsNullOrWhiteSpace(successMessage))
            {
                await NotificationService.ShowInformationAsync(
                    successMessage,
                    "Sucesso");
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            // Cancelamento solicitado pelo utilizador/aplicação.
        }
        catch (Exception ex)
        {
            if (showErrorNotification)
            {
                await NotificationService.ShowErrorAsync(
                    $"Ocorreu um erro: {ex.Message}",
                    "Erro");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected virtual void OnPropertyChanged(
        params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            OnPropertyChanged(propertyName);
        }
    }
}