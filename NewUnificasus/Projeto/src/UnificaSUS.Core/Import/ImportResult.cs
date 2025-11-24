namespace UnificaSUS.Core.Import;

/// <summary>
/// Representa o resultado da importação de uma tabela
/// </summary>
public class ImportResult
{
    /// <summary>
    /// Nome da tabela importada
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// Indica se a importação foi bem-sucedida
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Número de registros importados com sucesso
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Número de registros com erro
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Mensagem de erro (se houver)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Lista de avisos/erros não-críticos
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Tempo decorrido na importação
    /// </summary>
    public TimeSpan ElapsedTime { get; set; }

    /// <summary>
    /// Categoria do erro principal (se houver)
    /// </summary>
    public ImportErrorCategory? ErrorCategory { get; set; }

    /// <summary>
    /// Contador de erros por categoria
    /// </summary>
    public Dictionary<ImportErrorCategory, int> ErrorsByCategory { get; set; } = new();
}
