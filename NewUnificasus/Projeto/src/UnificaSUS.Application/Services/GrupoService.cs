using Microsoft.Extensions.Logging;
using UnificaSUS.Core.Entities;
using UnificaSUS.Core.Interfaces;

namespace UnificaSUS.Application.Services;

/// <summary>
/// Serviço de aplicação para grupos
/// </summary>
public class GrupoService
{
    private readonly IGrupoRepository _repository;
    private readonly ILogger<GrupoService> _logger;

    public GrupoService(IGrupoRepository repository, ILogger<GrupoService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Busca todos os grupos por competência
    /// </summary>
    public async Task<IEnumerable<Grupo>> BuscarTodosAsync(string competencia, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Buscando grupos por competência: {Competencia}", competencia);
        
        try
        {
            return await _repository.BuscarTodosAsync(competencia, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar grupos por competência {Competencia}", competencia);
            throw;
        }
    }

    /// <summary>
    /// Busca um grupo específico
    /// </summary>
    public async Task<Grupo?> BuscarPorCodigoAsync(string codigo, string competencia, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Buscando grupo {Codigo} por competência: {Competencia}", codigo, competencia);
        
        try
        {
            return await _repository.BuscarPorCodigoAsync(codigo, competencia, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar grupo {Codigo} por competência {Competencia}", codigo, competencia);
            throw;
        }
    }
}

