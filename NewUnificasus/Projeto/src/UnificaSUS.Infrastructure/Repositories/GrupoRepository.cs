using FirebirdSql.Data.FirebirdClient;
using Microsoft.Extensions.Logging;
using System.Text;
using UnificaSUS.Core.Entities;
using UnificaSUS.Core.Interfaces;
using UnificaSUS.Infrastructure.Data;
using UnificaSUS.Infrastructure.Helpers;

namespace UnificaSUS.Infrastructure.Repositories;

/// <summary>
/// Implementação do repositório de grupos
/// </summary>
public class GrupoRepository : IGrupoRepository
{
    private readonly FirebirdContext _context;
    private readonly ILogger<GrupoRepository> _logger;

    public GrupoRepository(FirebirdContext context, ILogger<GrupoRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Grupo>> BuscarTodosAsync(string competencia, CancellationToken cancellationToken = default)
    {
        // Otimizado: carrega tudo em uma única transação e faz cache dos sub-grupos
        // Usa CAST para BLOB nos campos de texto para garantir acesso aos bytes brutos
        const string sqlGrupos = @"
            SELECT 
                CO_GRUPO,
                CAST(NO_GRUPO AS BLOB) AS NO_GRUPO_BLOB,
                NO_GRUPO,
                DT_COMPETENCIA
            FROM TB_GRUPO
            WHERE DT_COMPETENCIA = @competencia
            ORDER BY CO_GRUPO";

        var grupos = new List<Grupo>();

        try
        {
            await _context.OpenAsync(cancellationToken);

            // Operações de leitura não precisam de transação explícita no Firebird
            // Primeiro, carrega todos os grupos
            using var command = new FbCommand(sqlGrupos, _context.Connection);
            command.Parameters.AddWithValue("@competencia", competencia);

            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var gruposList = new List<Grupo>();
            while (await reader.ReadAsync(cancellationToken))
            {
                var grupo = new Grupo
                {
                    CoGrupo = FirebirdReaderHelper.GetStringSafe(reader, "CO_GRUPO") ?? string.Empty,
                    NoGrupo = LerCampoTextoDoBlob(reader, "NO_GRUPO_BLOB", "NO_GRUPO"),
                    DtCompetencia = FirebirdReaderHelper.GetStringSafe(reader, "DT_COMPETENCIA")
                };
                gruposList.Add(grupo);
            }

            // Agora carrega todos os sub-grupos de uma vez (evita N+1 queries)
            if (gruposList.Count > 0)
            {
                var gruposDict = gruposList.ToDictionary(g => g.CoGrupo);
                var coGrupos = gruposList.Select(g => g.CoGrupo).Distinct().ToList();
                
                // Carrega todos os sub-grupos de todos os grupos de uma vez
                var subGruposMap = await BuscarTodosSubGruposAsync(coGrupos, competencia, cancellationToken);
                
                // Para cada grupo, atribui seus sub-grupos
                foreach (var grupo in gruposList)
                {
                    if (subGruposMap.TryGetValue(grupo.CoGrupo, out var subGrupos))
                    {
                        grupo.SubGrupos = subGrupos;
                    }
                    else
                    {
                        grupo.SubGrupos = new List<SubGrupo>();
                    }
                }
            }
            
            grupos = gruposList;
        }
        catch (FirebirdSql.Data.FirebirdClient.FbException ex)
        {
            _logger.LogError(ex, "Erro de banco de dados ao buscar grupos por competência {Competencia}. Mensagem: {Mensagem}", competencia, ex.Message);
            
            // Relançar com mensagem mais amigável
            throw new InvalidOperationException(
                $"Erro ao buscar grupos no banco de dados:\n\n{ex.Message}\n\n" +
                $"Verifique:\n" +
                $"- Se o banco de dados está acessível\n" +
                $"- Se a competência {competencia} existe no banco\n" +
                $"- Se a tabela TB_GRUPO existe", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar grupos por competência {Competencia}", competencia);
            throw;
        }

        return grupos;
    }

    public async Task<Grupo?> BuscarPorCodigoAsync(string codigo, string competencia, CancellationToken cancellationToken = default)
    {
        // Usa CAST para BLOB nos campos de texto para garantir acesso aos bytes brutos
        const string sql = @"
            SELECT 
                CO_GRUPO,
                CAST(NO_GRUPO AS BLOB) AS NO_GRUPO_BLOB,
                NO_GRUPO,
                DT_COMPETENCIA
            FROM TB_GRUPO
            WHERE CO_GRUPO = @codigo
              AND DT_COMPETENCIA = @competencia";

        await _context.OpenAsync(cancellationToken);

        // Operações de leitura não precisam de transação explícita no Firebird
        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@codigo", codigo);
        command.Parameters.AddWithValue("@competencia", competencia);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            var grupo = new Grupo
            {
                CoGrupo = FirebirdReaderHelper.GetStringSafe(reader, "CO_GRUPO") ?? string.Empty,
                NoGrupo = LerCampoTextoDoBlob(reader, "NO_GRUPO_BLOB", "NO_GRUPO"),
                DtCompetencia = FirebirdReaderHelper.GetStringSafe(reader, "DT_COMPETENCIA")
            };

            // Busca sub-grupos
            grupo.SubGrupos = await BuscarSubGruposAsync(grupo.CoGrupo, competencia, cancellationToken);
            
            return grupo;
        }

        return null;
    }

    private async Task<List<SubGrupo>> BuscarSubGruposAsync(string coGrupo, string competencia, CancellationToken cancellationToken)
    {
        // Usa CAST para BLOB nos campos de texto para garantir acesso aos bytes brutos
        const string sql = @"
            SELECT 
                CO_GRUPO,
                CO_SUB_GRUPO,
                CAST(NO_SUB_GRUPO AS BLOB) AS NO_SUB_GRUPO_BLOB,
                NO_SUB_GRUPO,
                DT_COMPETENCIA
            FROM TB_SUB_GRUPO
            WHERE CO_GRUPO = @coGrupo
              AND DT_COMPETENCIA = @competencia
            ORDER BY CO_SUB_GRUPO";

        var subGrupos = new List<SubGrupo>();

        // Operações de leitura não precisam de transação explícita no Firebird
        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@coGrupo", coGrupo);
        command.Parameters.AddWithValue("@competencia", competencia);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var subGrupo = new SubGrupo
            {
                CoGrupo = FirebirdReaderHelper.GetStringSafe(reader, "CO_GRUPO") ?? string.Empty,
                CoSubGrupo = FirebirdReaderHelper.GetStringSafe(reader, "CO_SUB_GRUPO") ?? string.Empty,
                NoSubGrupo = LerCampoTextoDoBlob(reader, "NO_SUB_GRUPO_BLOB", "NO_SUB_GRUPO"),
                DtCompetencia = FirebirdReaderHelper.GetStringSafe(reader, "DT_COMPETENCIA")
            };

            // Carregar formas de organização
            subGrupo.FormasOrganizacao = await BuscarFormasOrganizacaoAsync(
                subGrupo.CoGrupo, 
                subGrupo.CoSubGrupo, 
                competencia, 
                cancellationToken);

            subGrupos.Add(subGrupo);
        }

        return subGrupos;
    }

