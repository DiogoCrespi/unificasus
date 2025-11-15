using FirebirdSql.Data.FirebirdClient;
using System.Text.RegularExpressions;
using UnificaSUS.Core.Interfaces;

namespace UnificaSUS.Infrastructure.Data;

/// <summary>
/// Contexto de acesso ao banco de dados Firebird
/// </summary>
public class FirebirdContext : IDisposable
{
    private readonly FbConnection _connection;
    private bool _disposed = false;

    public FirebirdContext(IConfigurationReader configurationReader)
    {
        var connectionString = configurationReader.GetConnectionString();
        _connection = new FbConnection(connectionString);
    }

    public FbConnection Connection => _connection;

    public async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        if (_connection.State != System.Data.ConnectionState.Open)
        {
            try
            {
                await _connection.OpenAsync(cancellationToken);
            }
            catch (FirebirdSql.Data.FirebirdClient.FbException ex)
            {
                var errorDetails = ex.Message;
                
                // Extrai o caminho do banco da string de conexão para melhor diagnóstico
                var connectionString = _connection.ConnectionString;
                var databaseMatch = Regex.Match(connectionString, @"Database=([^;]+)");
                var databasePath = databaseMatch.Success ? databaseMatch.Groups[1].Value : "não especificado";
                
                // Informações adicionais para erros comuns
                if (ex.Message.Contains("Invalid character set", StringComparison.OrdinalIgnoreCase))
                {
                    errorDetails += "\n\nO charset especificado pode não ser suportado pelo banco.";
                }
                
                if (ex.Message.Contains("unavailable database", StringComparison.OrdinalIgnoreCase))
                {
                    errorDetails += "\n\nO banco de dados não está acessível no caminho especificado.";
                }
                
                throw new InvalidOperationException(
                    $"Erro ao conectar com o banco de dados Firebird:\n\n" +
                    $"Caminho do banco: {databasePath}\n\n" +
                    $"Erro: {errorDetails}\n\n" +
                    $"Verifique:\n" +
                    $"- Se o caminho do banco está correto: {databasePath}\n" +
                    $"- Se o arquivo do banco existe no caminho especificado\n" +
                    $"- Se o Firebird está rodando (se usar servidor remoto)\n" +
                    $"- Se as credenciais estão corretas (SYSDBA/masterkey)\n" +
                    $"- Se há firewall bloqueando a conexão (se usar servidor remoto)\n" +
                    $"- Se o arquivo unificasus.ini está correto: C:\\Program Files\\claupers\\unificasus\\unificasus.ini", ex);
            }
        }
    }

    public void Open()
    {
        if (_connection.State != System.Data.ConnectionState.Open)
        {
            try
            {
                _connection.Open();
            }
            catch (FirebirdSql.Data.FirebirdClient.FbException ex)
            {
                var errorDetails = ex.Message;
                
                // Extrai o caminho do banco da string de conexão para melhor diagnóstico
                var connectionString = _connection.ConnectionString;
                var databaseMatch = Regex.Match(connectionString, @"Database=([^;]+)");
                var databasePath = databaseMatch.Success ? databaseMatch.Groups[1].Value : "não especificado";
                
                // Informações adicionais para erros comuns
                if (ex.Message.Contains("Invalid character set", StringComparison.OrdinalIgnoreCase))
                {
                    errorDetails += "\n\nO charset especificado pode não ser suportado pelo banco.";
                }
                
                if (ex.Message.Contains("unavailable database", StringComparison.OrdinalIgnoreCase))
                {
                    errorDetails += "\n\nO banco de dados não está acessível no caminho especificado.";
                }
                
                throw new InvalidOperationException(
                    $"Erro ao conectar com o banco de dados Firebird:\n\n" +
                    $"Caminho do banco: {databasePath}\n\n" +
                    $"Erro: {errorDetails}\n\n" +
                    $"Verifique:\n" +
                    $"- Se o caminho do banco está correto: {databasePath}\n" +
                    $"- Se o arquivo do banco existe no caminho especificado\n" +
                    $"- Se o Firebird está rodando (se usar servidor remoto)\n" +
                    $"- Se as credenciais estão corretas (SYSDBA/masterkey)\n" +
                    $"- Se há firewall bloqueando a conexão (se usar servidor remoto)\n" +
                    $"- Se o arquivo unificasus.ini está correto: C:\\Program Files\\claupers\\unificasus\\unificasus.ini", ex);
            }
        }
    }

    public async Task CloseAsync()
    {
        if (_connection.State != System.Data.ConnectionState.Closed)
        {
            await _connection.CloseAsync();
        }
    }

    public void Close()
    {
        if (_connection.State != System.Data.ConnectionState.Closed)
        {
            _connection.Close();
        }
    }

    public FbTransaction BeginTransaction()
    {
        return _connection.BeginTransaction();
    }

    public async Task<FbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return await _connection.BeginTransactionAsync(cancellationToken);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _connection?.Dispose();
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

