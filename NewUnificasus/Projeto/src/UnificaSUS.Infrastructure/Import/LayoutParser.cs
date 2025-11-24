using System.Text;
using UnificaSUS.Core.Import;

namespace UnificaSUS.Infrastructure.Import;

/// <summary>
/// Parser resiliente para arquivos de layout SIGTAP
/// Lê arquivos *_layout.txt e extrai metadados de colunas
/// </summary>
public class LayoutParser
{
    private readonly ILogger? _logger;

    public LayoutParser(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Parse um arquivo de layout e retorna os metadados das colunas
    /// </summary>
    /// <param name="layoutFilePath">Caminho do arquivo de layout</param>
    /// <param name="encoding">Encoding do arquivo (padrão: ISO-8859-1)</param>
    /// <returns>Lista de metadados de colunas</returns>
    public List<ImportColumnMetadata> ParseLayoutFile(string layoutFilePath, Encoding? encoding = null)
    {
        encoding ??= Encoding.GetEncoding("ISO-8859-1");
        var columns = new List<ImportColumnMetadata>();

        try
        {
            if (!File.Exists(layoutFilePath))
            {
                _logger?.LogError($"Arquivo de layout não encontrado: {layoutFilePath}");
                return columns;
            }

            var lines = File.ReadAllLines(layoutFilePath, encoding);
            
            // Ignora primeira linha (cabeçalho: Coluna,Tamanho,Inicio,Fim,Tipo)
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                
                // Ignora linhas vazias
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                try
                {
                    var column = ParseLayoutLine(line, i + 1);
                    if (column != null)
                    {
                        columns.Add(column);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning($"Erro ao parsear linha {i + 1} do layout: {ex.Message}. Linha: {line}");
                    // Continua com próxima linha
                }
            }

            _logger?.LogInformation($"Layout parseado: {columns.Count} colunas encontradas em {layoutFilePath}");
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Erro ao ler arquivo de layout {layoutFilePath}: {ex.Message}");
        }

        return columns;
    }

    /// <summary>
    /// Parse uma linha do arquivo de layout
    /// Formato esperado: Coluna,Tamanho,Inicio,Fim,Tipo
    /// Exemplo: CO_PROCEDIMENTO,10,1,10,VARCHAR2
    /// </summary>
    private ImportColumnMetadata? ParseLayoutLine(string line, int lineNumber)
    {
        var parts = line.Split(',');

        if (parts.Length < 5)
        {
            _logger?.LogWarning($"Linha {lineNumber} com formato inválido (esperado 5 campos, encontrado {parts.Length}): {line}");
            return null;
        }

        try
        {
            var column = new ImportColumnMetadata
            {
                ColumnName = parts[0].Trim(),
                Length = int.Parse(parts[1].Trim()),
                StartPosition = int.Parse(parts[2].Trim()),
                EndPosition = int.Parse(parts[3].Trim()),
                DataType = parts[4].Trim()
            };

            // Validações básicas
            if (string.IsNullOrWhiteSpace(column.ColumnName))
            {
                _logger?.LogWarning($"Linha {lineNumber}: Nome de coluna vazio");
                return null;
            }

            if (column.StartPosition <= 0 || column.EndPosition <= 0)
            {
                _logger?.LogWarning($"Linha {lineNumber}: Posições inválidas (Start: {column.StartPosition}, End: {column.EndPosition})");
                return null;
            }

            if (column.EndPosition < column.StartPosition)
            {
                _logger?.LogWarning($"Linha {lineNumber}: EndPosition ({column.EndPosition}) menor que StartPosition ({column.StartPosition})");
                return null;
            }

            // Verifica se Length é consistente com posições
            int calculatedLength = column.EndPosition - column.StartPosition + 1;
            if (column.Length != calculatedLength)
            {
                _logger?.LogDebug($"Linha {lineNumber}: Length informado ({column.Length}) difere do calculado ({calculatedLength}). Usando calculado.");
                column.Length = calculatedLength;
            }

            // Define se é chave primária (heurística: primeira coluna geralmente é PK)
            // Isso pode ser refinado posteriormente
            column.IsPrimaryKey = false; // Será definido posteriormente

            return column;
        }
        catch (FormatException ex)
        {
            _logger?.LogWarning($"Linha {lineNumber}: Erro ao converter valores numéricos: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Cria objeto ImportTableMetadata a partir de arquivos de layout e dados
    /// </summary>
    /// <param name="tableName">Nome da tabela</param>
    /// <param name="dataFilePath">Caminho do arquivo de dados (.txt)</param>
    /// <param name="layoutFilePath">Caminho do arquivo de layout (*_layout.txt)</param>
    /// <param name="priority">Prioridade de importação</param>
    /// <returns>Metadados da tabela</returns>
    public ImportTableMetadata CreateTableMetadata(
        string tableName,
        string dataFilePath,
        string layoutFilePath,
        int priority = 10)
    {
        var columns = ParseLayoutFile(layoutFilePath);

        return new ImportTableMetadata
        {
            TableName = tableName,
            DataFileName = Path.GetFileName(dataFilePath),
            LayoutFileName = Path.GetFileName(layoutFilePath),
            Columns = columns,
            ImportPriority = priority
        };
    }
}

/// <summary>
/// Interface para logging (compatível com ILogger do .NET)
/// </summary>
public interface ILogger
{
    void LogInformation(string message);
    void LogWarning(string message);
    void LogError(string message);
    void LogDebug(string message);
}
