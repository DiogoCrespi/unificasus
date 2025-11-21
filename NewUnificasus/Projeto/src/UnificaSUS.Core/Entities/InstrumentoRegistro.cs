namespace UnificaSUS.Core.Entities;

/// <summary>
/// Representa um instrumento de registro
/// </summary>
public class InstrumentoRegistro
{
    public string CoRegistro { get; set; } = string.Empty;
    public string? NoRegistro { get; set; }
    public string? DtCompetencia { get; set; }
    public int? Indice { get; set; }
    
    // Navegação
    public List<ProcedimentoRegistro> ProcedimentoRegistros { get; set; } = new();
}
