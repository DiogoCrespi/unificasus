using FirebirdSql.Data.FirebirdClient;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text;
using UnificaSUS.Core.Entities;
using UnificaSUS.Core.Interfaces;
using UnificaSUS.Infrastructure.Data;
using UnificaSUS.Infrastructure.Helpers;

namespace UnificaSUS.Infrastructure.Repositories;

/// <summary>
/// Implementação do repositório de procedimentos
/// </summary>
public class ProcedimentoRepository : IProcedimentoRepository
{
    private readonly FirebirdContext _context;
    private readonly ILogger<ProcedimentoRepository> _logger;

    public ProcedimentoRepository(FirebirdContext context, ILogger<ProcedimentoRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Procedimento>> BuscarPorCompetenciaAsync(string competencia, CancellationToken cancellationToken = default)
    {
        // Usa CAST para BLOB nos campos de texto para garantir acesso aos bytes brutos
        // Isso permite conversão correta de encoding
        const string sql = @"
            SELECT 
                pr.CO_PROCEDIMENTO,
                CAST(pr.NO_PROCEDIMENTO AS BLOB) AS NO_PROCEDIMENTO_BLOB,
                pr.NO_PROCEDIMENTO,
                pr.TP_COMPLEXIDADE,
                pr.TP_SEXO,
                pr.QT_MAXIMA_EXECUCAO,
                pr.QT_DIAS_PERMANENCIA,
                pr.QT_PONTOS,
                pr.VL_IDADE_MINIMA,
                pr.VL_IDADE_MAXIMA,
                pr.VL_SH,
                pr.VL_SA,
                pr.VL_SP,
                pr.CO_FINANCIAMENTO,
                pr.CO_RUBRICA,
                pr.QT_TEMPO_PERMANENCIA,
                pr.DT_COMPETENCIA
            FROM TB_PROCEDIMENTO pr
            WHERE pr.DT_COMPETENCIA = @competencia
            ORDER BY pr.CO_PROCEDIMENTO";

        await _context.OpenAsync(cancellationToken);

        var procedimentos = new List<Procedimento>();

        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@competencia", competencia);

        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                procedimentos.Add(MapProcedimento(reader));
            }
        }
        catch (FirebirdSql.Data.FirebirdClient.FbException ex)
        {
            _logger.LogError(ex, "Erro de banco de dados ao buscar procedimentos por competência {Competencia}. Mensagem: {Mensagem}", competencia, ex.Message);
            
            // Relançar com mensagem mais amigável
            throw new InvalidOperationException(
                $"Erro ao buscar procedimentos no banco de dados:\n\n{ex.Message}\n\n" +
                $"Verifique:\n" +
                $"- Se o banco de dados está acessível\n" +
                $"- Se a competência {competencia} existe no banco\n" +
                $"- Se a tabela TB_PROCEDIMENTO existe", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar procedimentos por competência {Competencia}", competencia);
            throw;
        }

        return procedimentos;
    }

    public async Task<Procedimento?> BuscarPorCodigoAsync(string codigo, string competencia, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                pr.CO_PROCEDIMENTO,
                CAST(pr.NO_PROCEDIMENTO AS BLOB) AS NO_PROCEDIMENTO_BLOB,
                pr.NO_PROCEDIMENTO,
                pr.TP_COMPLEXIDADE,
                pr.TP_SEXO,
                pr.QT_MAXIMA_EXECUCAO,
                pr.QT_DIAS_PERMANENCIA,
                pr.QT_PONTOS,
                pr.VL_IDADE_MINIMA,
                pr.VL_IDADE_MAXIMA,
                pr.VL_SH,
                pr.VL_SA,
                pr.VL_SP,
                pr.CO_FINANCIAMENTO,
                pr.CO_RUBRICA,
                pr.QT_TEMPO_PERMANENCIA,
                pr.DT_COMPETENCIA
            FROM TB_PROCEDIMENTO pr
            WHERE pr.CO_PROCEDIMENTO = @codigo
              AND pr.DT_COMPETENCIA = @competencia";

        await _context.OpenAsync(cancellationToken);

        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@codigo", codigo);
        command.Parameters.AddWithValue("@competencia", competencia);

        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (await reader.ReadAsync(cancellationToken))
            {
                return MapProcedimento(reader);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar procedimento por código {Codigo} e competência {Competencia}", codigo, competencia);
            throw;
        }

        return null;
    }

