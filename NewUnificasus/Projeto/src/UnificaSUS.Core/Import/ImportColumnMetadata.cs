namespace UnificaSUS.Core.Import;

/// <summary>
/// Representa os metadados de uma coluna em um arquivo SIGTAP
/// </summary>
public class ImportColumnMetadata
{
    /// <summary>
    /// Nome da coluna no banco de dados
    /// </summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// Posição inicial no arquivo (1-indexed)
    /// </summary>
    public int StartPosition { get; set; }

    /// <summary>
    /// Posição final no arquivo (1-indexed, inclusive)
    /// </summary>
    public int EndPosition { get; set; }

    /// <summary>
    /// Tamanho da coluna em caracteres
    /// </summary>
    public int Length { get; set; }

    /// <summary>
    /// Tipo de dados (VARCHAR2, NUMBER, CHAR)
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Indica se a coluna é chave primária
    /// </summary>
    public bool IsPrimaryKey { get; set; }

    /// <summary>
    /// Indica se a coluna permite valores nulos
    /// </summary>
    public bool AllowNull { get; set; } = true;
}
