using FirebirdSql.Data.FirebirdClient;
using Microsoft.Extensions.Logging;
using System.Text;
using UnificaSUS.Core.Entities;
using UnificaSUS.Core.Interfaces;
using UnificaSUS.Infrastructure.Data;
using UnificaSUS.Infrastructure.Helpers;

namespace UnificaSUS.Infrastructure.Repositories;

/// <summary>
/// Implementação do repositório de classificações de serviços
/// </summary>
public class ServicoClassificacaoRepository : IServicoClassificacaoRepository
{
    private readonly FirebirdContext _context;
    private readonly ILogger<ServicoClassificacaoRepository> _logger;

    public ServicoClassificacaoRepository(FirebirdContext context, ILogger<ServicoClassificacaoRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<ServicoClassificacao>> BuscarTodosAsync(string competencia, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                sc.CO_SERVICO,
                sc.CO_CLASSIFICACAO,
                CAST(sc.NO_CLASSIFICACAO AS BLOB) AS NO_CLASSIFICACAO_BLOB,
                sc.NO_CLASSIFICACAO,
                sc.DT_COMPETENCIA,
                s.CO_SERVICO AS SERVICO_CO_SERVICO,
                CAST(s.NO_SERVICO AS BLOB) AS SERVICO_NO_SERVICO_BLOB,
                s.NO_SERVICO AS SERVICO_NO_SERVICO
            FROM TB_SERVICO_CLASSIFICACAO sc
            INNER JOIN TB_SERVICO s ON sc.CO_SERVICO = s.CO_SERVICO 
                AND sc.DT_COMPETENCIA = s.DT_COMPETENCIA
            WHERE sc.DT_COMPETENCIA = @competencia
            ORDER BY sc.CO_SERVICO, sc.CO_CLASSIFICACAO";

        await _context.OpenAsync(cancellationToken);

        var classificacoes = new List<ServicoClassificacao>();

        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@competencia", competencia);

        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                classificacoes.Add(MapServicoClassificacao(reader));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar classificações de serviços para competência {Competencia}", competencia);
            throw;
        }

        return classificacoes;
    }

    public async Task<ServicoClassificacao?> BuscarPorCodigosAsync(string coServico, string coClassificacao, string competencia, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                sc.CO_SERVICO,
                sc.CO_CLASSIFICACAO,
                CAST(sc.NO_CLASSIFICACAO AS BLOB) AS NO_CLASSIFICACAO_BLOB,
                sc.NO_CLASSIFICACAO,
                sc.DT_COMPETENCIA,
                s.CO_SERVICO AS SERVICO_CO_SERVICO,
                CAST(s.NO_SERVICO AS BLOB) AS SERVICO_NO_SERVICO_BLOB,
                s.NO_SERVICO AS SERVICO_NO_SERVICO
            FROM TB_SERVICO_CLASSIFICACAO sc
            INNER JOIN TB_SERVICO s ON sc.CO_SERVICO = s.CO_SERVICO 
                AND sc.DT_COMPETENCIA = s.DT_COMPETENCIA
            WHERE sc.CO_SERVICO = @coServico
              AND sc.CO_CLASSIFICACAO = @coClassificacao
              AND sc.DT_COMPETENCIA = @competencia";

        await _context.OpenAsync(cancellationToken);

        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@coServico", coServico);
        command.Parameters.AddWithValue("@coClassificacao", coClassificacao);
        command.Parameters.AddWithValue("@competencia", competencia);

        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (await reader.ReadAsync(cancellationToken))
            {
                return MapServicoClassificacao(reader);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar classificação de serviço {Servico}/{Classificacao} para competência {Competencia}", 
                coServico, coClassificacao, competencia);
            throw;
        }

