using UnificaSUS.Core.Entities;

namespace UnificaSUS.Core.Interfaces;

/// <summary>
/// Interface do repositório de competência
/// </summary>
public interface ICompetenciaRepository
{
    /// <summary>
    /// Busca a competência ativa
    /// </summary>
    Task<CompetenciaAtiva?> BuscarAtivaAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Ativa uma competência
    /// </summary>
    Task<bool> AtivarAsync(string competencia, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Lista todas as competências disponíveis
    /// </summary>
    Task<IEnumerable<string>> ListarDisponiveisAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Registra uma nova competência na tabela TB_COMPETENCIA_ATIVA sem ativá-la
    /// </summary>
    Task<bool> RegistrarCompetenciaAsync(string competencia, CancellationToken cancellationToken = default);
}

