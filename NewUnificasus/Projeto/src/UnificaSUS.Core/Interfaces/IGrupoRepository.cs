using UnificaSUS.Core.Entities;

namespace UnificaSUS.Core.Interfaces;

/// <summary>
/// Interface do repositório de grupos
/// </summary>
public interface IGrupoRepository
{
    /// <summary>
    /// Busca todos os grupos por competência
    /// </summary>
    Task<IEnumerable<Grupo>> BuscarTodosAsync(string competencia, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Busca um grupo específico
    /// </summary>
    Task<Grupo?> BuscarPorCodigoAsync(string codigo, string competencia, CancellationToken cancellationToken = default);
}

