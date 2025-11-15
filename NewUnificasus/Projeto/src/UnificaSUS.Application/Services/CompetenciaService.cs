using Microsoft.Extensions.Logging;
using UnificaSUS.Core.Entities;
using UnificaSUS.Core.Interfaces;

namespace UnificaSUS.Application.Services;

/// <summary>
/// Serviço de aplicação para competências
/// </summary>
public class CompetenciaService
{
    private readonly ICompetenciaRepository _repository;
    private readonly ILogger<CompetenciaService> _logger;

    public CompetenciaService(ICompetenciaRepository repository, ILogger<CompetenciaService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Busca a competência ativa
    /// </summary>
    public async Task<CompetenciaAtiva?> BuscarAtivaAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Buscando competência ativa");
        
        try
        {
            return await _repository.BuscarAtivaAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar competência ativa");
            throw;
        }
    }

    /// <summary>
    /// Ativa uma competência
    /// </summary>
    public async Task<bool> AtivarAsync(string competencia, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Ativando competência: {Competencia}", competencia);
        
        try
        {
            return await _repository.AtivarAsync(competencia, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao ativar competência {Competencia}", competencia);
            throw;
        }
    }

    /// <summary>
    /// Lista todas as competências disponíveis
    /// </summary>
    public async Task<IEnumerable<string>> ListarDisponiveisAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Listando competências disponíveis");
        
        try
        {
            return await _repository.ListarDisponiveisAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar competências disponíveis");
            throw;
        }
    }
}

