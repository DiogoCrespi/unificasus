using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Windows;
using UnificaSUS.Core.Interfaces;
using UnificaSUS.Infrastructure.Data;
using UnificaSUS.Infrastructure.Repositories;
using ApplicationService = UnificaSUS.Application.Services;

namespace UnificaSUS.WPF;

public partial class App : System.Windows.Application
{
    private IHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            // Configurar Host e Dependency Injection
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Configuração
                    services.AddSingleton<IConfigurationReader, ConfigurationReader>();

                    // Data Access
                    services.AddScoped<FirebirdContext>();
                    services.AddScoped<IProcedimentoRepository, ProcedimentoRepository>();
                    services.AddScoped<ICompetenciaRepository, CompetenciaRepository>();
                    services.AddScoped<IGrupoRepository, GrupoRepository>();
                    services.AddScoped<IProcedimentoComumRepository, ProcedimentoComumRepository>();
                    services.AddScoped<IRelatorioRepository, RelatorioRepository>();

                    // Application Services
                    services.AddScoped<ApplicationService.ProcedimentoService>();
                    services.AddScoped<ApplicationService.CompetenciaService>();
                    services.AddScoped<ApplicationService.GrupoService>();
                    services.AddScoped<ApplicationService.ProcedimentoComumService>();
                    services.AddScoped<ApplicationService.RelatorioService>();

                    // UI - MainWindow precisa de IConfigurationReader
                    services.AddTransient<MainWindow>();
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Information);
                })
                .Build();

            // Inicializar e mostrar MainWindow
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Erro ao iniciar a aplicação:\n\n{ex.Message}\n\nDetalhes:\n{ex.StackTrace}",
                "Erro Fatal",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            
            Shutdown();
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        base.OnExit(e);
    }
}