        return null;
    }

    public async Task<IEnumerable<ServicoClassificacao>> BuscarPorServicoAsync(string coServico, string competencia, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                sc.CO_SERVICO,
                sc.CO_CLASSIFICACAO,
                CAST(sc.NO_CLASSIFICACAO AS BLOB) AS NO_CLASSIFICACAO_BLOB,
                sc.NO_CLASSIFICACAO,
                sc.DT_COMPETENCIA,
                s.CO_SERVICO AS SERVICO_CO_SERVICO,
                CAST(s.NO_SERVICO AS BLOB) AS SERVICO_NO_SERVICO_BLOB,
                s.NO_SERVICO AS SERVICO_NO_SERVICO
            FROM TB_SERVICO_CLASSIFICACAO sc
            INNER JOIN TB_SERVICO s ON sc.CO_SERVICO = s.CO_SERVICO 
                AND sc.DT_COMPETENCIA = s.DT_COMPETENCIA
            WHERE sc.CO_SERVICO = @coServico
              AND sc.DT_COMPETENCIA = @competencia
            ORDER BY sc.CO_CLASSIFICACAO";

        await _context.OpenAsync(cancellationToken);

        var classificacoes = new List<ServicoClassificacao>();

        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@coServico", coServico);
        command.Parameters.AddWithValue("@competencia", competencia);

        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                classificacoes.Add(MapServicoClassificacao(reader));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar classificações do serviço {Servico} para competência {Competencia}", 
                coServico, competencia);
            throw;
        }

        return classificacoes;
    }

    public async Task AdicionarAsync(ServicoClassificacao servicoClassificacao, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO TB_SERVICO_CLASSIFICACAO (CO_SERVICO, CO_CLASSIFICACAO, NO_CLASSIFICACAO, DT_COMPETENCIA)
            VALUES (@coServico, @coClassificacao, @noClassificacao, @dtCompetencia)";

        await _context.OpenAsync(cancellationToken);

        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@coServico", servicoClassificacao.CoServico);
        command.Parameters.AddWithValue("@coClassificacao", servicoClassificacao.CoClassificacao);
        command.Parameters.AddWithValue("@noClassificacao", servicoClassificacao.NoClassificacao ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@dtCompetencia", servicoClassificacao.DtCompetencia ?? (object)DBNull.Value);

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao adicionar classificação de serviço");
            throw;
        }
    }

    public async Task AtualizarAsync(ServicoClassificacao servicoClassificacao, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE TB_SERVICO_CLASSIFICACAO 
            SET NO_CLASSIFICACAO = @noClassificacao
            WHERE CO_SERVICO = @coServico
              AND CO_CLASSIFICACAO = @coClassificacao
              AND DT_COMPETENCIA = @dtCompetencia";

        await _context.OpenAsync(cancellationToken);

        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@coServico", servicoClassificacao.CoServico);
        command.Parameters.AddWithValue("@coClassificacao", servicoClassificacao.CoClassificacao);
        command.Parameters.AddWithValue("@noClassificacao", servicoClassificacao.NoClassificacao ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@dtCompetencia", servicoClassificacao.DtCompetencia ?? (object)DBNull.Value);

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar classificação de serviço");
            throw;
        }
    }

    public async Task RemoverAsync(string coServico, string coClassificacao, string competencia, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            DELETE FROM TB_SERVICO_CLASSIFICACAO 
            WHERE CO_SERVICO = @coServico
              AND CO_CLASSIFICACAO = @coClassificacao
              AND DT_COMPETENCIA = @competencia";

        await _context.OpenAsync(cancellationToken);

        using var transaction = await _context.BeginTransactionAsync(cancellationToken);

        try
        {
            using var command = new FbCommand(sql, _context.Connection, transaction);
            command.Parameters.AddWithValue("@coServico", coServico);
            command.Parameters.AddWithValue("@coClassificacao", coClassificacao);
            command.Parameters.AddWithValue("@competencia", competencia);

            await command.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Erro ao remover classificação de serviço");
            throw;
        }
    }

    public async Task<bool> ExisteAsync(string coServico, string coClassificacao, string competencia, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT COUNT(*) 
            FROM TB_SERVICO_CLASSIFICACAO
            WHERE CO_SERVICO = @coServico
              AND CO_CLASSIFICACAO = @coClassificacao
              AND DT_COMPETENCIA = @competencia";

        await _context.OpenAsync(cancellationToken);

        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@coServico", coServico);
        command.Parameters.AddWithValue("@coClassificacao", coClassificacao);
        command.Parameters.AddWithValue("@competencia", competencia);

        try
        {
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result != null && Convert.ToInt32(result) > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar existência de classificação de serviço");
            throw;
        }
    }

    private static ServicoClassificacao MapServicoClassificacao(FbDataReader reader)
    {
        var noClassificacao = LerCampoTextoDoBlob(reader, "NO_CLASSIFICACAO_BLOB", "NO_CLASSIFICACAO");

        var servico = new Servico
        {
            CoServico = FirebirdReaderHelper.GetStringSafe(reader, "SERVICO_CO_SERVICO") ?? string.Empty,
            NoServico = LerCampoTextoDoBlob(reader, "SERVICO_NO_SERVICO_BLOB", "SERVICO_NO_SERVICO"),
            DtCompetencia = FirebirdReaderHelper.GetStringSafe(reader, "DT_COMPETENCIA")
        };

        return new ServicoClassificacao
        {
            CoServico = FirebirdReaderHelper.GetStringSafe(reader, "CO_SERVICO") ?? string.Empty,
            CoClassificacao = FirebirdReaderHelper.GetStringSafe(reader, "CO_CLASSIFICACAO") ?? string.Empty,
            NoClassificacao = noClassificacao,
            DtCompetencia = FirebirdReaderHelper.GetStringSafe(reader, "DT_COMPETENCIA"),
            Servico = servico
        };
    }

    private static string? LerCampoTextoDoBlob(FbDataReader reader, string blobColumnName, string directColumnName)
    {
        string? resultado = null;

        // Prioridade 1: Tenta ler do BLOB
        try
        {
            var blobOrdinal = reader.GetOrdinal(blobColumnName);
            if (!reader.IsDBNull(blobOrdinal))
            {
                var blobValue = reader.GetValue(blobOrdinal);
                if (blobValue is byte[] blobBytes && blobBytes.Length > 0)
                {
                    int validLength = blobBytes.Length;
                    while (validLength > 0 && blobBytes[validLength - 1] == 0)
                    {
                        validLength--;
                    }

                    if (validLength > 0)
                    {
                        byte[] validBytes = new byte[validLength];
                        Array.Copy(blobBytes, 0, validBytes, 0, validLength);
                        resultado = Encoding.UTF8.GetString(validBytes);

                        if (!string.IsNullOrEmpty(resultado) && 
                            (resultado.Contains('\uFFFD') || resultado.Contains('?')))
                        {
                            resultado = FirebirdReaderHelper.GetStringSafe(reader, blobColumnName);
                        }
                    }
                }

                if (string.IsNullOrEmpty(resultado))
                {
                    resultado = FirebirdReaderHelper.GetStringSafe(reader, blobColumnName);
                }
            }
        }
        catch
        {
            // Se não encontrar o BLOB, continua para campo direto
        }

        // Prioridade 2: Se BLOB não funcionou, tenta campo direto
        if (string.IsNullOrEmpty(resultado))
        {
            try
            {
                var campoOrdinal = reader.GetOrdinal(directColumnName);
                if (!reader.IsDBNull(campoOrdinal))
                {
                    resultado = FirebirdReaderHelper.GetStringSafe(reader, directColumnName);
                }
            }
            catch
            {
                // Se não conseguir ler, deixa null
            }
        }

        return resultado;
    }
}

