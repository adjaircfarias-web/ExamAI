# 🏥 AI Agents para Extração de Dados de Exames Clínicos

**Data:** 01/02/2026  
**Autor:** Clawdex 🔍  
**Caso de Uso:** Leitura e extração de dados de exames médicos (PDF, Word, Excel)

---

## 📋 Visão Geral

Este documento descreve como usar **AI Agents do .NET 10** para extrair dados estruturados de documentos médicos em formatos diversos (PDF, Word, Excel, imagens escaneadas).

### **Objetivo**
Transformar documentos em formatos diferentes contendo resultados de exames clínicos (Colesterol, Urina, Fezes, etc.) em **dados estruturados** para análise e armazenamento.

### **Por que AI Agents são ideais para isso?**

1. ✅ **Inteligência de Extração** - Entende contexto, não precisa de regex complexo
2. ✅ **Formatos Variados** - Lida com layouts diferentes (PDF bagunçado, Excel formatado, Word escaneado)
3. ✅ **Normalização Automática** - Transforma "Col. Total: 250mg/dL" em estrutura padronizada
4. ✅ **Resistente a Variações** - "Colesterol", "Cholesterol", "COL" → reconhece como a mesma coisa
5. ✅ **Extração Semântica** - Entende que "glicose em jejum" e "glicemia de jejum" são iguais

---

## 🏗️ Arquitetura do Sistema

```
┌─────────────────────────────┐
│  Entrada: PDF/Word/Excel    │
│  (Exames Clínicos)          │
└──────────┬──────────────────┘
           │
     ┌─────▼─────────┐
     │ Document      │
     │ Parser        │ ← iText7, OpenXml, EPPlus
     └─────┬─────────┘
           │
     ┌─────▼─────────┐
     │ Text/Image    │
     │ Extraction    │
     └─────┬─────────┘
           │
     ┌─────▼─────────┐
     │ AI Agent      │ ← GPT-4 Vision ou GPT-4 + OCR
     │ (Análise)     │
     └─────┬─────────┘
           │
     ┌─────▼─────────────────┐
     │ Structured JSON       │
     │ {                     │
     │   "paciente": {...},  │
     │   "exames": [         │
     │     {                 │
     │       "tipo": "...",  │
     │       "valor": 200,   │
     │       "unidade": "..."│
     │     }                 │
     │   ]                   │
     │ }                     │
     └───────┬───────────────┘
             │
     ┌───────▼────────┐
     │ MongoDB/SQL    │
     │ (Histórico)    │
     └────────────────┘
```

---

## 🔧 Implementação Completa

### 1️⃣ **Interface do Document Parser**

```csharp
public interface IDocumentParser
{
    Task<string> ExtractTextAsync(Stream fileStream, string fileType);
}
```

### 2️⃣ **Implementação do Parser**