    /// <summary>
    /// Otimizado: carrega todos os sub-grupos de múltiplos grupos de uma vez (evita N+1 queries)
    /// </summary>
    private async Task<Dictionary<string, List<SubGrupo>>> BuscarTodosSubGruposAsync(
        List<string> coGrupos, 
        string competencia, 
        CancellationToken cancellationToken)
    {
        if (coGrupos.Count == 0)
            return new Dictionary<string, List<SubGrupo>>();

        // Cria uma lista de parâmetros dinamicamente
        var parametros = new List<string>();
        for (int i = 0; i < coGrupos.Count; i++)
        {
            parametros.Add($"@grupo{i}");
        }

        // Usa CAST para BLOB nos campos de texto para garantir acesso aos bytes brutos
        var sql = $@"
            SELECT 
                CO_GRUPO,
                CO_SUB_GRUPO,
                CAST(NO_SUB_GRUPO AS BLOB) AS NO_SUB_GRUPO_BLOB,
                NO_SUB_GRUPO,
                DT_COMPETENCIA
            FROM TB_SUB_GRUPO
            WHERE DT_COMPETENCIA = @competencia
              AND CO_GRUPO IN ({string.Join(", ", parametros)})
            ORDER BY CO_GRUPO, CO_SUB_GRUPO";

        var resultado = new Dictionary<string, List<SubGrupo>>();

        // Operações de leitura não precisam de transação explícita no Firebird
        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@competencia", competencia);
        
        for (int i = 0; i < coGrupos.Count; i++)
        {
            command.Parameters.AddWithValue($"@grupo{i}", coGrupos[i]);
        }

        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var coGrupo = FirebirdReaderHelper.GetStringSafe(reader, "CO_GRUPO") ?? string.Empty;
            
            if (!resultado.ContainsKey(coGrupo))
            {
                resultado[coGrupo] = new List<SubGrupo>();
            }

            var subGrupo = new SubGrupo
            {
                CoGrupo = coGrupo,
                CoSubGrupo = FirebirdReaderHelper.GetStringSafe(reader, "CO_SUB_GRUPO") ?? string.Empty,
                NoSubGrupo = LerCampoTextoDoBlob(reader, "NO_SUB_GRUPO_BLOB", "NO_SUB_GRUPO"),
                DtCompetencia = FirebirdReaderHelper.GetStringSafe(reader, "DT_COMPETENCIA")
            };

            resultado[coGrupo].Add(subGrupo);
        }

