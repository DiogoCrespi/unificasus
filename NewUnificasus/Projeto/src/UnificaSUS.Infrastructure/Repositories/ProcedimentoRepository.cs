using FirebirdSql.Data.FirebirdClient;
using Microsoft.Extensions.Logging;
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

    public async Task<IEnumerable<Procedimento>> BuscarPorFiltroAsync(string filtro, string competencia, CancellationToken cancellationToken = default)
    {
        // Otimizado: usa LIKE com % apenas no final quando possível (mais rápido que CONTAINING)
        // Limita a 1000 resultados para melhor performance
        // Usa CAST para BLOB para garantir acesso aos bytes brutos
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
              AND (pr.CO_PROCEDIMENTO LIKE @filtro 
                   OR UPPER(CAST(pr.NO_PROCEDIMENTO AS VARCHAR(250))) LIKE @filtroUpper)
            ORDER BY pr.CO_PROCEDIMENTO";

        await _context.OpenAsync(cancellationToken);

        var procedimentos = new List<Procedimento>();
        // Se filtro começa com texto específico, usa STARTING WITH (mais rápido)
        // Senão, usa LIKE com wildcards
        var filtroUpper = $"%{filtro.ToUpper()}%";
        var filtroLike = $"%{filtro}%";

        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@competencia", competencia);
        command.Parameters.AddWithValue("@filtro", filtroLike);
        command.Parameters.AddWithValue("@filtroUpper", filtroUpper);

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
            WHERE pc.DT_COMPETENCIA = @competencia
              AND pc.CO_CID = @cid
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

    private static Procedimento MapProcedimento(FbDataReader reader)
    {
        // Tenta ler NO_PROCEDIMENTO como BLOB primeiro (se usar CAST no SQL)
        // Isso permite acesso aos bytes brutos antes de qualquer interpretação
        string? noProcedimento = null;
        try
        {
            var blobOrdinal = reader.GetOrdinal("NO_PROCEDIMENTO_BLOB");
            if (!reader.IsDBNull(blobOrdinal))
            {
                byte[]? blobBytes = null;
                
                // Tenta diferentes formas de obter os bytes do BLOB
                var blobValue = reader.GetValue(blobOrdinal);
                
                if (blobValue is byte[] bytes)
                {
                    // Retornou diretamente como byte[]
                    blobBytes = bytes;
                }
                else
                {
                    // Tenta usar GetBytes para obter os bytes do BLOB
                    try
                    {
                        // Primeiro obtém o tamanho do BLOB
                        var length = reader.GetBytes(blobOrdinal, 0, null, 0, 0);
                        if (length > 0)
                        {
                            blobBytes = new byte[(int)length];
                            // Lê todos os bytes do BLOB
                            reader.GetBytes(blobOrdinal, 0, blobBytes, 0, (int)length);
                        }
                    }
                    catch
                    {
                        // Se GetBytes falhar, tenta ler como string (pode ser que o driver já tenha convertido)
                        try
                        {
                            var blobString = reader.GetString(blobOrdinal);
                            if (!string.IsNullOrEmpty(blobString))
                            {
                                // Converte usando o helper
                                noProcedimento = ConvertBlobToString(blobString);
                            }
                        }
                        catch
                        {
                            // Se tudo falhar, deixa null
                            noProcedimento = null;
                        }
                    }
                }
                
                // Se conseguiu obter bytes, converte para string usando Windows-1252
                if (blobBytes != null && blobBytes.Length > 0)
                {
                    var encoding = System.Text.Encoding.GetEncoding(1252); // Windows-1252
                    noProcedimento = encoding.GetString(blobBytes);
                }
            }
            
            // Se ainda não conseguiu ler do BLOB, tenta ler o campo NO_PROCEDIMENTO diretamente
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
                    // Se não encontrar o campo, deixa null
                    noProcedimento = null;
                }
            }
        }
        catch (Exception ex)
        {
            // Log do erro para debug, mas não interrompe o processamento
            // Se não conseguiu ler o BLOB, tenta ler o campo diretamente
            System.Diagnostics.Debug.WriteLine($"Erro ao ler BLOB NO_PROCEDIMENTO: {ex.Message}\n{ex.StackTrace}");
            
            try
            {
                // Fallback: tenta ler o campo NO_PROCEDIMENTO diretamente
                noProcedimento = FirebirdReaderHelper.GetStringSafe(reader, "NO_PROCEDIMENTO");
            }
            catch
            {
                noProcedimento = null;
            }
        }

        return new Procedimento
        {
            CoProcedimento = FirebirdReaderHelper.GetStringSafe(reader, "CO_PROCEDIMENTO") ?? string.Empty,
            NoProcedimento = noProcedimento, // Usa apenas o valor do BLOB (ou null se não encontrou)
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
            CoFinanciamento = FirebirdReaderHelper.GetStringSafe(reader, "CO_FINANCIAMENTO"),
            CoRubrica = FirebirdReaderHelper.GetStringSafe(reader, "CO_RUBRICA"),
            QtTempoPermanencia = reader.IsDBNull(reader.GetOrdinal("QT_TEMPO_PERMANENCIA")) ? null : reader.GetInt32(reader.GetOrdinal("QT_TEMPO_PERMANENCIA")),
            DtCompetencia = FirebirdReaderHelper.GetStringSafe(reader, "DT_COMPETENCIA")
        };
    }

    /// <summary>
    /// Converte uma string que veio de BLOB para o encoding correto (Windows-1252)
    /// </summary>
    private static string? ConvertBlobToString(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        try
        {
            // Se a string contém caracteres que parecem errados, tenta reconverter
            // Assume que os bytes foram interpretados incorretamente e precisa reconverter
            var encoding = System.Text.Encoding.GetEncoding(1252); // Windows-1252
            
            // Se a string tem caracteres especiais mas parecem errados, tenta reconverter
            // Primeiro, converte para bytes usando o encoding atual
            var currentBytes = System.Text.Encoding.Default.GetBytes(value);
            
            // Agora reinterpreta como Windows-1252
            return encoding.GetString(currentBytes);
        }
        catch
        {
            return value;
        }
    }

}

