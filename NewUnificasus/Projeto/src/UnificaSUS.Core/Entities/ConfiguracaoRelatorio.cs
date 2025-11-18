namespace UnificaSUS.Core.Entities;

/// <summary>
/// Representa as configurações do relatório
/// </summary>
public class ConfiguracaoRelatorio
{
    public string Titulo { get; set; } = "Relatório de Procedimentos";
    public bool NaoImprimirSPZerado { get; set; } = false;
    public string Modelo { get; set; } = "CodigoNomeValorSP"; // "CodigoNomeValorSP"
    public string OrdenarPor { get; set; } = "Codigo"; // "Codigo", "Nome", "ValorSP"
}

