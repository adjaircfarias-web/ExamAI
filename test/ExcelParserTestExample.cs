// Exemplo de teste manual do ExcelParser
// Copie este código para um Console App ou teste unitário

using ExamAI.Infrastructure.Parsers;
using Microsoft.Extensions.Logging;

namespace ExamAI.Tests;

public class ExcelParserTestExample
{
    public static async Task Main(string[] args)
    {
        // Configurar logger
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddConsole()
                .SetMinimumLevel(LogLevel.Debug);
        });

        var logger = loggerFactory.CreateLogger<ExcelParser>();
        var parser = new ExcelParser(logger);

        // Teste 1: Arquivo Excel válido
        Console.WriteLine("=== Teste 1: Arquivo Excel Válido ===");
        try
        {
            using var stream1 = File.OpenRead(@"C:\temp\exame-sangue.xlsx");
            var text1 = await parser.ExtractTextAsync(stream1, ".xlsx");
            Console.WriteLine("Texto extraído:");
            Console.WriteLine(text1);
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro: {ex.Message}");
        }

        // Teste 2: Arquivo Excel com múltiplas planilhas
        Console.WriteLine("=== Teste 2: Excel com Múltiplas Planilhas ===");
        try
        {
            using var stream2 = File.OpenRead(@"C:\temp\exames-completos.xlsx");
            var text2 = await parser.ExtractTextAsync(stream2, ".xlsx");
            Console.WriteLine("Texto extraído:");
            Console.WriteLine(text2);
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro: {ex.Message}");
        }

        // Teste 3: Arquivo Excel vazio
        Console.WriteLine("=== Teste 3: Excel Vazio ===");
        try
        {
            using var stream3 = File.OpenRead(@"C:\temp\vazio.xlsx");
            var text3 = await parser.ExtractTextAsync(stream3, ".xlsx");
            Console.WriteLine("Texto extraído:");
            Console.WriteLine(text3);
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro: {ex.Message}");
        }

        // Teste 4: Arquivo corrompido (deve lançar exceção)
        Console.WriteLine("=== Teste 4: Arquivo Corrompido ===");
        try
        {
            using var stream4 = File.OpenRead(@"C:\temp\corrompido.xlsx");
            var text4 = await parser.ExtractTextAsync(stream4, ".xlsx");
            Console.WriteLine("Texto extraído:");
            Console.WriteLine(text4);
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"✅ Exceção esperada capturada: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exceção inesperada: {ex.Message}");
        }

        Console.WriteLine("=== Testes Concluídos ===");
    }
}

/* 
 * Para executar:
 * 
 * 1. Criar 3 arquivos Excel de teste:
 *    - C:\temp\exame-sangue.xlsx (com dados tabulares simulados)
 *    - C:\temp\exames-completos.xlsx (com múltiplas planilhas)
 *    - C:\temp\vazio.xlsx (planilha vazia)
 *    - C:\temp\corrompido.xlsx (arquivo texto renomeado como .xlsx)
 * 
 * 2. Adicionar este arquivo a um projeto console:
 *    dotnet new console -n ExamAI.TestConsole
 *    dotnet add reference ../ExamAI.Infrastructure/ExamAI.Infrastructure.csproj
 *    
 * 3. Executar:
 *    dotnet run
 *    
 * Resultado esperado:
 * - Teste 1 e 2: Texto extraído em formato tabular (col1 | col2 | col3)
 * - Teste 3: Mensagem de planilhas vazias
 * - Teste 4: Exceção InvalidOperationException capturada
 */
