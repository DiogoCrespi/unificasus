using UnificaSUS.Core.Entities;

namespace UnificaSUS.Core.Interfaces;

/// <summary>
/// Interface para repositório de classificações de serviços
/// </summary>
public interface IServicoClassificacaoRepository
{
    /// <summary>
    /// Busca todas as classificações de serviços para uma competência
    /// </summary>
    Task<IEnumerable<ServicoClassificacao>> BuscarTodosAsync(string competencia, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca uma classificação específica por código de serviço e classificação
    /// </summary>
    Task<ServicoClassificacao?> BuscarPorCodigosAsync(string coServico, string coClassificacao, string competencia, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca classificações por código de serviço
    /// </summary>
    Task<IEnumerable<ServicoClassificacao>> BuscarPorServicoAsync(string coServico, string competencia, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona uma nova classificação de serviço
    /// </summary>
    Task AdicionarAsync(ServicoClassificacao servicoClassificacao, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza uma classificação de serviço existente
    /// </summary>
    Task AtualizarAsync(ServicoClassificacao servicoClassificacao, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove uma classificação de serviço
    /// </summary>
    Task RemoverAsync(string coServico, string coClassificacao, string competencia, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se uma classificação já existe
    /// </summary>
    Task<bool> ExisteAsync(string coServico, string coClassificacao, string competencia, CancellationToken cancellationToken = default);
}

