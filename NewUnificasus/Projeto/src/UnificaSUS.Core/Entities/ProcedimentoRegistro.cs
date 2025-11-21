namespace UnificaSUS.Core.Entities;

/// <summary>
/// Relacionamento entre Procedimento e Instrumento de Registro
/// </summary>
public class ProcedimentoRegistro
{
    public string CoProcedimento { get; set; } = string.Empty;
    public string CoRegistro { get; set; } = string.Empty;
    public string? DtCompetencia { get; set; }
    
    // Navegação
    public Procedimento? Procedimento { get; set; }
    public InstrumentoRegistro? InstrumentoRegistro { get; set; }
}
