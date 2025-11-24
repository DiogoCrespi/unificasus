using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using UnificaSUS.Core.Import;
using UnificaSUS.Infrastructure.Import;
using UnificaSUS.Infrastructure.Repositories;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace UnificaSUS.Application.Services.Import;

/// <summary>
/// Serviço principal de importação de dados SIGTAP
/// Coordena todo o processo de importação com resiliência e progresso em tempo real
/// </summary>
public class ImportService
{
    private readonly LayoutParser _layoutParser;
    private readonly FixedWidthParser _fixedWidthParser;
    private readonly DataValidator _dataValidator;
    private readonly EncodingDetector _encodingDetector;
    private readonly ImportRepository? _importRepository;
    private readonly ILogger? _logger;

    public ImportService(ILogger? logger = null, ImportRepository? importRepository = null)
    {
        _logger = logger;
        _importRepository = importRepository;
        
        // Adaptador para converter Microsoft.Extensions.Logging.ILogger para ILogger customizado
        Infrastructure.Import.ILogger? customLogger = logger != null 
            ? new LoggerAdapter(logger) 
            : null;
        
        _layoutParser = new LayoutParser(customLogger);
        _fixedWidthParser = new FixedWidthParser(customLogger);
        _dataValidator = new DataValidator(customLogger);
        _encodingDetector = new EncodingDetector(customLogger);
    }
    
    /// <summary>
    /// Adaptador para converter Microsoft.Extensions.Logging.ILogger para ILogger customizado
    /// </summary>
    private class LoggerAdapter : Infrastructure.Import.ILogger
    {
        private readonly Microsoft.Extensions.Logging.ILogger _logger;
        
        public LoggerAdapter(Microsoft.Extensions.Logging.ILogger logger)
        {
            _logger = logger;
        }
        
        public void LogInformation(string message) => _logger.LogInformation(message);
        public void LogWarning(string message) => _logger.LogWarning(message);
        public void LogError(string message) => _logger.LogError(message);
        public void LogDebug(string message) => _logger.LogDebug(message);
    }

