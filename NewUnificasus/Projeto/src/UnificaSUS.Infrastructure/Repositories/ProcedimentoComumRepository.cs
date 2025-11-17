using FirebirdSql.Data.FirebirdClient;
using Microsoft.Extensions.Logging;
using System.Text;
using UnificaSUS.Core.Entities;
using UnificaSUS.Core.Interfaces;
using UnificaSUS.Infrastructure.Data;
using UnificaSUS.Infrastructure.Helpers;

namespace UnificaSUS.Infrastructure.Repositories;

/// <summary>
/// Implementação do repositório de procedimentos comuns
/// </summary>
public class ProcedimentoComumRepository : IProcedimentoComumRepository
{
    private readonly FirebirdContext _context;
    private readonly ILogger<ProcedimentoComumRepository> _logger;

    public ProcedimentoComumRepository(FirebirdContext context, ILogger<ProcedimentoComumRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<ProcedimentoComum>> BuscarTodosAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                PRC_COD,
                PRC_CODPROC,
                CAST(PRC_NO_PROCEDIMENTO AS BLOB) AS PRC_NO_PROCEDIMENTO_BLOB,
                PRC_NO_PROCEDIMENTO,
                CAST(PRC_OBSERVACOES AS BLOB) AS PRC_OBSERVACOES_BLOB,
                PRC_OBSERVACOES
            FROM TB_PROCOMUNS
            ORDER BY PRC_COD";

        await _context.OpenAsync(cancellationToken);

        var procedimentos = new List<ProcedimentoComum>();

        using var command = new FbCommand(sql, _context.Connection);

        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                procedimentos.Add(MapProcedimentoComum(reader));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar procedimentos comuns");
            throw;
        }

