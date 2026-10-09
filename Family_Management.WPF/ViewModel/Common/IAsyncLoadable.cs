namespace Family_Management.WPF.ViewModel.Common;

public interface IAsyncLoadable
{
    bool IsInitialized { get; }

    Task InitializeAsync(
        CancellationToken cancellationToken = default);
}