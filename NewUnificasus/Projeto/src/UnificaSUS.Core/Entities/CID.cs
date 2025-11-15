namespace UnificaSUS.Core.Entities;

/// <summary>
/// Representa uma Classificação Internacional de Doenças (CID)
/// </summary>
public class CID
{
    public string CoCid { get; set; } = string.Empty;
    public string? NoCid { get; set; }
    public string? TpAgravo { get; set; }
    public string? TpSexo { get; set; }
    public string? TpEstadio { get; set; }
    public int? VlCamposIrradiados { get; set; }
    
    // Navegação
    public List<ProcedimentoCID> ProcedimentoCIDs { get; set; } = new();
}

