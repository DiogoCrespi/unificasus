namespace UnificaSUS.Core.Entities;

/// <summary>
/// Representa um procedimento do SUS
/// </summary>
public class Procedimento
{
    public string CoProcedimento { get; set; } = string.Empty;
    public string? NoProcedimento { get; set; }
    public string? TpComplexidade { get; set; }
    public string? TpSexo { get; set; }
    public int? QtMaximaExecucao { get; set; }
    public int? QtDiasPermanencia { get; set; }
    public int? QtPontos { get; set; }
    public int? VlIdadeMinima { get; set; }
    public int? VlIdadeMaxima { get; set; }
    public decimal? VlSh { get; set; }
    public decimal? VlSa { get; set; }
    public decimal? VlSp { get; set; }
    public string? CoFinanciamento { get; set; }
    public string? CoRubrica { get; set; }
    public int? QtTempoPermanencia { get; set; }
    public string? DtCompetencia { get; set; }
    
    // Navegação
    public Financiamento? Financiamento { get; set; }
    public Rubrica? Rubrica { get; set; }
    public List<ProcedimentoCID> ProcedimentoCIDs { get; set; } = new();
    public List<ProcedimentoServico> ProcedimentoServicos { get; set; } = new();
    public List<ProcedimentoModalidade> ProcedimentoModalidades { get; set; } = new();
    public Descricao? Descricao { get; set; }
}

