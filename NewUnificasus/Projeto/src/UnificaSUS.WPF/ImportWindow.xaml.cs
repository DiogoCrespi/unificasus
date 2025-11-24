using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using UnificaSUS.Application.Services.Import;
using UnificaSUS.Core.Import;
using UnificaSUS.Core.Interfaces;
using UnificaSUS.Infrastructure.Data;
using UnificaSUS.Infrastructure.Repositories;

namespace UnificaSUS.WPF;

/// <summary>
/// Janela para importação de dados SIGTAP
/// </summary>
public partial class ImportWindow : Window
{
    private string? _selectedPath;
    private bool _isImporting;
    private CancellationTokenSource? _cancellationTokenSource;
    private ImportService? _importService;
    private readonly FirebirdContext _context;
    private readonly IConfigurationReader _configurationReader;
    private readonly ObservableCollection<TableStatusItem> _tableStatuses = new();

    /// <summary>
    /// Indica se a importação foi concluída com sucesso (pelo menos uma tabela importada)
    /// </summary>
    public bool ImportacaoConcluidaComSucesso { get; private set; }

    public ImportWindow(FirebirdContext context, IConfigurationReader configurationReader)
    {
        InitializeComponent();
        _context = context;
        _configurationReader = configurationReader;
        DataContext = this;
        lstTabelas.ItemsSource = _tableStatuses;
    }

