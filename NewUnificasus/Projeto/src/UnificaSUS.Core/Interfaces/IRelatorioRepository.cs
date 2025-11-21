using UnificaSUS.Core.Entities;

namespace UnificaSUS.Core.Interfaces;

/// <summary>
/// Interface do repositório de relatórios
/// </summary>
public interface IRelatorioRepository
{
    /// <summary>
    /// Busca procedimentos por grupo
    /// </summary>
    Task<IEnumerable<ItemRelatorioProcedimento>> BuscarProcedimentosPorGrupoAsync(
        string coGrupo, 
        string competencia, 
        bool naoImprimirSPZerado, 
        string ordenarPor, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca procedimentos por sub-grupo
    /// </summary>
    Task<IEnumerable<ItemRelatorioProcedimento>> BuscarProcedimentosPorSubGrupoAsync(
        string coGrupo, 
        string coSubGrupo, 
        string competencia, 
        bool naoImprimirSPZerado, 
        string ordenarPor, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca procedimentos por forma de organização
    /// </summary>
    Task<IEnumerable<ItemRelatorioProcedimento>> BuscarProcedimentosPorFormaOrganizacaoAsync(
        string coGrupo, 
        string coSubGrupo, 
        string coFormaOrganizacao, 
        string competencia, 
        bool naoImprimirSPZerado, 
        string ordenarPor, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca procedimentos por código ou nome
    /// </summary>
    Task<IEnumerable<ItemRelatorioProcedimento>> BuscarProcedimentosPorCodigoOuNomeAsync(
        string codigoOuNome, 
        string competencia, 
        bool naoImprimirSPZerado, 
        string ordenarPor, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca grupos disponíveis para seleção
    /// </summary>
    Task<IEnumerable<ItemRelatorio>> BuscarGruposDisponiveisAsync(
        string competencia, 
        string? filtro, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca sub-grupos disponíveis para seleção
    /// </summary>
    Task<IEnumerable<ItemRelatorio>> BuscarSubGruposDisponiveisAsync(
        string competencia, 
        string? filtro, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca formas de organização disponíveis para seleção
    /// </summary>
    Task<IEnumerable<ItemRelatorio>> BuscarFormasOrganizacaoDisponiveisAsync(
        string competencia, 
        string? filtro, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca procedimentos disponíveis para seleção
    /// </summary>
    Task<IEnumerable<ItemRelatorio>> BuscarProcedimentosDisponiveisAsync(
        string competencia, 
        string? filtro, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca tipos de leito disponíveis para seleção
    /// </summary>
    Task<IEnumerable<ItemRelatorio>> BuscarTiposLeitoDisponiveisAsync(
        string competencia, 
        string? filtro, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca procedimentos por tipo de leito
    /// </summary>
    Task<IEnumerable<ItemRelatorioProcedimento>> BuscarProcedimentosPorTipoLeitoAsync(
        string coTipoLeito, 
        string competencia, 
        bool naoImprimirSPZerado, 
        string ordenarPor, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca instrumentos de registro disponíveis para seleção
    /// </summary>
    Task<IEnumerable<ItemRelatorio>> BuscarInstrumentosRegistroDisponiveisAsync(
        string competencia, 
        string? filtro, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca procedimentos por instrumento de registro
    /// </summary>
    Task<IEnumerable<ItemRelatorioProcedimento>> BuscarProcedimentosPorInstrumentoRegistroAsync(
        string coRegistro, 
        string competencia, 
        bool naoImprimirSPZerado, 
        string ordenarPor, 
        CancellationToken cancellationToken = default);
}

