using Microsoft.Extensions.Logging;
using UnificaSUS.Core.Entities;
using UnificaSUS.Core.Interfaces;

namespace UnificaSUS.Application.Services;

/// <summary>
/// Serviço de aplicação para classificações de serviços
/// </summary>
public class ServicoClassificacaoService
{
    private readonly IServicoClassificacaoRepository _repository;
    private readonly ILogger<ServicoClassificacaoService> _logger;

    public ServicoClassificacaoService(IServicoClassificacaoRepository repository, ILogger<ServicoClassificacaoService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<ServicoClassificacao>> BuscarTodosAsync(string competencia, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Buscando todas as classificações de serviços para competência {Competencia}", competencia);
        
        try
        {
            return await _repository.BuscarTodosAsync(competencia, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar classificações de serviços");
            throw;
        }
    }

    public async Task<ServicoClassificacao?> BuscarPorCodigosAsync(string coServico, string coClassificacao, string competencia, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Buscando classificação {Servico}/{Classificacao} para competência {Competencia}", 
            coServico, coClassificacao, competencia);
        
        try
        {
            return await _repository.BuscarPorCodigosAsync(coServico, coClassificacao, competencia, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar classificação de serviço");
            throw;
        }
    }

    public async Task<IEnumerable<ServicoClassificacao>> BuscarPorServicoAsync(string coServico, string competencia, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Buscando classificações do serviço {Servico} para competência {Competencia}", 
            coServico, competencia);
        
        try
        {
            return await _repository.BuscarPorServicoAsync(coServico, competencia, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar classificações por serviço");
            throw;
        }
    }

    public async Task AdicionarAsync(ServicoClassificacao servicoClassificacao, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adicionando classificação de serviço {Servico}/{Classificacao}", 
            servicoClassificacao.CoServico, servicoClassificacao.CoClassificacao);
        
        try
        {
            // Verificar se já existe
            var existe = await _repository.ExisteAsync(
                servicoClassificacao.CoServico, 
                servicoClassificacao.CoClassificacao, 
                servicoClassificacao.DtCompetencia ?? string.Empty, 
                cancellationToken);

            if (existe)
            {
                throw new InvalidOperationException(
                    $"Já existe uma classificação {servicoClassificacao.CoClassificacao} para o serviço {servicoClassificacao.CoServico} na competência {servicoClassificacao.DtCompetencia}");
            }

            await _repository.AdicionarAsync(servicoClassificacao, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao adicionar classificação de serviço");
            throw;
        }
    }

    public async Task AtualizarAsync(ServicoClassificacao servicoClassificacao, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Atualizando classificação de serviço {Servico}/{Classificacao}", 
            servicoClassificacao.CoServico, servicoClassificacao.CoClassificacao);
        
        try
        {
            await _repository.AtualizarAsync(servicoClassificacao, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar classificação de serviço");
            throw;
        }
    }

    public async Task RemoverAsync(string coServico, string coClassificacao, string competencia, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Removendo classificação de serviço {Servico}/{Classificacao}", coServico, coClassificacao);
        
        try
        {
            await _repository.RemoverAsync(coServico, coClassificacao, competencia, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao remover classificação de serviço");
            throw;
        }
    }

    public async Task<bool> ExisteAsync(string coServico, string coClassificacao, string competencia, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.ExisteAsync(coServico, coClassificacao, competencia, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar existência de classificação de serviço");
            throw;
        }
    }
}

