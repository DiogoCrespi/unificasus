using FirebirdSql.Data.FirebirdClient;
using System.Linq;
using System.Text;

namespace UnificaSUS.Infrastructure.Helpers;

/// <summary>
/// Helper para leitura segura de strings do Firebird com tratamento correto de encoding
/// </summary>
public static class FirebirdReaderHelper
{
    private static readonly Encoding Windows1252 = Encoding.GetEncoding(1252);
    private static readonly Encoding Latin1 = Encoding.GetEncoding("ISO-8859-1");

    /// <summary>
    /// Lê uma string do reader com tratamento correto de encoding para acentuação
    /// Com Charset=NONE, precisamos ler os bytes brutos e converter manualmente
    /// Prioriza sempre leitura de bytes para garantir acesso aos dados brutos
    /// </summary>
    public static string? GetStringSafe(FbDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal))
            return null;

        try
        {
            // ESTRATÉGIA 1: Tenta ler diretamente como byte[] (mais confiável com Charset=NONE)
            var rawValue = reader.GetValue(ordinal);
            
            if (rawValue is byte[] bytes)
            {
                return ConvertBytesToString(bytes);
            }

            // ESTRATÉGIA 2: Tenta usar GetBytes() para ler os bytes brutos do campo
            // Isso funciona mesmo quando o driver retorna como string
            // IMPORTANTE: Sempre tenta ler bytes primeiro para garantir acesso aos dados brutos
            try
            {
                // Obtém o tamanho do campo
                long dataLength = reader.GetBytes(ordinal, 0, null, 0, 0);
                
                if (dataLength > 0)
                {
                    // Lê todos os bytes do campo
                    byte[] fieldBytes = new byte[dataLength];
                    long bytesRead = reader.GetBytes(ordinal, 0, fieldBytes, 0, (int)dataLength);
                    
                    if (bytesRead > 0)
                    {
                        // Remove bytes nulos no final se houver
                        int validLength = (int)bytesRead;
                        while (validLength > 0 && fieldBytes[validLength - 1] == 0)
                        {
                            validLength--;
                        }
                        
                        if (validLength > 0)
                        {
                            byte[] validBytes = new byte[validLength];
                            Array.Copy(fieldBytes, 0, validBytes, 0, validLength);
                            string result = ConvertBytesToString(validBytes);
                            
                            // Se a conversão resultou em texto válido (sem caracteres de substituição), retorna
                            if (!string.IsNullOrEmpty(result) && !result.Contains('\uFFFD') && !result.Contains('?'))
                            {
                                return result;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Se GetBytes falhar, continua para próxima estratégia
            }

            // ESTRATÉGIA 3: Se não conseguiu ler como bytes, tenta como string
            // e reconverte usando diferentes abordagens
            try
            {
                var value = reader.GetString(ordinal);
                
                // Se não há caracteres especiais (ASCII puro), retorna como está
                if (string.IsNullOrEmpty(value) || !value.Any(c => c > 127))
                {
                    return value;
                }
                
                // Se contém caracteres de substituição, tenta recuperar os bytes originais
                if (value.Contains('\uFFFD') || value.Contains('?'))
                {
                    // Tenta usar GetChars() para obter os bytes via encoding
                    try
                    {
                        // Obtém o tamanho do campo em caracteres
                        long charLength = reader.GetChars(ordinal, 0, null, 0, 0);
                        if (charLength > 0)
                        {
                            char[] chars = new char[charLength];
                            long charsRead = reader.GetChars(ordinal, 0, chars, 0, (int)charLength);
                            
                            if (charsRead > 0)
                            {
                                // Converte chars para bytes usando Latin1 (preserva bytes 1:1)
                                byte[] charBytes = new byte[charsRead];
                                for (int i = 0; i < charsRead; i++)
                                {
                                    // Latin1 mapeia 1:1, então podemos converter diretamente
                                    if (chars[i] <= 255)
                                    {
                                        charBytes[i] = (byte)chars[i];
                                    }
                                    else
                                    {
                                        // Se o char é > 255, pode ser um caractere Unicode corrompido
                                        // Tenta converter usando Latin1
                                        byte[] tempBytes = Latin1.GetBytes(new char[] { chars[i] });
                                        if (tempBytes.Length > 0)
                                            charBytes[i] = tempBytes[0];
                                    }
                                }
                                
                                // Converte os bytes para Windows-1252
                                string corrected = Windows1252.GetString(charBytes);
                                if (!corrected.Contains('?') && !corrected.Contains('\uFFFD'))
                                {
                                    return corrected;
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Se GetChars falhar, continua para próxima abordagem
                    }
                }
                
                // Abordagem 3.2: Reconversão via Latin1 (preserva bytes 1:1)
                // Latin1 mapeia cada byte para um char, então podemos recuperar os bytes originais
                byte[] originalBytes = Latin1.GetBytes(value);
                
                // Agora converte os bytes para Windows-1252
                string corrected2 = Windows1252.GetString(originalBytes);
                
                // Verifica se a conversão resultou em caracteres válidos (sem ? ou)
                if (!corrected2.Contains('?') && !corrected2.Contains('\uFFFD'))
                {
                    return corrected2;
                }
                
                // Abordagem 3.3: Se ainda tem problemas, tenta outras codificações
                string? alternativa = TryAlternativeEncodings(originalBytes);
                if (alternativa != null && !alternativa.Contains('?') && !alternativa.Contains('\uFFFD'))
                {
                    return alternativa;
                }
                
                // Abordagem 3.4: Tenta usar o encoding padrão do sistema como intermediário
                try
                {
                    var defaultEncoding = Encoding.Default;
                    byte[] defaultBytes = defaultEncoding.GetBytes(value);
                    string corrected3 = Windows1252.GetString(defaultBytes);
                    
                    if (!corrected3.Contains('?') && !corrected3.Contains('\uFFFD'))
                    {
                        return corrected3;
                    }
                }
                catch { }
                
                // Se nada funcionou, retorna o valor original (melhor que nada)
                return corrected2;
            }
            catch
            {
                // Se falhar, tenta ler como string direto
                return reader.GetString(ordinal);
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

    /// <summary>
    /// Converte bytes para string usando Windows-1252, com fallback para outras codificações
    /// </summary>
    private static string ConvertBytesToString(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
            return string.Empty;

        // Remove bytes nulos no final
        int validLength = bytes.Length;
        while (validLength > 0 && bytes[validLength - 1] == 0)
        {
            validLength--;
        }

        if (validLength == 0)
            return string.Empty;

        byte[] validBytes = new byte[validLength];
        Array.Copy(bytes, 0, validBytes, 0, validLength);

        // Tenta Windows-1252 primeiro (padrão para bancos brasileiros)
        try
        {
            string result = Windows1252.GetString(validBytes);
            // Verifica se não tem caracteres de substituição e se tem caracteres válidos
            if (!result.Contains('?') && !result.Contains('\uFFFD'))
            {
                // Verifica se tem pelo menos alguns caracteres válidos (não só espaços)
                if (result.Trim().Length > 0)
                {
                    return result;
                }
            }
        }
        catch { }

        // Se Windows-1252 não funcionou, tenta outras codificações
        string? alternativa = TryAlternativeEncodings(validBytes);
        if (alternativa != null && !alternativa.Contains('?') && !alternativa.Contains('\uFFFD'))
        {
            return alternativa;
        }

        // Último recurso: retorna Windows-1252 mesmo com problemas
        return Windows1252.GetString(validBytes);
    }

    /// <summary>
    /// Tenta converter bytes usando diferentes codificações
    /// </summary>
    private static string? TryAlternativeEncodings(byte[] bytes)
    {
        // Lista de codificações para tentar (em ordem de probabilidade)
        var encodings = new[]
        {
            Encoding.GetEncoding(1252), // Windows-1252
            Encoding.GetEncoding("ISO-8859-1"), // Latin1
            Encoding.GetEncoding(850), // DOS Latin-1
            Encoding.GetEncoding(437), // DOS US
            Encoding.GetEncoding(1250), // Windows-1250 (Central Europe)
            Encoding.GetEncoding(1251), // Windows-1251 (Cyrillic)
            Encoding.Default // Encoding padrão do sistema
        };

        foreach (var encoding in encodings)
        {
            try
            {
                string result = encoding.GetString(bytes);
                // Verifica se não contém caracteres de substituição e tem caracteres válidos
                bool semSubstituicao = !result.Contains('?') && !result.Contains('\uFFFD');
                bool temCaracteresValidos = result.Any(c => !char.IsControl(c) || c == '\r' || c == '\n' || c == '\t');
                bool temAcentos = result.Any(c => "áàâãéêíóôõúçÁÀÂÃÉÊÍÓÔÕÚÇ".Contains(c));
                
                // Se não tem substituição e tem caracteres válidos, provavelmente é a codificação correta
                if (semSubstituicao && temCaracteresValidos)
                {
                    // Prioriza resultados que têm acentos (mais provável de estar correto)
                    if (temAcentos)
                    {
                        return result;
                    }
                }
            }
            catch { }
        }

        // Se nenhuma codificação funcionou perfeitamente, tenta novamente sem verificar acentos
        foreach (var encoding in encodings)
        {
            try
            {
                string result = encoding.GetString(bytes);
                if (!result.Contains('?') && !result.Contains('\uFFFD'))
                {
                    return result;
                }
            }
            catch { }
        }

        return null;
    }
}