```csharp
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using DocumentFormat.OpenXml.Packaging;
using OfficeOpenXml;

public class DocumentParser : IDocumentParser
{
    public async Task<string> ExtractTextAsync(Stream fileStream, string fileType)
    {
        return fileType.ToLower() switch
        {
            ".pdf" => await ExtractFromPdfAsync(fileStream),
            ".docx" => await ExtractFromWordAsync(fileStream),
            ".xlsx" => await ExtractFromExcelAsync(fileStream),
            ".jpg" or ".png" or ".jpeg" => await ExtractFromImageAsync(fileStream),
            _ => throw new NotSupportedException($"Tipo {fileType} não suportado")
        };
    }

    // ========================================
    // PDF - usa iText7 ou PdfPig
    // ========================================
    private async Task<string> ExtractFromPdfAsync(Stream stream)
    {
        using var pdfReader = new PdfReader(stream);
        using var pdfDoc = new PdfDocument(pdfReader);
        var text = new StringBuilder();
        
        for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
        {
            var page = pdfDoc.GetPage(i);
            text.AppendLine(PdfTextExtractor.GetTextFromPage(page));
        }
        
        return text.ToString();
    }

    // ========================================
    // Word - usa DocumentFormat.OpenXml
    // ========================================
    private async Task<string> ExtractFromWordAsync(Stream stream)
    {
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart.Document.Body;
        return body.InnerText;
    }

    // ========================================
    // Excel - usa EPPlus
    // ========================================
    private async Task<string> ExtractFromExcelAsync(Stream stream)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        
        using var package = new ExcelPackage(stream);
        var worksheet = package.Workbook.Worksheets[0];
        var text = new StringBuilder();
        
        for (int row = 1; row <= worksheet.Dimension.Rows; row++)
        {
            for (int col = 1; col <= worksheet.Dimension.Columns; col++)
            {
                text.Append(worksheet.Cells[row, col].Text + "\t");
            }
            text.AppendLine();
        }
        
        return text.ToString();
    }

    // ========================================
    // Imagem - usa Azure AI Vision ou Tesseract
    // ========================================
    private async Task<string> ExtractFromImageAsync(Stream stream)
    {
        // Opção 1: Azure Computer Vision (melhor qualidade)
        var client = new ComputerVisionClient(
            new ApiKeyServiceClientCredentials(apiKey))
        {
            Endpoint = endpoint
        };
        
        var result = await client.RecognizePrintedTextInStreamAsync(
            detectOrientation: true,
            image: stream
        );
        
        // Concatenar texto das linhas reconhecidas
        var text = new StringBuilder();
        foreach (var region in result.Regions)
        {
            foreach (var line in region.Lines)
            {
                foreach (var word in line.Words)
                {
                    text.Append(word.Text + " ");
                }
                text.AppendLine();
            }
        }
        
        return text.ToString();
        
        // Opção 2: Tesseract (local, grátis)
        /*
        using var engine = new TesseractEngine(@"./tessdata", "por");
        using var img = Pix.LoadFromMemory(await stream.ToArrayAsync());
        using var page = engine.Process(img);
        return page.GetText();
        */
    }
}
```

---

### 3️⃣ **Medical Exam Analyzer (AI Agent)**

