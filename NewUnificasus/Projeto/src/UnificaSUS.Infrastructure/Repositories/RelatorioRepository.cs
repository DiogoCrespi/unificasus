using FirebirdSql.Data.FirebirdClient;
using Microsoft.Extensions.Logging;
using System.Text;
using UnificaSUS.Core.Entities;
using UnificaSUS.Core.Interfaces;
using UnificaSUS.Infrastructure.Data;
using UnificaSUS.Infrastructure.Helpers;

namespace UnificaSUS.Infrastructure.Repositories;

/// <summary>
/// Implementação do repositório de relatórios
/// </summary>
public class RelatorioRepository : IRelatorioRepository
{
    private readonly FirebirdContext _context;
    private readonly ILogger<RelatorioRepository> _logger;

    public RelatorioRepository(FirebirdContext context, ILogger<RelatorioRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<ItemRelatorioProcedimento>> BuscarProcedimentosPorGrupoAsync(
        string coGrupo, 
        string competencia, 
        bool naoImprimirSPZerado, 
        string ordenarPor, 
        CancellationToken cancellationToken = default)
    {
        var whereClause = "SUBSTRING(pr.CO_PROCEDIMENTO FROM 1 FOR 2) = @coGrupo AND pr.DT_COMPETENCIA = @competencia";
        
        if (naoImprimirSPZerado)
        {
            whereClause += " AND pr.VL_SP > 0";
        }

        var orderByClause = ordenarPor switch
        {
            "Nome" => "pr.NO_PROCEDIMENTO",
            "ValorSP" => "pr.VL_SP DESC",
            _ => "pr.CO_PROCEDIMENTO"
        };

        var sql = $@"
            SELECT 
                pr.CO_PROCEDIMENTO,
                CAST(pr.NO_PROCEDIMENTO AS BLOB) AS NO_PROCEDIMENTO_BLOB,
                pr.NO_PROCEDIMENTO,
                pr.VL_SP
            FROM TB_PROCEDIMENTO pr
            WHERE {whereClause}
            ORDER BY {orderByClause}";

        await _context.OpenAsync(cancellationToken);

        var procedimentos = new List<ItemRelatorioProcedimento>();

        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@coGrupo", coGrupo.PadLeft(2, '0'));
        command.Parameters.AddWithValue("@competencia", competencia);

        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                procedimentos.Add(MapItemRelatorioProcedimento(reader));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar procedimentos por grupo {Grupo} e competência {Competencia}", coGrupo, competencia);
            throw;
        }

        return procedimentos;
    }

    public async Task<IEnumerable<ItemRelatorioProcedimento>> BuscarProcedimentosPorSubGrupoAsync(
        string coGrupo, 
        string coSubGrupo, 
        string competencia, 
        bool naoImprimirSPZerado, 
        string ordenarPor, 
        CancellationToken cancellationToken = default)
    {
        var whereClause = "SUBSTRING(pr.CO_PROCEDIMENTO FROM 1 FOR 4) = @codigoSubGrupo AND pr.DT_COMPETENCIA = @competencia";
        
        if (naoImprimirSPZerado)
        {
            whereClause += " AND pr.VL_SP > 0";
        }

        var orderByClause = ordenarPor switch
        {
            "Nome" => "pr.NO_PROCEDIMENTO",
            "ValorSP" => "pr.VL_SP DESC",
            _ => "pr.CO_PROCEDIMENTO"
        };

        var codigoSubGrupo = (coGrupo.PadLeft(2, '0') + coSubGrupo.PadLeft(2, '0'));

        var sql = $@"
            SELECT 
                pr.CO_PROCEDIMENTO,
                CAST(pr.NO_PROCEDIMENTO AS BLOB) AS NO_PROCEDIMENTO_BLOB,
                pr.NO_PROCEDIMENTO,
                pr.VL_SP
            FROM TB_PROCEDIMENTO pr
            WHERE {whereClause}
            ORDER BY {orderByClause}";

        await _context.OpenAsync(cancellationToken);

        var procedimentos = new List<ItemRelatorioProcedimento>();

        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@codigoSubGrupo", codigoSubGrupo);
        command.Parameters.AddWithValue("@competencia", competencia);

        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                procedimentos.Add(MapItemRelatorioProcedimento(reader));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar procedimentos por sub-grupo {SubGrupo} e competência {Competencia}", codigoSubGrupo, competencia);
            throw;
        }