    public async Task<IEnumerable<Procedimento>> BuscarPorCodigosAsync(IEnumerable<string> codigos, string competencia, CancellationToken cancellationToken = default)
    {
        var codigosList = codigos.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
        
        if (!codigosList.Any())
        {
            return Enumerable.Empty<Procedimento>();
        }

        var procedimentos = new List<Procedimento>();

        await _context.OpenAsync(cancellationToken);

        // Firebird tem limite de parâmetros, então vamos processar em lotes de 100
        const int batchSize = 100;
        
        for (int i = 0; i < codigosList.Count; i += batchSize)
        {
            var batch = codigosList.Skip(i).Take(batchSize).ToList();
            
            var sql = @"
                SELECT 
                    pr.CO_PROCEDIMENTO,
                    CAST(pr.NO_PROCEDIMENTO AS BLOB) AS NO_PROCEDIMENTO_BLOB,
                    pr.NO_PROCEDIMENTO,
                    pr.TP_COMPLEXIDADE,
                    pr.TP_SEXO,
                    pr.QT_MAXIMA_EXECUCAO,
                    pr.QT_DIAS_PERMANENCIA,
                    pr.QT_PONTOS,
                    pr.VL_IDADE_MINIMA,
                    pr.VL_IDADE_MAXIMA,
                    pr.VL_SH,
                    pr.VL_SA,
                    pr.VL_SP,
                    pr.CO_FINANCIAMENTO,
                    pr.CO_RUBRICA,
                    pr.QT_TEMPO_PERMANENCIA,
                    pr.DT_COMPETENCIA
                FROM TB_PROCEDIMENTO pr
                WHERE pr.DT_COMPETENCIA = @competencia
                  AND pr.CO_PROCEDIMENTO IN (";

            var paramNames = new List<string>();
            using var command = new FbCommand();
            command.Connection = _context.Connection;
            
            for (int j = 0; j < batch.Count; j++)
            {
                var paramName = $"@codigo{j}";
                paramNames.Add(paramName);
                command.Parameters.AddWithValue(paramName, batch[j]);
            }

            sql += string.Join(", ", paramNames) + ")";

            command.CommandText = sql;
            command.Parameters.AddWithValue("@competencia", competencia);

            try
            {
                using var reader = await command.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    var procedimento = MapProcedimento(reader);
                    if (procedimento != null)
                    {
                        procedimentos.Add(procedimento);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar procedimentos por códigos na competência {Competencia}", competencia);
                // Continua processando os próximos lotes mesmo se um falhar
            }
        }

        return procedimentos;
    }

    public async Task<IEnumerable<Procedimento>> BuscarPorFiltroAsync(string filtro, string competencia, CancellationToken cancellationToken = default)
    {
        // Otimizado: usa LIKE com % apenas no final quando possível (mais rápido que CONTAINING)
        // Limita a 1000 resultados para melhor performance
        // Usa CAST para BLOB para garantir acesso aos bytes brutos
        // Simplificado para evitar erro de parâmetro com CAST dentro de UPPER
        const string sql = @"
            SELECT FIRST 1000
                pr.CO_PROCEDIMENTO,
                CAST(pr.NO_PROCEDIMENTO AS BLOB) AS NO_PROCEDIMENTO_BLOB,
                pr.NO_PROCEDIMENTO,
                pr.TP_COMPLEXIDADE,
                pr.TP_SEXO,
                pr.QT_MAXIMA_EXECUCAO,
                pr.QT_DIAS_PERMANENCIA,
                pr.QT_PONTOS,
                pr.VL_IDADE_MINIMA,
                pr.VL_IDADE_MAXIMA,
                pr.VL_SH,
                pr.VL_SA,
                pr.VL_SP,
                pr.CO_FINANCIAMENTO,
                pr.CO_RUBRICA,
                pr.QT_TEMPO_PERMANENCIA,
                pr.DT_COMPETENCIA
            FROM TB_PROCEDIMENTO pr
            WHERE pr.DT_COMPETENCIA = @competencia
              AND (pr.CO_PROCEDIMENTO CONTAINING @filtro 
                   OR pr.NO_PROCEDIMENTO CONTAINING @filtro)
            ORDER BY pr.CO_PROCEDIMENTO";

        await _context.OpenAsync(cancellationToken);

        var procedimentos = new List<Procedimento>();
        // Usa CONTAINING que é mais simples e não precisa de wildcards
        // CONTAINING faz busca case-insensitive automaticamente

        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@competencia", competencia);
        command.Parameters.AddWithValue("@filtro", filtro);

        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                procedimentos.Add(MapProcedimento(reader));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar procedimentos por filtro {Filtro} e competência {Competencia}", filtro, competencia);
            throw;
        }

        return procedimentos;
    }

    public async Task<IEnumerable<Procedimento>> BuscarPorCIDAsync(string cid, string competencia, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT DISTINCT
                pr.CO_PROCEDIMENTO,
                CAST(pr.NO_PROCEDIMENTO AS BLOB) AS NO_PROCEDIMENTO_BLOB,
                pr.NO_PROCEDIMENTO,
                pr.TP_COMPLEXIDADE,
                pr.TP_SEXO,
                pr.QT_MAXIMA_EXECUCAO,
                pr.QT_DIAS_PERMANENCIA,
                pr.QT_PONTOS,
                pr.VL_IDADE_MINIMA,
                pr.VL_IDADE_MAXIMA,
                pr.VL_SH,
                pr.VL_SA,
                pr.VL_SP,
                pr.CO_FINANCIAMENTO,
                pr.CO_RUBRICA,
                pr.QT_TEMPO_PERMANENCIA,
                pr.DT_COMPETENCIA
            FROM TB_PROCEDIMENTO pr
            INNER JOIN RL_PROCEDIMENTO_CID pc ON pr.CO_PROCEDIMENTO = pc.CO_PROCEDIMENTO
            WHERE pc.CO_CID = @cid
              AND pc.DT_COMPETENCIA = @competencia
            ORDER BY pr.CO_PROCEDIMENTO";

        await _context.OpenAsync(cancellationToken);

        var procedimentos = new List<Procedimento>();

        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@competencia", competencia);
        command.Parameters.AddWithValue("@cid", cid);

        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                procedimentos.Add(MapProcedimento(reader));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar procedimentos por CID {CID} e competência {Competencia}", cid, competencia);
            throw;
        }

        return procedimentos;
    }