        // Carrega formas de organização para todos os sub-grupos (em batch)
        foreach (var grupoKey in resultado.Keys.ToList())
        {
            var subGrupos = resultado[grupoKey];
            foreach (var subGrupo in subGrupos)
            {
                subGrupo.FormasOrganizacao = await BuscarFormasOrganizacaoAsync(
                    subGrupo.CoGrupo,
                    subGrupo.CoSubGrupo,
                    competencia,
                    cancellationToken);
            }
        }

        return resultado;
    }

    private async Task<List<FormaOrganizacao>> BuscarFormasOrganizacaoAsync(
        string coGrupo, 
        string coSubGrupo, 
        string competencia, 
        CancellationToken cancellationToken)
    {
        // Usa CAST para BLOB nos campos de texto para garantir acesso aos bytes brutos
        const string sql = @"
            SELECT 
                CO_GRUPO,
                CO_SUB_GRUPO,
                CO_FORMA_ORGANIZACAO,
                CAST(NO_FORMA_ORGANIZACAO AS BLOB) AS NO_FORMA_ORGANIZACAO_BLOB,
                NO_FORMA_ORGANIZACAO,
                DT_COMPETENCIA
            FROM TB_FORMA_ORGANIZACAO
            WHERE CO_GRUPO = @coGrupo
              AND CO_SUB_GRUPO = @coSubGrupo
              AND DT_COMPETENCIA = @competencia
            ORDER BY CO_FORMA_ORGANIZACAO";

        var formas = new List<FormaOrganizacao>();

        // Operações de leitura não precisam de transação explícita no Firebird
        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@coGrupo", coGrupo);
        command.Parameters.AddWithValue("@coSubGrupo", coSubGrupo);
        command.Parameters.AddWithValue("@competencia", competencia);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            formas.Add(new FormaOrganizacao
            {
                CoGrupo = FirebirdReaderHelper.GetStringSafe(reader, "CO_GRUPO") ?? string.Empty,
                CoSubGrupo = FirebirdReaderHelper.GetStringSafe(reader, "CO_SUB_GRUPO") ?? string.Empty,
                CoFormaOrganizacao = FirebirdReaderHelper.GetStringSafe(reader, "CO_FORMA_ORGANIZACAO") ?? string.Empty,
                NoFormaOrganizacao = LerCampoTextoDoBlob(reader, "NO_FORMA_ORGANIZACAO_BLOB", "NO_FORMA_ORGANIZACAO"),
                DtCompetencia = FirebirdReaderHelper.GetStringSafe(reader, "DT_COMPETENCIA")
            });
        }

        return formas;
    }

    /// <summary>
    /// Lê um campo de texto do BLOB primeiro, depois do campo direto se necessário
    /// Garante conversão correta de encoding para acentuação
    /// </summary>
    private static string? LerCampoTextoDoBlob(FbDataReader reader, string blobColumnName, string directColumnName)
    {
        string? resultado = null;
        
        // Prioridade 1: Tenta ler do BLOB (CAST para BLOB garante acesso aos bytes brutos)
        try
        {
            var blobOrdinal = reader.GetOrdinal(blobColumnName);
            if (!reader.IsDBNull(blobOrdinal))
            {
                // Lê diretamente como byte[] do BLOB (mais confiável)
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
                        // Converte diretamente para Windows-1252
                        resultado = ConvertBytesToWindows1252(validBytes);
                        
                        // Se a conversão resultou em caracteres corrompidos, tenta o helper
                        if (!string.IsNullOrEmpty(resultado) && 
                            (resultado.Contains('\uFFFD') || resultado.Contains('?')))
                        {
                            resultado = FirebirdReaderHelper.GetStringSafe(reader, blobColumnName);
                        }
                    }
                }
                
                // Se não conseguiu ler como byte[], usa o helper
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
        
        // Prioridade 2: Se BLOB não funcionou ou está vazio, tenta campo direto
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

    /// <summary>
    /// Converte bytes diretamente para Windows-1252 (helper local)
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

