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
}

