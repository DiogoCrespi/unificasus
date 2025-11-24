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

        // Não usa transação explícita, como o ImportRepository faz
        // O Firebird gerencia transações automaticamente para INSERT simples
        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@prcCod", procedimentoComum.PrcCod);
        command.Parameters.AddWithValue("@prcCodProc", procedimentoComum.PrcCodProc ?? (object)DBNull.Value);
        
        // Passa strings diretamente, como o ImportRepository faz
        // O driver salva como UTF-8, que é o que está acontecendo
        // O problema está na LEITURA, não na inserção
        command.Parameters.AddWithValue("@prcNoProcedimento", procedimentoComum.PrcNoProcedimento ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@prcObservacoes", procedimentoComum.PrcObservacoes ?? (object)DBNull.Value);

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
            return procedimentoComum.PrcCod;
        }
        catch (Exception ex)
        {
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

        // Não usa transação explícita, como o ImportRepository faz
        // O Firebird gerencia transações automaticamente para UPDATE simples
        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@prcCod", procedimentoComum.PrcCod);
        command.Parameters.AddWithValue("@prcCodProc", procedimentoComum.PrcCodProc ?? (object)DBNull.Value);
        
        // Passa strings diretamente, como o ImportRepository faz
        // O driver salva como UTF-8, que é o que está acontecendo
        // O problema está na LEITURA, não na inserção
        command.Parameters.AddWithValue("@prcNoProcedimento", procedimentoComum.PrcNoProcedimento ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@prcObservacoes", procedimentoComum.PrcObservacoes ?? (object)DBNull.Value);

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
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
        // Query simplificada para obter próximo código
        const string sql = @"
            SELECT COALESCE(MAX(PRC_COD), 0) + 1
            FROM TB_PROCOMUNS";

        try
        {
            await _context.OpenAsync(cancellationToken);

            using var command = new FbCommand(sql, _context.Connection);
            
            // Usa ExecuteScalar - mais simples para queries de valor único
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
            // Retorna 1 em caso de erro (evita falha total)
            return 1;
        }
    }

    private static ProcedimentoComum MapProcedimentoComum(FbDataReader reader)
    {
        // O driver salva strings como UTF-8 automaticamente
        // Na leitura, precisamos converter de UTF-8 para Windows-1252
        // Estratégia: Ler bytes do BLOB (que estão em UTF-8) e converter para Windows-1252
        
        string? prcNoProcedimento = null;
        string? prcObservacoes = null;
        
        // Prioridade 1: Tenta ler do BLOB como byte[] (contém bytes UTF-8)
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
                        
                        // Os bytes estão em UTF-8 (0xC3 0x87 para Ç)
                        // Converte de UTF-8 para string .NET (Unicode)
                        prcNoProcedimento = Encoding.UTF8.GetString(validBytes);
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
                prcNoProcedimento = FirebirdReaderHelper.GetStringSafe(reader, "PRC_NO_PROCEDIMENTO");
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
                        
                        // Os bytes estão em UTF-8 (pelo que vimos no debug)
                        // Converte de UTF-8 para string .NET (Unicode)
                        prcObservacoes = Encoding.UTF8.GetString(validBytes);
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
                prcObservacoes = FirebirdReaderHelper.GetStringSafe(reader, "PRC_OBSERVACOES");
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
    /// Normaliza string para inserção no Firebird com Charset=NONE
    /// PROBLEMA: O driver .NET sempre converte strings para UTF-8 antes de salvar
    /// SOLUÇÃO: Usar Latin1 (ISO-8859-1) como intermediário para "enganar" o driver
    /// Latin1 mapeia 1:1 os bytes, então podemos fazer a string conter os bytes Windows-1252
    /// </summary>
    private static object NormalizeStringForInsert(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return DBNull.Value;

        try
        {
            // Obtém encodings necessários
            Encoding windows1252;
            try
            {
                windows1252 = Encoding.GetEncoding(1252);
            }
            catch
            {
                try
                {
                    windows1252 = Encoding.GetEncoding("Windows-1252");
                }
                catch
                {
                    windows1252 = Encoding.GetEncoding("ISO-8859-1"); // Fallback
                }
            }
            
            Encoding latin1 = Encoding.GetEncoding("ISO-8859-1");

            // Converte a string para bytes em Windows-1252
            byte[] bytesWindows1252 = windows1252.GetBytes(value);
            
            // Converte os bytes Windows-1252 para string usando Latin1
            // Latin1 mapeia 1:1 os bytes (byte 0xC7 vira char 0xC7)
            // Isso "engana" o .NET para que a string contenha os bytes Windows-1252 como caracteres
            string stringComBytesWindows1252 = latin1.GetString(bytesWindows1252);
            
            // Quando o driver salvar, ele vai converter esta string para bytes
            // Como usamos Latin1 (1:1), os bytes salvos serão exatamente os Windows-1252
            return stringComBytesWindows1252;
        }
        catch
        {
            // Se falhar, retorna string original
            return value;
        }
    }

    /// <summary>
    /// Cria parâmetro para inserção de string no Firebird com Charset=NONE
    /// Com Charset=NONE, o driver não faz conversão automática, então precisamos garantir bytes corretos
    /// Estratégia: Converte para bytes Windows-1252 e tenta passar como string normalizada
    /// Se o driver fizer conversão incorreta, os bytes já estarão corretos na string normalizada
    /// </summary>
    private static FbParameter CreateStringParameter(string parameterName, string? value)
    {
        var parameter = new FbParameter(parameterName, FbDbType.VarChar);
        
        if (string.IsNullOrEmpty(value))
        {
            parameter.Value = DBNull.Value;
            return parameter;
        }

        try
        {
            // Obtém encoding Windows-1252
            Encoding encoding;
            try
            {
                encoding = Encoding.GetEncoding(1252);
            }
            catch
            {
                encoding = Encoding.GetEncoding("ISO-8859-1");
            }

            // Converte para bytes em Windows-1252
            byte[] bytes = encoding.GetBytes(value);
            
            // Converte bytes de volta para string usando o mesmo encoding
            // Isso garante que a string contenha exatamente os bytes Windows-1252 corretos
            // Quando o driver salvar, mesmo com Charset=NONE, os bytes devem ser preservados
            string normalized = encoding.GetString(bytes);
            
            parameter.Value = normalized;
            return parameter;
        }
        catch
        {
            // Se falhar, usa string original
            parameter.Value = value;
            return parameter;
        }
    }

    /// <summary>
    /// Converte bytes diretamente para Windows-1252 (helper local)
    /// Mesma lógica usada no ProcedimentoRepository
    /// </summary>
    private static string? ConvertBytesToWindows1252(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
            return string.Empty;

        try
        {
            // Obtém encoding Windows-1252 de forma segura
            Encoding encoding;
            try
            {
                encoding = Encoding.GetEncoding(1252);
            }
            catch
            {
                encoding = Encoding.GetEncoding("ISO-8859-1"); // Fallback
            }
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