        return procedimentos;
    }

    public async Task<IEnumerable<ItemRelatorioProcedimento>> BuscarProcedimentosPorFormaOrganizacaoAsync(
        string coGrupo, 
        string coSubGrupo, 
        string coFormaOrganizacao, 
        string competencia, 
        bool naoImprimirSPZerado, 
        string ordenarPor, 
        CancellationToken cancellationToken = default)
    {
        var whereClause = "SUBSTRING(pr.CO_PROCEDIMENTO FROM 1 FOR 6) = @codigoFormaOrg AND pr.DT_COMPETENCIA = @competencia";
        
        if (naoImprimirSPZerado)
        {
            whereClause += " AND pr.VL_SP > 0";
        }

        var orderByClause = ordenarPor switch
        {
            "Nome" => "pr.NO_PROCEDIMENTO",
            "ValorSP" => "pr.VL_SP DESC",
            _ => "pr.CO_PROCEDIMENTO"
        };

        var codigoFormaOrg = (coGrupo.PadLeft(2, '0') + coSubGrupo.PadLeft(2, '0') + coFormaOrganizacao.PadLeft(2, '0'));

        var sql = $@"
            SELECT 
                pr.CO_PROCEDIMENTO,
                CAST(pr.NO_PROCEDIMENTO AS BLOB) AS NO_PROCEDIMENTO_BLOB,
                pr.NO_PROCEDIMENTO,
                pr.VL_SP
            FROM TB_PROCEDIMENTO pr
            WHERE {whereClause}
            ORDER BY {orderByClause}";

        await _context.OpenAsync(cancellationToken);

        var procedimentos = new List<ItemRelatorioProcedimento>();

        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@codigoFormaOrg", codigoFormaOrg);
        command.Parameters.AddWithValue("@competencia", competencia);

        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                procedimentos.Add(MapItemRelatorioProcedimento(reader));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar procedimentos por forma de organização {FormaOrg} e competência {Competencia}", codigoFormaOrg, competencia);
            throw;
        }

        return procedimentos;
    }

    public async Task<IEnumerable<ItemRelatorioProcedimento>> BuscarProcedimentosPorCodigoOuNomeAsync(
        string codigoOuNome, 
        string competencia, 
        bool naoImprimirSPZerado, 
        string ordenarPor, 
        CancellationToken cancellationToken = default)
    {
        var whereClause = "pr.DT_COMPETENCIA = @competencia AND (pr.CO_PROCEDIMENTO CONTAINING @filtro OR UPPER(CAST(pr.NO_PROCEDIMENTO AS VARCHAR(250))) CONTAINING @filtro)";
        
        if (naoImprimirSPZerado)
        {
            whereClause += " AND pr.VL_SP > 0";
        }

        var orderByClause = ordenarPor switch
        {
            "Nome" => "pr.NO_PROCEDIMENTO",
            "ValorSP" => "pr.VL_SP DESC",
            _ => "pr.CO_PROCEDIMENTO"
        };

        var sql = $@"
            SELECT 
                pr.CO_PROCEDIMENTO,
                CAST(pr.NO_PROCEDIMENTO AS BLOB) AS NO_PROCEDIMENTO_BLOB,
                pr.NO_PROCEDIMENTO,
                pr.VL_SP
            FROM TB_PROCEDIMENTO pr
            WHERE {whereClause}
            ORDER BY {orderByClause}";

        await _context.OpenAsync(cancellationToken);

        var procedimentos = new List<ItemRelatorioProcedimento>();

        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@competencia", competencia);
        command.Parameters.AddWithValue("@filtro", codigoOuNome.ToUpper());

        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                procedimentos.Add(MapItemRelatorioProcedimento(reader));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar procedimentos por código ou nome {Filtro} e competência {Competencia}", codigoOuNome, competencia);
            throw;
        }

        return procedimentos;
    }

    public async Task<IEnumerable<ItemRelatorio>> BuscarGruposDisponiveisAsync(
        string competencia, 
        string? filtro, 
        CancellationToken cancellationToken = default)
    {
        var whereClause = "g.DT_COMPETENCIA = @competencia";
        
        if (!string.IsNullOrWhiteSpace(filtro))
        {
            whereClause += " AND (g.CO_GRUPO CONTAINING @filtro OR UPPER(CAST(g.NO_GRUPO AS VARCHAR(100))) CONTAINING @filtro)";
        }

        var sql = $@"
            SELECT DISTINCT
                g.CO_GRUPO AS CODIGO,
                CAST(g.NO_GRUPO AS BLOB) AS NO_GRUPO_BLOB,
                g.NO_GRUPO AS NOME
            FROM TB_GRUPO g
            WHERE {whereClause}
            ORDER BY g.CO_GRUPO";

        await _context.OpenAsync(cancellationToken);

        var grupos = new List<ItemRelatorio>();

        // Operações de leitura não precisam de transação explícita no Firebird
        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@competencia", competencia);
        
        if (!string.IsNullOrWhiteSpace(filtro))
        {
            command.Parameters.AddWithValue("@filtro", filtro.ToUpper());
        }

        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            int contador = 0;
            while (await reader.ReadAsync(cancellationToken))
            {
                var item = MapItemRelatorio(reader, "Grupo");
                grupos.Add(item);
                contador++;
                
                // Log para debug (primeiros 3 itens)
                if (contador <= 3)
                {
                    _logger.LogDebug("Grupo mapeado: Código={Codigo}, Nome={Nome}", item.Codigo, item.Nome);
                }
            }
            
            _logger.LogInformation("Total de grupos encontrados: {Total} para competência {Competencia}", grupos.Count, competencia);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar grupos disponíveis para competência {Competencia}", competencia);
            throw;
        }

        return grupos;
    }

    public async Task<IEnumerable<ItemRelatorio>> BuscarSubGruposDisponiveisAsync(
        string competencia, 
        string? filtro, 
        CancellationToken cancellationToken = default)
    {
        var whereClause = "sg.DT_COMPETENCIA = @competencia";
        
        if (!string.IsNullOrWhiteSpace(filtro))
        {
            whereClause += " AND ((sg.CO_GRUPO || sg.CO_SUB_GRUPO) CONTAINING @filtro OR UPPER(CAST(sg.NO_SUB_GRUPO AS VARCHAR(100))) CONTAINING @filtro)";
        }

        var sql = $@"
            SELECT DISTINCT
                (sg.CO_GRUPO || sg.CO_SUB_GRUPO) AS CODIGO,
                CAST(sg.NO_SUB_GRUPO AS BLOB) AS NO_SUB_GRUPO_BLOB,
                sg.NO_SUB_GRUPO AS NOME
            FROM TB_SUB_GRUPO sg
            WHERE {whereClause}
            ORDER BY sg.CO_GRUPO, sg.CO_SUB_GRUPO";

        await _context.OpenAsync(cancellationToken);

        var subGrupos = new List<ItemRelatorio>();

        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@competencia", competencia);
        
        if (!string.IsNullOrWhiteSpace(filtro))
        {
            command.Parameters.AddWithValue("@filtro", filtro.ToUpper());
        }

        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                subGrupos.Add(MapItemRelatorio(reader, "SubGrupo"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar sub-grupos disponíveis para competência {Competencia}", competencia);
            throw;
        }

        return subGrupos;
    }

    public async Task<IEnumerable<ItemRelatorio>> BuscarFormasOrganizacaoDisponiveisAsync(
        string competencia, 
        string? filtro, 
        CancellationToken cancellationToken = default)
    {
        var whereClause = "fo.DT_COMPETENCIA = @competencia";
        
        if (!string.IsNullOrWhiteSpace(filtro))
        {
            whereClause += " AND ((fo.CO_GRUPO || fo.CO_SUB_GRUPO || fo.CO_FORMA_ORGANIZACAO) CONTAINING @filtro OR UPPER(CAST(fo.NO_FORMA_ORGANIZACAO AS VARCHAR(100))) CONTAINING @filtro)";
        }

        var sql = $@"
            SELECT DISTINCT
                (fo.CO_GRUPO || fo.CO_SUB_GRUPO || fo.CO_FORMA_ORGANIZACAO) AS CODIGO,
                CAST(fo.NO_FORMA_ORGANIZACAO AS BLOB) AS NO_FORMA_ORGANIZACAO_BLOB,
                fo.NO_FORMA_ORGANIZACAO AS NOME
            FROM TB_FORMA_ORGANIZACAO fo
            WHERE {whereClause}
            ORDER BY fo.CO_GRUPO, fo.CO_SUB_GRUPO, fo.CO_FORMA_ORGANIZACAO";

        await _context.OpenAsync(cancellationToken);

        var formasOrg = new List<ItemRelatorio>();

        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@competencia", competencia);
        
        if (!string.IsNullOrWhiteSpace(filtro))
        {
            command.Parameters.AddWithValue("@filtro", filtro.ToUpper());
        }

        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                formasOrg.Add(MapItemRelatorio(reader, "FormaOrganizacao"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar formas de organização disponíveis para competência {Competencia}", competencia);
            throw;
        }

        return formasOrg;
    }

    public async Task<IEnumerable<ItemRelatorio>> BuscarProcedimentosDisponiveisAsync(
        string competencia, 
        string? filtro, 
        CancellationToken cancellationToken = default)
    {
        var whereClause = "pr.DT_COMPETENCIA = @competencia";
        
        if (!string.IsNullOrWhiteSpace(filtro))
        {
            whereClause += " AND (pr.CO_PROCEDIMENTO CONTAINING @filtro OR UPPER(CAST(pr.NO_PROCEDIMENTO AS VARCHAR(250))) CONTAINING @filtro)";
        }

        var sql = $@"
            SELECT DISTINCT
                pr.CO_PROCEDIMENTO AS CODIGO,
                CAST(pr.NO_PROCEDIMENTO AS BLOB) AS NO_PROCEDIMENTO_BLOB,
                pr.NO_PROCEDIMENTO AS NOME
            FROM TB_PROCEDIMENTO pr
            WHERE {whereClause}
            ORDER BY pr.CO_PROCEDIMENTO";

        await _context.OpenAsync(cancellationToken);

        var procedimentos = new List<ItemRelatorio>();

        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@competencia", competencia);
        
        if (!string.IsNullOrWhiteSpace(filtro))
        {
            command.Parameters.AddWithValue("@filtro", filtro.ToUpper());
        }

        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                procedimentos.Add(MapItemRelatorio(reader, "Procedimento"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar procedimentos disponíveis para competência {Competencia}", competencia);
            throw;
        }

        return procedimentos;
    }

    private static ItemRelatorioProcedimento MapItemRelatorioProcedimento(FbDataReader reader)
    {
        var codigo = FirebirdReaderHelper.GetStringSafe(reader, "CO_PROCEDIMENTO") ?? string.Empty;
        // Usa o mesmo método da tela inicial para garantir acentuação correta
        var nome = LerCampoTextoDoBlob(reader, "NO_PROCEDIMENTO_BLOB", "NO_PROCEDIMENTO");
        
        decimal? vlSp = null;
        try
        {
            var ordinal = reader.GetOrdinal("VL_SP");
            if (!reader.IsDBNull(ordinal))
            {
                vlSp = reader.GetDecimal(ordinal);
            }
        }
        catch { }

        return new ItemRelatorioProcedimento
        {
            CoProcedimento = codigo,
            NoProcedimento = nome,
            VlSp = vlSp
        };
    }

    private static ItemRelatorio MapItemRelatorio(FbDataReader reader, string tipo)
    {
        var codigo = FirebirdReaderHelper.GetStringSafe(reader, "CODIGO") ?? string.Empty;
        string? nome = null;

        // Usa o mesmo método da tela inicial para garantir acentuação correta
        try
        {
            switch (tipo)
            {
                case "Grupo":
                    nome = LerCampoTextoDoBlob(reader, "NO_GRUPO_BLOB", "NOME");
                    break;
                case "SubGrupo":
                    nome = LerCampoTextoDoBlob(reader, "NO_SUB_GRUPO_BLOB", "NOME");
                    break;
                case "FormaOrganizacao":
                    nome = LerCampoTextoDoBlob(reader, "NO_FORMA_ORGANIZACAO_BLOB", "NOME");
                    break;
                case "Procedimento":
                    nome = LerCampoTextoDoBlob(reader, "NO_PROCEDIMENTO_BLOB", "NOME");
                    break;
                case "TipoLeito":
                    nome = LerCampoTextoDoBlob(reader, "NO_TIPO_LEITO_BLOB", "NOME");
                    break;
                case "InstrumentoRegistro":
                    nome = LerCampoTextoDoBlob(reader, "NO_REGISTRO_BLOB", "NOME");
                    break;
            }
        }
        catch (Exception ex)
        {
            // Log do erro para debug
            System.Diagnostics.Debug.WriteLine($"Erro ao ler BLOB para tipo {tipo}: {ex.Message}");
        }

        // Se não conseguiu ler do BLOB, tenta campo direto
        if (string.IsNullOrEmpty(nome))
        {
            try
            {
                nome = FirebirdReaderHelper.GetStringSafe(reader, "NOME");
            }
            catch
            {
                // Se também falhar, tenta ler diretamente como string
                try
                {
                    var nomeOrdinal = reader.GetOrdinal("NOME");
                    if (!reader.IsDBNull(nomeOrdinal))
                    {
                        nome = reader.GetString(nomeOrdinal);
                    }
                }
                catch { }
            }
        }

        // Garantir que pelo menos o código está preenchido
        if (string.IsNullOrEmpty(codigo) && string.IsNullOrEmpty(nome))
        {
            // Se ambos estão vazios, algo está errado
            System.Diagnostics.Debug.WriteLine($"Aviso: ItemRelatorio com código e nome vazios para tipo {tipo}");
        }

        return new ItemRelatorio
        {
            Tipo = tipo,
            Codigo = codigo,
            Nome = nome ?? string.Empty
        };
    }

    /// <summary>
    /// Lê um campo de texto do BLOB primeiro, depois do campo direto se necessário
    /// Garante conversão correta de encoding para acentuação (mesmo método usado na tela inicial)
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
                        
                        // CORREÇÃO: Os dados são salvos como UTF-8 pelo driver, não Windows-1252
                        // Converte de UTF-8 para string .NET (Unicode)
                        resultado = Encoding.UTF8.GetString(validBytes);
                        
                        // Se a conversão resultou em caracteres corrompidos, tenta o helper como fallback
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
    public async Task<IEnumerable<ItemRelatorio>> BuscarTiposLeitoDisponiveisAsync(
        string competencia, 
        string? filtro, 
        CancellationToken cancellationToken = default)
    {
        var whereClause = "tl.DT_COMPETENCIA = @competencia";
        
        if (!string.IsNullOrWhiteSpace(filtro))
        {
            whereClause += " AND (tl.CO_TIPO_LEITO CONTAINING @filtro OR UPPER(CAST(tl.NO_TIPO_LEITO AS VARCHAR(100))) CONTAINING @filtro)";
        }

        var sql = $@"
            SELECT DISTINCT
                tl.CO_TIPO_LEITO AS CODIGO,
                CAST(tl.NO_TIPO_LEITO AS BLOB) AS NO_TIPO_LEITO_BLOB,
                tl.NO_TIPO_LEITO AS NOME
            FROM TB_TIPO_LEITO tl
            WHERE {whereClause}
            ORDER BY tl.CO_TIPO_LEITO";

        await _context.OpenAsync(cancellationToken);

        var tiposLeito = new List<ItemRelatorio>();

        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@competencia", competencia);
        
        if (!string.IsNullOrWhiteSpace(filtro))
        {
            command.Parameters.AddWithValue("@filtro", filtro.ToUpper());
        }

        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                // Reutiliza a lógica de mapeamento, tratando como "TipoLeito" (que usará a lógica genérica ou específica se adicionarmos)
                // Como MapItemRelatorio usa switch no tipo, precisamos garantir que ele saiba lidar ou usar um fallback
                // Vamos adicionar um case no MapItemRelatorio ou usar um genérico.
                // Olhando o MapItemRelatorio existente, ele tem cases específicos. Vamos adicionar um novo case lá também.
                tiposLeito.Add(MapItemRelatorio(reader, "TipoLeito"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar tipos de leito disponíveis para competência {Competencia}", competencia);
            throw;
        }

        return tiposLeito;
    }

    public async Task<IEnumerable<ItemRelatorioProcedimento>> BuscarProcedimentosPorTipoLeitoAsync(
        string coTipoLeito, 
        string competencia, 
        bool naoImprimirSPZerado, 
        string ordenarPor, 
        CancellationToken cancellationToken = default)
    {
        var whereClause = "pl.CO_TIPO_LEITO = @coTipoLeito AND pr.DT_COMPETENCIA = @competencia AND pl.DT_COMPETENCIA = @competencia";
        
        if (naoImprimirSPZerado)
        {
            whereClause += " AND pr.VL_SP > 0";
        }

        var orderByClause = ordenarPor switch
        {
            "Nome" => "pr.NO_PROCEDIMENTO",
            "ValorSP" => "pr.VL_SP DESC",
            _ => "pr.CO_PROCEDIMENTO"
        };

        var sql = $@"
            SELECT 
                pr.CO_PROCEDIMENTO,
                CAST(pr.NO_PROCEDIMENTO AS BLOB) AS NO_PROCEDIMENTO_BLOB,
                pr.NO_PROCEDIMENTO,
                pr.VL_SP
            FROM TB_PROCEDIMENTO pr
            INNER JOIN RL_PROCEDIMENTO_LEITO pl ON pr.CO_PROCEDIMENTO = pl.CO_PROCEDIMENTO
            WHERE {whereClause}
            ORDER BY {orderByClause}";

        await _context.OpenAsync(cancellationToken);

        var procedimentos = new List<ItemRelatorioProcedimento>();

        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@coTipoLeito", coTipoLeito);
        command.Parameters.AddWithValue("@competencia", competencia);

        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                procedimentos.Add(MapItemRelatorioProcedimento(reader));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar procedimentos por tipo de leito {TipoLeito} e competência {Competencia}", coTipoLeito, competencia);
            throw;
        }

        return procedimentos;
    }

    public async Task<IEnumerable<ItemRelatorio>> BuscarInstrumentosRegistroDisponiveisAsync(
        string competencia, 
        string? filtro, 
        CancellationToken cancellationToken = default)
    {
        var whereClause = "reg.DT_COMPETENCIA = @competencia";
        
        if (!string.IsNullOrWhiteSpace(filtro))
        {
            whereClause += " AND (reg.CO_REGISTRO CONTAINING @filtro OR UPPER(CAST(reg.NO_REGISTRO AS VARCHAR(100))) CONTAINING @filtro)";
        }

        var sql = $@"
            SELECT 
                reg.CO_REGISTRO AS CODIGO,
                reg.NO_REGISTRO AS NOME
            FROM TB_REGISTRO reg
            WHERE {whereClause}
            ORDER BY reg.CO_REGISTRO";

        await _context.OpenAsync(cancellationToken);

        var instrumentos = new List<ItemRelatorio>();

        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@competencia", competencia);
        
        if (!string.IsNullOrWhiteSpace(filtro))
        {
            command.Parameters.AddWithValue("@filtro", filtro.ToUpper());
        }

        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var codigo = FirebirdReaderHelper.GetStringSafe(reader, "CODIGO") ?? string.Empty;
                var nome = FirebirdReaderHelper.GetStringSafe(reader, "NOME");
                
                instrumentos.Add(new ItemRelatorio
                {
                    Tipo = "InstrumentoRegistro",
                    Codigo = codigo,
                    Nome = nome ?? string.Empty
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar instrumentos de registro disponíveis para competência {Competencia}", competencia);
            throw;
        }

        return instrumentos;
    }

    public async Task<IEnumerable<ItemRelatorioProcedimento>> BuscarProcedimentosPorInstrumentoRegistroAsync(
        string coRegistro, 
        string competencia, 
        bool naoImprimirSPZerado, 
        string ordenarPor, 
        CancellationToken cancellationToken = default)
    {
        var whereClause = "prreg.CO_REGISTRO = @coRegistro AND pr.DT_COMPETENCIA = @competencia AND prreg.DT_COMPETENCIA = @competencia";
        
        if (naoImprimirSPZerado)
        {
            whereClause += " AND pr.VL_SP > 0";
        }

        var orderByClause = ordenarPor switch
        {
            "Nome" => "pr.NO_PROCEDIMENTO",
            "ValorSP" => "pr.VL_SP DESC",
            _ => "pr.CO_PROCEDIMENTO"
        };

        var sql = $@"
            SELECT 
                pr.CO_PROCEDIMENTO,
                CAST(pr.NO_PROCEDIMENTO AS BLOB) AS NO_PROCEDIMENTO_BLOB,
                pr.NO_PROCEDIMENTO,
                pr.VL_SP
            FROM TB_PROCEDIMENTO pr
            INNER JOIN RL_PROCEDIMENTO_REGISTRO prreg ON pr.CO_PROCEDIMENTO = prreg.CO_PROCEDIMENTO
            WHERE {whereClause}
            ORDER BY {orderByClause}";

        await _context.OpenAsync(cancellationToken);

        var procedimentos = new List<ItemRelatorioProcedimento>();

        using var command = new FbCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@coRegistro", coRegistro);
        command.Parameters.AddWithValue("@competencia", competencia);

        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                procedimentos.Add(MapItemRelatorioProcedimento(reader));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar procedimentos por instrumento de registro {Registro} e competência {Competencia}", coRegistro, competencia);
            throw;
        }

        return procedimentos;
    }
}

