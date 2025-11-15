namespace UnificaSUS.Core.Entities;

/// <summary>
/// Representa a competência ativa no sistema
/// </summary>
public class CompetenciaAtiva
{
    public string DtCompetencia { get; set; } = string.Empty;
    public DateTime? DtAtivacao { get; set; }
    public string? StAtiva { get; set; }
}

