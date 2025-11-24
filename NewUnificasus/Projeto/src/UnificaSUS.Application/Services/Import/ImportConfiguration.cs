using UnificaSUS.Core.Import;

namespace UnificaSUS.Application.Services.Import;

/// <summary>
/// Configuração para importação de dados SIGTAP
/// </summary>
public class ImportConfiguration
{
    /// <summary>
    /// Encoding padrão para leitura de arquivos
    /// </summary>
    public string DefaultEncoding { get; set; } = "ISO-8859-1";

    /// <summary>
    /// Tamanho do lote para inserção em massa
    /// </summary>
    public int BatchSize { get; set; } = 1000;

    /// <summary>
    /// Habilita detecção de mudanças de estrutura
    /// </summary>
    public bool EnableStructureDetection { get; set; } = true;

    /// <summary>
    /// Prioridades de importação customizadas
    /// </summary>
    public Dictionary<string, int> ImportPriorities { get; set; } = new();

    /// <summary>
    /// Tabelas obrigatórias
    /// </summary>
    public HashSet<string> RequiredTables { get; set; } = new()
    {
        "TB_GRUPO",
        "TB_SUB_GRUPO",
        "TB_FORMA_ORGANIZACAO",
        "TB_PROCEDIMENTO"
    };

    /// <summary>
    /// Tabelas opcionais (não causam erro se ausentes)
    /// </summary>
    public HashSet<string> OptionalTables { get; set; } = new()
    {
        "TB_CID",
        "RL_PROCEDIMENTO_CID",
        "TB_OCUPACAO",
        "RL_PROCEDIMENTO_OCUPACAO"
    };

    /// <summary>
    /// Modo de tratamento de duplicatas
    /// </summary>
    public DuplicateHandlingMode DuplicateMode { get; set; } = DuplicateHandlingMode.Update;

    /// <summary>
    /// Número máximo de erros antes de parar importação
    /// 0 = sem limite (continua sempre)
    /// </summary>
    public int MaxErrorsBeforeStop { get; set; } = 0;

    /// <summary>
    /// Diretório para logs de importação
    /// </summary>
    public string? LogDirectory { get; set; }

    /// <summary>
    /// Carrega configuração de um arquivo JSON
    /// </summary>
    public static ImportConfiguration LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return new ImportConfiguration();
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var config = System.Text.Json.JsonSerializer.Deserialize<ImportConfiguration>(json);
            return config ?? new ImportConfiguration();
        }
        catch
        {
            return new ImportConfiguration();
        }
    }

    /// <summary>
    /// Salva configuração em arquivo JSON
    /// </summary>
    public void SaveToFile(string filePath)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(this, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(filePath, json);
    }
}

/// <summary>
/// Modo de tratamento de duplicatas
/// </summary>
public enum DuplicateHandlingMode
{
    /// <summary>
    /// Ignora registros duplicados (não insere)
    /// </summary>
    Ignore,

    /// <summary>
    /// Atualiza registros duplicados
    /// </summary>
    Update,

    /// <summary>
    /// Gera erro ao encontrar duplicata
    /// </summary>
    Error
}

/// <summary>
/// Modo de importação
/// </summary>
public enum ImportMode
{
    /// <summary>
    /// Importação completa (limpa e reimporta tudo)
    /// </summary>
    Full,

    /// <summary>
    /// Importação incremental (apenas novos registros)
    /// </summary>
    Incremental,

    /// <summary>
    /// Apenas registros novos (ignora existentes)
    /// </summary>
    NewOnly
}
