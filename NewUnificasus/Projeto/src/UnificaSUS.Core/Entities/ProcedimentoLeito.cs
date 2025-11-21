namespace UnificaSUS.Core.Entities;

/// <summary>
/// Relacionamento entre Procedimento e Tipo de Leito
/// </summary>
public class ProcedimentoLeito
{
    public string CoProcedimento { get; set; } = string.Empty;
    public string CoTipoLeito { get; set; } = string.Empty;
    public string? DtCompetencia { get; set; }
    
    // Navegação
    public Procedimento? Procedimento { get; set; }
    public TipoLeito? TipoLeito { get; set; }
}