```csharp
using Microsoft.Extensions.AI;
using System.Text.Json;

public class MedicalExamAnalyzer
{
    private readonly IChatClient _agent;
    private readonly IDocumentParser _parser;
    private readonly ILogger<MedicalExamAnalyzer> _logger;

    public MedicalExamAnalyzer(
        IChatClient agent,
        IDocumentParser parser,
        ILogger<MedicalExamAnalyzer> logger)
    {
        _agent = agent;
        _parser = parser;
        _logger = logger;
    }

    public async Task<ExamResult> AnalyzeExamAsync(
        Stream fileStream,
        string fileType)
    {
        // 1. Extrair texto do documento
        _logger.LogInformation("Extracting text from {FileType}", fileType);
        var documentText = await _parser.ExtractTextAsync(fileStream, fileType);

        // 2. Criar prompt estruturado
        var systemPrompt = @"
            Você é um especialista em análise de exames clínicos.
            Sua tarefa é extrair dados estruturados de laudos médicos.
            
            INSTRUÇÕES:
            - Identifique TODOS os exames presentes no documento
            - Extraia valores numéricos com suas unidades
            - Identifique valores de referência quando disponíveis
            - Classifique o status como: normal, baixo, alto, crítico
            - Retorne SEMPRE em formato JSON válido
            - Se um exame não estiver presente, não invente dados
            - Normalize nomes de exames (ex: 'Col. Total' → 'Colesterol Total')
            - Converta unidades se necessário
        ";

        var userPrompt = $@"
            Analise este resultado de exame e extraia as informações em JSON:

            === INÍCIO DO DOCUMENTO ===
            {documentText}
            === FIM DO DOCUMENTO ===

            Retorne no seguinte formato JSON (sem markdown):
            {{
                ""paciente"": {{
                    ""nome"": ""Nome do Paciente"",
                    ""data_nascimento"": ""yyyy-MM-dd ou null"",
                    ""data_coleta"": ""yyyy-MM-dd ou null"",
                    ""medico_solicitante"": ""Nome ou null""
                }},
                ""exames"": [
                    {{
                        ""tipo"": ""Colesterol Total"",
                        ""valor"": 200,
                        ""unidade"": ""mg/dL"",
                        ""referencia_min"": 0,
                        ""referencia_max"": 200,
                        ""status"": ""normal"",
                        ""observacoes"": ""Texto adicional ou null""
                    }}
                ]
            }}
        ";

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, userPrompt)
        };

        // 3. Chamar Agent com JSON mode
        var options = new ChatOptions
        {
            ResponseFormat = ChatResponseFormat.Json,
            Temperature = 0.1f,  // Baixa temperatura para consistência
            MaxTokens = 4000
        };

        _logger.LogInformation("Calling AI Agent for analysis");
        var response = await _agent.CompleteAsync(messages, options);
        
        _logger.LogInformation("AI Response received: {Length} chars", 
            response.Message.Text.Length);

        // 4. Deserializar JSON
        var result = JsonSerializer.Deserialize<ExamResult>(
            response.Message.Text,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }
        );

        return result ?? throw new InvalidOperationException(
            "Failed to deserialize exam result");
    }

    // ========================================
    // Análise com GPT-4 Vision (PDF escaneado)
    // ========================================
    public async Task<ExamResult> AnalyzeExamWithVisionAsync(
        Stream imageStream,
        string imageType)
    {
        _logger.LogInformation("Analyzing document with Vision API");

        // Converter para base64
        var imageBytes = await ReadAllBytesAsync(imageStream);
        var base64Image = Convert.ToBase64String(imageBytes);
        var mimeType = imageType.ToLower() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".pdf" => "application/pdf",
            _ => "image/png"
        };

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, @"
                Você é um especialista em análise de exames clínicos.
                Extraia TODOS os dados do exame em formato JSON estruturado.
            "),
            new(ChatRole.User, new[]
            {
                new ChatMessageContentPart(
                    ChatMessageContentPartKind.Text,
                    "Extraia os resultados deste exame em formato JSON"
                ),
                new ChatMessageContentPart(
                    ChatMessageContentPartKind.Image,
                    new ImageContent($"data:{mimeType};base64,{base64Image}")
                )
            })
        };

        var options = new ChatOptions
        {
            ResponseFormat = ChatResponseFormat.Json,
            Temperature = 0.1f
        };

        var response = await _agent.CompleteAsync(messages, options);
        return JsonSerializer.Deserialize<ExamResult>(response.Message.Text);
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }
}
```

---

### 4️⃣ **DTOs (Data Transfer Objects)**

```csharp
public record ExamResult(
    PatientInfo Paciente,
    List<ExamItem> Exames
);

public record PatientInfo(
    string? Nome,
    DateTime? DataNascimento,
    DateTime? DataColeta,
    string? MedicoSolicitante
);

public record ExamItem(
    string Tipo,
    decimal Valor,
    string Unidade,
    decimal? ReferenciaMin,
    decimal? ReferenciaMax,
    string Status,
    string? Observacoes
);
```

---

### 5️⃣ **Controller (API Endpoint)**

