namespace UnificaSUS.Core.Entities;

/// <summary>
/// Relacionamento entre Procedimento e CID
/// </summary>
public class ProcedimentoCID
{
    public string CoProcedimento { get; set; } = string.Empty;
    public string CoCid { get; set; } = string.Empty;
    public string? StPrincipal { get; set; }
    public string? DtCompetencia { get; set; }
    
    // Navegação
    public Procedimento? Procedimento { get; set; }
    public CID? Cid { get; set; }
}

