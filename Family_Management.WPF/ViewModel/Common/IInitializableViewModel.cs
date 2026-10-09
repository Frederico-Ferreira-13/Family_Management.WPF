namespace Family_Management.WPF.ViewModel.Common;

public interface IInitializableViewModel<in T>
{
    Task InitializeAsync(
        T parameter,
        CancellationToken cancellationToken = default);
}