namespace UnificaSUS.Core.Entities;

/// <summary>
/// Relacionamento entre Procedimento e Modalidade
/// </summary>
public class ProcedimentoModalidade
{
    public string CoProcedimento { get; set; } = string.Empty;
    public string CoModalidade { get; set; } = string.Empty;
    public string? DtCompetencia { get; set; }
    
    // Navegação
    public Procedimento? Procedimento { get; set; }
    public Modalidade? Modalidade { get; set; }
}

