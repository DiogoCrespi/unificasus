using UnificaSUS.Core.Entities;

namespace UnificaSUS.Core.Interfaces;

/// <summary>
/// Interface do repositório de procedimentos
/// </summary>
public interface IProcedimentoRepository
{
    /// <summary>
    /// Busca procedimentos por competência
    /// </summary>
    Task<IEnumerable<Procedimento>> BuscarPorCompetenciaAsync(string competencia, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Busca um procedimento por código
    /// </summary>
    Task<Procedimento?> BuscarPorCodigoAsync(string codigo, string competencia, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Busca múltiplos procedimentos por uma lista de códigos
    /// </summary>
    Task<IEnumerable<Procedimento>> BuscarPorCodigosAsync(IEnumerable<string> codigos, string competencia, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Busca procedimentos por filtro (código ou nome)
    /// </summary>
    Task<IEnumerable<Procedimento>> BuscarPorFiltroAsync(string filtro, string competencia, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Busca procedimentos relacionados a um CID
    /// </summary>
    Task<IEnumerable<Procedimento>> BuscarPorCIDAsync(string cid, string competencia, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Busca procedimentos relacionados a um serviço
    /// </summary>
    Task<IEnumerable<Procedimento>> BuscarPorServicoAsync(string servico, string competencia, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Busca procedimentos por grupo, sub-grupo e forma de organização
    /// </summary>
    /// <param name="coGrupo">Código do grupo (2 dígitos) - posições 1-2 do código do procedimento</param>
    /// <param name="coSubGrupo">Código do sub-grupo (2 dígitos) - posições 3-4 do código do procedimento. Null para buscar todos do grupo</param>
    /// <param name="coFormaOrganizacao">Código da forma de organização (2 dígitos) - posições 5-6 do código do procedimento. Null para buscar todos do sub-grupo</param>
    /// <param name="competencia">Competência</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    Task<IEnumerable<Procedimento>> BuscarPorEstruturaAsync(string? coGrupo, string? coSubGrupo, string? coFormaOrganizacao, string competencia, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Busca CID10 relacionados a um procedimento
    /// </summary>
    Task<IEnumerable<RelacionadoItem>> BuscarCID10RelacionadosAsync(string coProcedimento, string competencia, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Busca procedimentos compatíveis relacionados a um procedimento
    /// </summary>
    Task<IEnumerable<RelacionadoItem>> BuscarCompativeisRelacionadosAsync(string coProcedimento, string competencia, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Busca habilitações relacionadas a um procedimento
    /// </summary>
    Task<IEnumerable<RelacionadoItem>> BuscarHabilitacoesRelacionadasAsync(string coProcedimento, string competencia, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Busca CBOs (ocupações) relacionados a um procedimento
    /// </summary>
    Task<IEnumerable<RelacionadoItem>> BuscarCBOsRelacionadosAsync(string coProcedimento, string competencia, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Busca serviços relacionados a um procedimento
    /// </summary>
    Task<IEnumerable<RelacionadoItem>> BuscarServicosRelacionadosAsync(string coProcedimento, string competencia, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Busca tipos de leito relacionados a um procedimento
    /// </summary>
    Task<IEnumerable<RelacionadoItem>> BuscarTiposLeitoRelacionadosAsync(string coProcedimento, string competencia, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Busca modalidades relacionadas a um procedimento
    /// </summary>
    Task<IEnumerable<RelacionadoItem>> BuscarModalidadesRelacionadasAsync(string coProcedimento, string competencia, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Busca descrição relacionada a um procedimento
    /// </summary>
    Task<IEnumerable<RelacionadoItem>> BuscarDescricaoRelacionadaAsync(string coProcedimento, string competencia, CancellationToken cancellationToken = default);
}

/// <summary>
/// Item relacionado a um procedimento (usado para exibir resultados de busca)
/// </summary>
public class RelacionadoItem
{
    public string Codigo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string? InformacaoAdicional { get; set; }
}

