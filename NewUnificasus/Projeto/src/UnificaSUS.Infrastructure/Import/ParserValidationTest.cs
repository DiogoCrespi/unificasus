using System.Text;
using UnificaSUS.Infrastructure.Import;

namespace UnificaSUS.Tests.Import;

/// <summary>
/// Teste simples para validar os parsers implementados
/// Pode ser executado manualmente antes de criar testes automatizados
/// </summary>
public class ParserValidationTest
{
    private class ConsoleLogger : ILogger
    {
        public void LogInformation(string message) => Console.WriteLine($"[INFO] {message}");
        public void LogWarning(string message) => Console.WriteLine($"[WARN] {message}");
        public void LogError(string message) => Console.WriteLine($"[ERROR] {message}");
        public void LogDebug(string message) => Console.WriteLine($"[DEBUG] {message}");
    }

    public static void Main(string[] args)
    {
        Console.WriteLine("=== Teste de Validação dos Parsers ===\n");

        var logger = new ConsoleLogger();
        
        // Caminho para os arquivos SIGTAP
        string sigtapPath = @"C:\Program Files\claupers\unificasus\TabelaUnificada_202510_v2510160954";

        // Teste 1: LayoutParser
        TestLayoutParser(sigtapPath, logger);

        // Teste 2: FixedWidthParser
        TestFixedWidthParser(sigtapPath, logger);

        // Teste 3: EncodingDetector
        TestEncodingDetector(sigtapPath, logger);

        Console.WriteLine("\n=== Testes Concluídos ===");
        Console.WriteLine("Pressione qualquer tecla para sair...");
        Console.ReadKey();
    }

    static void TestLayoutParser(string sigtapPath, ILogger logger)
    {
        Console.WriteLine("\n--- Teste 1: LayoutParser ---");
        
        try
        {
            var layoutParser = new LayoutParser(logger);
            
            // Testa parsing do layout de TB_GRUPO
            string layoutFile = Path.Combine(sigtapPath, "tb_grupo_layout.txt");
            
            if (!File.Exists(layoutFile))
            {
                Console.WriteLine($"Arquivo não encontrado: {layoutFile}");
                return;
            }

            var columns = layoutParser.ParseLayoutFile(layoutFile);
            
            Console.WriteLine($"\nColunas parseadas: {columns.Count}");
            foreach (var col in columns)
            {
                Console.WriteLine($"  - {col.ColumnName}: {col.DataType} (pos {col.StartPosition}-{col.EndPosition}, length {col.Length})");
            }

            // Teste com arquivo de layout malformado (se existir)
            Console.WriteLine("\nTeste de resiliência (ignorar linhas malformadas):");
            var testData = "Coluna,Tamanho,Inicio,Fim,Tipo\nCO_TESTE,10,1,10,VARCHAR2\n,,,\nCO_TESTE2,5,11,15,NUMBER\ninvalid-line-without-commas";
            string testFile = Path.Combine(Path.GetTempPath(), "test_layout.txt");
            File.WriteAllText(testFile, testData);
            
            var testColumns = layoutParser.ParseLayoutFile(testFile);
            Console.WriteLine($"Colunas válidas parseadas de arquivo com erros: {testColumns.Count}");
            
            File.Delete(testFile);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro no teste: {ex.Message}");
        }
    }

    static void TestFixedWidthParser(string sigtapPath, ILogger logger)
    {
        Console.WriteLine("\n--- Teste 2: FixedWidthParser ---");
        
        try
        {
            var layoutParser = new LayoutParser(logger);
            var fixedWidthParser = new FixedWidthParser(logger);
            
            // Cria metadata para TB_GRUPO
            string layoutFile = Path.Combine(sigtapPath, "tb_grupo_layout.txt");
            string dataFile = Path.Combine(sigtapPath, "tb_grupo.txt");
            
            if (!File.Exists(layoutFile) || !File.Exists(dataFile))
            {
                Console.WriteLine($"Arquivos não encontrados");
                return;
            }

            var metadata = layoutParser.CreateTableMetadata("TB_GRUPO", dataFile, layoutFile);
            
            // Parse primeiras 3 linhas
            var encoding = Encoding.GetEncoding("ISO-8859-1");
            var lines = File.ReadLines(dataFile, encoding).Take(3).ToArray();
            
            Console.WriteLine($"\nParseando {lines.Length} linhas:");
            
            for (int i = 0; i < lines.Length; i++)
            {
                Console.WriteLine($"\nLinha {i + 1}:");
                var data = fixedWidthParser.ParseLine(lines[i], metadata);
                
                foreach (var kvp in data)
                {
                    Console.WriteLine($"  {kvp.Key}: {kvp.Value ?? "<null>"}");
                }
            }

            // Teste de resiliência
            Console.WriteLine("\n\nTeste de resiliência (linha curta):");
            string shortLine = "01Teste";
            var shortData = fixedWidthParser.ParseLine(shortLine, metadata);
            Console.WriteLine($"Dados parseados de linha curta: {shortData.Count} campos");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro no teste: {ex.Message}");
        }
    }

    static void TestEncodingDetector(string sigtapPath, ILogger logger)
    {
        Console.WriteLine("\n--- Teste 3: EncodingDetector ---");
        
        try
        {
            var detector = new EncodingDetector(logger);
            
            // Testa detecção em alguns arquivos
            string[] filesToTest = {
                "tb_grupo.txt",
                "tb_procedimento.txt",
                "tb_cid.txt"
            };

            foreach (var file in filesToTest)
            {
                string fullPath = Path.Combine(sigtapPath, file);
                
                if (File.Exists(fullPath))
                {
                    var encoding = detector.DetectEncoding(fullPath);
                    Console.WriteLine($"{file}: {encoding.WebName}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro no teste: {ex.Message}");
        }
    }
}
