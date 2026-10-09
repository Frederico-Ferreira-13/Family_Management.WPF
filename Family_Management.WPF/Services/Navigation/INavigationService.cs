using Family_Management.WPF.ViewModel.Common;

namespace Family_Management.WPF.Services.Navigation;

public interface INavigationService
{
    event Action<BaseViewModel>? CurrentChildViewModelChanged;

    BaseViewModel? CurrentViewModel { get; }

    bool CanGoBack { get; }

    Task NavigateTo<TViewModel>()
        where TViewModel : BaseViewModel;

    Task NavigateTo<TViewModel, TParameter>(TParameter parameter)
        where TViewModel : BaseViewModel;

    Task NavigateToScreen(AppScreen screen);

    Task NavigateToLoginView();

    Task NavigateToRegisterView();

    Task ShowModal<TViewModel>()
        where TViewModel : BaseViewModel;

    Task ShowModal<TViewModel, TParameter>(TParameter parameter)
        where TViewModel : BaseViewModel;

    void CloseModal();

    void GoBack();
}