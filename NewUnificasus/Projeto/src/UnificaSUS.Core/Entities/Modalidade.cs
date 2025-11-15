namespace UnificaSUS.Core.Entities;

/// <summary>
/// Representa uma modalidade de atendimento
/// </summary>
public class Modalidade
{
    public string CoModalidade { get; set; } = string.Empty;
    public string? NoModalidade { get; set; }
    public string? DtCompetencia { get; set; }
    
    // Navegação
    public List<ProcedimentoModalidade> ProcedimentoModalidades { get; set; } = new();
}

