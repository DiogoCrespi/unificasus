using System.Globalization;
using System.Text;
using UnificaSUS.Core.Import;

namespace UnificaSUS.Infrastructure.Import;

/// <summary>
/// Parser para arquivos de posição fixa (fixed-width)
/// Extrai dados de arquivos .txt SIGTAP com tratamento resiliente de erros
/// </summary>
public class FixedWidthParser
{
    private readonly ILogger? _logger;

    public FixedWidthParser(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Parse uma linha de arquivo de posição fixa baseado nos metadados
    /// </summary>
    /// <param name="line">Linha a ser parseada</param>
    /// <param name="metadata">Metadados da tabela</param>
    /// <returns>Dicionário com nome da coluna e valor</returns>
    public Dictionary<string, object?> ParseLine(string line, ImportTableMetadata metadata)
    {
        var result = new Dictionary<string, object?>();

        if (string.IsNullOrEmpty(line))
        {
            _logger?.LogWarning("Linha vazia encontrada");
            return result;
        }

        foreach (var column in metadata.Columns)
        {
            try
            {
                string value = ExtractValue(line, column);
                object? convertedValue = ConvertValue(value, column);
                result[column.ColumnName] = convertedValue;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"Erro ao parsear coluna {column.ColumnName}: {ex.Message}");
                result[column.ColumnName] = null; // Continua com null
            }
        }

        return result;
    }

    /// <summary>
    /// Extrai o valor de uma coluna da linha usando posições fixas
    /// </summary>
    private string ExtractValue(string line, ImportColumnMetadata column)
    {
        // Ajusta para índices base-0
        int startIndex = column.StartPosition - 1;
        int length = column.Length;

        // Verifica se a linha é curta demais
        if (startIndex >= line.Length)
        {
            _logger?.LogDebug($"Coluna {column.ColumnName}: Linha muito curta (tamanho: {line.Length}, início esperado: {column.StartPosition})");
            return string.Empty;
        }

        // Ajusta length se a linha for mais curta que o esperado
        if (startIndex + length > line.Length)
        {
            length = line.Length - startIndex;
            _logger?.LogDebug($"Coluna {column.ColumnName}: Ajustando length de {column.Length} para {length}");
        }

        string value = line.Substring(startIndex, length);
        return value.Trim(); // Remove espaços em branco
    }

    /// <summary>
    /// Converte o valor string para o tipo apropriado
    /// </summary>
    private object? ConvertValue(string value, ImportColumnMetadata column)
    {
        // String vazia retorna null (comportamento resiliente)
        if (string.IsNullOrWhiteSpace(value))
        {
            return column.AllowNull ? null : GetDefaultValue(column.DataType);
        }

        try
        {
            return column.DataType.ToUpper() switch
            {
                "NUMBER" or "NUMERIC" => ConvertToNumber(value, column),
                "CHAR" or "VARCHAR2" or "VARCHAR" or "TEXT" => value,
                "DATE" => ConvertToDate(value),
                _ => value // Tipo desconhecido, retorna como string
            };
        }
        catch (Exception ex)
        {
            _logger?.LogWarning($"Erro ao converter valor '{value}' para tipo {column.DataType} na coluna {column.ColumnName}: {ex.Message}");
            return column.AllowNull ? null : GetDefaultValue(column.DataType);
        }
    }

    /// <summary>
    /// Converte string para número (int, decimal, etc)
    /// </summary>
    private object? ConvertToNumber(string value, ImportColumnMetadata column)
    {
        // Remove espaços
        value = value.Trim();

        // Tenta converter para inteiro primeiro
        if (int.TryParse(value, out int intValue))
        {
            return intValue;
        }

        // Tenta converter para decimal
        // Usa InvariantCulture para garantir que ponto seja decimal separator
        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal decimalValue))
        {
            return decimalValue;
        }

        // Tenta com vírgula como separador decimal (padrão brasileiro)
        value = value.Replace(',', '.');
        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimalValue))
        {
            return decimalValue;
        }

        _logger?.LogWarning($"Não foi possível converter '{value}' para número na coluna {column.ColumnName}");
        return null;
    }

    /// <summary>
    /// Converte string para data (formato AAAAMM)
    /// </summary>
    private object? ConvertToDate(string value)
    {
        value = value.Trim();

        // Formato esperado: AAAAMM (202510)
        if (value.Length == 6)
        {
            return value; // Retorna como string no formato AAAAMM
        }

        _logger?.LogWarning($"Data em formato inválido: '{value}' (esperado AAAAMM)");
        return null;
    }

    /// <summary>
    /// Retorna valor padrão para um tipo de dados
    /// </summary>
    private object GetDefaultValue(string dataType)
    {
        return dataType.ToUpper() switch
        {
            "NUMBER" or "NUMERIC" => 0,
            "CHAR" or "VARCHAR2" or "VARCHAR" or "TEXT" => string.Empty,
            _ => string.Empty
        };
    }

    /// <summary>
    /// Parse múltiplas linhas de um arquivo
    /// </summary>
    /// <param name="filePath">Caminho do arquivo</param>
    /// <param name="metadata">Metadados da tabela</param>
    /// <param name="encoding">Encoding (padrão: ISO-8859-1)</param>
    /// <returns>Lista de dicionários com dados parseados</returns>
    public List<Dictionary<string, object?>> ParseFile(
        string filePath,
        ImportTableMetadata metadata,
        Encoding? encoding = null)
    {
        encoding ??= Encoding.GetEncoding("ISO-8859-1");
        var results = new List<Dictionary<string, object?>>();

        try
        {
            var lines = File.ReadAllLines(filePath, encoding);

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                
                if (string.IsNullOrWhiteSpace(line))
                {
                    _logger?.LogDebug($"Linha {i + 1}: Linha vazia, ignorando");
                    continue;
                }

                var data = ParseLine(line, metadata);
                results.Add(data);
            }

            _logger?.LogInformation($"Arquivo parseado: {results.Count} registros de {lines.Length} linhas em {filePath}");
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Erro ao parsear arquivo {filePath}: {ex.Message}");
        }

        return results;
    }
}
