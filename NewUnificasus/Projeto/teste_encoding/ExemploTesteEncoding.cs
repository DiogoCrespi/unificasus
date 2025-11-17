using System;
using System.Text;
using System.IO;

namespace TesteEncoding
{
    /// <summary>
    /// Exemplo de como testar diferentes codificações de bytes
    /// Use este código como referência para entender como o encoding funciona
    /// </summary>
    public class ExemploTesteEncoding
    {
        /// <summary>
        /// Testa diferentes codificações com bytes de exemplo
        /// </summary>
        public static void TestarCodificacoes()
        {
            // Exemplo: bytes que representam "CALÇADOS" em Windows-1252
            // C=67, A=65, L=76, Ç=199, A=65, D=68, O=79, S=83
            byte[] bytesExemplo = { 67, 65, 76, 199, 65, 68, 79, 83 };

            Console.WriteLine("=== Teste de Codificações ===\n");

            // Testa Windows-1252 (padrão para bancos brasileiros)
            TestarEncoding("Windows-1252", Encoding.GetEncoding(1252), bytesExemplo);

            // Testa Latin1 (ISO-8859-1)
            TestarEncoding("Latin1 (ISO-8859-1)", Encoding.GetEncoding("ISO-8859-1"), bytesExemplo);

            // Testa UTF-8
            TestarEncoding("UTF-8", Encoding.UTF8, bytesExemplo);

            // Testa encoding padrão do sistema
            TestarEncoding("Encoding Default", Encoding.Default, bytesExemplo);
        }

        private static void TestarEncoding(string nome, Encoding encoding, byte[] bytes)
        {
            try
            {
                string resultado = encoding.GetString(bytes);
                Console.WriteLine($"{nome}: {resultado}");
                
                // Verifica se contém caracteres de substituição (?)
                if (resultado.Contains('?'))
                {
                    Console.WriteLine($"  ⚠️  Contém caracteres de substituição!");
                }
                else
                {
                    Console.WriteLine($"  ✓ Resultado válido");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{nome}: ERRO - {ex.Message}");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Demonstra como converter uma string corrompida de volta para bytes
        /// </summary>
        public static void DemonstrarReconversao()
        {
            Console.WriteLine("=== Demonstração de Reconversão ===\n");

            // Simula uma string que veio corrompida do banco
            // O caractere '' (U+FFFD) é o caractere de substituição
            string corrompida = "CALADOS";

            Console.WriteLine($"String corrompida: {corrompida}");

            // Tenta recuperar os bytes originais usando Latin1
            // Latin1 mapeia byte para char 1:1, então podemos obter os bytes
            var latin1 = Encoding.GetEncoding("ISO-8859-1");
            byte[] bytesRecuperados = latin1.GetBytes(corrompida);

            Console.WriteLine($"Bytes recuperados: {string.Join(", ", bytesRecuperados)}");

            // Tenta converter para Windows-1252
            var win1252 = Encoding.GetEncoding(1252);
            string corrigida = win1252.GetString(bytesRecuperados);

            Console.WriteLine($"String corrigida: {corrigida}");
        }

        /// <summary>
        /// Salva um arquivo de teste com diferentes codificações
        /// </summary>
        public static void CriarArquivoTeste()
        {
            string texto = "CALÇADOS ORTOPÉDICOS CONFECCIONADOS SOB MEDIDA ATÉ NÚMERO 45 (PAR)";

            string pastaTeste = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "TesteEncoding"
            );

            Directory.CreateDirectory(pastaTeste);

            // Salva em diferentes codificações
            var codificacoes = new[]
            {
                ("Windows-1252", Encoding.GetEncoding(1252)),
                ("Latin1", Encoding.GetEncoding("ISO-8859-1")),
                ("UTF-8", Encoding.UTF8)
            };

            foreach (var (nome, encoding) in codificacoes)
            {
                string arquivo = Path.Combine(pastaTeste, $"teste_{nome}.txt");
                File.WriteAllText(arquivo, texto, encoding);
                Console.WriteLine($"Arquivo criado: {arquivo}");
            }

            Console.WriteLine($"\nArquivos de teste criados em: {pastaTeste}");
        }
    }
}

