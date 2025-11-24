using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text;
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

        // Registra o provider de code pages para suportar Windows-1252 e outros encodings
        // Usa reflection para evitar dependência direta do namespace
        try
        {
            var codePagesType = Type.GetType("System.Text.Encoding.CodePages.CodePagesEncodingProvider, System.Text.Encoding.CodePages");
            if (codePagesType != null)
            {
                var instanceProperty = codePagesType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (instanceProperty != null)
                {
                    var instance = instanceProperty.GetValue(null);
                    if (instance != null && instance is EncodingProvider provider)
                    {
                        Encoding.RegisterProvider(provider);
                    }
                }
            }
        }
        catch
        {
            // Já registrado ou não disponível - continua normalmente
        }

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
                    services.AddScoped<IServicoClassificacaoRepository, ServicoClassificacaoRepository>();

                    // Application Services
                    services.AddScoped<ApplicationService.ProcedimentoService>();
                    services.AddScoped<ApplicationService.CompetenciaService>();
                    services.AddScoped<ApplicationService.GrupoService>();
                    services.AddScoped<ApplicationService.ProcedimentoComumService>();
                    services.AddScoped<ApplicationService.RelatorioService>();
                    services.AddScoped<ApplicationService.ServicoClassificacaoService>();

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

