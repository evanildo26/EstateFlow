using EstateFlow.Desktop.Mvvm;
using EstateFlow.Desktop.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace EstateFlow.Desktop;

public partial class App : Application
{
    private const string DefaultApiBaseUrl =
        "https://estateflow-hmbmeqd3brdnfab6.uaenorth-01.azurewebsites.net/";

    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // ==========================================
        // GLOBAL ERROR HANDLING
        // An unexpected error shows a message and is logged, instead of
        // closing the app and losing the controller's work.
        // ==========================================

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            ErrorReporter.Log((Exception)args.ExceptionObject, "Fatal");

        // Remove documents left behind by a previous session.
        TempFiles.CleanUp();

        try
        {
            // ==========================================
            // CONFIGURATION
            // appsettings.json next to the .exe, overridable with
            // ESTATEFLOW_Api__BaseUrl for local development.
            // ==========================================

            var configuration =
                new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddEnvironmentVariables("ESTATEFLOW_")
                    .Build();

            var apiBaseUrl =
                configuration["Api:BaseUrl"] ?? DefaultApiBaseUrl;

            if (!apiBaseUrl.EndsWith('/'))
                apiBaseUrl += "/";

            var timeoutSeconds =
                int.TryParse(configuration["Api:TimeoutSeconds"], out var seconds) && seconds > 0
                    ? seconds
                    : 60;

            var services = new ServiceCollection();

            // ==========================================
            // AUTHENTICATION
            // ==========================================

            services.AddSingleton<IAuthState, AuthState>();
            services.AddTransient<AuthTokenHandler>();

            // ==========================================
            // API CLIENT
            // ==========================================

            services.AddHttpClient<IApiClient, ApiClient>(client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            })
            .AddHttpMessageHandler<AuthTokenHandler>();

            // ==========================================
            // VIEW MODELS
            // ==========================================

            services.AddTransient<LoginViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<EstateDetailViewModel>();
            services.AddTransient<DiaryViewModel>();
            services.AddTransient<ReportsViewModel>();
            services.AddTransient<DocumentScanViewModel>();
            services.AddTransient<EstateFormViewModel>();
            services.AddTransient<AdminViewModel>();
            services.AddTransient<SecurityViewModel>();

            // ==========================================
            // SHELL / NAVIGATION
            // ==========================================

            services.AddSingleton<ShellViewModel>();

            services.AddSingleton<INavigationService>(sp =>
                sp.GetRequiredService<ShellViewModel>());

            services.AddSingleton<IShellState>(sp =>
                sp.GetRequiredService<ShellViewModel>());

            // ==========================================
            // MAIN WINDOW
            // ==========================================

            services.AddSingleton<MainWindow>();

            // ==========================================
            // BUILD DI CONTAINER
            // ==========================================

            _serviceProvider = services.BuildServiceProvider();

            // IMPORTANT:
            // ShellViewModel must finish constructing BEFORE
            // LoginViewModel is resolved.
            var shell =
                _serviceProvider.GetRequiredService<ShellViewModel>();

            // An expired or revoked session returns the user to sign-in.
            ErrorReporter.SessionExpired = () =>
                Dispatcher.BeginInvoke(() =>
                {
                    if (shell.IsLoggedIn)
                        shell.SignOut();
                });

            // Initial navigation happens HERE,
            // not inside ShellViewModel's constructor.
            shell.NavigateToLogin();

            // ==========================================
            // SHOW MAIN WINDOW
            // ==========================================

            var mainWindow =
                _serviceProvider.GetRequiredService<MainWindow>();

            MainWindow = mainWindow;

            mainWindow.Show();
            mainWindow.Activate();
        }
        catch (Exception ex)
        {
            ErrorReporter.Log(ex, "Startup");

            MessageBox.Show(
                $"EstateFlow could not start.\n\n{ex.Message}",
                "EstateFlow — Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown();
        }
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ErrorReporter.Report(e.Exception, "Unhandled UI exception");
        e.Handled = true;
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        ErrorReporter.Log(e.Exception, "Unobserved task exception");
        e.SetObserved();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        TempFiles.CleanUp();

        _serviceProvider?.Dispose();

        base.OnExit(e);
    }
}
