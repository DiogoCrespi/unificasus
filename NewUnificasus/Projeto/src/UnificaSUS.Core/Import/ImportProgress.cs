namespace UnificaSUS.Core.Import;

/// <summary>
/// Representa o progresso da importação em tempo real
/// </summary>
public class ImportProgress
{
    /// <summary>
    /// Nome da tabela sendo importada
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// Número de linhas processadas
    /// </summary>
    public int ProcessedLines { get; set; }

    /// <summary>
    /// Total de linhas a processar
    /// </summary>
    public int TotalLines { get; set; }

    /// <summary>
    /// Número de registros importados com sucesso
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Número de registros com erro
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Percentual de conclusão (0-100)
    /// </summary>
    public double PercentComplete => TotalLines > 0 ? (ProcessedLines * 100.0 / TotalLines) : 0;

    /// <summary>
    /// Mensagem de status atual
    /// </summary>
    public string? StatusMessage { get; set; }
}
