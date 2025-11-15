using FirebirdSql.Data.FirebirdClient;
using Microsoft.Extensions.Logging;
using UnificaSUS.Core.Entities;
using UnificaSUS.Core.Interfaces;
using UnificaSUS.Infrastructure.Data;
using UnificaSUS.Infrastructure.Helpers;

namespace UnificaSUS.Infrastructure.Repositories;

/// <summary>
/// Implementação do repositório de competência
/// </summary>
public class CompetenciaRepository : ICompetenciaRepository
{
    private readonly FirebirdContext _context;
    private readonly ILogger<CompetenciaRepository> _logger;

    public CompetenciaRepository(FirebirdContext context, ILogger<CompetenciaRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CompetenciaAtiva?> BuscarAtivaAsync(CancellationToken cancellationToken = default)
    {
        await _context.OpenAsync(cancellationToken);

        // Primeiro, tenta buscar da tabela TB_COMPETENCIA_ATIVA
        try
        {
            const string sqlAtiva = @"
                SELECT FIRST 1
                    DT_COMPETENCIA,
                    DT_ATIVACAO,
                    ST_ATIVA
                FROM TB_COMPETENCIA_ATIVA
                WHERE ST_ATIVA = 'S'
                ORDER BY DT_ATIVACAO DESC";

            using var commandAtiva = new FbCommand(sqlAtiva, _context.Connection);
            using var reader = await commandAtiva.ExecuteReaderAsync(cancellationToken);

            if (await reader.ReadAsync(cancellationToken))
            {
                return new CompetenciaAtiva
                {
                    DtCompetencia = FirebirdReaderHelper.GetStringSafe(reader, "DT_COMPETENCIA") ?? string.Empty,
                    DtAtivacao = reader.IsDBNull(reader.GetOrdinal("DT_ATIVACAO")) 
                        ? null 
                        : reader.GetDateTime(reader.GetOrdinal("DT_ATIVACAO")),
                    StAtiva = reader.IsDBNull(reader.GetOrdinal("ST_ATIVA")) 
                        ? null 
                        : FirebirdReaderHelper.GetStringSafe(reader, "ST_ATIVA")
                };
            }
        }
        catch (FirebirdSql.Data.FirebirdClient.FbException ex) when (ex.Message.Contains("Table unknown", StringComparison.OrdinalIgnoreCase) || 
                                                                      ex.ErrorCode == -204)
        {
            // Tabela TB_COMPETENCIA_ATIVA não existe - usar competência mais recente de TB_PROCEDIMENTO
            _logger.LogWarning("Tabela TB_COMPETENCIA_ATIVA não existe. Usando competência mais recente de TB_PROCEDIMENTO.");
            
            try
            {
                const string sqlRecente = @"
                    SELECT FIRST 1
                        DT_COMPETENCIA
                    FROM TB_PROCEDIMENTO
                    WHERE DT_COMPETENCIA IS NOT NULL
                    ORDER BY DT_COMPETENCIA DESC";

                using var commandRecente = new FbCommand(sqlRecente, _context.Connection);
                using var reader = await commandRecente.ExecuteReaderAsync(cancellationToken);

                if (await reader.ReadAsync(cancellationToken))
                {
                    return new CompetenciaAtiva
                    {
                        DtCompetencia = FirebirdReaderHelper.GetStringSafe(reader, "DT_COMPETENCIA") ?? string.Empty,
                        DtAtivacao = DateTime.Now,
                        StAtiva = "S"
                    };
                }
            }
            catch (Exception exRecente)
            {
                _logger.LogError(exRecente, "Erro ao buscar competência mais recente de TB_PROCEDIMENTO");
                throw new InvalidOperationException(
                    $"Erro ao buscar competência ativa no banco de dados:\n\n{exRecente.Message}\n\n" +
                    $"Verifique:\n" +
                    $"- Se o banco de dados está acessível\n" +
                    $"- Se existem procedimentos no banco", exRecente);
            }
        }
        catch (FirebirdSql.Data.FirebirdClient.FbException ex)
        {
            _logger.LogError(ex, "Erro de banco de dados ao buscar competência ativa. Mensagem: {Mensagem}", ex.Message);
            
            // Relançar com mensagem mais amigável
            throw new InvalidOperationException(
                $"Erro ao buscar competência ativa no banco de dados:\n\n{ex.Message}\n\n" +
                $"Verifique:\n" +
                $"- Se o banco de dados está acessível\n" +
                $"- Se a tabela TB_COMPETENCIA_ATIVA existe", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar competência ativa");
            throw;
        }

        return null;
    }

    public async Task<bool> AtivarAsync(string competencia, CancellationToken cancellationToken = default)
    {
        await _context.OpenAsync(cancellationToken);

        // Verificar se a tabela TB_COMPETENCIA_ATIVA existe
        bool tabelaExiste = await VerificarTabelaExisteAsync("TB_COMPETENCIA_ATIVA", cancellationToken);

        if (!tabelaExiste)
        {
            // Criar a tabela se não existir
            await CriarTabelaCompetenciaAtivaAsync(cancellationToken);
        }

        using var transaction = await _context.BeginTransactionAsync(cancellationToken);

        try
        {
            // Desativar todas as competências
            var desativarSql = "UPDATE TB_COMPETENCIA_ATIVA SET ST_ATIVA = 'N'";
            using var desativarCommand = new FbCommand(desativarSql, _context.Connection, transaction);
            await desativarCommand.ExecuteNonQueryAsync(cancellationToken);

            // Verificar se a competência existe
            var verificarSql = "SELECT COUNT(*) FROM TB_COMPETENCIA_ATIVA WHERE DT_COMPETENCIA = @competencia";
            using var verificarCommand = new FbCommand(verificarSql, _context.Connection, transaction);
            verificarCommand.Parameters.AddWithValue("@competencia", competencia);
            
            var existe = Convert.ToInt32(await verificarCommand.ExecuteScalarAsync(cancellationToken)) > 0;

            if (!existe)
            {
                // Inserir nova competência
                var inserirSql = @"
                    INSERT INTO TB_COMPETENCIA_ATIVA (DT_COMPETENCIA, ST_ATIVA, DT_ATIVACAO)
                    VALUES (@competencia, 'S', CURRENT_TIMESTAMP)";
                
                using var inserirCommand = new FbCommand(inserirSql, _context.Connection, transaction);
                inserirCommand.Parameters.AddWithValue("@competencia", competencia);
                await inserirCommand.ExecuteNonQueryAsync(cancellationToken);
            }
            else
            {
                // Ativar competência existente
                var ativarSql = @"
                    UPDATE TB_COMPETENCIA_ATIVA 
                    SET ST_ATIVA = 'S', DT_ATIVACAO = CURRENT_TIMESTAMP
                    WHERE DT_COMPETENCIA = @competencia";
                
                using var ativarCommand = new FbCommand(ativarSql, _context.Connection, transaction);
                ativarCommand.Parameters.AddWithValue("@competencia", competencia);
                await ativarCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Erro ao ativar competência {Competencia}", competencia);
            throw;
        }
    }

    private async Task<bool> VerificarTabelaExisteAsync(string nomeTabela, CancellationToken cancellationToken)
    {
        try
        {
            const string sql = @"
                SELECT COUNT(*) 
                FROM RDB$RELATIONS 
                WHERE RDB$RELATION_NAME = UPPER(@tabela)";

            using var command = new FbCommand(sql, _context.Connection);
            command.Parameters.AddWithValue("@tabela", nomeTabela);
            
            var resultado = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(resultado) > 0;
        }
        catch
        {
            return false;
        }
    }

    private async Task CriarTabelaCompetenciaAtivaAsync(CancellationToken cancellationToken)
    {
        const string sql = @"
            CREATE TABLE TB_COMPETENCIA_ATIVA (
                DT_COMPETENCIA VARCHAR(6) NOT NULL PRIMARY KEY,
                DT_ATIVACAO TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                ST_ATIVA CHAR(1) DEFAULT 'S'
            )";

        using var command = new FbCommand(sql, _context.Connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
        
        _logger.LogInformation("Tabela TB_COMPETENCIA_ATIVA criada com sucesso");
    }

    public async Task<IEnumerable<string>> ListarDisponiveisAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT DISTINCT DT_COMPETENCIA
            FROM TB_PROCEDIMENTO
            WHERE DT_COMPETENCIA IS NOT NULL
            ORDER BY DT_COMPETENCIA DESC";

        await _context.OpenAsync(cancellationToken);

        var competencias = new List<string>();

        using var command = new FbCommand(sql, _context.Connection);

        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var competencia = FirebirdReaderHelper.GetStringSafe(reader, "DT_COMPETENCIA");
                if (!string.IsNullOrEmpty(competencia))
                    competencias.Add(competencia);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar competências disponíveis");
            throw;
        }

        return competencias;
    }
}