    private void BtnSelecionarZip_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Arquivos ZIP (*.zip)|*.zip|Todos os arquivos (*.*)|*.*",
            Title = "Selecionar arquivo ZIP SIGTAP"
        };

        if (dialog.ShowDialog() == true)
        {
            _selectedPath = dialog.FileName;
            txtCaminhoArquivo.Text = dialog.FileName;
            AdicionarLog($"[INFO] Arquivo selecionado: {Path.GetFileName(dialog.FileName)}");
        }
    }

    private void BtnSelecionarPasta_Click(object sender, RoutedEventArgs e)
    {
        // Usa OpenFileDialog como alternativa ao FolderBrowserDialog
        var dialog = new OpenFileDialog
        {
            Title = "Selecionar pasta com arquivos SIGTAP descompactados",
            ValidateNames = false,
            CheckFileExists = false,
            CheckPathExists = true,
            FileName = "Selecione uma pasta"
        };

        if (dialog.ShowDialog() == true)
        {
            var selectedPath = Path.GetDirectoryName(dialog.FileName);
            if (!string.IsNullOrEmpty(selectedPath))
            {
                _selectedPath = selectedPath;
                txtCaminhoArquivo.Text = selectedPath;
                AdicionarLog($"[INFO] Pasta selecionada: {Path.GetFileName(selectedPath)}");
            }
        }
    }

    private async void BtnIniciar_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedPath))
        {
            MessageBox.Show("Selecione um arquivo ZIP ou pasta antes de iniciar.", "Aviso",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!File.Exists(_selectedPath) && !Directory.Exists(_selectedPath))
        {
            MessageBox.Show("Arquivo ou pasta não encontrado.", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            _isImporting = true;
            btnIniciar.IsEnabled = false;
            btnCancelar.IsEnabled = true;
            _tableStatuses.Clear();
            txtLogs.Clear();

            await IniciarImportacaoAsync();
        }
        catch (Exception ex)
        {
            AdicionarLog($"[ERRO] {ex.Message}");
            MessageBox.Show($"Erro durante importação:\n\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _isImporting = false;
            btnIniciar.IsEnabled = true;
            btnCancelar.IsEnabled = false;
        }
    }

    private async Task IniciarImportacaoAsync()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;

        AdicionarLog("[INFO] Iniciando importação...");
        txtStatusGeral.Text = "Inicializando...";

        // Variáveis para registro de competência mesmo em caso de cancelamento
        string? competenciaParaRegistrar = null;
        List<ImportResult>? resultadosParciais = null;

        try
        {
            // Determina o diretório de trabalho
            string workingDirectory;

            if (File.Exists(_selectedPath) && Path.GetExtension(_selectedPath).ToLower() == ".zip")
            {
                // É um ZIP, precisa extrair
                AdicionarLog("[INFO] Extraindo arquivo ZIP...");
                var extractor = new SigtapFileExtractor(null); // SigtapFileExtractor aceita ILogger opcional
                workingDirectory = extractor.ExtractZipFile(_selectedPath);
                AdicionarLog($"[INFO] Arquivos extraídos para: {workingDirectory}");
            }
            else
            {
                // Já é uma pasta
                workingDirectory = _selectedPath!;
            }

            // Valida diretório
            var validator = new SigtapFileExtractor(null);
            if (!validator.IsValidSigtapDirectory(workingDirectory))
            {
                throw new InvalidOperationException("O diretório selecionado não contém arquivos SIGTAP válidos.");
            }

            // Obtém informações do arquivo
            var info = validator.GetFileInfo(workingDirectory);
            competenciaParaRegistrar = info.Competencia; // Guarda para usar mesmo se cancelar
            AdicionarLog($"[INFO] Versão: {info.Version ?? "desconhecida"}");
            AdicionarLog($"[INFO] Competência: {info.Competencia?? "desconhecida"}");
            AdicionarLog($"[INFO] Total de tabelas: {info.LayoutFileCount}");
            
            // IMPORTANTE: Registra a competência IMEDIATAMENTE após identificá-la
            // Isso garante que ela apareça na listagem mesmo se a importação for cancelada
            if (!string.IsNullOrEmpty(competenciaParaRegistrar))
            {
                try
                {
                    AdicionarLog($"[INFO] Registrando competência {competenciaParaRegistrar} na TB_COMPETENCIA_ATIVA...");
                    var competenciaLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<CompetenciaRepository>.Instance;
                    var competenciaRepository = new CompetenciaRepository(_context, competenciaLogger);
                    // Usa um token de cancelamento separado para não ser afetado pelo cancelamento da importação
                    var registroToken = new CancellationTokenSource().Token;
                    var registrado = await competenciaRepository.RegistrarCompetenciaAsync(competenciaParaRegistrar, registroToken);
                    if (registrado)
                    {
                        AdicionarLog($"[OK] Competência {competenciaParaRegistrar} registrada com sucesso!");
                    }
                    else
                    {
                        AdicionarLog($"[AVISO] Não foi possível registrar competência {competenciaParaRegistrar}");
                    }
                }
                catch (Exception ex)
                {
                    AdicionarLog($"[AVISO] Erro ao registrar competência no início: {ex.Message}");
                    // Não falha a importação - continua normalmente
                }
            }

            // Cria serviço de importação
            var logger = new WindowLoggerAdapter(this);
            
            // Cria ImportRepository com FirebirdContext real
            var importRepository = new ImportRepository(_context, logger);
            
            // Cria ImportService com repository
            _importService = new ImportService(logger, importRepository);

            // Configura progresso
            var progress = new Progress<ImportProgress>(ReportarProgresso);

            // Inicia importação
            AdicionarLog("[INFO] Iniciando processamento de tabelas...");
            var results = await _importService.ImportAllTablesAsync(
                workingDirectory,
                progress,
                cancellationToken);

            // Guarda resultados para usar mesmo se cancelar depois
            resultadosParciais = results;

            // Exibe resultado final
            var totalTabelas = results.Count;
            var tabelasSucesso = results.Count(r => r.Success);
            var totalRegistros = results.Sum(r => r.SuccessCount);
            var totalErros = results.Sum(r => r.ErrorCount);

            // Marca que a importação foi concluída com sucesso se pelo menos uma tabela foi importada
            ImportacaoConcluidaComSucesso = tabelasSucesso > 0;

            txtStatusGeral.Text = "Importação concluída!";
            txtPercentual.Text = "100%";
            progressBarGeral.Value = 100;

            AdicionarLog("");
            AdicionarLog("=== IMPORTAÇÃO CONCLUÍDA ===");
            AdicionarLog($"Tabelas processadas: {totalTabelas}");
            AdicionarLog($"Tabelas com sucesso: {tabelasSucesso}");
            AdicionarLog($"Registros importados: {totalRegistros}");
            AdicionarLog($"Registros com erro: {totalErros}");

            // Registra a competência na TB_COMPETENCIA_ATIVA se a importação foi bem-sucedida
            if (tabelasSucesso > 0 && !string.IsNullOrEmpty(info.Competencia))
            {
                try
                {
                    AdicionarLog($"[INFO] Registrando competência {info.Competencia} na TB_COMPETENCIA_ATIVA...");
                    // Cria um logger simples usando NullLogger
                    var competenciaLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<CompetenciaRepository>.Instance;
                    var competenciaRepository = new CompetenciaRepository(_context, competenciaLogger);
                    await competenciaRepository.RegistrarCompetenciaAsync(info.Competencia, cancellationToken);
                    AdicionarLog($"[OK] Competência {info.Competencia} registrada com sucesso!");
                }
                catch (Exception ex)
                {
                    AdicionarLog($"[AVISO] Erro ao registrar competência: {ex.Message}");
                    // Não falha a importação se houver erro ao registrar competência
                }
            }

            MessageBox.Show(
                $"Importação concluída!\n\n" +
                $"Tabelas: {tabelasSucesso}/{totalTabelas}\n" +
                $"Registros: {totalRegistros}\n" +
                $"Erros: {totalErros}",
                "Sucesso",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (OperationCanceledException)
        {
            AdicionarLog("[AVISO] Importação cancelada pelo usuário");
            txtStatusGeral.Text = "Cancelado";
            
            // IMPORTANTE: Registra a competência mesmo quando cancelada
            // Registra SEMPRE que houver uma competência identificada, mesmo sem progresso
            // Isso garante que a competência apareça na listagem para o usuário poder ativar depois
            if (!string.IsNullOrEmpty(competenciaParaRegistrar))
            {
                try
                {
                    var tabelasSucesso = resultadosParciais?.Count(r => r.Success) ?? 0;
                    var totalRegistros = resultadosParciais?.Sum(r => r.SuccessCount) ?? 0;
                    
                    if (tabelasSucesso > 0 || totalRegistros > 0)
                    {
                        AdicionarLog($"[INFO] Registrando competência {competenciaParaRegistrar} na TB_COMPETENCIA_ATIVA (importação cancelada, mas houve progresso)...");
                    }
                    else
                    {
                        AdicionarLog($"[INFO] Registrando competência {competenciaParaRegistrar} na TB_COMPETENCIA_ATIVA (importação cancelada sem progresso, mas competência identificada)...");
                    }
                    
                    var competenciaLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<CompetenciaRepository>.Instance;
                    var competenciaRepository = new CompetenciaRepository(_context, competenciaLogger);
                    // Usa um novo token de cancelamento para o registro (não cancela esta operação)
                    var registroToken = new CancellationTokenSource().Token;
                    await competenciaRepository.RegistrarCompetenciaAsync(competenciaParaRegistrar, registroToken);
                    AdicionarLog($"[OK] Competência {competenciaParaRegistrar} registrada com sucesso!");
                }
                catch (Exception ex)
                {
                    AdicionarLog($"[AVISO] Erro ao registrar competência após cancelamento: {ex.Message}");
                    AdicionarLog($"[AVISO] Stack trace: {ex.StackTrace}");
                }
            }
            
            MessageBox.Show("Importação cancelada.", "Aviso",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ReportarProgresso(ImportProgress progress)
    {
        Dispatcher.Invoke(() =>
        {
            // Atualiza status geral
            txtTabelaAtual.Text = progress.TableName;
            txtStatusGeral.Text = progress.StatusMessage ?? "Processando...";
            txtPercentual.Text = $"{progress.PercentComplete:F1}%";
            progressBarGeral.Value = progress.PercentComplete;

            // Atualiza contadores
            txtContadorSucesso.Text = progress.SuccessCount.ToString();
            txtContadorErros.Text = progress.ErrorCount.ToString();

            // Atualiza ou adiciona status da tabela
            var existingStatus = _tableStatuses.FirstOrDefault(t => t.TableName == progress.TableName);
            if (existingStatus != null)
            {
                existingStatus.Status = $"{progress.ProcessedLines}/{progress.TotalLines}";
                existingStatus.StatusIcon = progress.ProcessedLines >= progress.TotalLines ? "✓" : "⚙";
            }
            else
            {
                _tableStatuses.Add(new TableStatusItem
                {
                    TableName = progress.TableName,
                    Status = $"{progress.ProcessedLines}/{progress.TotalLines}",
                    StatusIcon = "⚙"
                });
            }

            // Auto-scroll logs
            txtLogs.ScrollToEnd();
        });
    }

    public void AdicionarLog(string mensagem)
    {
        Dispatcher.Invoke(() =>
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            txtLogs.AppendText($"[{timestamp}] {mensagem}\n");
            txtLogs.ScrollToEnd();
        });
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        if (_isImporting && _cancellationTokenSource != null)
        {
            var result = MessageBox.Show(
                "Deseja realmente cancelar a importação?",
                "Confirmar Cancelamento",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                AdicionarLog("[AVISO] Solicitando cancelamento...");
                _cancellationTokenSource.Cancel();
                btnCancelar.IsEnabled = false;
            }
        }
    }

    private void BtnFechar_Click(object sender, RoutedEventArgs e)
    {
        if (_isImporting)
        {
            var result = MessageBox.Show(
                "Importação em andamento. Deseja realmente fechar?",
                "Confirmar",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.No)
                return;

            _cancellationTokenSource?.Cancel();
        }

        Close();
    }

    private void BtnSalvarLogs_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Arquivos de texto (*.txt)|*.txt|Todos os arquivos (*.*)|*.*",
            FileName = $"ImportLog_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                File.WriteAllText(dialog.FileName, txtLogs.Text);
                MessageBox.Show("Logs salvos com sucesso!", "Sucesso",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar logs:\n\n{ex.Message}", "Erro",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

/// <summary>
/// Item de status de uma tabela na lista
/// </summary>
public class TableStatusItem : System.ComponentModel.INotifyPropertyChanged
{
    private string _tableName = string.Empty;
    private string _status = string.Empty;
    private string _statusIcon = "⏸";

    public string TableName
    {
        get => _tableName;
        set
        {
            _tableName = value;
            OnPropertyChanged(nameof(TableName));
        }
    }

    public string Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged(nameof(Status));
        }
    }

    public string StatusIcon
    {
        get => _statusIcon;
        set
        {
            _statusIcon = value;
            OnPropertyChanged(nameof(StatusIcon));
        }
    }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Adaptador para converter ImportWindow.AdicionarLog para Microsoft.Extensions.Logging.ILogger
/// </summary>
public class WindowLoggerAdapter : Microsoft.Extensions.Logging.ILogger
{
    private readonly ImportWindow _window;

    public WindowLoggerAdapter(ImportWindow window)
    {
        _window = window;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

    public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        var prefix = logLevel switch
        {
            Microsoft.Extensions.Logging.LogLevel.Information => "[INFO]",
            Microsoft.Extensions.Logging.LogLevel.Warning => "[WARN]",
            Microsoft.Extensions.Logging.LogLevel.Error => "[ERROR]",
            Microsoft.Extensions.Logging.LogLevel.Debug => "[DEBUG]",
            _ => "[LOG]"
        };
        _window.AdicionarLog($"{prefix} {message}");
    }
}