    public async Task<IEnumerable<Procedimento>> BuscarPorServicoAsync(string servico, string competencia, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT DISTINCT
                pr.CO_PROCEDIMENTO,
                CAST(pr.NO_PROCEDIMENTO AS BLOB) AS NO_PROCEDIMENTO_BLOB,
                pr.NO_PROCEDIMENTO,
                pr.TP_COMPLEXIDADE,
                pr.TP_SEXO,
                pr.QT_MAXIMA_EXECUCAO,
                pr.QT_DIAS_PERMANENCIA,
                pr.QT_PONTOS,
                pr.VL_IDADE_MINIMA,
                pr.VL_IDADE_MAXIMA,
                pr.VL_SH,
                pr.VL_SA,
                pr.VL_SP,
                pr.CO_FINANCIAMENTO,
                pr.CO_RUBRICA,
                pr.QT_TEMPO_PERMANENCIA,
                pr.DT_COMPETENCIA
            FROM TB_PROCEDIMENTO pr
            INNER JOIN RL_PROCEDIMENTO_SERVICO ps ON pr.CO_PROCEDIMENTO = ps.CO_PROCEDIMENTO
            WHERE ps.DT_COMPETENCIA = @competencia
              AND ps.CO_SERVICO = @servico
            ORDER BY pr.CO_PROCEDIMENTO";

        await _context.OpenAsync(cancellationToken);

        var procedimentos = new List<Procedimento>();

        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@competencia", competencia);
        command.Parameters.AddWithValue("@servico", servico);

        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                procedimentos.Add(MapProcedimento(reader));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar procedimentos por serviço {Servico} e competência {Competencia}", servico, competencia);
            throw;
        }

