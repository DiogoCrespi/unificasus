namespace UnificaSUS.Core.Import;

/// <summary>
/// Representa os metadados de uma tabela SIGTAP
/// </summary>
public class ImportTableMetadata
{
    /// <summary>
    /// Nome da tabela no banco de dados
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// Nome do arquivo de dados (.txt)
    /// </summary>
    public string DataFileName { get; set; } = string.Empty;

    /// <summary>
    /// Nome do arquivo de layout (*_layout.txt)
    /// </summary>
    public string LayoutFileName { get; set; } = string.Empty;

    /// <summary>
    /// Lista de colunas com metadados
    /// </summary>
    public List<ImportColumnMetadata> Columns { get; set; } = new();

    /// <summary>
    /// Prioridade de importação (1 = primeiro, maior = depois)
    /// Para resolver dependências entre tabelas
    /// </summary>
    public int ImportPriority { get; set; } = 10;

    /// <summary>
    /// Indica se a tabela é obrigatória
    /// </summary>
    public bool IsRequired { get; set; } = false;

    /// <summary>
    /// Nome da competência (AAAAMM) se aplicável
    /// </summary>
    public string? Competencia { get; set; }
}
