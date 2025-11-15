using Microsoft.Extensions.Logging;
using UnificaSUS.Core.Entities;
using UnificaSUS.Core.Interfaces;

namespace UnificaSUS.Application.Services;

/// <summary>
/// Serviço de aplicação para procedimentos
/// </summary>
public class ProcedimentoService
{
    private readonly IProcedimentoRepository _repository;
    private readonly ILogger<ProcedimentoService> _logger;

    public ProcedimentoService(IProcedimentoRepository repository, ILogger<ProcedimentoService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Busca procedimentos por competência
    /// </summary>
    public async Task<IEnumerable<Procedimento>> BuscarPorCompetenciaAsync(string competencia, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Buscando procedimentos por competência: {Competencia}", competencia);
        
        try
        {
            return await _repository.BuscarPorCompetenciaAsync(competencia, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar procedimentos por competência {Competencia}", competencia);
            throw;
        }
    }

    /// <summary>
    /// Busca um procedimento por código
    /// </summary>
    public async Task<Procedimento?> BuscarPorCodigoAsync(string codigo, string competencia, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Buscando procedimento por código: {Codigo} e competência: {Competencia}", codigo, competencia);
        
        try
        {
            return await _repository.BuscarPorCodigoAsync(codigo, competencia, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar procedimento por código {Codigo} e competência {Competencia}", codigo, competencia);
            throw;
        }
    }

    /// <summary>
    /// Busca procedimentos por filtro
    /// </summary>
    public async Task<IEnumerable<Procedimento>> BuscarPorFiltroAsync(string filtro, string competencia, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Buscando procedimentos por filtro: {Filtro} e competência: {Competencia}", filtro, competencia);
        
        if (string.IsNullOrWhiteSpace(filtro))
        {
            return await BuscarPorCompetenciaAsync(competencia, cancellationToken);
        }

        try
        {
            return await _repository.BuscarPorFiltroAsync(filtro, competencia, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar procedimentos por filtro {Filtro} e competência {Competencia}", filtro, competencia);
            throw;
        }
    }

    /// <summary>
    /// Busca procedimentos relacionados a um CID
    /// </summary>
    public async Task<IEnumerable<Procedimento>> BuscarPorCIDAsync(string cid, string competencia, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Buscando procedimentos por CID: {CID} e competência: {Competencia}", cid, competencia);
        
        try
        {
            return await _repository.BuscarPorCIDAsync(cid, competencia, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar procedimentos por CID {CID} e competência {Competencia}", cid, competencia);
            throw;
        }
    }

    /// <summary>
    /// Busca procedimentos relacionados a um serviço
    /// </summary>
    public async Task<IEnumerable<Procedimento>> BuscarPorServicoAsync(string servico, string competencia, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Buscando procedimentos por serviço: {Servico} e competência: {Competencia}", servico, competencia);
        
        try
        {
            return await _repository.BuscarPorServicoAsync(servico, competencia, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar procedimentos por serviço {Servico} e competência {Competencia}", servico, competencia);
            throw;
        }
    }

    /// <summary>
    /// Busca procedimentos por estrutura (grupo, sub-grupo, forma de organização)
    /// </summary>
    public async Task<IEnumerable<Procedimento>> BuscarPorEstruturaAsync(string? coGrupo, string? coSubGrupo, string? coFormaOrganizacao, string competencia, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Buscando procedimentos por estrutura. Grupo: {Grupo}, SubGrupo: {SubGrupo}, FormaOrganizacao: {FormaOrganizacao}, Competencia: {Competencia}", 
            coGrupo, coSubGrupo, coFormaOrganizacao, competencia);
        
        try
        {
            return await _repository.BuscarPorEstruturaAsync(coGrupo, coSubGrupo, coFormaOrganizacao, competencia, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar procedimentos por estrutura. Grupo: {Grupo}, SubGrupo: {SubGrupo}, FormaOrganizacao: {FormaOrganizacao}, Competencia: {Competencia}", 
                coGrupo, coSubGrupo, coFormaOrganizacao, competencia);
            throw;
        }
    }
}

