namespace UnificaSUS.Core.Entities;

/// <summary>
/// Representa a descrição detalhada de um procedimento
/// </summary>
public class Descricao
{
    public string CoProcedimento { get; set; } = string.Empty;
    public string? DsProcedimento { get; set; }
    public string? DtCompetencia { get; set; }
    
    // Navegação
    public Procedimento? Procedimento { get; set; }
}

