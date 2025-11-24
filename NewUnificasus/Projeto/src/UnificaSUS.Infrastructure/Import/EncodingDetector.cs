using System.Text;

namespace UnificaSUS.Infrastructure.Import;

/// <summary>
/// Detector automático de encoding de arquivos
/// Usa heurísticas para identificar o encoding correto
/// </summary>
public class EncodingDetector
{
    private readonly ILogger? _logger;
    private static bool _encodingProviderRegistered = false;

    public EncodingDetector(ILogger? logger = null)
    {
        _logger = logger;
        
        // Registra o provider de code pages para suportar Windows-1252 no .NET Core/.NET 5+
        if (!_encodingProviderRegistered)
        {
            try
            {
                // Tenta registrar via reflection para evitar dependência direta
                var codePagesType = Type.GetType("System.Text.Encoding.CodePages.CodePagesEncodingProvider, System.Text.Encoding.CodePages");
                if (codePagesType != null)
                {
                    var instanceProperty = codePagesType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (instanceProperty != null)
                    {
                        var instance = instanceProperty.GetValue(null);
                        if (instance != null)
                        {
                            var registerMethod = typeof(Encoding).GetMethod("RegisterProvider", new[] { typeof(EncodingProvider) });
                            registerMethod?.Invoke(null, new[] { instance });
                            _encodingProviderRegistered = true;
                        }
                    }
                }
            }
            catch
            {
                // Já registrado ou não disponível - continua sem Windows-1252
            }
        }
    }

    /// <summary>
    /// Detecta o encoding de um arquivo
    /// Prioriza Windows-1252 para arquivos SIGTAP brasileiros
    /// </summary>
    /// <param name="filePath">Caminho do arquivo</param>
    /// <returns>Encoding detectado</returns>
    public Encoding DetectEncoding(string filePath)
    {
        // Tenta obter encodings com fallback seguro
        var encodingsToTry = new List<Encoding>();
        
        // PRIORIDADE 1: Windows-1252 (padrão para arquivos brasileiros/SIGTAP)
        // Arquivos mais recentes do DATASUS geralmente usam Windows-1252
        if (_encodingProviderRegistered)
        {
            try
            {
                encodingsToTry.Add(Encoding.GetEncoding(1252)); // Windows-1252 (usando código numérico)
            }
            catch
            {
                try
                {
                    encodingsToTry.Add(Encoding.GetEncoding("Windows-1252")); // Tenta nome também
                }
                catch { }
            }
        }
        
        // PRIORIDADE 2: ISO-8859-1 (padrão SIGTAP antigo)
        try
        {
            encodingsToTry.Add(Encoding.GetEncoding("ISO-8859-1"));
        }
        catch { }
        
        // PRIORIDADE 3: UTF-8 (fallback)
        encodingsToTry.Add(Encoding.UTF8);
        
        if (encodingsToTry.Count == 0)
        {
            // Fallback mínimo: UTF-8
            return Encoding.UTF8;
        }

        // Lê primeiras linhas do arquivo para análise
        const int linesToAnalyze = 20; // Aumentado para melhor detecção
        
        // Score para cada encoding (quanto maior, melhor)
        var encodingScores = new Dictionary<Encoding, int>();
        
        foreach (var encoding in encodingsToTry)
        {
            try
            {
                var lines = File.ReadLines(filePath, encoding).Take(linesToAnalyze).ToList();
                int score = 0;
                
                // Verifica se há caracteres inválidos (replacement character)
                bool hasInvalidChars = lines.Any(line => line.Contains('\uFFFD'));
                if (hasInvalidChars)
                {
                    continue; // Pula este encoding
                }
                
                // Verifica se contém caracteres acentuados válidos (português)
                bool hasValidAccents = lines.Any(line => HasValidPortugueseChars(line));
                if (hasValidAccents)
                {
                    score += 10; // Bonus por ter acentos válidos
                }
                
                // Verifica padrões de corrupção comum (indica encoding errado)
                bool hasCorruptionPatterns = lines.Any(line => 
                    line.Contains("Ã§") || line.Contains("Ãƒ") || line.Contains("Ã¡") || 
                    line.Contains("Ã©") || line.Contains("Ã­") || line.Contains("Ã³") || 
                    line.Contains("Ãº") || line.Contains("Ã£") || line.Contains("Ãµ"));
                
                if (hasCorruptionPatterns)
                {
                    score -= 20; // Penalidade por padrões de corrupção
                }
                
                // Bonus para Windows-1252 se não tiver padrões de corrupção
                if (encoding.CodePage == 1252 && !hasCorruptionPatterns)
                {
                    score += 5;
                }
                
                encodingScores[encoding] = score;
            }
            catch (Exception ex)
            {
                _logger?.LogDebug($"Falha ao tentar encoding {encoding.WebName}: {ex.Message}");
            }
        }

        // Seleciona o encoding com maior score
        if (encodingScores.Any())
        {
            var bestEncoding = encodingScores.OrderByDescending(kvp => kvp.Value).First().Key;
            _logger?.LogInformation($"Encoding detectado: {bestEncoding.WebName} (score: {encodingScores[bestEncoding]}) para arquivo {Path.GetFileName(filePath)}");
            return bestEncoding;
        }

        // Fallback para Windows-1252 se disponível, senão ISO-8859-1
        if (_encodingProviderRegistered)
        {
            try
            {
                var win1252 = Encoding.GetEncoding(1252);
                _logger?.LogWarning($"Não foi possível detectar encoding com precisão, usando Windows-1252 como padrão");
                return win1252;
            }
            catch { }
        }
        
        _logger?.LogWarning($"Não foi possível detectar encoding, usando padrão ISO-8859-1");
        return Encoding.GetEncoding("ISO-8859-1");
    }

