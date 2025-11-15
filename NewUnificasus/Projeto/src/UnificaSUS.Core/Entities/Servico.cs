namespace UnificaSUS.Core.Entities;

/// <summary>
/// Representa um serviço
/// </summary>
public class Servico
{
    public string CoServico { get; set; } = string.Empty;
    public string? NoServico { get; set; }
    public string? DtCompetencia { get; set; }
    
    // Navegação
    public List<ProcedimentoServico> ProcedimentoServicos { get; set; } = new();
}

