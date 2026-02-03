# 📄 Document Parsers - ExamAI

## Visão Geral

Os **Document Parsers** são responsáveis por extrair texto bruto de documentos em diversos formatos (PDF, Word, Excel). O texto extraído é então enviado para o **ExtractionAgent** (Ollama) para processamento com IA.

---

## 🏗️ Arquitetura

```
Arquivo (Stream)
     ↓
IDocumentParser
     ↓
Parser específico (PdfParser, WordParser, ExcelParser)
     ↓
Texto bruto
     ↓
ExtractionAgent (Ollama)
     ↓
JSON estruturado
```

---

## 📦 Parsers Implementados

### ✅ **PdfParser** (US-005)

**Status:** Implementado  
**Biblioteca:** iText7 (9.5.0)  
**Suporta:** Arquivos `.pdf`

#### **Características:**

- ✅ Extrai texto de PDFs digitais (não escaneados)
- ✅ Suporta PDFs multi-página
- ✅ Identifica páginas no output
- ✅ Tratamento de erros (PDF corrompido, página com erro)
- ✅ Logging detalhado
- ⚠️ **Limitação:** Não extrai texto de PDFs escaneados (apenas imagens)

#### **Uso:**

```csharp
public class ExampleController : ControllerBase
{
    private readonly IDocumentParser _parser;

    public ExampleController(IDocumentParser parser)
    {
        _parser = parser;
    }

    [HttpPost("test-pdf")]
    public async Task<IActionResult> TestPdf(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        var text = await _parser.ExtractTextAsync(stream, ".pdf");
        return Ok(new { extractedText = text });
    }
}
```

#### **Output Exemplo:**

```
--- Página 1 ---
LABORATÓRIO CENTRAL
Paciente: João Silva
CPF: 123.456.789-00

Exame: Lipidograma
Data de Coleta: 28/01/2026

Colesterol Total: 210 mg/dL (Referência: até 200)
HDL: 45 mg/dL (Referência: > 40)
LDL: 140 mg/dL (Referência: < 100)

--- Página 2 ---
...
```

---

### ✅ **WordParser** (US-006)

**Status:** Implementado  
**Biblioteca:** DocumentFormat.OpenXml (3.4.1)  
**Suporta:** Arquivos `.docx`

#### **Características:**

- ✅ Extrai texto de documentos Word (.docx)
- ✅ Suporta múltiplos parágrafos
- ✅ Extrai texto de tabelas
- ✅ Preserva formatação básica (parágrafos separados)
- ✅ Tratamento de erros (documento corrompido, vazio)
- ✅ Logging detalhado
- ⚠️ **Limitação:** Apenas .docx (não suporta .doc antigo)

#### **Uso:**

```csharp
public class ExampleController : ControllerBase
{
    private readonly IEnumerable<IDocumentParser> _parsers;

    public ExampleController(IEnumerable<IDocumentParser> parsers)
    {
        _parsers = parsers;
    }

    [HttpPost("test-word")]
    public async Task<IActionResult> TestWord(IFormFile file)
    {
        var parser = _parsers.FirstOrDefault(p => p.SupportsFileType(".docx"));
        
        if (parser == null)
            return BadRequest("No parser found for .docx");

        using var stream = file.OpenReadStream();
        var text = await parser.ExtractTextAsync(stream, ".docx");
        return Ok(new { extractedText = text });
    }
}
```

#### **Output Exemplo:**

```
LABORATÓRIO CENTRAL
Paciente: João Silva
CPF: 123.456.789-00

Exame: Lipidograma
Data de Coleta: 28/01/2026

Colesterol Total: 210 mg/dL (Referência: até 200)
HDL: 45 mg/dL (Referência: > 40)
LDL: 140 mg/dL (Referência: < 100)

--- TABELA ---
Parâmetro	Valor	Referência
Triglicerídeos	125 mg/dL	< 150
Glicemia	92 mg/dL	70-100
--- FIM TABELA ---
```

---

### ✅ **ExcelParser** (US-007)

**Status:** Implementado  
**Biblioteca:** EPPlus (8.4.1)  
**Suporta:** Arquivos `.xlsx`

#### **Características:**

