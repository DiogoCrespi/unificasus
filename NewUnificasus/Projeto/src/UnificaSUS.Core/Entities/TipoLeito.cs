namespace UnificaSUS.Core.Entities;

/// <summary>
/// Representa um tipo de leito
/// </summary>
public class TipoLeito
{
    public string CoTipoLeito { get; set; } = string.Empty;
    public string? NoTipoLeito { get; set; }
    public string? DtCompetencia { get; set; }
    
    // Navegação
    public List<ProcedimentoLeito> ProcedimentoLeitos { get; set; } = new();
}
