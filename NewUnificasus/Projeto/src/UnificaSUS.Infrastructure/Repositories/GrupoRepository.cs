using FirebirdSql.Data.FirebirdClient;
using Microsoft.Extensions.Logging;
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
        const string sqlGrupos = @"
            SELECT 
                CO_GRUPO,
                NO_GRUPO,
                DT_COMPETENCIA
            FROM TB_GRUPO
            WHERE DT_COMPETENCIA = @competencia
            ORDER BY CO_GRUPO";

        var grupos = new List<Grupo>();

        try
        {
            await _context.OpenAsync(cancellationToken);

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
                    NoGrupo = FirebirdReaderHelper.GetStringSafe(reader, "NO_GRUPO"),
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
        const string sql = @"
            SELECT 
                CO_GRUPO,
                NO_GRUPO,
                DT_COMPETENCIA
            FROM TB_GRUPO
            WHERE CO_GRUPO = @codigo
              AND DT_COMPETENCIA = @competencia";

        await _context.OpenAsync(cancellationToken);

        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@codigo", codigo);
        command.Parameters.AddWithValue("@competencia", competencia);

        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (await reader.ReadAsync(cancellationToken))
            {
                var grupo = new Grupo
                {
                    CoGrupo = FirebirdReaderHelper.GetStringSafe(reader, "CO_GRUPO") ?? string.Empty,
                    NoGrupo = FirebirdReaderHelper.GetStringSafe(reader, "NO_GRUPO"),
                    DtCompetencia = FirebirdReaderHelper.GetStringSafe(reader, "DT_COMPETENCIA")
                };

                grupo.SubGrupos = await BuscarSubGruposAsync(grupo.CoGrupo, competencia, CancellationToken.None);
                
                return grupo;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar grupo {Codigo} por competência {Competencia}", codigo, competencia);
            throw;
        }

        return null;
    }

    private async Task<List<SubGrupo>> BuscarSubGruposAsync(string coGrupo, string competencia, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT 
                CO_GRUPO,
                CO_SUB_GRUPO,
                NO_SUB_GRUPO,
                DT_COMPETENCIA
            FROM TB_SUB_GRUPO
            WHERE CO_GRUPO = @coGrupo
              AND DT_COMPETENCIA = @competencia
            ORDER BY CO_SUB_GRUPO";

        var subGrupos = new List<SubGrupo>();

        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@coGrupo", coGrupo);
        command.Parameters.AddWithValue("@competencia", competencia);

        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var subGrupo = new SubGrupo
                {
                    CoGrupo = FirebirdReaderHelper.GetStringSafe(reader, "CO_GRUPO") ?? string.Empty,
                    CoSubGrupo = FirebirdReaderHelper.GetStringSafe(reader, "CO_SUB_GRUPO") ?? string.Empty,
                    NoSubGrupo = FirebirdReaderHelper.GetStringSafe(reader, "NO_SUB_GRUPO"),
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar sub-grupos do grupo {CoGrupo}", coGrupo);
            throw;
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

        var sql = $@"
            SELECT 
                CO_GRUPO,
                CO_SUB_GRUPO,
                NO_SUB_GRUPO,
                DT_COMPETENCIA
            FROM TB_SUB_GRUPO
            WHERE DT_COMPETENCIA = @competencia
              AND CO_GRUPO IN ({string.Join(", ", parametros)})
            ORDER BY CO_GRUPO, CO_SUB_GRUPO";

        var resultado = new Dictionary<string, List<SubGrupo>>();

        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@competencia", competencia);
        
        for (int i = 0; i < coGrupos.Count; i++)
        {
            command.Parameters.AddWithValue($"@grupo{i}", coGrupos[i]);
        }

        try
        {
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
                    NoSubGrupo = FirebirdReaderHelper.GetStringSafe(reader, "NO_SUB_GRUPO"),
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar sub-grupos em batch");
            throw;
        }

        return resultado;
    }

    private async Task<List<FormaOrganizacao>> BuscarFormasOrganizacaoAsync(
        string coGrupo, 
        string coSubGrupo, 
        string competencia, 
        CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT 
                CO_GRUPO,
                CO_SUB_GRUPO,
                CO_FORMA_ORGANIZACAO,
                NO_FORMA_ORGANIZACAO,
                DT_COMPETENCIA
            FROM TB_FORMA_ORGANIZACAO
            WHERE CO_GRUPO = @coGrupo
              AND CO_SUB_GRUPO = @coSubGrupo
              AND DT_COMPETENCIA = @competencia
            ORDER BY CO_FORMA_ORGANIZACAO";

        var formas = new List<FormaOrganizacao>();

        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@coGrupo", coGrupo);
        command.Parameters.AddWithValue("@coSubGrupo", coSubGrupo);
        command.Parameters.AddWithValue("@competencia", competencia);

        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                formas.Add(new FormaOrganizacao
                {
                    CoGrupo = FirebirdReaderHelper.GetStringSafe(reader, "CO_GRUPO") ?? string.Empty,
                    CoSubGrupo = FirebirdReaderHelper.GetStringSafe(reader, "CO_SUB_GRUPO") ?? string.Empty,
                    CoFormaOrganizacao = FirebirdReaderHelper.GetStringSafe(reader, "CO_FORMA_ORGANIZACAO") ?? string.Empty,
                    NoFormaOrganizacao = FirebirdReaderHelper.GetStringSafe(reader, "NO_FORMA_ORGANIZACAO"),
                    DtCompetencia = FirebirdReaderHelper.GetStringSafe(reader, "DT_COMPETENCIA")
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar formas de organização do grupo {CoGrupo} sub-grupo {CoSubGrupo}", coGrupo, coSubGrupo);
            throw;
        }

        return formas;
    }
}

