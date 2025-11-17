using System;
using System.Text;
using System.IO;
using FirebirdSql.Data.FirebirdClient;
using UnificaSUS.Infrastructure.Data;
using UnificaSUS.Infrastructure.Helpers;

namespace TesteEncodingManual
{
    /// <summary>
    /// Utilitário para testar encoding manualmente e diagnosticar problemas
    /// </summary>
    public class TesteEncodingManual
    {
        public static void ExecutarTeste()
        {
            Console.WriteLine("=== Teste Manual de Encoding ===\n");

            try
            {
                // Lê a configuração
                var configReader = new UnificaSUS.Infrastructure.Data.ConfigurationReader();
                var connectionString = configReader.GetConnectionString();
                
                Console.WriteLine($"String de conexão: {connectionString}\n");

                using var connection = new FbConnection(connectionString);
                connection.Open();

                // Testa diferentes campos
                TestarCampo(connection, "TB_GRUPO", "NO_GRUPO", "01");
                TestarCampo(connection, "TB_PROCEDIMENTO", "NO_PROCEDIMENTO", "0701010061");

                connection.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERRO: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");
            }

            Console.WriteLine("\nPressione qualquer tecla para sair...");
            Console.ReadKey();
        }

        private static void TestarCampo(FbConnection connection, string tabela, string campo, string codigoFiltro)
        {
            Console.WriteLine($"\n{'='.PadRight(60, '=')}");
            Console.WriteLine($"Testando: {tabela}.{campo} (filtro: {codigoFiltro})");
            Console.WriteLine($"{'='.PadRight(60, '=')}\n");

            try
            {
                // Query com CAST para BLOB
                string sql = $@"
                    SELECT 
                        CAST({campo} AS BLOB) AS {campo}_BLOB,
                        {campo}
                    FROM {tabela}
                    WHERE CO_GRUPO = @codigo OR CO_PROCEDIMENTO = @codigo
                    ROWS 1";

                using var command = new FbCommand(sql, connection);
                command.Parameters.AddWithValue("@codigo", codigoFiltro);

                using var reader = command.ExecuteReader();

                if (reader.Read())
                {
                    // Testa o campo direto
                    Console.WriteLine("--- Campo Direto (reader.GetString) ---");
                    try
                    {
                        var campoOrdinal = reader.GetOrdinal(campo);
                        if (!reader.IsDBNull(campoOrdinal))
                        {
                            var valorDireto = reader.GetString(campoOrdinal);
                            Console.WriteLine($"Valor: {valorDireto}");
                            MostrarBytes("Bytes (via Latin1):", Encoding.GetEncoding("ISO-8859-1").GetBytes(valorDireto));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erro: {ex.Message}");
                    }

                    // Testa o BLOB
                    Console.WriteLine("\n--- Campo BLOB (CAST) ---");
                    try
                    {
                        var blobOrdinal = reader.GetOrdinal($"{campo}_BLOB");
                        if (!reader.IsDBNull(blobOrdinal))
                        {
                            // Tenta ler como byte[]
                            var blobValue = reader.GetValue(blobOrdinal);
                            if (blobValue is byte[] bytes)
                            {
                                Console.WriteLine($"Tamanho: {bytes.Length} bytes");
                                MostrarBytes("Bytes brutos:", bytes);
                                
                                // Testa diferentes codificações
                                TestarCodificacoes(bytes);
                            }
                            else
                            {
                                // Tenta usar GetBytes()
                                long length = reader.GetBytes(blobOrdinal, 0, null, 0, 0);
                                if (length > 0)
                                {
                                    byte[] blobBytes = new byte[length];
                                    reader.GetBytes(blobOrdinal, 0, blobBytes, 0, (int)length);
                                    
                                    // Remove bytes nulos
                                    int validLength = blobBytes.Length;
                                    while (validLength > 0 && blobBytes[validLength - 1] == 0)
                                        validLength--;
                                    
                                    if (validLength > 0)
                                    {
                                        byte[] validBytes = new byte[validLength];
                                        Array.Copy(blobBytes, 0, validBytes, 0, validLength);
                                        
                                        Console.WriteLine($"Tamanho: {validLength} bytes (após remover nulos)");
                                        MostrarBytes("Bytes brutos:", validBytes);
                                        
                                        // Testa diferentes codificações
                                        TestarCodificacoes(validBytes);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erro ao ler BLOB: {ex.Message}");
                    }

                    // Testa usando o helper
                    Console.WriteLine("\n--- Usando FirebirdReaderHelper ---");
                    try
                    {
                        // Precisa reposicionar o reader
                        // Vamos criar um novo reader
                        using var reader2 = command.ExecuteReader();
                        if (reader2.Read())
                        {
                            var valorHelper = FirebirdReaderHelper.GetStringSafe(reader2, campo);
                            Console.WriteLine($"Valor do Helper: {valorHelper}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erro no Helper: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"Nenhum registro encontrado com código {codigoFiltro}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao executar query: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");
            }
        }

        private static void MostrarBytes(string label, byte[] bytes)
        {
            Console.Write($"{label} ");
            for (int i = 0; i < Math.Min(bytes.Length, 50); i++)
            {
                Console.Write($"{bytes[i]:X2} ");
            }
            if (bytes.Length > 50)
                Console.Write("...");
            Console.WriteLine();
        }

        private static void TestarCodificacoes(byte[] bytes)
        {
            Console.WriteLine("\n--- Testando Diferentes Codificações ---");

            var codificacoes = new[]
            {
                ("Windows-1252", Encoding.GetEncoding(1252)),
                ("Latin1 (ISO-8859-1)", Encoding.GetEncoding("ISO-8859-1")),
                ("UTF-8", Encoding.UTF8),
                ("DOS Latin-1 (850)", Encoding.GetEncoding(850)),
                ("DOS US (437)", Encoding.GetEncoding(437)),
                ("Default", Encoding.Default)
            };

            foreach (var (nome, encoding) in codificacoes)
            {
                try
                {
                    string resultado = encoding.GetString(bytes);
                    bool temAcentos = resultado.Any(c => "áàâãéêíóôõúçÁÀÂÃÉÊÍÓÔÕÚÇ".Contains(c));
                    bool temInterrogacao = resultado.Contains('?');
                    
                    Console.Write($"{nome,-25}: {resultado.Substring(0, Math.Min(40, resultado.Length))}");
                    if (resultado.Length > 40) Console.Write("...");
                    
                    if (temAcentos)
                        Console.Write(" ✓ (tem acentos)");
                    if (temInterrogacao)
                        Console.Write(" ⚠ (tem ?)");
                    
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{nome,-25}: ERRO - {ex.Message}");
                }
            }
        }
    }
}

