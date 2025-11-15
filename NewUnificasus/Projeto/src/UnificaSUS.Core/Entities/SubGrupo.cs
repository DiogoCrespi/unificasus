namespace UnificaSUS.Core.Entities;

/// <summary>
/// Representa um sub-grupo de procedimentos
/// </summary>
public class SubGrupo
{
    public string CoGrupo { get; set; } = string.Empty;
    public string CoSubGrupo { get; set; } = string.Empty;
    public string? NoSubGrupo { get; set; }
    public string? DtCompetencia { get; set; }
    
    // Navegação
    public Grupo? Grupo { get; set; }
    public List<FormaOrganizacao> FormasOrganizacao { get; set; } = new();
}

