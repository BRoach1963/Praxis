using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Praxis.Data;
using Praxis.Services;
using Praxis.ViewModels;
using Praxis.Views;
using System.IO;
using System.Windows;

namespace Praxis;

/// <summary>
/// Praxis Application Entry Point
/// A calm, private workspace for reflective professional practice.
/// </summary>
public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                ConfigureServices(services);
            })
            .Build();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();
        services.AddSingleton<IConfiguration>(configuration);
        
        // Get connection string
        var connectionString = configuration.GetConnectionString("PraxisDb") 
            ?? "Host=localhost;Port=5432;Database=praxis;Username=postgres;Password=$teelers4Ever";
        
        // Data - PostgreSQL context with connection string
        services.AddDbContext<PraxisContext>(options => 
        {
            options.UseNpgsql(connectionString);
        });
        
        // Services
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IClientService, ClientService>();
        services.AddSingleton<ISessionService, SessionService>();
        
        // Authentication - Local PostgreSQL
        services.AddSingleton(sp => new AuthenticationService(connectionString));
        services.AddSingleton(sp => SessionManager.Instance);
        services.AddSingleton<TokenEncryptionService>();
        
        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<ClientListViewModel>();
        services.AddTransient<ClientDetailViewModel>();
        services.AddTransient<SessionPrepViewModel>();
        services.AddTransient<SessionReflectViewModel>();
        
        // Views
        services.AddSingleton<MainWindow>();
        services.AddTransient<LoginDialog>();
    }

    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        await _host.StartAsync();

        // Test PostgreSQL connection
        try
        {
            using var scope = _host.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PraxisContext>();
            var canConnect = await dbContext.Database.CanConnectAsync();
            if (!canConnect)
            {
                throw new Exception("Cannot connect to PostgreSQL database.");
            }
            System.Diagnostics.Debug.WriteLine("✓ PostgreSQL database connected successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"✗ Database connection error: {ex.Message}");
            MessageBox.Show($"Database connection error: {ex.Message}\n\nPlease ensure PostgreSQL is running and the 'praxis' database exists.", "Connection Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        // Check for existing session
        var sessionManager = _host.Services.GetRequiredService<SessionManager>();
        
        if (sessionManager.IsAuthenticated)
        {
            // Session exists - go straight to main window
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
        else
        {
            // No session - show login dialog
            var loginDialog = _host.Services.GetRequiredService<LoginDialog>();
            bool? result = loginDialog.ShowDialog();
            
            if (result == true)
            {
                // Login successful
                try
                {
                    System.Diagnostics.Debug.WriteLine("LOGIN SUCCESS - Creating MainWindow...");
                    var mainWindow = _host.Services.GetRequiredService<MainWindow>();
                    System.Diagnostics.Debug.WriteLine("MainWindow created - Showing...");
                    
                    // Set as main window and change shutdown mode
                    Application.Current.MainWindow = mainWindow;
                    Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
                    
                    mainWindow.Show();
                    System.Diagnostics.Debug.WriteLine("MainWindow shown successfully");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening main window: {ex.Message}\n\n{ex.StackTrace}", "Praxis Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Shutdown();
                }
            }
            else
            {
                // Login cancelled
                Shutdown();
            }
        }
    }

    private async void Application_Exit(object sender, ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
    }

    /// <summary>
    /// Gets the current application's service provider.
    /// </summary>
    public static IServiceProvider Services => ((App)Current)._host.Services;
}
