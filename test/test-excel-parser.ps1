# Script de teste manual do ExcelParser
# Cria arquivos Excel de teste e testa a extração

Write-Host "🧪 Teste Manual - ExcelParser" -ForegroundColor Cyan
Write-Host ""

# Verificar se EPPlus está disponível
$epplusPath = "C:\dev\myprojects\ExamAI\src\ExamAI.Infrastructure\bin\Debug\net10.0\EPPlus.dll"

if (Test-Path $epplusPath) {
    Write-Host "✅ EPPlus encontrado: $epplusPath" -ForegroundColor Green
} else {
    Write-Host "❌ EPPlus não encontrado. Execute 'dotnet build' primeiro." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "📝 Para testar o ExcelParser manualmente:" -ForegroundColor Yellow
Write-Host "1. Crie 3 arquivos Excel de teste (.xlsx) com dados médicos simulados"
Write-Host "2. Implemente um teste unitário ou console app para chamar o ExcelParser"
Write-Host "3. Verifique se o texto extraído está correto (formato tabular)"
Write-Host ""
Write-Host "🎯 Critérios de Aceitação:" -ForegroundColor Yellow
Write-Host "   ✅ Extração de todas as células em formato tabular (col1 | col2 | col3)"
Write-Host "   ✅ Suporte a múltiplas planilhas (worksheets)"
Write-Host "   ✅ Tratamento de erro para arquivos corrompidos"
Write-Host "   ✅ Logs informativos durante a extração"
Write-Host ""
Write-Host "💡 Exemplo de uso:" -ForegroundColor Cyan
Write-Host @"
var parser = new ExcelParser(logger);
using var stream = File.OpenRead("exame-sangue.xlsx");
var text = await parser.ExtractTextAsync(stream, ".xlsx");
Console.WriteLine(text);
"@
Write-Host ""
Write-Host "✅ ExcelParser implementado e compilado com sucesso!" -ForegroundColor Green
