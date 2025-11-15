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
            // O .NET pode estar interpretando incorretamente
            // Vamos tentar diferentes abordagens de reconversão
            try
            {
                // Se não há caracteres especiais, retorna como está
                if (!value.Any(c => c > 127))
                {
                    return value;
                }
                
                var win1252 = Encoding.GetEncoding(1252);
                
                // Abordagem 1: Usa Latin1 (ISO-8859-1) para preservar bytes exatamente
                // Latin1 mapeia byte para char 1:1, então podemos obter os bytes originais
                try
                {
                    var latin1 = Encoding.GetEncoding("ISO-8859-1");
                    // Converte a string para bytes usando Latin1 (preserva bytes 1:1)
                    byte[] originalBytes = latin1.GetBytes(value);
                    // Agora converte os bytes para Windows-1252
                    string corrected = win1252.GetString(originalBytes);
                    
                    // Verifica se a conversão resultou em caracteres válidos
                    if (!corrected.Contains('?'))
                    {
                        return corrected;
                    }
                }
                catch { }
                
                // Abordagem 2: Tenta obter os bytes usando o encoding padrão do sistema
                // e depois reconverter com Windows-1252
                try
                {
                    var defaultEncoding = Encoding.Default;
                    byte[] defaultBytes = defaultEncoding.GetBytes(value);
                    string corrected = win1252.GetString(defaultBytes);
                    
                    if (!corrected.Contains('?'))
                    {
                        return corrected;
                    }
                }
                catch { }
                
                // Se nada funcionou, retorna o valor original
                return value;
            }
            catch
            {
                // Se falhar tudo, retorna como está
                return value;
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