    /// <summary>
    /// Verifica se o texto contém caracteres acentuados válidos do português
    /// </summary>
    private bool HasValidPortugueseChars(string text)
    {
        // Caracteres acentuados comuns em português
        char[] portugueseChars = { 'á', 'à', 'â', 'ã', 'é', 'ê', 'í', 'ó', 'ô', 'õ', 'ú', 'ç',
                                   'Á', 'À', 'Â', 'Ã', 'É', 'Ê', 'Í', 'Ó', 'Ô', 'Õ', 'Ú', 'Ç' };
        
        return text.IndexOfAny(portugueseChars) >= 0;
    }

    /// <summary>
    /// Normaliza texto convertendo de um encoding para outro
    /// </summary>
    /// <param name="text">Texto a normalizar</param>
    /// <param name="sourceEncoding">Encoding de origem</param>
    /// <param name="targetEncoding">Encoding de destino (padrão: UTF-8)</param>
    /// <returns>Texto normalizado</returns>
    public string NormalizeText(string text, Encoding sourceEncoding, Encoding? targetEncoding = null)
    {
        targetEncoding ??= Encoding.UTF8;

        try
        {
            // Converte string de volta para bytes no encoding original
            byte[] sourceBytes = sourceEncoding.GetBytes(text);
            
            // Decodifica usando encoding de origem e recodifica em destino
            string normalized = targetEncoding.GetString(sourceBytes);
            
            return normalized;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning($"Erro ao normalizar texto: {ex.Message}");
            return text; // Retorna original se falhar
        }
    }

    /// <summary>
    /// Lê arquivo com detecção automática de encoding
    /// </summary>
    /// <param name="filePath">Caminho do arquivo</param>
    /// <returns>Linhas do arquivo e encoding detectado</returns>
    public (string[] Lines, Encoding DetectedEncoding) ReadFileWithDetection(string filePath)
    {
        var encoding = DetectEncoding(filePath);
        var lines = File.ReadAllLines(filePath, encoding);
        return (lines, encoding);
    }
}
