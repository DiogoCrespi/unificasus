using Microsoft.Extensions.Logging;
using UnificaSUS.Core.Entities;
using UnificaSUS.Core.Interfaces;

namespace UnificaSUS.Application.Services;

/// <summary>
/// Serviço de aplicação para relatórios
/// </summary>
public class RelatorioService
{
    private readonly IRelatorioRepository _repository;
    private readonly ILogger<RelatorioService> _logger;

    public RelatorioService(IRelatorioRepository repository, ILogger<RelatorioService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<ItemRelatorioProcedimento>> BuscarProcedimentosPorGrupoAsync(
        string coGrupo, 
        string competencia, 
        bool naoImprimirSPZerado, 
        string ordenarPor, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Buscando procedimentos por grupo {Grupo} para relatório", coGrupo);
        return await _repository.BuscarProcedimentosPorGrupoAsync(coGrupo, competencia, naoImprimirSPZerado, ordenarPor, cancellationToken);
    }

    public async Task<IEnumerable<ItemRelatorioProcedimento>> BuscarProcedimentosPorSubGrupoAsync(
        string coGrupo, 
        string coSubGrupo, 
        string competencia, 
        bool naoImprimirSPZerado, 
        string ordenarPor, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Buscando procedimentos por sub-grupo {SubGrupo} para relatório", $"{coGrupo}{coSubGrupo}");
        return await _repository.BuscarProcedimentosPorSubGrupoAsync(coGrupo, coSubGrupo, competencia, naoImprimirSPZerado, ordenarPor, cancellationToken);
    }

    public async Task<IEnumerable<ItemRelatorioProcedimento>> BuscarProcedimentosPorFormaOrganizacaoAsync(
        string coGrupo, 
        string coSubGrupo, 
        string coFormaOrganizacao, 
        string competencia, 
        bool naoImprimirSPZerado, 
        string ordenarPor, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Buscando procedimentos por forma de organização {FormaOrg} para relatório", $"{coGrupo}{coSubGrupo}{coFormaOrganizacao}");
        return await _repository.BuscarProcedimentosPorFormaOrganizacaoAsync(coGrupo, coSubGrupo, coFormaOrganizacao, competencia, naoImprimirSPZerado, ordenarPor, cancellationToken);
    }

    public async Task<IEnumerable<ItemRelatorioProcedimento>> BuscarProcedimentosPorCodigoOuNomeAsync(
        string codigoOuNome, 
        string competencia, 
        bool naoImprimirSPZerado, 
        string ordenarPor, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Buscando procedimentos por código ou nome {Filtro} para relatório", codigoOuNome);
        return await _repository.BuscarProcedimentosPorCodigoOuNomeAsync(codigoOuNome, competencia, naoImprimirSPZerado, ordenarPor, cancellationToken);
    }

    public async Task<IEnumerable<ItemRelatorio>> BuscarGruposDisponiveisAsync(
        string competencia, 
        string? filtro, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Buscando grupos disponíveis para competência {Competencia}", competencia);
        return await _repository.BuscarGruposDisponiveisAsync(competencia, filtro, cancellationToken);
    }

    public async Task<IEnumerable<ItemRelatorio>> BuscarSubGruposDisponiveisAsync(
        string competencia, 
        string? filtro, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Buscando sub-grupos disponíveis para competência {Competencia}", competencia);
        return await _repository.BuscarSubGruposDisponiveisAsync(competencia, filtro, cancellationToken);
    }

    public async Task<IEnumerable<ItemRelatorio>> BuscarFormasOrganizacaoDisponiveisAsync(
        string competencia, 
        string? filtro, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Buscando formas de organização disponíveis para competência {Competencia}", competencia);
        return await _repository.BuscarFormasOrganizacaoDisponiveisAsync(competencia, filtro, cancellationToken);
    }

    public async Task<IEnumerable<ItemRelatorio>> BuscarProcedimentosDisponiveisAsync(
        string competencia, 
        string? filtro, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Buscando procedimentos disponíveis para competência {Competencia}", competencia);
        return await _repository.BuscarProcedimentosDisponiveisAsync(competencia, filtro, cancellationToken);
    }

    /// <summary>
    /// Busca procedimentos para relatório baseado nos itens selecionados
    /// </summary>
    public async Task<IEnumerable<ItemRelatorioProcedimento>> BuscarProcedimentosParaRelatorioAsync(
        IEnumerable<ItemRelatorio> itensSelecionados,
        string competencia,
        ConfiguracaoRelatorio configuracao,
        CancellationToken cancellationToken = default)
    {
        var todosProcedimentos = new List<ItemRelatorioProcedimento>();

        foreach (var item in itensSelecionados)
        {
            IEnumerable<ItemRelatorioProcedimento> procedimentos = item.Tipo switch
            {
                "Grupo" => await BuscarProcedimentosPorGrupoAsync(
                    item.Codigo, competencia, configuracao.NaoImprimirSPZerado, configuracao.OrdenarPor, cancellationToken),
                
                "SubGrupo" => await BuscarProcedimentosPorSubGrupoAsync(
                    item.Codigo.Substring(0, 2), 
                    item.Codigo.Substring(2, 2), 
                    competencia, 
                    configuracao.NaoImprimirSPZerado, 
                    configuracao.OrdenarPor, 
                    cancellationToken),
                
                "FormaOrganizacao" => await BuscarProcedimentosPorFormaOrganizacaoAsync(
                    item.Codigo.Substring(0, 2), 
                    item.Codigo.Substring(2, 2), 
                    item.Codigo.Substring(4, 2), 
                    competencia, 
                    configuracao.NaoImprimirSPZerado, 
                    configuracao.OrdenarPor, 
                    cancellationToken),
                
                "Procedimento" => await BuscarProcedimentosPorCodigoOuNomeAsync(
                    item.Codigo, competencia, configuracao.NaoImprimirSPZerado, configuracao.OrdenarPor, cancellationToken),
                
                _ => Enumerable.Empty<ItemRelatorioProcedimento>()
            };

            todosProcedimentos.AddRange(procedimentos);
        }

        // Remove duplicatas baseado no código do procedimento
        return todosProcedimentos
            .GroupBy(p => p.CoProcedimento)
            .Select(g => g.First())
            .ToList();
    }
}