    /// <summary>
    /// Importa todas as tabelas de um diretório SIGTAP
    /// </summary>
    /// <param name="sigtapDirectory">Diretório com arquivos descompactados</param>
    /// <param name="progress">Reporter de progresso (opcional)</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Lista de resultados de importação</returns>
    public async Task<List<ImportResult>> ImportAllTablesAsync(
        string sigtapDirectory,
        IProgress<ImportProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var results = new List<ImportResult>();

        try
        {
            _logger?.LogInformation($"Iniciando importação de {sigtapDirectory}");

            // Descobre todas as tabelas disponíveis
            var tableMetadataList = DiscoverTables(sigtapDirectory);
            
            _logger?.LogInformation($"Encontradas {tableMetadataList.Count} tabelas para importar");

            // Verifica existência de tabelas no banco e cria/adiciona colunas automaticamente se necessário
            if (_importRepository != null)
            {
                _logger?.LogInformation("Verificando e preparando tabelas (criando tabelas e adicionando colunas faltantes)...");
                
                // Para TODAS as tabelas, verificar e criar/adicionar colunas faltantes
                // CreateTableAsync já faz isso: cria se não existe, adiciona colunas se existe
                foreach (var metadata in tableMetadataList)
                {
                    try
                    {
                        await _importRepository.CreateTableAsync(metadata, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning($"Erro ao verificar/criar tabela {metadata.TableName}: {ex.Message}");
                        // Continua com próxima tabela - não bloqueia importação
                    }
                }
                
                _logger?.LogInformation("Verificação de tabelas concluída.");
            }

            // Ordena por prioridade de importação
            var sortedTables = tableMetadataList.OrderBy(t => t.ImportPriority).ToList();

            // Importa cada tabela
            int tableIndex = 0;
            foreach (var metadata in sortedTables)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger?.LogWarning("Importação cancelada pelo usuário");
                    break;
                }

                tableIndex++;
                _logger?.LogInformation($"[{tableIndex}/{sortedTables.Count}] Importando {metadata.TableName}...");

                // Reporta progresso
                progress?.Report(new ImportProgress
                {
                    TableName = metadata.TableName,
                    ProcessedLines = 0,
                    TotalLines = 0,
                    StatusMessage = $"Iniciando importação de {metadata.TableName}..."
                });

                try
                {
                    string dataFilePath = Path.Combine(sigtapDirectory, metadata.DataFileName);
                    var result = await ImportTableAsync(metadata, dataFilePath, progress, cancellationToken);
                    results.Add(result);

                    if (result.Success)
                    {
                        _logger?.LogInformation($"✓ {metadata.TableName}: {result.SuccessCount} registros importados");
                    }
                    else
                    {
                        _logger?.LogError($"✗ {metadata.TableName}: {result.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    var errorCategory = ImportErrorCategorizer.CategorizeError(ex);
                    var categoryDesc = ImportErrorCategorizer.GetCategoryDescription(errorCategory);
                    var suggestion = ImportErrorCategorizer.GetCorrectionSuggestion(errorCategory);
                    
                    _logger?.LogError(
                        $"Erro ao importar {metadata.TableName} [{categoryDesc}]: {ex.Message}. " +
                        $"Sugestão: {suggestion}");
                    
                    results.Add(new ImportResult
                    {
                        TableName = metadata.TableName,
                        Success = false,
                        ErrorMessage = ex.Message,
                        ErrorCategory = errorCategory
                    });
                }
            }

            stopwatch.Stop();
            _logger?.LogInformation($"Importação concluída em {stopwatch.Elapsed:mm\\:ss}");
        }
        catch (Exception ex)
        {
            var errorCategory = ImportErrorCategorizer.CategorizeError(ex);
            var categoryDesc = ImportErrorCategorizer.GetCategoryDescription(errorCategory);
            var suggestion = ImportErrorCategorizer.GetCorrectionSuggestion(errorCategory);
            
            _logger?.LogError(
                $"Erro fatal na importação [{categoryDesc}]: {ex.Message}. " +
                $"Sugestão: {suggestion}");
            throw;
        }

        return results;
    }

    /// <summary>
    /// Importa uma tabela específica
    /// </summary>
    public async Task<ImportResult> ImportTableAsync(
        ImportTableMetadata metadata,
        string dataFilePath,
        IProgress<ImportProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new ImportResult
        {
            TableName = metadata.TableName,
            Success = false,
            ErrorsByCategory = new Dictionary<ImportErrorCategory, int>()
        };

        try
        {
            if (!File.Exists(dataFilePath))
            {
                result.ErrorMessage = $"Arquivo não encontrado: {dataFilePath}";
                return result;
            }

            // Detecta encoding
            var encoding = _encodingDetector.DetectEncoding(dataFilePath);
            _logger?.LogDebug($"Encoding detectado: {encoding.WebName}");

            // Lê arquivo
            var lines = File.ReadAllLines(dataFilePath, encoding);
            int totalLines = lines.Length;

            if (totalLines == 0)
            {
                result.ErrorMessage = "Arquivo vazio";
                return result;
            }

            _logger?.LogInformation($"Processando {totalLines} linhas de {metadata.TableName}...");

            int successCount = 0;
            int errorCount = 0;

            // Processa cada linha
            for (int i = 0; i < totalLines; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    result.ErrorMessage = "Importação cancelada";
                    break;
                }

                var line = lines[i];

                try
                {
                    // Corrige encoding se necessário (detecta e corrige caracteres corrompidos)
                    line = FixEncodingIfCorrupted(line, encoding);
                    
                    // Parse da linha
                    var data = _fixedWidthParser.ParseLine(line, metadata);

                    // Valida dados
                    var validationResult = _dataValidator.Validate(data, metadata);

                    if (validationResult.IsValid)
                    {
                        // Insere no banco de dados (se repository disponível)
                        if (_importRepository != null)
                        {
                            try
                            {
                                await _importRepository.InsertOrUpdateAsync(
                                    metadata.TableName,
                                    data,
                                    metadata,
                                    Infrastructure.Repositories.DuplicateHandlingMode.Update,
                                    cancellationToken);
                                successCount++;
                            }
                            catch (Exception insertEx)
                            {
                                var errorCategory = ImportErrorCategorizer.CategorizeError(insertEx);
                                var categoryDesc = ImportErrorCategorizer.GetCategoryDescription(errorCategory);
                                
                                // Incrementa contador por categoria
                                if (!result.ErrorsByCategory.ContainsKey(errorCategory))
                                    result.ErrorsByCategory[errorCategory] = 0;
                                result.ErrorsByCategory[errorCategory]++;
                                
                                _logger?.LogError(
                                    $"Linha {i + 1}: Erro ao inserir no banco [{categoryDesc}]: {insertEx.Message}");
                                errorCount++;
                            }
                        }
                        else
                        {
                            // Modo simulação (sem banco)
                            successCount++;
                        }
                    }
                    else
                    {
                        _logger?.LogWarning($"Linha {i + 1}: {validationResult.ErrorMessage}");
                        result.Warnings.Add($"Linha {i + 1}: {validationResult.ErrorMessage}");
                        errorCount++;
                    }

                    // Adiciona avisos
                    foreach (var warning in validationResult.Warnings)
                    {
                        result.Warnings.Add($"Linha {i + 1}: {warning}");
                    }
                }
                catch (Exception ex)
                {
                    var errorCategory = ImportErrorCategorizer.CategorizeError(ex);
                    var categoryDesc = ImportErrorCategorizer.GetCategoryDescription(errorCategory);
                    
                    // Incrementa contador por categoria
                    if (!result.ErrorsByCategory.ContainsKey(errorCategory))
                        result.ErrorsByCategory[errorCategory] = 0;
                    result.ErrorsByCategory[errorCategory]++;
                    
                    _logger?.LogError(
                        $"Linha {i + 1}: Erro ao processar [{categoryDesc}]: {ex.Message}");
                    errorCount++;
                }

                // Reporta progresso a cada 100 linhas ou na última linha
                if ((i + 1) % 100 == 0 || i == totalLines - 1)
                {
                    progress?.Report(new ImportProgress
                    {
                        TableName = metadata.TableName,
                        ProcessedLines = i + 1,
                        TotalLines = totalLines,
                        SuccessCount = successCount,
                        ErrorCount = errorCount,
                        StatusMessage = $"Processando {metadata.TableName}... ({i + 1}/{totalLines})"
                    });
                }
            }

            stopwatch.Stop();

            result.Success = successCount > 0;
            result.SuccessCount = successCount;
            result.ErrorCount = errorCount;
            result.ElapsedTime = stopwatch.Elapsed;

            if (!cancellationToken.IsCancellationRequested)
            {
                _logger?.LogInformation($"✓ {metadata.TableName}: {successCount} sucessos, {errorCount} erros em {stopwatch.Elapsed:mm\\:ss}");
                
                // Log de erros por categoria se houver
                if (result.ErrorsByCategory.Any())
                {
                    var categorySummary = string.Join(", ", 
                        result.ErrorsByCategory.Select(kvp => 
                            $"{ImportErrorCategorizer.GetCategoryDescription(kvp.Key)}: {kvp.Value}"));
                    _logger?.LogInformation($"  Erros por categoria: {categorySummary}");
                }
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.ElapsedTime = stopwatch.Elapsed;
            _logger?.LogError($"Erro ao importar {metadata.TableName}: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Corrige encoding de uma linha se detectar caracteres corrompidos
    /// Detecta padrões como "ORIENTAÃ§ÃƒO" que indicam Windows-1252 lido como UTF-8/ISO-8859-1
    /// </summary>
    private string FixEncodingIfCorrupted(string line, Encoding detectedEncoding)
    {
        // Detecta padrões comuns de corrupção de encoding
        // Ex: "ORIENTAÃ§ÃƒO" indica que Windows-1252 foi lido como UTF-8 ou ISO-8859-1
        if (line.Contains("Ã§") || line.Contains("Ãƒ") || line.Contains("Ã¡") || 
            line.Contains("Ã©") || line.Contains("Ã­") || line.Contains("Ã³") || 
            line.Contains("Ãº") || line.Contains("Ã£") || line.Contains("Ãµ") ||
            line.Contains("Ã‰") || line.Contains("Ã") || line.Contains("Ã"))
        {
            try
            {
                // Se detectou corrupção e o encoding atual não é Windows-1252, tenta corrigir
                if (detectedEncoding.CodePage != 1252)
                {
                    // Tenta Windows-1252
                    var win1252 = Encoding.GetEncoding(1252);
                    // Converte a string atual para bytes e tenta decodificar como Windows-1252
                    byte[] bytes = detectedEncoding.GetBytes(line);
                    string corrected = win1252.GetString(bytes);
                    
                    // Verifica se a correção melhorou (não tem mais padrões de corrupção)
                    if (!corrected.Contains("Ã§") && !corrected.Contains("Ãƒ") && 
                        !corrected.Contains("Ã¡") && !corrected.Contains("Ã©"))
                    {
                        _logger?.LogDebug($"Encoding corrigido de {detectedEncoding.WebName} para Windows-1252");
                        return corrected;
                    }
                }
                else
                {
                    // Se já está em Windows-1252 mas ainda tem corrupção, pode ser o contrário
                    // Tenta ISO-8859-1
                    try
                    {
                        var iso8859 = Encoding.GetEncoding("ISO-8859-1");
                        byte[] bytes = detectedEncoding.GetBytes(line);
                        string corrected = iso8859.GetString(bytes);
                        
                        if (!corrected.Contains("Ã§") && !corrected.Contains("Ãƒ"))
                        {
                            _logger?.LogDebug($"Encoding corrigido de {detectedEncoding.WebName} para ISO-8859-1");
                            return corrected;
                        }
                    }
                    catch { }
                }
            }
            catch
            {
                // Se falhar, retorna original
            }
        }
        
        return line;
    }

    /// <summary>
    /// Descobre todas as tabelas disponíveis no diretório SIGTAP
    /// </summary>
    private List<ImportTableMetadata> DiscoverTables(string sigtapDirectory)
    {
        var tables = new List<ImportTableMetadata>();

        try
        {
            // Procura por arquivos de layout (*_layout.txt)
            var layoutFiles = Directory.GetFiles(sigtapDirectory, "*_layout.txt");

            foreach (var layoutFile in layoutFiles)
            {
                try
                {
                    // Extrai nome da tabela do arquivo de layout
                    // Ex: tb_grupo_layout.txt -> tb_grupo
                    var fileName = Path.GetFileNameWithoutExtension(layoutFile); // tb_grupo_layout
                    var tableName = fileName.Replace("_layout", "").ToUpper(); // TB_GRUPO

                    // Nome do arquivo de dados
                    var dataFileName = fileName.Replace("_layout", "") + ".txt";
                    var dataFilePath = Path.Combine(sigtapDirectory, dataFileName);

                    // Verifica se arquivo de dados existe
                    if (!File.Exists(dataFilePath))
                    {
                        _logger?.LogWarning($"Arquivo de dados não encontrado para {tableName}: {dataFileName}");
                        continue;
                    }

                    // Cria metadata
                    var metadata = _layoutParser.CreateTableMetadata(
                        tableName,
                        dataFilePath,
                        layoutFile,
                        priority: GetTablePriority(tableName)
                    );

                    metadata.IsRequired = IsRequiredTable(tableName);

                    tables.Add(metadata);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning($"Erro ao processar layout {Path.GetFileName(layoutFile)}: {ex.Message}");
                }
            }

            _logger?.LogInformation($"Descobertas {tables.Count} tabelas no diretório");
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Erro ao descobrir tabelas: {ex.Message}");
        }

        return tables;
    }

    /// <summary>
    /// Define prioridade de importação baseada no nome da tabela
    /// Tabelas base devem ser importadas primeiro (menor prioridade numérica)
    /// </summary>
    private int GetTablePriority(string tableName)
    {
        // Prioridade 1: Tabelas de domínio base
        if (tableName.StartsWith("TB_FINANCIAMENTO")) return 1;
        if (tableName.StartsWith("TB_RUBRICA")) return 1;
        if (tableName.StartsWith("TB_DETALHE") && !tableName.Contains("DESCRICAO")) return 1;
        if (tableName.StartsWith("TB_REGISTRO")) return 1;
        if (tableName.StartsWith("TB_MODALIDADE")) return 1;
        if (tableName.StartsWith("TB_TIPO_LEITO")) return 1;

        // Prioridade 2: Estrutura hierárquica e outras tabelas base
        if (tableName == "TB_GRUPO") return 2;
        if (tableName.StartsWith("TB_CID")) return 2;
        if (tableName.StartsWith("TB_OCUPACAO")) return 2;
        if (tableName.StartsWith("TB_SERVICO") && !tableName.Contains("CLASSIFICACAO")) return 2;
        if (tableName.StartsWith("TB_HABILITACAO")) return 2;

        // Prioridade 3: Segundo nível da hierarquia
        if (tableName == "TB_SUB_GRUPO") return 3;
        if (tableName.StartsWith("TB_SERVICO_CLASSIFICACAO")) return 3;
        if (tableName.StartsWith("TB_GRUPO_HABILITACAO")) return 3;

        // Prioridade 4: Terceiro nível
        if (tableName == "TB_FORMA_ORGANIZACAO") return 4;

        // Prioridade 5: Procedimento (depende de tudo acima)
        if (tableName == "TB_PROCEDIMENTO") return 5;
        if (tableName == "TB_DESCRICAO") return 5;

        // Prioridade 6: Relacionamentos (dependem de procedimento)
        if (tableName.StartsWith("RL_")) return 6;

        // Prioridade 7: Outras tabelas
        return 7;
    }

    /// <summary>
    /// Verifica se uma tabela é obrigatória
    /// </summary>
    private bool IsRequiredTable(string tableName)
    {
        string[] requiredTables = {
            "TB_GRUPO",
            "TB_SUB_GRUPO",
            "TB_FORMA_ORGANIZACAO",
            "TB_PROCEDIMENTO"
        };

        return requiredTables.Contains(tableName);
    }
}
