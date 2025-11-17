using Microsoft.Extensions.Logging;
using UnificaSUS.Core.Entities;
using UnificaSUS.Core.Interfaces;

namespace UnificaSUS.Application.Services;

/// <summary>
/// Serviço de aplicação para procedimentos comuns
/// </summary>
public class ProcedimentoComumService
{
    private readonly IProcedimentoComumRepository _repository;
    private readonly ILogger<ProcedimentoComumService> _logger;

    public ProcedimentoComumService(IProcedimentoComumRepository repository, ILogger<ProcedimentoComumService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Busca todos os procedimentos comuns
    /// </summary>
    public async Task<IEnumerable<ProcedimentoComum>> BuscarTodosAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Buscando todos os procedimentos comuns");
        
        try
        {
            return await _repository.BuscarTodosAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar procedimentos comuns");
            throw;
        }
    }

    /// <summary>
    /// Busca um procedimento comum por código
    /// </summary>
    public async Task<ProcedimentoComum?> BuscarPorCodigoAsync(int prcCod, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Buscando procedimento comum por código: {Codigo}", prcCod);
        
        try
        {
            return await _repository.BuscarPorCodigoAsync(prcCod, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar procedimento comum por código {Codigo}", prcCod);
            throw;
        }
    }

    /// <summary>
    /// Busca procedimento comum por código de procedimento
    /// </summary>
    public async Task<ProcedimentoComum?> BuscarPorCodigoProcedimentoAsync(string codigoProcedimento, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Buscando procedimento comum por código de procedimento: {Codigo}", codigoProcedimento);
        
        try
        {
            return await _repository.BuscarPorCodigoProcedimentoAsync(codigoProcedimento, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar procedimento comum por código de procedimento {Codigo}", codigoProcedimento);
            throw;
        }
    }

    /// <summary>
    /// Adiciona um novo procedimento comum
    /// </summary>
    public async Task<int> AdicionarAsync(ProcedimentoComum procedimentoComum, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adicionando procedimento comum: {Codigo}", procedimentoComum.PrcCodProc);
        
        try
        {
            return await _repository.AdicionarAsync(procedimentoComum, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao adicionar procedimento comum");
            throw;
        }
    }

    /// <summary>
    /// Atualiza um procedimento comum existente
    /// </summary>
    public async Task AtualizarAsync(ProcedimentoComum procedimentoComum, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Atualizando procedimento comum: {Codigo}", procedimentoComum.PrcCod);
        
        try
        {
            await _repository.AtualizarAsync(procedimentoComum, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar procedimento comum {Codigo}", procedimentoComum.PrcCod);
            throw;
        }
    }

    /// <summary>
    /// Remove um procedimento comum
    /// </summary>
    public async Task RemoverAsync(int prcCod, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Removendo procedimento comum: {Codigo}", prcCod);
        
        try
        {
            await _repository.RemoverAsync(prcCod, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao remover procedimento comum {Codigo}", prcCod);
            throw;
        }
    }

    /// <summary>
    /// Obtém o próximo código disponível
    /// </summary>
    public async Task<int> ObterProximoCodigoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.ObterProximoCodigoAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter próximo código de procedimento comum");
            throw;
        }
    }
}