- ✅ Extrai texto de planilhas Excel (.xlsx)
- ✅ Suporta múltiplas planilhas (worksheets)
- ✅ Formato tabular separado por pipe (`|`)
- ✅ Ignora linhas completamente vazias
- ✅ Tratamento de erros (Excel corrompido, planilhas vazias)
- ✅ Logging detalhado
- ⚠️ **Limitação:** Apenas .xlsx (não suporta .xls antigo)
- ⚠️ **Licença:** EPPlus 8+ usa licença PolyForm Noncommercial (uso comercial requer licença paga)

#### **Uso:**

```csharp
public class ExampleController : ControllerBase
{
    private readonly IEnumerable<IDocumentParser> _parsers;

    public ExampleController(IEnumerable<IDocumentParser> parsers)
    {
        _parsers = parsers;
    }

    [HttpPost("test-excel")]
    public async Task<IActionResult> TestExcel(IFormFile file)
    {
        var parser = _parsers.FirstOrDefault(p => p.SupportsFileType(".xlsx"));
        
        if (parser == null)
            return BadRequest("No parser found for .xlsx");

        using var stream = file.OpenReadStream();
        var text = await parser.ExtractTextAsync(stream, ".xlsx");
        return Ok(new { extractedText = text });
    }
}
```

#### **Output Exemplo:**

```
=== Planilha: Exame de Sangue ===

Parâmetro | Valor | Unidade | Referência
Colesterol Total | 210 | mg/dL | < 200
Glicemia | 95 | mg/dL | 70-100
Hemoglobina | 14.5 | g/dL | 12-16

=== Planilha: Observações ===

Data | Médico | Observação
28/01/2026 | Dra. Maria | Valores ligeiramente elevados
```

---

## 🧪 Testes Manuais

### **Teste 1: Extrair texto de PDF simples**

#### **Preparar arquivo de teste:**

Crie um PDF simples com este conteúdo:

```
LABORATÓRIO TESTE

Paciente: Maria Santos
CPF: 987.654.321-00
Data: 03/02/2026

EXAME: HEMOGRAMA

Hemácias: 4.5 milhões/mm³ (Ref: 4.0-5.5)
Hemoglobina: 13.5 g/dL (Ref: 12-16)
Leucócitos: 7000/mm³ (Ref: 4000-11000)
```

Salve como: `teste-exame.pdf`

#### **Testar via código:**

```csharp
// No Program.cs, adicionar endpoint de teste temporário:

app.MapPost("/test/pdf", async (
    IFormFile file, 
    IDocumentParser parser,
    ILogger<Program> logger) =>
{
    try
    {
        using var stream = file.OpenReadStream();
        var text = await parser.ExtractTextAsync(stream, ".pdf");
        
        return Results.Ok(new
        {
            success = true,
            fileName = file.FileName,
            fileSize = file.Length,
            extractedChars = text.Length,
            extractedText = text
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to parse PDF");
        return Results.BadRequest(new
        {
            success = false,
            error = ex.Message
        });
    }
})
.WithName("TestPdfParsing")
.WithTags("Testing")
.DisableAntiforgery();
```

#### **Testar via curl:**

```bash
curl -X POST http://localhost:5000/test/pdf \
  -F "file=@teste-exame.pdf" \
  -H "Content-Type: multipart/form-data"
```

#### **Resultado esperado:**

```json
{
  "success": true,
  "fileName": "teste-exame.pdf",
  "fileSize": 12345,
  "extractedChars": 250,
  "extractedText": "--- Página 1 ---\nLABORATÓRIO TESTE\n\nPaciente: Maria Santos\n..."
}
```

---

### **Teste 2: PDF corrompido**

#### **Testar comportamento:**

```bash
# Criar arquivo inválido
echo "not a pdf" > corrupted.pdf

curl -X POST http://localhost:5000/test/pdf \
  -F "file=@corrupted.pdf"
```

#### **Resultado esperado:**

```json
{
  "success": false,
  "error": "The PDF file is corrupted or invalid"
}
```

---

### **Teste 3: PDF escaneado (sem texto)**

Se você testar com um PDF que é apenas imagem escaneada, o resultado será:

```json
{
  "success": true,
  "extractedText": "AVISO: Nenhum texto foi extraído do PDF. O documento pode ser uma imagem escaneada."
}
```

**Solução futura:** Implementar OCR (US-XXX - backlog)

---

## 🔍 Interface IDocumentParser

