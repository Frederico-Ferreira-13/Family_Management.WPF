using System;
using System.Threading.Tasks;
using System.Windows;

using Family_Management.WPF.ViewModel.Shell;

namespace Family_Management.WPF.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow(MainWindowViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();

        _viewModel = viewModel;

        DataContext = _viewModel;

        Loaded += OnLoaded;
    }

    private async void OnLoaded(
        object sender,
        RoutedEventArgs e)
    {
        Loaded -= OnLoaded;

        try
        {
            await _viewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Não foi possível inicializar a aplicação."
                + Environment.NewLine
                + Environment.NewLine
                + ex.Message,
                "Erro de inicialização",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Application.Current.Shutdown(-1);
        }
    }
}