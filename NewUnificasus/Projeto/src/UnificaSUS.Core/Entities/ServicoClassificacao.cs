namespace UnificaSUS.Core.Entities;

/// <summary>
/// Representa a classificação de um serviço
/// Tabela: TB_SERVICO_CLASSIFICACAO
/// </summary>
public class ServicoClassificacao
{
    /// <summary>
    /// Código do serviço (chave primária composta - parte 1)
    /// </summary>
    public string CoServico { get; set; } = string.Empty;

    /// <summary>
    /// Código da classificação (chave primária composta - parte 2)
    /// </summary>
    public string CoClassificacao { get; set; } = string.Empty;

    /// <summary>
    /// Nome/Descrição da classificação
    /// </summary>
    public string? NoClassificacao { get; set; }

    /// <summary>
    /// Data de competência no formato AAAAMM (chave primária composta - parte 3)
    /// </summary>
    public string? DtCompetencia { get; set; }

    /// <summary>
    /// Navegação: Serviço relacionado
    /// </summary>
    public Servico? Servico { get; set; }
}