        return procedimentos;
    }

    public async Task<ProcedimentoComum?> BuscarPorCodigoAsync(int prcCod, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                PRC_COD,
                PRC_CODPROC,
                CAST(PRC_NO_PROCEDIMENTO AS BLOB) AS PRC_NO_PROCEDIMENTO_BLOB,
                PRC_NO_PROCEDIMENTO,
                CAST(PRC_OBSERVACOES AS BLOB) AS PRC_OBSERVACOES_BLOB,
                PRC_OBSERVACOES
            FROM TB_PROCOMUNS
            WHERE PRC_COD = @prcCod";

        await _context.OpenAsync(cancellationToken);

        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@prcCod", prcCod);

        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (await reader.ReadAsync(cancellationToken))
            {
                return MapProcedimentoComum(reader);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar procedimento comum por código {Codigo}", prcCod);
            throw;
        }

        return null;
    }

    public async Task<ProcedimentoComum?> BuscarPorCodigoProcedimentoAsync(string codigoProcedimento, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                PRC_COD,
                PRC_CODPROC,
                CAST(PRC_NO_PROCEDIMENTO AS BLOB) AS PRC_NO_PROCEDIMENTO_BLOB,
                PRC_NO_PROCEDIMENTO,
                CAST(PRC_OBSERVACOES AS BLOB) AS PRC_OBSERVACOES_BLOB,
                PRC_OBSERVACOES
            FROM TB_PROCOMUNS
            WHERE PRC_CODPROC = @codigoProcedimento";

        await _context.OpenAsync(cancellationToken);

        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@codigoProcedimento", codigoProcedimento);

        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (await reader.ReadAsync(cancellationToken))
            {
                return MapProcedimentoComum(reader);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar procedimento comum por código de procedimento {Codigo}", codigoProcedimento);
            throw;
        }

        return null;
    }

    public async Task<int> AdicionarAsync(ProcedimentoComum procedimentoComum, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO TB_PROCOMUNS (PRC_COD, PRC_CODPROC, PRC_NO_PROCEDIMENTO, PRC_OBSERVACOES)
            VALUES (@prcCod, @prcCodProc, @prcNoProcedimento, @prcObservacoes)";

        await _context.OpenAsync(cancellationToken);

        // Usar transação explícita para evitar deadlocks
        using var transaction = await _context.BeginTransactionAsync(cancellationToken);
        
        try
        {
            using var command = new FbCommand(sql, _context.Connection, transaction);
            command.Parameters.AddWithValue("@prcCod", procedimentoComum.PrcCod);
            command.Parameters.AddWithValue("@prcCodProc", procedimentoComum.PrcCodProc ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@prcNoProcedimento", procedimentoComum.PrcNoProcedimento ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@prcObservacoes", procedimentoComum.PrcObservacoes ?? (object)DBNull.Value);

            await command.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            
            return procedimentoComum.PrcCod;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Erro ao adicionar procedimento comum");
            throw;
        }
    }

    public async Task AtualizarAsync(ProcedimentoComum procedimentoComum, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE TB_PROCOMUNS 
            SET PRC_CODPROC = @prcCodProc,
                PRC_NO_PROCEDIMENTO = @prcNoProcedimento,
                PRC_OBSERVACOES = @prcObservacoes
            WHERE PRC_COD = @prcCod";

        await _context.OpenAsync(cancellationToken);

        // Usar transação explícita para evitar deadlocks
        using var transaction = await _context.BeginTransactionAsync(cancellationToken);
        
        try
        {
            using var command = new FbCommand(sql, _context.Connection, transaction);
            command.Parameters.AddWithValue("@prcCod", procedimentoComum.PrcCod);
            command.Parameters.AddWithValue("@prcCodProc", procedimentoComum.PrcCodProc ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@prcNoProcedimento", procedimentoComum.PrcNoProcedimento ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@prcObservacoes", procedimentoComum.PrcObservacoes ?? (object)DBNull.Value);

            await command.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Erro ao atualizar procedimento comum {Codigo}", procedimentoComum.PrcCod);
            throw;
        }
    }

    public async Task RemoverAsync(int prcCod, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            DELETE FROM TB_PROCOMUNS 
            WHERE PRC_COD = @prcCod";

        await _context.OpenAsync(cancellationToken);

        // Usar transação explícita para evitar deadlocks
        using var transaction = await _context.BeginTransactionAsync(cancellationToken);
        
        try
        {
            using var command = new FbCommand(sql, _context.Connection, transaction);
            command.Parameters.AddWithValue("@prcCod", prcCod);

            await command.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Erro ao remover procedimento comum {Codigo}", prcCod);
            throw;
        }
    }

    public async Task<int> ObterProximoCodigoAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT MAX(PRC_COD) + 1
            FROM TB_PROCOMUNS";

        await _context.OpenAsync(cancellationToken);

        using var command = new FbCommand(sql, _context.Connection);

        try
        {
            var result = await command.ExecuteScalarAsync(cancellationToken);
            
            if (result != null && result != DBNull.Value)
            {
                return Convert.ToInt32(result);
            }
            
            return 1; // Se não houver registros, começa com 1
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter próximo código de procedimento comum");
            throw;
        }
    }

    private static ProcedimentoComum MapProcedimentoComum(FbDataReader reader)
    {
        // Tenta ler PRC_NO_PROCEDIMENTO do BLOB primeiro (mais confiável para encoding)
        // Mesma lógica usada no MapProcedimento do ProcedimentoRepository
        string? prcNoProcedimento = null;
        string? prcObservacoes = null;
        
        // Prioridade 1: Tenta ler do BLOB (CAST para BLOB garante acesso aos bytes brutos)
        try
        {
            var blobOrdinal = reader.GetOrdinal("PRC_NO_PROCEDIMENTO_BLOB");
            if (!reader.IsDBNull(blobOrdinal))
            {
                var blobValue = reader.GetValue(blobOrdinal);
                if (blobValue is byte[] blobBytes && blobBytes.Length > 0)
                {
                    // Remove bytes nulos no final
                    int validLength = blobBytes.Length;
                    while (validLength > 0 && blobBytes[validLength - 1] == 0)
                    {
                        validLength--;
                    }
                    
                    if (validLength > 0)
                    {
                        byte[] validBytes = new byte[validLength];
                        Array.Copy(blobBytes, 0, validBytes, 0, validLength);
                        // Converte diretamente para Windows-1252 (mais rápido e confiável)
                        prcNoProcedimento = ConvertBytesToWindows1252(validBytes);
                        
                        // Se a conversão resultou em caracteres corrompidos, tenta o helper
                        if (!string.IsNullOrEmpty(prcNoProcedimento) && 
                            (prcNoProcedimento.Contains('\uFFFD') || prcNoProcedimento.Contains('?')))
                        {
                            prcNoProcedimento = FirebirdReaderHelper.GetStringSafe(reader, "PRC_NO_PROCEDIMENTO_BLOB");
                        }
                    }
                }
                
                // Se não conseguiu ler como byte[], usa o helper
                if (string.IsNullOrEmpty(prcNoProcedimento))
                {
                    prcNoProcedimento = FirebirdReaderHelper.GetStringSafe(reader, "PRC_NO_PROCEDIMENTO_BLOB");
                }
            }
        }
        catch
        {
            // Se não encontrar o BLOB, continua para campo direto
        }
        
        // Prioridade 2: Se BLOB não funcionou ou está vazio, tenta campo direto
        if (string.IsNullOrEmpty(prcNoProcedimento))
        {
            try
            {
                var campoOrdinal = reader.GetOrdinal("PRC_NO_PROCEDIMENTO");
                if (!reader.IsDBNull(campoOrdinal))
                {
                    prcNoProcedimento = FirebirdReaderHelper.GetStringSafe(reader, "PRC_NO_PROCEDIMENTO");
                }
            }
            catch
            {
                // Se não conseguir ler, deixa null
            }
        }

        // Mesma lógica para PRC_OBSERVACOES
        try
        {
            var blobOrdinal = reader.GetOrdinal("PRC_OBSERVACOES_BLOB");
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
                        prcObservacoes = ConvertBytesToWindows1252(validBytes);
                        
                        if (!string.IsNullOrEmpty(prcObservacoes) && 
                            (prcObservacoes.Contains('\uFFFD') || prcObservacoes.Contains('?')))
                        {
                            prcObservacoes = FirebirdReaderHelper.GetStringSafe(reader, "PRC_OBSERVACOES_BLOB");
                        }
                    }
                }
                
                if (string.IsNullOrEmpty(prcObservacoes))
                {
                    prcObservacoes = FirebirdReaderHelper.GetStringSafe(reader, "PRC_OBSERVACOES_BLOB");
                }
            }
        }
        catch
        {
            // Se não encontrar o BLOB, continua para campo direto
        }
        
        if (string.IsNullOrEmpty(prcObservacoes))
        {
            try
            {
                var campoOrdinal = reader.GetOrdinal("PRC_OBSERVACOES");
                if (!reader.IsDBNull(campoOrdinal))
                {
                    prcObservacoes = FirebirdReaderHelper.GetStringSafe(reader, "PRC_OBSERVACOES");
                }
            }
            catch
            {
                // Se não conseguir ler, deixa null
            }
        }

        return new ProcedimentoComum
        {
            PrcCod = reader.GetInt32(reader.GetOrdinal("PRC_COD")),
            PrcCodProc = FirebirdReaderHelper.GetStringSafe(reader, "PRC_CODPROC"),
            PrcNoProcedimento = prcNoProcedimento,
            PrcObservacoes = prcObservacoes
        };
    }

    /// <summary>
    /// Converte bytes diretamente para Windows-1252 (helper local)
    /// Mesma lógica usada no ProcedimentoRepository
    /// </summary>
    private static string ConvertBytesToWindows1252(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
            return string.Empty;

        try
        {
            var encoding = Encoding.GetEncoding(1252); // Windows-1252
            return encoding.GetString(bytes);
        }
        catch
        {
            // Se falhar, tenta Latin1
            try
            {
                var encoding = Encoding.GetEncoding("ISO-8859-1");
                return encoding.GetString(bytes);
            }
            catch
            {
                // Último recurso: usa encoding padrão
                return Encoding.Default.GetString(bytes);
            }
        }
    }
}

