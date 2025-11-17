namespace UnificaSUS.Core.Entities;

/// <summary>
/// Representa um procedimento comum (favorito) do usuário
/// </summary>
public class ProcedimentoComum
{
    /// <summary>
    /// Código único do registro (chave primária)
    /// </summary>
    public int PrcCod { get; set; }

    /// <summary>
    /// Código do procedimento (relaciona com TB_PROCEDIMENTO.CO_PROCEDIMENTO)
    /// </summary>
    public string? PrcCodProc { get; set; }

    /// <summary>
    /// Nome do procedimento
    /// </summary>
    public string? PrcNoProcedimento { get; set; }

    /// <summary>
    /// Observações do usuário sobre o procedimento
    /// </summary>
    public string? PrcObservacoes { get; set; }
}

