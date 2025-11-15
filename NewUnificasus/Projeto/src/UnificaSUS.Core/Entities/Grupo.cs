namespace UnificaSUS.Core.Entities;

/// <summary>
/// Representa um grupo de procedimentos
/// </summary>
public class Grupo
{
    public string CoGrupo { get; set; } = string.Empty;
    public string? NoGrupo { get; set; }
    public string? DtCompetencia { get; set; }
    
    // Navegação
    public List<SubGrupo> SubGrupos { get; set; } = new();
}

