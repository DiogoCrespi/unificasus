using FirebirdSql.Data.FirebirdClient;
using System.Linq;
using System.Text;

namespace UnificaSUS.Infrastructure.Helpers;

/// <summary>
/// Helper para leitura segura de strings do Firebird com tratamento correto de encoding
/// </summary>
public static class FirebirdReaderHelper
{
    /// <summary>
    /// Lê uma string do reader com tratamento correto de encoding para acentuação
    /// Como usamos Charset=NONE, o Firebird retorna os dados como bytes brutos
    /// Precisamos garantir que sempre convertemos usando Windows-1252
    /// </summary>
    public static string? GetStringSafe(FbDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal))
            return null;

        try
        {
            // Primeiro, tenta ler diretamente como bytes (caso o driver retorne assim)
            var obj = reader[ordinal];
            
            if (obj is byte[] bytes)
            {
                // Converte os bytes usando Windows-1252 (padrão para bancos brasileiros)
                var encoding = Encoding.GetEncoding(1252); // Windows-1252
                return encoding.GetString(bytes);
            }
            
            // Se não for byte[], lê como string
            // Com Charset=NONE, o Firebird retorna strings interpretadas de alguma forma
            // Precisamos acessar os bytes brutos de outra forma
            var value = reader.GetString(ordinal);
            
            // Tenta usar GetValue() e ver se retorna bytes
            var rawValue = reader.GetValue(ordinal);
            if (rawValue is byte[] rawBytes)
            {
                var encoding = Encoding.GetEncoding(1252); // Windows-1252
                return encoding.GetString(rawBytes);
            }
            
            // Se chegou aqui, o valor já veio como string
            // Com Charset=NONE, o Firebird retorna dados brutos do banco
            // O .NET pode estar interpretando como UTF-8, mas os dados estão em Windows-1252 (pt-BR)
            // Precisamos reconstruir os bytes originais e reconverter corretamente
            try
            {
                // Se não há caracteres especiais, retorna como está
                if (!value.Any(c => c > 127))
                {
                    return value;
                }
                
                // Tem caracteres especiais - precisa converter
                // A string atual pode estar sendo interpretada como UTF-8, mas os bytes originais são Windows-1252
                // Vamos reconstruir os bytes originais usando UTF-8 (como o .NET interpretou)
                // e depois reinterpretar como Windows-1252
                var utf8 = Encoding.UTF8;
                var win1252 = Encoding.GetEncoding(1252);
                
                // Primeiro: converte a string atual (interpretada como UTF-8) para bytes
                byte[] bytesFromUtf8 = utf8.GetBytes(value);
                
                // Agora: interpreta esses bytes como se fossem Windows-1252
                // Isso corrige a acentuação
                string corrected = win1252.GetString(bytesFromUtf8);
                
                // Se a conversão resultou em caracteres de substituição, tenta outra abordagem
                if (corrected.Contains('?'))
                {
                    // Tenta como se a string já estivesse em Windows-1252 mas mal interpretada
                    // Converte para bytes usando o encoding padrão do sistema
                    var defaultEncoding = Encoding.Default;
                    byte[] defaultBytes = defaultEncoding.GetBytes(value);
                    return win1252.GetString(defaultBytes);
                }
                
                return corrected;
            }
            catch
            {
                // Se falhar, tenta abordagem alternativa: assume que já está em Windows-1252
                try
                {
                    var defaultEncoding = Encoding.Default;
                    var win1252 = Encoding.GetEncoding(1252);
                    byte[] defaultBytes = defaultEncoding.GetBytes(value);
                    return win1252.GetString(defaultBytes);
                }
                catch
                {
                    // Última tentativa: retorna como está
                    return value;
                }
            }
        }
        catch
        {
            // Fallback final: retorna a string como está
            try
            {
                return reader.GetString(ordinal);
            }
            catch
            {
                return null;
            }
        }
    }
}