```csharp
public interface IDocumentParser
{
    /// <summary>
    /// Extrai texto de um documento
    /// </summary>
    Task<string> ExtractTextAsync(
        Stream fileStream, 
        string fileType, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se o parser suporta o tipo de arquivo
    /// </summary>
    bool SupportsFileType(string fileType);
}
```

### **Implementação de novos parsers:**

Para adicionar suporte a novos formatos:

1. Criar classe que implementa `IDocumentParser`
2. Implementar `SupportsFileType()` e `ExtractTextAsync()`
3. Registrar no `Program.cs`: `builder.Services.AddScoped<IDocumentParser, NovoParse>();`

---

## 🐛 Troubleshooting

### **Erro: "File stream is empty or null"**

**Causa:** Stream vazio ou arquivo não foi carregado corretamente

**Solução:** Verificar se `IFormFile.Length > 0` antes de chamar o parser

---

### **Erro: "PDF file is corrupted or invalid"**

**Causa:** Arquivo não é um PDF válido ou está corrompido

**Solução:** 
1. Verificar se o arquivo é realmente PDF
2. Tentar abrir o arquivo em um leitor de PDF (Adobe, Chrome)
3. Se corrompido, solicitar novo arquivo ao usuário

---

### **Aviso: "Nenhum texto extraído" mas PDF não é escaneado**

**Causa:** PDF pode ter proteção ou codificação especial

**Soluções:**
1. Verificar se PDF tem senha/proteção
2. Tentar abrir e salvar novamente sem proteção
3. Converter para PDF/A (formato padrão)

---

### **Performance lenta em PDFs grandes**

**Causa:** PDFs com centenas de páginas podem demorar

**Soluções:**
1. Processar assíncrono (background job)
2. Limitar tamanho máximo de arquivo
3. Implementar timeout no parser

---

## 📊 Limitações Conhecidas

### **PdfParser (iText7)**

| Limitação | Impacto | Solução |
|-----------|---------|---------|
| Não extrai de PDFs escaneados | Alto | Implementar OCR (Tesseract/Azure Vision) |
| Performance em PDFs grandes | Médio | Background processing |
| PDFs protegidos com senha | Médio | Solicitar PDF sem proteção |
| Layouts complexos (tabelas) | Baixo | Pós-processamento com IA |

### **WordParser (OpenXml)**

| Limitação | Impacto | Solução |
|-----------|---------|---------|
| Não suporta .doc antigo | Médio | Pedir para salvar como .docx |
| Imagens não são extraídas | Baixo | OCR futuro |
| Formatação complexa perdida | Baixo | Suficiente para extração de dados |
| Cabeçalhos/rodapés não extraídos | Baixo | Adicionar extração se necessário |

### **ExcelParser (EPPlus)**

| Limitação | Impacto | Solução |
|-----------|---------|---------|
| Não suporta .xls antigo | Médio | Pedir para salvar como .xlsx |
| Fórmulas não são calculadas | Baixo | EPPlus já retorna valores calculados |
| Gráficos não são extraídos | Baixo | Não necessário para extração de dados |
| Licença comercial necessária | Alto | Adquirir licença EPPlus para uso empresarial |
| Formatação de células perdida | Baixo | Suficiente para extração de dados |

---

## 🚀 Próximos Passos

### **Implementar (próximas USs):**

1. ~~**US-005:** PdfParser (.pdf)~~ ✅ COMPLETO
2. ~~**US-006:** WordParser (.docx)~~ ✅ COMPLETO
3. ~~**US-007:** ExcelParser (.xlsx)~~ ✅ COMPLETO
4. **US-008:** DocumentParserAgent (orquestrador que escolhe o parser correto) 🔜 PRÓXIMO

### **Backlog futuro:**

- OCR para PDFs escaneados (Tesseract ou Azure Computer Vision)
- Suporte para imagens (.jpg, .png) com OCR
- Extração de tabelas estruturadas
- Suporte para formatos antigos (.doc, .xls)
- Processamento paralelo de múltiplas páginas

---

## 📚 Referências

- **iText7 Documentation:** https://itextpdf.com/products/itext-7
- **iText7 GitHub:** https://github.com/itext/itext7-dotnet
- **PDF Structure:** https://en.wikipedia.org/wiki/PDF

---

**Última atualização:** 03/02/2026 - 23:15  
**Versão:** 1.2  
**Parsers implementados:** 3/3 (PDF ✅, Word ✅, Excel ✅) - **Sprint 2 Completo!** 🎉
