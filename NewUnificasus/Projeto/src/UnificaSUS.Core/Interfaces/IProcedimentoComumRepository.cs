using UnificaSUS.Core.Entities;

namespace UnificaSUS.Core.Interfaces;

/// <summary>
/// Interface do repositório de procedimentos comuns
/// </summary>
public interface IProcedimentoComumRepository
{
    /// <summary>
    /// Busca todos os procedimentos comuns
    /// </summary>
    Task<IEnumerable<ProcedimentoComum>> BuscarTodosAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca um procedimento comum por código
    /// </summary>
    Task<ProcedimentoComum?> BuscarPorCodigoAsync(int prcCod, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca procedimentos comuns por código de procedimento
    /// </summary>
    Task<ProcedimentoComum?> BuscarPorCodigoProcedimentoAsync(string codigoProcedimento, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona um novo procedimento comum
    /// </summary>
    Task<int> AdicionarAsync(ProcedimentoComum procedimentoComum, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza um procedimento comum existente
    /// </summary>
    Task AtualizarAsync(ProcedimentoComum procedimentoComum, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove um procedimento comum
    /// </summary>
    Task RemoverAsync(int prcCod, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém o próximo código disponível para um novo registro
    /// </summary>
    Task<int> ObterProximoCodigoAsync(CancellationToken cancellationToken = default);
}

