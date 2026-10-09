using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

using Family_Management.WPF.DependencyInjection;
using Family_Management.WPF.Views;

using FamilyManagement.Application;
using FamilyManagement.Infrastructure;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Family_Management.WPF;

public partial class App : Application
{
    private IHost? _host;
    private IServiceScope? _applicationScope;

    public App()
    {
        ShutdownMode = ShutdownMode.OnMainWindowClose;

        DispatcherUnhandledException += (_, e) =>
        {
            ReportFatalError(
                "Erro não tratado na interface WPF",
                e.Exception);

            e.Handled = true;

            Shutdown(-1);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception exception)
            {
                WriteDiagnosticLog(
                    "Erro não tratado no processo",
                    exception);
            }
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            WriteDiagnosticLog(
                "Erro numa tarefa assíncrona",
                e.Exception);

            e.SetObserved();
        };
    }

    protected override async void OnStartup(
        StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            _host = Host
                .CreateDefaultBuilder()
                .ConfigureAppConfiguration(
                    (context, configuration) =>
                    {
                        configuration.SetBasePath(
                            AppContext.BaseDirectory);

                        configuration.AddJsonFile(
                            "appsettings.json",
                            optional: false,
                            reloadOnChange: true);

                        if (context.HostingEnvironment.IsDevelopment())
                        {
                            configuration.AddUserSecrets<App>(
                                optional: true,
                                reloadOnChange: false);
                        }
                    })
                .ConfigureServices(
                    (context, services) =>
                    {
                        services.AddApplication();

                        services.AddInfrastructure(
                            context.Configuration);

                        services.AddPresentation(
                            context.Configuration);
                    })
                .Build();

            await _host.StartAsync();

            _applicationScope = _host.Services.CreateScope();

            var mainWindow = _applicationScope.ServiceProvider
                .GetRequiredService<MainWindow>();

            MainWindow = mainWindow;

            mainWindow.Show();
        }
        catch (Exception ex)
        {
            ReportFatalError(
                "Erro durante a inicialização",
                ex);

            Shutdown(-1);
        }
    }

    protected override async void OnExit(
        ExitEventArgs e)
    {
        try
        {
            _applicationScope?.Dispose();
            _applicationScope = null;

            if (_host is not null)
            {
                await _host.StopAsync(
                    TimeSpan.FromSeconds(5));

                _host.Dispose();
                _host = null;
            }
        }
        catch (Exception ex)
        {
            WriteDiagnosticLog(
                "Erro durante o encerramento",
                ex);

            _applicationScope?.Dispose();
            _host?.Dispose();
        }
        finally
        {
            base.OnExit(e);
        }
    }

    private static void ReportFatalError(
        string title,
        Exception exception)
    {
        WriteDiagnosticLog(title, exception);

        MessageBox.Show(
            exception.ToString(),
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private static void WriteDiagnosticLog(
        string title,
        Exception exception)
    {
        try
        {
            var logPath = Path.Combine(
                Path.GetTempPath(),
                "FamilyManagement-startup-error.log");

            var content =
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {title}"
                + Environment.NewLine
                + exception
                + Environment.NewLine
                + new string('-', 70)
                + Environment.NewLine;

            File.AppendAllText(logPath, content);
        }
        catch
        {
            // O diagnóstico não deve provocar outra exceção.
        }
    }
}