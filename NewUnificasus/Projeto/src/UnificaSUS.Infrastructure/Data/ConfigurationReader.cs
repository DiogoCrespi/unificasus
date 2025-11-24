using System.Text.RegularExpressions;
using UnificaSUS.Core.Interfaces;

namespace UnificaSUS.Infrastructure.Data;

/// <summary>
/// Implementação do leitor de configuração do arquivo unificasus.ini
/// </summary>
public class ConfigurationReader : IConfigurationReader
{
    private const string ConfigFilePath = @"C:\Program Files\claupers\unificasus\unificasus.ini";
    private const string DefaultUser = "SYSDBA";
    private const string DefaultPassword = "masterkey";

    public bool ConfigFileExists()
    {
        return File.Exists(ConfigFilePath);
    }

    public string GetDatabasePath()
    {
        if (!ConfigFileExists())
        {
            throw new FileNotFoundException($"Arquivo de configuração não encontrado: {ConfigFilePath}");
        }

        var lines = File.ReadAllLines(ConfigFilePath);
        var inDbSection = false;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // Verifica se entrou na seção [DB]
            if (trimmedLine.Equals("[DB]", StringComparison.OrdinalIgnoreCase))
            {
                inDbSection = true;
                continue;
            }

            // Se saiu da seção [DB], para de procurar
            if (trimmedLine.StartsWith("[") && inDbSection)
            {
                break;
            }

            // Ignora linhas comentadas (que começam com ;) ou vazias
            if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith(";"))
            {
                continue;
            }

            // Procura pela chave "local="
            if (inDbSection && trimmedLine.StartsWith("local=", StringComparison.OrdinalIgnoreCase))
            {
                var value = trimmedLine.Substring(6).Trim();
                // Remove espaços extras e ignora se estiver vazio
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
        }

        throw new InvalidOperationException("Chave 'local' não encontrada na seção [DB] do arquivo de configuração");
    }

    public string GetConnectionString()
    {
        // Verifica se o arquivo existe antes de tentar ler
        if (!ConfigFileExists())
        {
            throw new FileNotFoundException($"Arquivo de configuração não encontrado: {ConfigFilePath}");
        }
        
        var databasePath = GetDatabasePath();
        
        // Detecta se é conexão local (embedded) ou servidor (remoto/localhost)
        // Se o caminho começa com letra de unidade (C:, D:, etc) ou é caminho absoluto sem "host:",
        // é embedded. Se contém "localhost:" ou IP seguido de ":", é servidor.
        int serverType = 0; // 0 = servidor, 1 = embedded
        
        // Verifica se é embedded
        // Embedded: "C:\Program Files\..." (caminho absoluto direto)
        // Servidor: "localhost:C:\..." ou "192.168.0.3:C:\..." (host:caminho)
        var trimmedPath = databasePath.Trim();
        
        // Se começa com letra de unidade (A: até Z:) e depois \, é embedded
        // Ou se não contém ":" seguido de um caminho (sem host: antes)
        if ((trimmedPath.Length >= 2 && 
             char.IsLetter(trimmedPath[0]) && 
             trimmedPath[1] == ':' && 
             trimmedPath.Length > 2 && 
             (trimmedPath[2] == '\\' || trimmedPath[2] == '/')))
        {
            // É um caminho absoluto direto (C:\...) - usa Embedded
            serverType = 1; // Embedded
        }
        else if (trimmedPath.StartsWith("localhost:", StringComparison.OrdinalIgnoreCase) ||
                 Regex.IsMatch(trimmedPath, @"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}:"))
        {
            // Contém host: ou IP: - usa servidor
            serverType = 0; // Server
        }
        else
        {
            // Por padrão, assume embedded para caminhos locais
            serverType = 1; // Embedded
        }
        
        // Constrói a string de conexão Firebird
        // IMPORTANTE: Este banco NÃO suporta Charset=WIN1252 (gera erro "Invalid character set specified")
        // Por isso usamos Charset=NONE e fazemos conversão manual de encoding
        // O FirebirdReaderHelper fará a conversão manual de encoding na leitura
        // A inserção também precisa garantir que os bytes sejam Windows-1252
        var charset = "NONE"; // Banco não suporta WIN1252, usa NONE e conversão manual
        
        var connectionString = $"Database={databasePath};" +
               $"User={DefaultUser};" +
               $"Password={DefaultPassword};" +
               $"Charset={charset};" +
               $"Dialect=3;" +
               $"Role=;" +
               $"Connection lifetime=0;" +
               $"Connection timeout=15;" +
               $"Pooling=true;" +
               $"Packet Size=8192;" +
               $"ServerType={serverType};";
        
        // Log para debug (pode ser removido em produção)
        System.Diagnostics.Debug.WriteLine($"[ConfigurationReader] Arquivo: {ConfigFilePath}");
        System.Diagnostics.Debug.WriteLine($"[ConfigurationReader] Database Path: {databasePath}");
        System.Diagnostics.Debug.WriteLine($"[ConfigurationReader] Server Type: {(serverType == 0 ? "Server" : "Embedded")}");
        System.Diagnostics.Debug.WriteLine($"[ConfigurationReader] Charset: {charset}");
        
        return connectionString;
    }
}