        return procedimentos;
    }

    public async Task<IEnumerable<Procedimento>> BuscarPorEstruturaAsync(string? coGrupo, string? coSubGrupo, string? coFormaOrganizacao, string competencia, CancellationToken cancellationToken = default)
    {
        // Constrói a query dinamicamente baseado nos parâmetros fornecidos
        // O código do procedimento tem a estrutura: AABBCCDDDD
        // AA = Grupo (posições 1-2)
        // BB = SubGrupo (posições 3-4)
        // CC = FormaOrganizacao (posições 5-6)
        // DDDD = Código específico
        
        var conditions = new List<string> { "pr.DT_COMPETENCIA = @competencia" };
        
        if (!string.IsNullOrEmpty(coGrupo))
        {
            conditions.Add("SUBSTRING(pr.CO_PROCEDIMENTO FROM 1 FOR 2) = @coGrupo");
        }
        
        if (!string.IsNullOrEmpty(coSubGrupo))
        {
            conditions.Add("SUBSTRING(pr.CO_PROCEDIMENTO FROM 3 FOR 2) = @coSubGrupo");
        }
        
        if (!string.IsNullOrEmpty(coFormaOrganizacao))
        {
            conditions.Add("SUBSTRING(pr.CO_PROCEDIMENTO FROM 5 FOR 2) = @coFormaOrganizacao");
        }
        
        var whereClause = string.Join(" AND ", conditions);
        
        // Tenta ler tanto como BLOB quanto como campo normal para garantir compatibilidade
        var sql = $@"
            SELECT FIRST 500
                pr.CO_PROCEDIMENTO,
                CAST(pr.NO_PROCEDIMENTO AS BLOB) AS NO_PROCEDIMENTO_BLOB,
                pr.NO_PROCEDIMENTO,
                pr.TP_COMPLEXIDADE,
                pr.TP_SEXO,
                pr.QT_MAXIMA_EXECUCAO,
                pr.QT_DIAS_PERMANENCIA,
                pr.QT_PONTOS,
                pr.VL_IDADE_MINIMA,
                pr.VL_IDADE_MAXIMA,
                pr.VL_SH,
                pr.VL_SA,
                pr.VL_SP,
                pr.CO_FINANCIAMENTO,
                pr.CO_RUBRICA,
                pr.QT_TEMPO_PERMANENCIA,
                pr.DT_COMPETENCIA
            FROM TB_PROCEDIMENTO pr
            WHERE {whereClause}
            ORDER BY pr.CO_PROCEDIMENTO";

        await _context.OpenAsync(cancellationToken);

        var procedimentos = new List<Procedimento>();

        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@competencia", competencia);
        
        if (!string.IsNullOrEmpty(coGrupo))
        {
            command.Parameters.AddWithValue("@coGrupo", coGrupo.PadLeft(2, '0'));
        }
        
        if (!string.IsNullOrEmpty(coSubGrupo))
        {
            command.Parameters.AddWithValue("@coSubGrupo", coSubGrupo.PadLeft(2, '0'));
        }
        
        if (!string.IsNullOrEmpty(coFormaOrganizacao))
        {
            command.Parameters.AddWithValue("@coFormaOrganizacao", coFormaOrganizacao.PadLeft(2, '0'));
        }

        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                procedimentos.Add(MapProcedimento(reader));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar procedimentos por estrutura. Grupo: {Grupo}, SubGrupo: {SubGrupo}, FormaOrganizacao: {FormaOrganizacao}, Competencia: {Competencia}", 
                coGrupo, coSubGrupo, coFormaOrganizacao, competencia);
            throw;
        }

        return procedimentos;
    }

    /// <summary>
    /// Converte bytes diretamente para Windows-1252 (helper local)
    /// </summary>
    private static string ConvertBytesToWindows1252(byte[] bytes)
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

    private static decimal? TryGetDecimal(FbDataReader reader, string columnName)
    {
        try
        {
            var ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal))
                return null;
            return reader.GetDecimal(ordinal);
        }
        catch
        {
            // Campo não existe ou não pode ser lido - retorna null
            return null;
        }
    }

    private static Procedimento MapProcedimento(FbDataReader reader)
    {
        // Tenta ler NO_PROCEDIMENTO do BLOB primeiro (mais confiável para encoding)
        // Se não houver BLOB, tenta o campo direto
        string? noProcedimento = null;
        
        // Prioridade 1: Tenta ler do BLOB (CAST para BLOB garante acesso aos bytes brutos)
        // O BLOB sempre retorna como byte[], então é mais confiável para conversão de encoding
        try
        {
            var blobOrdinal = reader.GetOrdinal("NO_PROCEDIMENTO_BLOB");
            if (!reader.IsDBNull(blobOrdinal))
            {
                // Lê diretamente como byte[] do BLOB (mais confiável)
                // O BLOB sempre retorna como byte[], então podemos converter diretamente
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
                        
                        // CORREÇÃO: Os dados são salvos como UTF-8 pelo driver, não Windows-1252
                        // Converte de UTF-8 para string .NET (Unicode)
                        noProcedimento = Encoding.UTF8.GetString(validBytes);
                        
                        // Se a conversão resultou em caracteres corrompidos, tenta o helper como fallback
                        if (!string.IsNullOrEmpty(noProcedimento) && 
                            (noProcedimento.Contains('\uFFFD') || noProcedimento.Contains('?')))
                        {
                            noProcedimento = FirebirdReaderHelper.GetStringSafe(reader, "NO_PROCEDIMENTO_BLOB");
                        }
                    }
                }
                
                // Se não conseguiu ler como byte[], usa o helper
                if (string.IsNullOrEmpty(noProcedimento))
                {
                noProcedimento = FirebirdReaderHelper.GetStringSafe(reader, "NO_PROCEDIMENTO_BLOB");
                }
            }
        }
        catch
        {
            // Se não encontrar o BLOB, continua para campo direto
        }
        
        // Prioridade 2: Se BLOB não funcionou ou está vazio, tenta campo direto
        if (string.IsNullOrEmpty(noProcedimento))
        {
            try
            {
                var campoOrdinal = reader.GetOrdinal("NO_PROCEDIMENTO");
                if (!reader.IsDBNull(campoOrdinal))
                {
                    // Usa o helper que já trata encoding corretamente
                    noProcedimento = FirebirdReaderHelper.GetStringSafe(reader, "NO_PROCEDIMENTO");
                }
            }
            catch
            {
                // Se não conseguir ler, deixa null
            }
        }

        return new Procedimento
        {
            CoProcedimento = FirebirdReaderHelper.GetStringSafe(reader, "CO_PROCEDIMENTO") ?? string.Empty,
            NoProcedimento = noProcedimento,
            TpComplexidade = FirebirdReaderHelper.GetStringSafe(reader, "TP_COMPLEXIDADE"),
            TpSexo = FirebirdReaderHelper.GetStringSafe(reader, "TP_SEXO"),
            QtMaximaExecucao = reader.IsDBNull(reader.GetOrdinal("QT_MAXIMA_EXECUCAO")) ? null : reader.GetInt32(reader.GetOrdinal("QT_MAXIMA_EXECUCAO")),
            QtDiasPermanencia = reader.IsDBNull(reader.GetOrdinal("QT_DIAS_PERMANENCIA")) ? null : reader.GetInt32(reader.GetOrdinal("QT_DIAS_PERMANENCIA")),
            QtPontos = reader.IsDBNull(reader.GetOrdinal("QT_PONTOS")) ? null : reader.GetInt32(reader.GetOrdinal("QT_PONTOS")),
            VlIdadeMinima = reader.IsDBNull(reader.GetOrdinal("VL_IDADE_MINIMA")) ? null : reader.GetInt32(reader.GetOrdinal("VL_IDADE_MINIMA")),
            VlIdadeMaxima = reader.IsDBNull(reader.GetOrdinal("VL_IDADE_MAXIMA")) ? null : reader.GetInt32(reader.GetOrdinal("VL_IDADE_MAXIMA")),
            VlSh = reader.IsDBNull(reader.GetOrdinal("VL_SH")) ? null : reader.GetDecimal(reader.GetOrdinal("VL_SH")),
            VlSa = reader.IsDBNull(reader.GetOrdinal("VL_SA")) ? null : reader.GetDecimal(reader.GetOrdinal("VL_SA")),
            VlSp = reader.IsDBNull(reader.GetOrdinal("VL_SP")) ? null : reader.GetDecimal(reader.GetOrdinal("VL_SP")),
            VlTa = TryGetDecimal(reader, "VL_TA"),
            VlTh = TryGetDecimal(reader, "VL_TH"),
            CoFinanciamento = FirebirdReaderHelper.GetStringSafe(reader, "CO_FINANCIAMENTO"),
            CoRubrica = FirebirdReaderHelper.GetStringSafe(reader, "CO_RUBRICA"),
            QtTempoPermanencia = reader.IsDBNull(reader.GetOrdinal("QT_TEMPO_PERMANENCIA")) ? null : reader.GetInt32(reader.GetOrdinal("QT_TEMPO_PERMANENCIA")),
            DtCompetencia = FirebirdReaderHelper.GetStringSafe(reader, "DT_COMPETENCIA")
        };
    }

    public async Task<IEnumerable<RelacionadoItem>> BuscarCID10RelacionadosAsync(string coProcedimento, string competencia, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                c.CO_CID,
                CAST(c.NO_CID AS BLOB) AS NO_CID_BLOB,
                c.NO_CID,
                pc.ST_PRINCIPAL
            FROM RL_PROCEDIMENTO_CID pc
            INNER JOIN TB_CID c ON pc.CO_CID = c.CO_CID
            WHERE pc.CO_PROCEDIMENTO = @coProcedimento
              AND pc.DT_COMPETENCIA = @competencia
            ORDER BY pc.ST_PRINCIPAL DESC, c.CO_CID";

        await _context.OpenAsync(cancellationToken);

        var itens = new List<RelacionadoItem>();

        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@coProcedimento", coProcedimento);
        command.Parameters.AddWithValue("@competencia", competencia);

        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var codigo = FirebirdReaderHelper.GetStringSafe(reader, "CO_CID") ?? string.Empty;
                var descricao = LerCampoTextoDoBlob(reader, "NO_CID_BLOB", "NO_CID");
                var principal = FirebirdReaderHelper.GetStringSafe(reader, "ST_PRINCIPAL");
                
                itens.Add(new RelacionadoItem
                {
                    Codigo = codigo,
                    Descricao = descricao,
                    InformacaoAdicional = principal == "S" ? "Principal" : null
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar CID10 relacionados ao procedimento {Procedimento}", coProcedimento);
            throw;
        }

        return itens;
    }

    public async Task<IEnumerable<RelacionadoItem>> BuscarCompativeisRelacionadosAsync(string coProcedimento, string competencia, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                pr.CO_PROCEDIMENTO,
                CAST(pr.NO_PROCEDIMENTO AS BLOB) AS NO_PROCEDIMENTO_BLOB,
                pr.NO_PROCEDIMENTO,
                pc.TP_COMPATIBILIDADE,
                pc.QT_PERMITIDA
            FROM RL_PROCEDIMENTO_COMPATIVEL pc
            INNER JOIN TB_PROCEDIMENTO pr ON pc.CO_PROCEDIMENTO_COMPATIVEL = pr.CO_PROCEDIMENTO
            WHERE pc.CO_PROCEDIMENTO_PRINCIPAL = @coProcedimento
              AND pc.DT_COMPETENCIA = @competencia
              AND pr.DT_COMPETENCIA = @competencia
            ORDER BY pr.CO_PROCEDIMENTO";

        await _context.OpenAsync(cancellationToken);

        var itens = new List<RelacionadoItem>();

        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@coProcedimento", coProcedimento);
        command.Parameters.AddWithValue("@competencia", competencia);

        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var codigo = FirebirdReaderHelper.GetStringSafe(reader, "CO_PROCEDIMENTO") ?? string.Empty;
                var descricao = LerCampoTextoDoBlob(reader, "NO_PROCEDIMENTO_BLOB", "NO_PROCEDIMENTO");
                
                // Formatar informações adicionais
                var informacoesAdicionais = new List<string>();
                
                var tipoCompatibilidade = FirebirdReaderHelper.GetStringSafe(reader, "TP_COMPATIBILIDADE");
                if (!string.IsNullOrEmpty(tipoCompatibilidade))
                {
                    informacoesAdicionais.Add($"Tipo: {tipoCompatibilidade}");
                }
                
                var qtPermitidaOrdinal = reader.GetOrdinal("QT_PERMITIDA");
                if (!reader.IsDBNull(qtPermitidaOrdinal))
                {
                    var qtPermitida = reader.GetInt32(qtPermitidaOrdinal);
                    if (qtPermitida > 0)
                    {
                        informacoesAdicionais.Add($"Qtd. Permitida: {qtPermitida}");
                    }
                }
                
                var informacaoAdicional = informacoesAdicionais.Any() 
                    ? string.Join(" | ", informacoesAdicionais) 
                    : null;
                
                itens.Add(new RelacionadoItem
                {
                    Codigo = codigo,
                    Descricao = descricao,
                    InformacaoAdicional = informacaoAdicional
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar procedimentos compatíveis relacionados ao procedimento {Procedimento}", coProcedimento);
            throw;
        }

        return itens;
    }

    public async Task<IEnumerable<RelacionadoItem>> BuscarHabilitacoesRelacionadasAsync(string coProcedimento, string competencia, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                h.CO_HABILITACAO,
                CAST(h.NO_HABILITACAO AS BLOB) AS NO_HABILITACAO_BLOB,
                h.NO_HABILITACAO,
                ph.NU_GRUPO_HABILITACAO
            FROM RL_PROCEDIMENTO_HABILITACAO ph
            INNER JOIN TB_HABILITACAO h ON ph.CO_HABILITACAO = h.CO_HABILITACAO
            WHERE ph.CO_PROCEDIMENTO = @coProcedimento
              AND ph.DT_COMPETENCIA = @competencia
              AND h.DT_COMPETENCIA = @competencia
            ORDER BY h.CO_HABILITACAO";

        await _context.OpenAsync(cancellationToken);

        var itens = new List<RelacionadoItem>();

        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@coProcedimento", coProcedimento);
        command.Parameters.AddWithValue("@competencia", competencia);

        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var codigo = FirebirdReaderHelper.GetStringSafe(reader, "CO_HABILITACAO") ?? string.Empty;
                var descricao = LerCampoTextoDoBlob(reader, "NO_HABILITACAO_BLOB", "NO_HABILITACAO");
                var grupo = FirebirdReaderHelper.GetStringSafe(reader, "NU_GRUPO_HABILITACAO");
                
                // Para Habilitações, o InformacaoAdicional será usado para exibir o Grupo
                // Se não houver grupo, usa string vazia para que a UI possa formatar como "Sem grupo" em vermelho
                var informacaoAdicional = !string.IsNullOrWhiteSpace(grupo) 
                    ? grupo 
                    : string.Empty; // String vazia indica que não há grupo
                
                itens.Add(new RelacionadoItem
                {
                    Codigo = codigo,
                    Descricao = descricao,
                    InformacaoAdicional = informacaoAdicional
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar habilitações relacionadas ao procedimento {Procedimento}", coProcedimento);
            throw;
        }

        return itens;
    }

    public async Task<IEnumerable<RelacionadoItem>> BuscarCBOsRelacionadosAsync(string coProcedimento, string competencia, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                o.CO_OCUPACAO,
                CAST(o.NO_OCUPACAO AS BLOB) AS NO_OCUPACAO_BLOB,
                o.NO_OCUPACAO
            FROM RL_PROCEDIMENTO_OCUPACAO po
            INNER JOIN TB_OCUPACAO o ON po.CO_OCUPACAO = o.CO_OCUPACAO
            WHERE po.CO_PROCEDIMENTO = @coProcedimento
              AND po.DT_COMPETENCIA = @competencia
            ORDER BY o.CO_OCUPACAO";

        await _context.OpenAsync(cancellationToken);

        var itens = new List<RelacionadoItem>();

        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@coProcedimento", coProcedimento);
        command.Parameters.AddWithValue("@competencia", competencia);

        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var codigo = FirebirdReaderHelper.GetStringSafe(reader, "CO_OCUPACAO") ?? string.Empty;
                var descricao = LerCampoTextoDoBlob(reader, "NO_OCUPACAO_BLOB", "NO_OCUPACAO");
                
                // Para CBO, InformacaoAdicional será usado para exibir a competência na coluna "Comp."
                itens.Add(new RelacionadoItem
                {
                    Codigo = codigo,
                    Descricao = descricao,
                    InformacaoAdicional = competencia // Competência será exibida na coluna "Comp."
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar CBOs relacionados ao procedimento {Procedimento}", coProcedimento);
            throw;
        }

        return itens;
    }

    public async Task<IEnumerable<ServicoRelacionadoItem>> BuscarServicosRelacionadosAsync(string coProcedimento, string competencia, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                s.CO_SERVICO,
                CAST(s.NO_SERVICO AS BLOB) AS NO_SERVICO_BLOB,
                s.NO_SERVICO,
                ps.CO_CLASSIFICACAO,
                CAST(sc.NO_CLASSIFICACAO AS BLOB) AS NO_CLASSIFICACAO_BLOB,
                sc.NO_CLASSIFICACAO
            FROM RL_PROCEDIMENTO_SERVICO ps
            INNER JOIN TB_SERVICO s ON ps.CO_SERVICO = s.CO_SERVICO
            LEFT JOIN TB_SERVICO_CLASSIFICACAO sc ON ps.CO_CLASSIFICACAO = sc.CO_CLASSIFICACAO 
                AND sc.CO_SERVICO = ps.CO_SERVICO
                AND sc.DT_COMPETENCIA = ps.DT_COMPETENCIA
            WHERE ps.CO_PROCEDIMENTO = @coProcedimento
              AND ps.DT_COMPETENCIA = @competencia
              AND s.DT_COMPETENCIA = @competencia
            ORDER BY s.CO_SERVICO, ps.CO_CLASSIFICACAO";

        await _context.OpenAsync(cancellationToken);

        var itens = new List<ServicoRelacionadoItem>();

        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@coProcedimento", coProcedimento);
        command.Parameters.AddWithValue("@competencia", competencia);

        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var codigoServico = FirebirdReaderHelper.GetStringSafe(reader, "CO_SERVICO") ?? string.Empty;
                var descricaoServico = LerCampoTextoDoBlob(reader, "NO_SERVICO_BLOB", "NO_SERVICO");
                var codigoClassificacao = FirebirdReaderHelper.GetStringSafe(reader, "CO_CLASSIFICACAO");
                var descricaoClassificacao = LerCampoTextoDoBlob(reader, "NO_CLASSIFICACAO_BLOB", "NO_CLASSIFICACAO");
                
                itens.Add(new ServicoRelacionadoItem
                {
                    Codigo = codigoServico,
                    Descricao = descricaoServico,
                    CodigoClassificacao = codigoClassificacao,
                    DescricaoClassificacao = descricaoClassificacao,
                    Competencia = competencia
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar serviços relacionados ao procedimento {Procedimento}", coProcedimento);
            throw;
        }

        return itens;
    }

    public async Task<IEnumerable<RelacionadoItem>> BuscarTiposLeitoRelacionadosAsync(string coProcedimento, string competencia, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                tl.CO_TIPO_LEITO,
                CAST(tl.NO_TIPO_LEITO AS BLOB) AS NO_TIPO_LEITO_BLOB,
                tl.NO_TIPO_LEITO
            FROM RL_PROCEDIMENTO_LEITO pl
            INNER JOIN TB_TIPO_LEITO tl ON pl.CO_TIPO_LEITO = tl.CO_TIPO_LEITO
                AND tl.DT_COMPETENCIA = (
                    SELECT MAX(DT_COMPETENCIA)
                    FROM TB_TIPO_LEITO tl2
                    WHERE tl2.CO_TIPO_LEITO = tl.CO_TIPO_LEITO
                      AND tl2.DT_COMPETENCIA <= @competencia
                )
            WHERE pl.CO_PROCEDIMENTO = @coProcedimento
              AND pl.DT_COMPETENCIA = @competencia
            ORDER BY tl.CO_TIPO_LEITO";

        _logger.LogInformation("BuscarTiposLeitoRelacionadosAsync: Iniciando busca para procedimento {Procedimento} na competência {Competencia}", 
            coProcedimento, competencia);

        await _context.OpenAsync(cancellationToken);

        var itens = new List<RelacionadoItem>();

        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@coProcedimento", coProcedimento);
        command.Parameters.AddWithValue("@competencia", competencia);

        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            _logger.LogInformation("BuscarTiposLeitoRelacionadosAsync: Reader criado, iniciando leitura dos registros...");

            while (await reader.ReadAsync(cancellationToken))
            {
                var codigo = FirebirdReaderHelper.GetStringSafe(reader, "CO_TIPO_LEITO") ?? string.Empty;
                var descricao = LerCampoTextoDoBlob(reader, "NO_TIPO_LEITO_BLOB", "NO_TIPO_LEITO");
                
                itens.Add(new RelacionadoItem
                {
                    Codigo = codigo,
                    Descricao = descricao
                });
            }
            
            _logger.LogInformation("BuscarTiposLeitoRelacionadosAsync: Encontrados {Count} tipos de leito para procedimento {Procedimento} na competência {Competencia}", 
                itens.Count, coProcedimento, competencia);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar tipos de leito relacionados ao procedimento {Procedimento} na competência {Competencia}", 
                coProcedimento, competencia);
            throw;
        }

        return itens;
    }

    public async Task<IEnumerable<RelacionadoItem>> BuscarInstrumentosRegistroRelacionadosAsync(string coProcedimento, string competencia, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                reg.CO_REGISTRO,
                CAST(reg.NO_REGISTRO AS BLOB) AS NO_REGISTRO_BLOB,
                reg.NO_REGISTRO,
                prreg.DT_COMPETENCIA
            FROM RL_PROCEDIMENTO_REGISTRO prreg
            INNER JOIN TB_REGISTRO reg ON prreg.CO_REGISTRO = reg.CO_REGISTRO
            WHERE prreg.CO_PROCEDIMENTO = @coProcedimento
              AND prreg.DT_COMPETENCIA = @competencia
              AND reg.DT_COMPETENCIA = @competencia
            ORDER BY reg.CO_REGISTRO";

        await _context.OpenAsync(cancellationToken);

        var itens = new List<RelacionadoItem>();

        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@coProcedimento", coProcedimento);
        command.Parameters.AddWithValue("@competencia", competencia);

        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var codigo = FirebirdReaderHelper.GetStringSafe(reader, "CO_REGISTRO") ?? string.Empty;
                var descricao = LerCampoTextoDoBlob(reader, "NO_REGISTRO_BLOB", "NO_REGISTRO");
                var dtCompetencia = FirebirdReaderHelper.GetStringSafe(reader, "DT_COMPETENCIA");
                
                itens.Add(new RelacionadoItem
                {
                    Codigo = codigo,
                    Descricao = descricao,
                    InformacaoAdicional = dtCompetencia
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar instrumentos de registro relacionados ao procedimento {Procedimento}", coProcedimento);
            throw;
        }

        return itens;
    }

    public async Task<IEnumerable<RelacionadoItem>> BuscarModalidadesRelacionadasAsync(string coProcedimento, string competencia, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                m.CO_MODALIDADE,
                CAST(m.NO_MODALIDADE AS BLOB) AS NO_MODALIDADE_BLOB,
                m.NO_MODALIDADE,
                pm.DT_COMPETENCIA
            FROM RL_PROCEDIMENTO_MODALIDADE pm
            INNER JOIN TB_MODALIDADE m ON pm.CO_MODALIDADE = m.CO_MODALIDADE
            WHERE pm.CO_PROCEDIMENTO = @coProcedimento
              AND pm.DT_COMPETENCIA = @competencia
              AND m.DT_COMPETENCIA = @competencia
            ORDER BY m.CO_MODALIDADE";

        await _context.OpenAsync(cancellationToken);

        var itens = new List<RelacionadoItem>();

        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@coProcedimento", coProcedimento);
        command.Parameters.AddWithValue("@competencia", competencia);

        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var codigo = FirebirdReaderHelper.GetStringSafe(reader, "CO_MODALIDADE") ?? string.Empty;
                var descricao = LerCampoTextoDoBlob(reader, "NO_MODALIDADE_BLOB", "NO_MODALIDADE");
                var dtCompetencia = FirebirdReaderHelper.GetStringSafe(reader, "DT_COMPETENCIA");
                
                itens.Add(new RelacionadoItem
                {
                    Codigo = codigo,
                    Descricao = descricao,
                    InformacaoAdicional = dtCompetencia
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar modalidades relacionadas ao procedimento {Procedimento}", coProcedimento);
            throw;
        }

        return itens;
    }

    public async Task<IEnumerable<RelacionadoItem>> BuscarDescricaoRelacionadaAsync(string coProcedimento, string competencia, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                d.CO_PROCEDIMENTO,
                CAST(d.DS_PROCEDIMENTO AS BLOB) AS DS_PROCEDIMENTO_BLOB,
                d.DS_PROCEDIMENTO
            FROM TB_DESCRICAO d
            WHERE d.CO_PROCEDIMENTO = @coProcedimento
              AND d.DT_COMPETENCIA = @competencia";

        await _context.OpenAsync(cancellationToken);

        var itens = new List<RelacionadoItem>();

        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@coProcedimento", coProcedimento);
        command.Parameters.AddWithValue("@competencia", competencia);

        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (await reader.ReadAsync(cancellationToken))
            {
                var codigo = FirebirdReaderHelper.GetStringSafe(reader, "CO_PROCEDIMENTO") ?? string.Empty;
                var descricao = LerCampoTextoDoBlob(reader, "DS_PROCEDIMENTO_BLOB", "DS_PROCEDIMENTO");
                
                itens.Add(new RelacionadoItem
                {
                    Codigo = codigo,
                    Descricao = descricao
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar descrição relacionada ao procedimento {Procedimento}", coProcedimento);
            throw;
        }

        return itens;
    }

    public async Task<IEnumerable<RelacionadoItem>> BuscarDetalhesRelacionadosAsync(string coProcedimento, string competencia, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                d.CO_DETALHE,
                CAST(d.NO_DETALHE AS BLOB) AS NO_DETALHE_BLOB,
                d.NO_DETALHE,
                pd.DT_COMPETENCIA,
                CAST(dd.DS_DETALHE AS BLOB) AS DS_DETALHE_BLOB,
                dd.DS_DETALHE
            FROM RL_PROCEDIMENTO_DETALHE pd
            INNER JOIN TB_DETALHE d ON pd.CO_DETALHE = d.CO_DETALHE
            LEFT JOIN TB_DESCRICAO_DETALHE dd ON d.CO_DETALHE = dd.CO_DETALHE 
                AND d.DT_COMPETENCIA = dd.DT_COMPETENCIA
            WHERE pd.CO_PROCEDIMENTO = @coProcedimento
              AND pd.DT_COMPETENCIA = @competencia
              AND d.DT_COMPETENCIA = @competencia
            ORDER BY d.CO_DETALHE";

        await _context.OpenAsync(cancellationToken);

        var itens = new List<RelacionadoItem>();

        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@coProcedimento", coProcedimento);
        command.Parameters.AddWithValue("@competencia", competencia);

        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var codigo = FirebirdReaderHelper.GetStringSafe(reader, "CO_DETALHE") ?? string.Empty;
                var nomeDetalhe = LerCampoTextoDoBlob(reader, "NO_DETALHE_BLOB", "NO_DETALHE");
                var dtCompetencia = FirebirdReaderHelper.GetStringSafe(reader, "DT_COMPETENCIA");
                var descricaoLonga = LerCampoTextoDoBlob(reader, "DS_DETALHE_BLOB", "DS_DETALHE");
                
                itens.Add(new RelacionadoItem
                {
                    Codigo = codigo,
                    Descricao = nomeDetalhe ?? string.Empty, // Nome curto para o grid
                    InformacaoAdicional = dtCompetencia,
                    DescricaoCompleta = descricaoLonga // Descrição longa para o campo de texto
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar detalhes relacionados ao procedimento {Procedimento}", coProcedimento);
            throw;
        }

        return itens;
    }

    public async Task<IEnumerable<RelacionadoItem>> BuscarIncrementosRelacionadosAsync(string coProcedimento, string competencia, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                pi.CO_HABILITACAO,
                h.CO_HABILITACAO AS HAB_CODIGO,
                CAST(h.NO_HABILITACAO AS BLOB) AS NO_HABILITACAO_BLOB,
                h.NO_HABILITACAO,
                pi.VL_PERCENTUAL_SH,
                pi.VL_PERCENTUAL_SA,
                pi.VL_PERCENTUAL_SP
            FROM RL_PROCEDIMENTO_INCREMENTO pi
            LEFT JOIN TB_HABILITACAO h ON pi.CO_HABILITACAO = h.CO_HABILITACAO 
                AND h.DT_COMPETENCIA = (
                    SELECT MAX(DT_COMPETENCIA)
                    FROM TB_HABILITACAO h2
                    WHERE h2.CO_HABILITACAO = h.CO_HABILITACAO
                      AND h2.DT_COMPETENCIA <= @competencia
                )
            WHERE pi.CO_PROCEDIMENTO = @coProcedimento
              AND pi.DT_COMPETENCIA = @competencia
            ORDER BY pi.CO_HABILITACAO";

        await _context.OpenAsync(cancellationToken);

        var itens = new List<RelacionadoItem>();

        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@coProcedimento", coProcedimento);
        command.Parameters.AddWithValue("@competencia", competencia);

        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var codigo = FirebirdReaderHelper.GetStringSafe(reader, "CO_HABILITACAO") ?? string.Empty;
                var descricao = LerCampoTextoDoBlob(reader, "NO_HABILITACAO_BLOB", "NO_HABILITACAO");
                
                // Formatar informações adicionais com os percentuais
                var percentuais = new List<string>();
                
                var percentualSH = reader.IsDBNull(reader.GetOrdinal("VL_PERCENTUAL_SH")) 
                    ? (double?)null 
                    : reader.GetDouble(reader.GetOrdinal("VL_PERCENTUAL_SH"));
                if (percentualSH.HasValue)
                {
                    percentuais.Add($"SH: {percentualSH.Value:F2}%");
                }
                
                var percentualSA = reader.IsDBNull(reader.GetOrdinal("VL_PERCENTUAL_SA")) 
                    ? (double?)null 
                    : reader.GetDouble(reader.GetOrdinal("VL_PERCENTUAL_SA"));
                if (percentualSA.HasValue)
                {
                    percentuais.Add($"SA: {percentualSA.Value:F2}%");
                }
                
                var percentualSP = reader.IsDBNull(reader.GetOrdinal("VL_PERCENTUAL_SP")) 
                    ? (double?)null 
                    : reader.GetDouble(reader.GetOrdinal("VL_PERCENTUAL_SP"));
                if (percentualSP.HasValue)
                {
                    percentuais.Add($"SP: {percentualSP.Value:F2}%");
                }
                
                var informacaoAdicional = percentuais.Any() ? string.Join(", ", percentuais) : null;
                
                itens.Add(new RelacionadoItem
                {
                    Codigo = codigo,
                    Descricao = descricao ?? "Habilitação sem descrição",
                    InformacaoAdicional = informacaoAdicional
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar incrementos relacionados ao procedimento {Procedimento}", coProcedimento);
            throw;
        }

        return itens;
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
                        resultado = ConvertBytesToWindows1252(validBytes);
                        
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