```csharp
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ExamsController : ControllerBase
{
    private readonly MedicalExamAnalyzer _analyzer;
    private readonly ILogger<ExamsController> _logger;

    public ExamsController(
        MedicalExamAnalyzer analyzer,
        ILogger<ExamsController> logger)
    {
        _analyzer = analyzer;
        _logger = logger;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(10_000_000)] // 10MB
    public async Task<IActionResult> UploadExam(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Nenhum arquivo enviado");

        var allowedExtensions = new[] { ".pdf", ".docx", ".xlsx", ".jpg", ".png" };
        var fileExtension = Path.GetExtension(file.FileName).ToLower();

        if (!allowedExtensions.Contains(fileExtension))
            return BadRequest($"Tipo de arquivo não suportado: {fileExtension}");

        try
        {
            using var stream = file.OpenReadStream();
            
            // Usar Vision para PDFs escaneados ou imagens
            var result = fileExtension is ".jpg" or ".png"
                ? await _analyzer.AnalyzeExamWithVisionAsync(stream, fileExtension)
                : await _analyzer.AnalyzeExamAsync(stream, fileExtension);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing exam file");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("batch")]
    public async Task<IActionResult> UploadBatch(List<IFormFile> files)
    {
        var results = new List<object>();

        foreach (var file in files)
        {
            try
            {
                using var stream = file.OpenReadStream();
                var fileExtension = Path.GetExtension(file.FileName).ToLower();
                
                var result = await _analyzer.AnalyzeExamAsync(stream, fileExtension);
                results.Add(new { file = file.FileName, success = true, data = result });
            }
            catch (Exception ex)
            {
                results.Add(new { file = file.FileName, success = false, error = ex.Message });
            }
        }

        return Ok(results);
    }
}
```

---

### 6️⃣ **Configuração (Program.cs)**

```csharp
var builder = WebApplication.CreateBuilder(args);

// AI Agent (GPT-4 Vision)
builder.Services.AddSingleton<IChatClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var apiKey = config["OpenAI:ApiKey"];
    
    return new OpenAIChatClient(apiKey, "gpt-4o")  // GPT-4 Omni (Vision)
        .AsBuilder()
        .UseLogging(sp.GetRequiredService<ILoggerFactory>())
        .Build();
});

// Document Parser e Analyzer
builder.Services.AddScoped<IDocumentParser, DocumentParser>();
builder.Services.AddScoped<MedicalExamAnalyzer>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
```

---

## 📦 Pacotes NuGet Necessários

```bash
# AI Agents
dotnet add package Microsoft.Extensions.AI.OpenAI --prerelease

# PDF
dotnet add package itext7
# ou alternativa:
dotnet add package PdfPig

# Word
dotnet add package DocumentFormat.OpenXml

# Excel
dotnet add package EPPlus

# OCR (opcional - se não usar Vision)
dotnet add package Tesseract

# Azure Computer Vision (opcional)
dotnet add package Azure.AI.Vision.ImageAnalysis

# Logging
dotnet add package Serilog.AspNetCore
```

---

## 📊 Comparativo de Abordagens

| Abordagem | Custo | Precisão | Velocidade | Complexidade | Recomendado |
|-----------|-------|----------|------------|--------------|-------------|
| **Regex + Parsing Manual** | 💰 Grátis | 🎯 60-70% | ⚡ Rápido | 🔧 Alta | ❌ |
| **OCR + GPT-4 Text** | 💰💰 $0.05/doc | 🎯 85-95% | ⚡ Médio | 🔧 Baixa | ✅ |
| **GPT-4 Vision Direto** | 💰💰💰 $0.10-0.20/doc | 🎯 90-98% | ⚡ Médio | 🔧 Muito Baixa | ✅✅ |
| **Azure Form Recognizer + GPT** | 💰💰 $0.08/doc | 🎯 95%+ | ⚡ Rápido | 🔧 Baixa | ✅ |

### **Recomendação:**
- **PDFs digitais bem formatados:** OCR + GPT-4 Text
- **PDFs escaneados/manuscritos:** GPT-4 Vision
- **Alta precisão crítica:** Azure Form Recognizer + GPT-4
- **Baixo orçamento:** Tesseract + GPT-3.5

---

## 💡 Melhorias Avançadas

### 1️⃣ **Validação Inteligente com Function Calling**

```csharp
[Description("Valida se um resultado de exame está dentro dos valores de referência")]
public ValidationResult ValidateExamResult(
    string examType,
    decimal value,
    decimal? min,
    decimal? max)
{
    if (min.HasValue && value < min)
        return new ValidationResult("baixo", $"Valor {value} abaixo do mínimo {min}");
    
    if (max.HasValue && value > max)
        return new ValidationResult("alto", $"Valor {value} acima do máximo {max}");
    
    return new ValidationResult("normal", "Valor dentro da faixa de referência");
}

public record ValidationResult(string Status, string Message);

// Adicionar ao Agent
var options = new ChatOptions
{
    Tools = [AIFunctionFactory.Create(ValidateExamResult)]
};
```

