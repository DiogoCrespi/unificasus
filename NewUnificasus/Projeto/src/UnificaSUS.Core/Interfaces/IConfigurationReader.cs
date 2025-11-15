namespace UnificaSUS.Core.Interfaces;

/// <summary>
/// Interface para leitura de configuração
/// </summary>
public interface IConfigurationReader
{
    /// <summary>
    /// Lê a string de conexão do arquivo de configuração
    /// </summary>
    string GetConnectionString();
    
    /// <summary>
    /// Verifica se o arquivo de configuração existe
    /// </summary>
    bool ConfigFileExists();
    
    /// <summary>
    /// Lê o caminho do banco de dados do arquivo de configuração
    /// </summary>
    string GetDatabasePath();
}

