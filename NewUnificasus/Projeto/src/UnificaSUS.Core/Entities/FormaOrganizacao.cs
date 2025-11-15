namespace UnificaSUS.Core.Entities;

/// <summary>
/// Representa uma forma de organização
/// </summary>
public class FormaOrganizacao
{
    public string CoGrupo { get; set; } = string.Empty;
    public string CoSubGrupo { get; set; } = string.Empty;
    public string CoFormaOrganizacao { get; set; } = string.Empty;
    public string? NoFormaOrganizacao { get; set; }
    public string? DtCompetencia { get; set; }
    
    // Navegação
    public SubGrupo? SubGrupo { get; set; }
}

