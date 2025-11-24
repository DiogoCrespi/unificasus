using System.Threading;

namespace UnificaSUS.Infrastructure.Helpers;

/// <summary>
/// Fila de requisições ao banco de dados para garantir execução sequencial
/// e evitar erros de concorrência no Firebird
/// </summary>
public class DatabaseRequestQueue
{
    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private static readonly DatabaseRequestQueue _instance = new DatabaseRequestQueue();
    
    /// <summary>
    /// Instância singleton da fila de requisições
    /// </summary>
    public static DatabaseRequestQueue Instance => _instance;
    
    private DatabaseRequestQueue() { }
    
    /// <summary>
    /// Enfileira uma requisição ao banco de dados, garantindo execução sequencial
    /// </summary>
    /// <typeparam name="T">Tipo de retorno da requisição</typeparam>
    /// <param name="request">Função assíncrona que executa a requisição</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resultado da requisição</returns>
    public async Task<T> EnqueueAsync<T>(Func<Task<T>> request, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await request();
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    /// <summary>
    /// Enfileira uma requisição ao banco de dados sem retorno, garantindo execução sequencial
    /// </summary>
    /// <param name="request">Função assíncrona que executa a requisição</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    public async Task EnqueueAsync(Func<Task> request, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            await request();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

