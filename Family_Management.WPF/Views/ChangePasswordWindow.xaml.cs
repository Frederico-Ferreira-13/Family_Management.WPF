using System.Windows;
using Family_Management.WPF.ViewModel.Authentication;

namespace Family_Management.WPF.Views;

public partial class ChangePasswordWindow : Window
{
    private ChangePasswordViewModel? _viewModel;

    public ChangePasswordWindow()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;
        Closed += OnClosed;
    }

    private void OnDataContextChanged(
        object sender,
        DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.CloseRequest -= OnCloseRequested;
        }

        _viewModel =
            e.NewValue as ChangePasswordViewModel;

        if (_viewModel is not null)
        {
            _viewModel.CloseRequest += OnCloseRequested;
        }
    }

    private void OnCloseRequested()
    {
        Close();
    }

    private void OnClosed(
        object? sender,
        EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.CloseRequest -= OnCloseRequested;
            _viewModel = null;
        }

        DataContextChanged -= OnDataContextChanged;
        Closed -= OnClosed;
    }
}