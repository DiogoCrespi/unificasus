namespace UnificaSUS.Core.Entities;

/// <summary>
/// Representa um item selecionado para o relatório (Grupo, Sub-grupo, Forma de Organização ou Procedimento)
/// </summary>
public class ItemRelatorio
{
    public string Tipo { get; set; } = string.Empty; // "Grupo", "SubGrupo", "FormaOrganizacao", "Procedimento"
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? Competencia { get; set; }
    
    public override string ToString()
    {
        if (string.IsNullOrWhiteSpace(Nome))
        {
            return Codigo;
        }
        return $"{Codigo} - {Nome}";
    }
}