---

### 2️⃣ **Embeddings para Comparação de Exames**

```csharp
public class ExamComparer
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embedder;

    public async Task<List<SimilarExam>> FindSimilarExamsAsync(
        ExamResult currentExam,
        List<ExamResult> historicalExams)
    {
        var currentText = SerializeExam(currentExam);
        
        var allTexts = new[] { currentText }
            .Concat(historicalExams.Select(SerializeExam))
            .ToArray();

        var embeddings = await _embedder.GenerateAsync(allTexts);

        var similarities = new List<SimilarExam>();
        for (int i = 1; i < embeddings.Count; i++)
        {
            var similarity = CosineSimilarity(
                embeddings[0].Vector,
                embeddings[i].Vector
            );

            similarities.Add(new SimilarExam(
                historicalExams[i - 1],
                similarity
            ));
        }

        return similarities
            .OrderByDescending(x => x.Similarity)
            .Take(5)
            .ToList();
    }

    private string SerializeExam(ExamResult exam)
    {
        return string.Join(", ",
            exam.Exames.Select(e => $"{e.Tipo}: {e.Valor} {e.Unidade}")
        );
    }

    private float CosineSimilarity(ReadOnlyMemory<float> a, ReadOnlyMemory<float> b)
    {
        var dotProduct = 0f;
        var magnitudeA = 0f;
        var magnitudeB = 0f;

        for (int i = 0; i < a.Length; i++)
        {
            dotProduct += a.Span[i] * b.Span[i];
            magnitudeA += a.Span[i] * a.Span[i];
            magnitudeB += b.Span[i] * b.Span[i];
        }

        return dotProduct / (MathF.Sqrt(magnitudeA) * MathF.Sqrt(magnitudeB));
    }
}

public record SimilarExam(ExamResult Exam, float Similarity);
```

---

### 3️⃣ **Geração de Relatório em Linguagem Natural**

```csharp
public async Task<string> GeneratePatientReportAsync(ExamResult result)
{
    var prompt = $@"
        Com base nestes resultados de exame, gere um relatório resumido:
        
        {JsonSerializer.Serialize(result, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        })}
        
        O relatório deve:
        - Destacar valores fora do normal em NEGRITO
        - Sugerir acompanhamento médico se necessário
        - Usar linguagem acessível ao paciente (sem jargão médico)
        - Ser conciso (máximo 200 palavras)
        - Incluir recomendações gerais de saúde
    ";

    var response = await _agent.CompleteAsync(prompt);
    return response.Message.Text;
}
```

**Exemplo de saída:**
```
📊 Relatório de Exame - João Silva (28/01/2026)

Seus resultados mostram:

✅ HDL (colesterol bom): 45 mg/dL - Normal
⚠️ **Colesterol Total: 210 mg/dL - ALTO** (referência: até 200)
⚠️ **LDL (colesterol ruim): 140 mg/dL - ALTO** (referência: até 100)
✅ Triglicerídeos: 125 mg/dL - Normal

RECOMENDAÇÕES:
- Agendar consulta com cardiologista para avaliar risco cardiovascular
- Considerar ajustes na dieta (reduzir gorduras saturadas)
- Aumentar atividade física (30min/dia, 5x/semana)
- Repetir exame em 3 meses após mudanças no estilo de vida

⚠️ Este relatório é apenas informativo. Consulte seu médico.
```

---

## 🧪 Exemplo de Uso (Teste)

```bash
# Upload de exame
curl -X POST http://localhost:5000/api/exams/upload \
  -F "file=@exame_colesterol.pdf"
```

