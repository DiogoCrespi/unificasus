namespace UnificaSUS.Core.Entities;

/// <summary>
/// Representa um tipo de financiamento
/// </summary>
public class Financiamento
{
    public string CoFinanciamento { get; set; } = string.Empty;
    public string? NoFinanciamento { get; set; }
    public string? DtCompetencia { get; set; }
}

