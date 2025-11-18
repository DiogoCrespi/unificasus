namespace UnificaSUS.Core.Entities;

/// <summary>
/// Representa um procedimento que será exibido no relatório
/// </summary>
public class ItemRelatorioProcedimento
{
    public string CoProcedimento { get; set; } = string.Empty;
    public string? NoProcedimento { get; set; }
    public decimal? VlSp { get; set; }
}