**Response:**
```json
{
  "paciente": {
    "nome": "João Silva",
    "dataNascimento": "1980-05-15",
    "dataColeta": "2026-01-28",
    "medicoSolicitante": "Dra. Maria Santos"
  },
  "exames": [
    {
      "tipo": "Colesterol Total",
      "valor": 210,
      "unidade": "mg/dL",
      "referenciaMin": 0,
      "referenciaMax": 200,
      "status": "alto",
      "observacoes": null
    },
    {
      "tipo": "HDL",
      "valor": 45,
      "unidade": "mg/dL",
      "referenciaMin": 40,
      "referenciaMax": 60,
      "status": "normal",
      "observacoes": null
    },
    {
      "tipo": "LDL",
      "valor": 140,
      "unidade": "mg/dL",
      "referenciaMin": 0,
      "referenciaMax": 100,
      "status": "alto",
      "observacoes": "Risco cardiovascular aumentado"
    },
    {
      "tipo": "Triglicerídeos",
      "valor": 125,
      "unidade": "mg/dL",
      "referenciaMin": 0,
      "referenciaMax": 150,
      "status": "normal",
      "observacoes": null
    },
    {
      "tipo": "Glicemia em Jejum",
      "valor": 92,
      "unidade": "mg/dL",
      "referenciaMin": 70,
      "referenciaMax": 100,
      "status": "normal",
      "observacoes": null
    }
  ]
}
```

---

## ⚡ Performance e Custos

### **Métricas Esperadas**

| Métrica | Valor |
|---------|-------|
| **Precisão** | 90-95% |
| **Tempo de Processamento** | 3-10 segundos |
| **Custo por Documento (GPT-4)** | $0.05-0.15 USD |
| **Custo por Documento (GPT-4 Vision)** | $0.10-0.25 USD |
| **Taxa de Sucesso** | 95%+ |

### **Otimizações**

1. **Cache de resultados** (evitar reprocessar o mesmo documento)
2. **Batch processing** (processar múltiplos documentos em paralelo)
3. **Fallback para modelos menores** (GPT-3.5 para documentos simples)
4. **Retry com exponential backoff** (para erros de API)

---

## 🚀 Próximos Passos

### **MVP (Minimum Viable Product)**
1. ✅ Parser de PDF/Word/Excel
2. ✅ Integração com GPT-4
3. ✅ API REST para upload
4. ✅ Estruturação JSON

### **Fase 2 - Melhorias**
1. 📊 Dashboard web para visualização
2. 💾 Persistência em banco de dados (MongoDB/SQL)
3. 📈 Gráficos de tendência (histórico de exames)
4. 🔔 Alertas automáticos (valores críticos)
5. 📧 Envio de relatórios por email

### **Fase 3 - Avançado**
1. 🤖 ML local para pré-classificação
2. 🔍 Busca semântica em histórico de exames
3. 📱 App mobile (React Native/Flutter)
4. 🏥 Integração com sistemas hospitalares (HL7/FHIR)

---

## ✅ Conclusão

Para o caso de uso de **exames clínicos**, a stack ideal é:

```
📄 PDF/Word/Excel/Imagem
    ↓
🔧 Document Parser (iText7, OpenXml, EPPlus)
    ↓ (ou)
👁️ GPT-4 Vision (para documentos escaneados)
    ↓
🤖 AI Agent (GPT-4 ou GPT-4o)
    ↓
📊 JSON estruturado
    ↓
💾 Banco de dados (MongoDB, PostgreSQL)
    ↓
📈 Dashboard (visualização + histórico)
```

**Vantagens:**
- ✅ Alta precisão (90-95%)
- ✅ Funciona com formatos variados
- ✅ Normalização automática
- ✅ Escalável
- ✅ Custo razoável ($0.05-0.20/doc)

**Limitações:**
- ⚠️ Requer API keys (OpenAI/Azure)
- ⚠️ Custo por documento (não é grátis)
- ⚠️ Latência de rede (3-10s por documento)
- ⚠️ Não é 100% preciso (sempre validar dados críticos)

---

**Data:** 01/02/2026  
**Versão:** 1.0  
**Status:** Pronto para implementação 🚀
