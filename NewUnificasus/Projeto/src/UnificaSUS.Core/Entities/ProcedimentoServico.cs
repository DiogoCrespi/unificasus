namespace UnificaSUS.Core.Entities;

/// <summary>
/// Relacionamento entre Procedimento e Serviço
/// </summary>
public class ProcedimentoServico
{
    public string CoProcedimento { get; set; } = string.Empty;
    public string CoServico { get; set; } = string.Empty;
    public string CoClassificacao { get; set; } = string.Empty;
    public string? DtCompetencia { get; set; }
    
    // Navegação
    public Procedimento? Procedimento { get; set; }
    public Servico? Servico { get; set; }
}

