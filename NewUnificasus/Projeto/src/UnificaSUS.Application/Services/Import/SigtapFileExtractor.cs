using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace UnificaSUS.Application.Services.Import;

/// <summary>
/// Utilitário para descompactar arquivos ZIP do SIGTAP
/// </summary>
public class SigtapFileExtractor
{
    private readonly ILogger? _logger;

    public SigtapFileExtractor(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Extrai arquivo ZIP do SIGTAP para um diretório temporário
    /// </summary>
    /// <param name="zipFilePath">Caminho do arquivo ZIP</param>
    /// <param name="targetDirectory">Diretório de destino (opcional, usa temp se não especificado)</param>
    /// <returns>Caminho do diretório extraído</returns>
    public string ExtractZipFile(string zipFilePath, string? targetDirectory = null)
    {
        if (!File.Exists(zipFilePath))
        {
            throw new FileNotFoundException($"Arquivo ZIP não encontrado: {zipFilePath}");
        }

        // Se não especificado, usa diretório temporário
        if (string.IsNullOrEmpty(targetDirectory))
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "SigtapImport", Path.GetFileNameWithoutExtension(zipFilePath));
            targetDirectory = tempDir;
        }

        try
        {
            _logger?.LogInformation($"Extraindo {Path.GetFileName(zipFilePath)} para {targetDirectory}...");

            // Remove diretório se já existir
            if (Directory.Exists(targetDirectory))
            {
                _logger?.LogDebug($"Removendo diretório existente: {targetDirectory}");
                Directory.Delete(targetDirectory, true);
            }

            // Cria diretório
            Directory.CreateDirectory(targetDirectory);

            // Extrai ZIP
            ZipFile.ExtractToDirectory(zipFilePath, targetDirectory);

            // Conta arquivos extraídos
            int fileCount = Directory.GetFiles(targetDirectory).Length;
            _logger?.LogInformation($"✓ Extraídos {fileCount} arquivos para {targetDirectory}");

            return targetDirectory;
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Erro ao extrair ZIP: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Verifica se um diretório contém arquivos SIGTAP válidos
    /// </summary>
    public bool IsValidSigtapDirectory(string directory)
    {
        if (!Directory.Exists(directory))
            return false;

        // Verifica se existe pelo menos um arquivo de layout
        var layoutFiles = Directory.GetFiles(directory, "*_layout.txt");
        if (layoutFiles.Length == 0)
            return false;

        // Verifica se existe arquivo de versão ou config
        bool hasVersion = File.Exists(Path.Combine(directory, "versao"));
        bool hasConfig = File.Exists(Path.Combine(directory, "config.inf"));
        bool hasLeiame = File.Exists(Path.Combine(directory, "LEIA_ME.TXT"));

        return hasVersion || hasConfig || hasLeiame;
    }

    /// <summary>
    /// Obtém informações do arquivo SIGTAP
    /// </summary>
    public SigtapFileInfo GetFileInfo(string directoryOrZip)
    {
        var info = new SigtapFileInfo();

        try
        {
            string directory;
            bool isZip = Path.GetExtension(directoryOrZip).ToLower() == ".zip";

            if (isZip)
            {
                info.IsZipFile = true;
                info.ZipFilePath = directoryOrZip;
                
                // Extrai para temp apenas para ler informações
                directory = ExtractZipFile(directoryOrZip);
                info.ExtractedDirectory = directory;
            }
            else
            {
                info.IsZipFile = false;
                directory = directoryOrZip;
                info.ExtractedDirectory = directory;
            }

            // Lê versão
            string versionFile = Path.Combine(directory, "versao");
            if (File.Exists(versionFile))
            {
                info.Version = File.ReadAllText(versionFile).Trim();
            }

            // Lê config.inf - pode conter competência ou outras informações
            string configFile = Path.Combine(directory, "config.inf");
            if (File.Exists(configFile))
            {
                var configContent = File.ReadAllText(configFile, Encoding.UTF8).Trim();
                
                // Tenta extrair competência do formato AAAAMM (6 dígitos)
                // Pode estar em formato: "202510" ou "Competencia=202510" ou similar
                var competenciaMatch = System.Text.RegularExpressions.Regex.Match(configContent, @"(\d{6})");
                if (competenciaMatch.Success)
                {
                    info.Competencia = competenciaMatch.Groups[1].Value;
                }
                else
                {
                    // Se não encontrar padrão, tenta usar o conteúdo completo se parecer com competência
                    if (configContent.Length == 6 && configContent.All(char.IsDigit))
                    {
                        info.Competencia = configContent;
                    }
                    else
                    {
                        // Se não for competência, tenta extrair do nome do arquivo ZIP ou diretório
                        var dirName = Path.GetFileName(directory);
                        var competenciaFromDir = System.Text.RegularExpressions.Regex.Match(dirName, @"(\d{6})");
                        if (competenciaFromDir.Success)
                        {
                            info.Competencia = competenciaFromDir.Groups[1].Value;
                        }
                    }
                }
            }

            // Conta arquivos
            info.TotalFiles = Directory.GetFiles(directory).Length;
            info.LayoutFileCount = Directory.GetFiles(directory, "*_layout.txt").Length;
            info.DataFileCount = Directory.GetFiles(directory, "*.txt").Length - info.LayoutFileCount;

        }
        catch (Exception ex)
        {
            _logger?.LogError($"Erro ao obter informações do arquivo: {ex.Message}");
        }

        return info;
    }

    /// <summary>
    /// Limpa diretórios temporários de extração
    /// </summary>
    public void CleanupTempDirectories()
    {
        try
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "SigtapImport");
            if (Directory.Exists(tempDir))
            {
                _logger?.LogInformation($"Limpando diretórios temporários: {tempDir}");
                Directory.Delete(tempDir, true);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning($"Erro ao limpar diretórios temporários: {ex.Message}");
        }
    }
}

/// <summary>
/// Informações sobre arquivo SIGTAP
/// </summary>
public class SigtapFileInfo
{
    public bool IsZipFile { get; set; }
    public string? ZipFilePath { get; set; }
    public string? ExtractedDirectory { get; set; }
    public string? Version { get; set; }
    public string? Competencia { get; set; }
    public int TotalFiles { get; set; }
    public int LayoutFileCount { get; set; }
    public int DataFileCount { get; set; }
}
