# 🤖 Multi-Agent Framework (MAF) com LLM Local - Recomendação

**Data:** 29/01/2026  
**Autor:** Clawdex 🔍  
**Contexto:** Extração de dados de exames clínicos usando arquitetura MAF com Ollama

---

## 🎯 Recomendação: Arquitetura MAF com LLM Local

### **Stack Sugerida**

```
┌─────────────────────────────────────────────────┐
│          Orquestrador Central (MAF)             │
│  (Gerencia workflow e comunicação entre agents) │
└───────┬─────────────────────────────────────────┘
        │
   ┌────┴────┬────────────┬────────────┬──────────┐
   │         │            │            │          │
   ▼         ▼            ▼            ▼          ▼
┌─────┐  ┌─────┐      ┌─────┐     ┌─────┐    ┌─────┐
│Agent│  │Agent│      │Agent│     │Agent│    │Agent│
│Doc  │  │OCR  │      │Extr.│     │Valid│    │Norm.│
│Parse│  │     │      │     │     │     │    │     │
└─────┘  └─────┘      └─────┘     └─────┘    └─────┘
   │         │            │            │          │
   └─────────┴────────────┴────────────┴──────────┘
                         │
                    ┌────▼────┐
                    │  Ollama │
                    │ (Local) │
                    └─────────┘
```

---

## 🛠️ Implementação Prática

### **1. LLM Local - Ollama**

```bash
# Instalar Ollama
# Windows: baixar de https://ollama.com
# Linux/Mac: curl -fsSL https://ollama.com/install.sh | sh

# Baixar modelo recomendado para extração médica
ollama pull llama3.1:8b           # Rápido, bom custo-benefício
ollama pull qwen2.5:14b           # Melhor em português + JSON
ollama pull mistral:7b-instruct   # Alternativa veloz

# Para melhor qualidade (precisa de GPU boa)
ollama pull llama3.1:70b
```

---

### **2. Definir os Agentes Especializados**

Cada agente tem uma responsabilidade única:

```csharp
// Agent 1: Document Parser
public class DocumentParserAgent
{
    // Recebe: Stream do arquivo
    // Retorna: Texto bruto extraído
    public Task<string> ParseAsync(Stream file, string type);
}

// Agent 2: OCR Agent (para imagens/PDFs escaneados)
public class OcrAgent
{
    // Recebe: Imagem/PDF escaneado
    // Retorna: Texto extraído via Tesseract ou Azure Vision
    public Task<string> ExtractTextFromImageAsync(Stream image);
}

// Agent 3: Extraction Agent (LLM local)
public class ExtractionAgent
{
    private readonly IChatClient _llm; // Ollama

    // Recebe: Texto bruto
    // Retorna: JSON estruturado com exames
    public Task<ExamResult> ExtractStructuredDataAsync(string text);
}

// Agent 4: Validation Agent
public class ValidationAgent
{
    // Recebe: JSON extraído
    // Retorna: JSON validado + warnings
    public Task<ValidationResult> ValidateAsync(ExamResult exam);
}

// Agent 5: Normalization Agent
public class NormalizationAgent
{
    // Recebe: JSON validado
    // Retorna: JSON normalizado (unidades, nomes, etc.)
    public Task<ExamResult> NormalizeAsync(ExamResult exam);
}
```

---

### **3. Orquestrador (MAF Controller)**

```csharp
public class MedicalExamPipeline
{
    private readonly DocumentParserAgent _parser;
    private readonly OcrAgent _ocr;
    private readonly ExtractionAgent _extractor;
    private readonly ValidationAgent _validator;
    private readonly NormalizationAgent _normalizer;
    private readonly ILogger<MedicalExamPipeline> _logger;

    public async Task<ExamResult> ProcessExamAsync(
        Stream fileStream,
        string fileType)
    {
        _logger.LogInformation("Starting exam processing pipeline");

        // Step 1: Parse documento
        var rawText = fileType is ".jpg" or ".png"
            ? await _ocr.ExtractTextFromImageAsync(fileStream)
            : await _parser.ParseAsync(fileStream, fileType);

        _logger.LogInformation("Document parsed: {Length} chars", rawText.Length);

        // Step 2: Extrair dados estruturados (LLM local)
        var extractedData = await _extractor.ExtractStructuredDataAsync(rawText);
        _logger.LogInformation("Extracted {Count} exams", extractedData.Exames.Count);

        // Step 3: Validar
        var validation = await _validator.ValidateAsync(extractedData);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Validation issues: {Issues}", 
                string.Join(", ", validation.Warnings));
        }

        // Step 4: Normalizar
        var normalized = await _normalizer.NormalizeAsync(extractedData);
        _logger.LogInformation("Pipeline completed successfully");

        return normalized;
    }
}
```

---

### **4. Configurar Ollama no .NET**

```csharp
// Program.cs
builder.Services.AddSingleton<IChatClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var ollamaUrl = config["Ollama:Url"] ?? "http://localhost:11434";
    var model = config["Ollama:Model"] ?? "qwen2.5:14b";
    
    return new OllamaChatClient(ollamaUrl, model)
        .AsBuilder()
        .UseLogging(sp.GetRequiredService<ILoggerFactory>())
        .Build();
});

// Agents
builder.Services.AddScoped<DocumentParserAgent>();
builder.Services.AddScoped<OcrAgent>();
builder.Services.AddScoped<ExtractionAgent>();
builder.Services.AddScoped<ValidationAgent>();
builder.Services.AddScoped<NormalizationAgent>();
builder.Services.AddScoped<MedicalExamPipeline>();

// appsettings.json
{
  "Ollama": {
    "Url": "http://localhost:11434",
    "Model": "qwen2.5:14b"
  }
}
```

---

### **5. Extraction Agent com Ollama**

```csharp
public class ExtractionAgent
{
    private readonly IChatClient _llm;
    private readonly ILogger<ExtractionAgent> _logger;

    public ExtractionAgent(IChatClient llm, ILogger<ExtractionAgent> logger)
    {
        _llm = llm;
        _logger = logger;
    }

    public async Task<ExamResult> ExtractStructuredDataAsync(string documentText)
    {
        var systemPrompt = @"
Você é um especialista em análise de exames clínicos brasileiros.
Extraia TODOS os resultados de exames em formato JSON.

REGRAS:
- Normalize nomes: 'Col. Total' → 'Colesterol Total'
- Extraia valores numéricos + unidades
- Identifique valores de referência
- Classifique: normal, baixo, alto, crítico
- SEMPRE retorne JSON válido (sem markdown)
- Se não encontrar dados, retorne arrays vazios

FORMATO DE SAÍDA:
{
  ""paciente"": {
    ""nome"": ""string ou null"",
    ""data_nascimento"": ""yyyy-MM-dd ou null"",
    ""data_coleta"": ""yyyy-MM-dd ou null"",
    ""medico_solicitante"": ""string ou null""
  },
  ""exames"": [
    {
      ""tipo"": ""Colesterol Total"",
      ""valor"": 200,
      ""unidade"": ""mg/dL"",
      ""referencia_min"": 0,
      ""referencia_max"": 200,
      ""status"": ""normal"",
      ""observacoes"": null
    }
  ]
}
";

        var userPrompt = $@"
Analise este laudo médico:

=== INÍCIO ===
{documentText}
=== FIM ===

Retorne JSON puro (sem ```json):
";

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, userPrompt)
        };

        var options = new ChatOptions
        {
            Temperature = 0.1f,  // Baixa temperatura = mais determinístico
            MaxTokens = 4000
        };

        _logger.LogInformation("Calling Ollama for extraction");
        var response = await _llm.CompleteAsync(messages, options);
        
        var jsonText = CleanJsonResponse(response.Message.Text);
        _logger.LogDebug("LLM Response: {Json}", jsonText);

        try
        {
            return JsonSerializer.Deserialize<ExamResult>(jsonText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new InvalidOperationException("Deserialization returned null");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse JSON from LLM: {Json}", jsonText);
            throw new InvalidOperationException("LLM returned invalid JSON", ex);
        }
    }

    private static string CleanJsonResponse(string response)
    {
        // Remove markdown code blocks se presentes
        response = response.Trim();
        if (response.StartsWith("```json"))
            response = response["```json".Length..];
        if (response.StartsWith("```"))
            response = response[3..];
        if (response.EndsWith("```"))
            response = response[..^3];
        
        return response.Trim();
    }
}
```

---

## 🚀 Comparativo: LLM Local vs Cloud

| Aspecto | **Ollama (Local)** | **OpenAI GPT-4** |
|---------|-------------------|------------------|
| **Custo** | 💰 Grátis (energia + hardware) | 💰💰💰 $0.05-0.20/doc |
| **Privacidade** | 🔒 100% local | ⚠️ Dados vão pra cloud |
| **Velocidade** | ⚡ 5-20s (depende da GPU) | ⚡ 3-10s |
| **Precisão** | 🎯 80-90% (Llama3.1 70B) | 🎯 90-98% |
| **Offline** | ✅ Sim | ❌ Não |
| **Setup** | 🔧 Médio (instalar Ollama) | 🔧 Fácil (só API key) |

---

## 💡 Recomendações Finais

### **Para Produção com Privacidade:**
```
✅ Use Ollama + Qwen2.5:14b (bom equilíbrio português/precisão)
✅ Implemente fallback para GPT-4 se LLM local falhar
✅ Use GPU decente (RTX 3060+ ou A4000+)
✅ Cache resultados (mesmo documento = mesma resposta)
```

### **Para Prototipagem Rápida:**
```
✅ Use GPT-4 (menos config, maior precisão)
✅ Migre pra local depois de validar o fluxo
✅ Mantenha a abstração IChatClient (troca fácil)
```

### **Modelos Locais Recomendados:**

1. **qwen2.5:14b** ← Melhor para português + JSON estruturado
2. **llama3.1:8b** ← Mais rápido, boa qualidade
3. **mistral:7b-instruct** ← Alternativa rápida
4. **llama3.1:70b** ← Máxima qualidade (precisa GPU potente)

---

## 📦 Setup Completo

```bash
# 1. Instalar Ollama
winget install Ollama.Ollama

# 2. Baixar modelo
ollama pull qwen2.5:14b

# 3. Testar
ollama run qwen2.5:14b
>>> Olá, você entende português?
```

```bash
# 4. No projeto .NET
dotnet add package Microsoft.Extensions.AI.Ollama --prerelease
dotnet add package itext7
dotnet add package DocumentFormat.OpenXml
dotnet add package EPPlus
```

---

## 🏗️ Arquitetura Completa do Sistema

### **Pipeline de Processamento**

```
┌──────────────────┐
│  Upload de Doc   │
│  (PDF/Word/Excel)│
└────────┬─────────┘
         │
    ┌────▼────────────────┐
    │ DocumentParserAgent │
    │ (iText7/OpenXml)    │
    └────────┬────────────┘
             │
        ┌────▼──────┐
        │ Texto Raw │
        └────┬──────┘
             │
    ┌────────▼─────────────┐
    │  ExtractionAgent     │
    │  (Ollama/Qwen2.5)    │
    └────────┬─────────────┘
             │
    ┌────────▼──────────┐
    │ JSON Estruturado  │
    └────────┬──────────┘
             │
    ┌────────▼──────────┐
    │ ValidationAgent   │
    │ (Regras negócio)  │
    └────────┬──────────┘
             │
    ┌────────▼──────────┐
    │ NormalizationAgent│
    │ (Padronização)    │
    └────────┬──────────┘
             │
    ┌────────▼──────────┐
    │   ExamResult      │
    │   (Finalizado)    │
    └───────────────────┘
```

---

## 📊 Performance Esperada

### **Métricas com Qwen2.5:14b**

| Métrica | Valor Esperado |
|---------|----------------|
| **Precisão** | 85-92% |
| **Tempo/Documento** | 8-15 segundos |
| **Consumo GPU** | 6-8GB VRAM |
| **Custo** | $0 (só energia) |
| **Taxa de Sucesso** | 90%+ |

### **Hardware Recomendado**

- **Mínimo:** RTX 3060 (12GB), 16GB RAM
- **Recomendado:** RTX 4070 (12GB), 32GB RAM
- **Ideal:** RTX 4090 (24GB), 64GB RAM ou A100/H100

---

## 🧪 Exemplo de Uso

### **1. Controller Endpoint**

```csharp
[ApiController]
[Route("api/[controller]")]
public class ExamsController : ControllerBase
{
    private readonly MedicalExamPipeline _pipeline;

    [HttpPost("upload")]
    public async Task<IActionResult> UploadExam(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        var extension = Path.GetExtension(file.FileName);
        
        var result = await _pipeline.ProcessExamAsync(stream, extension);
        
        return Ok(result);
    }
}
```

### **2. Request Example**

```bash
curl -X POST http://localhost:5000/api/exams/upload \
  -F "file=@exame_colesterol.pdf"
```

### **3. Response Example**

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
    }
  ]
}
```

---

## 🔄 Fallback Strategy (Híbrido Local + Cloud)

Para máxima confiabilidade, implemente fallback:

```csharp
public class HybridExtractionAgent
{
    private readonly IChatClient _localLlm;    // Ollama
    private readonly IChatClient _cloudLlm;    // GPT-4
    private readonly ILogger _logger;

    public async Task<ExamResult> ExtractAsync(string text)
    {
        try
        {
            _logger.LogInformation("Trying local LLM first");
            return await ExtractWithLlmAsync(_localLlm, text);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Local LLM failed, falling back to cloud");
            return await ExtractWithLlmAsync(_cloudLlm, text);
        }
    }

    private async Task<ExamResult> ExtractWithLlmAsync(
        IChatClient llm, 
        string text)
    {
        // Lógica de extração compartilhada
        var response = await llm.CompleteAsync(BuildPrompt(text));
        return ParseResponse(response);
    }
}
```

---

## 🎯 Casos de Uso Avançados

### **1. Batch Processing**

```csharp
public async Task<List<ExamResult>> ProcessBatchAsync(
    IEnumerable<Stream> files)
{
    var tasks = files.Select(f => 
        _pipeline.ProcessExamAsync(f, ".pdf")
    );
    
    return (await Task.WhenAll(tasks)).ToList();
}
```

### **2. Análise Comparativa (Temporal)**

```csharp
public class TemporalAnalyzer
{
    public async Task<TrendAnalysis> AnalyzeTrendAsync(
        List<ExamResult> historicalExams)
    {
        // Usar LLM para analisar tendências
        var prompt = $@"
            Analise esta série temporal de exames:
            {JsonSerializer.Serialize(historicalExams)}
            
            Identifique:
            - Tendências (subindo/descendo)
            - Valores anômalos
            - Padrões preocupantes
        ";
        
        var analysis = await _llm.CompleteAsync(prompt);
        return ParseTrendAnalysis(analysis);
    }
}
```

### **3. Geração de Relatórios**

```csharp
public async Task<string> GenerateReportAsync(ExamResult exam)
{
    var prompt = $@"
        Gere um relatório em português simples baseado nestes resultados:
        {JsonSerializer.Serialize(exam)}
        
        O relatório deve:
        - Destacar valores anormais
        - Sugerir acompanhamento médico se necessário
        - Usar linguagem acessível (sem jargão)
        - Máximo 200 palavras
    ";
    
    var response = await _llm.CompleteAsync(prompt);
    return response.Message.Text;
}
```

---

## ⚠️ Limitações e Considerações

### **Limitações do LLM Local**

1. **Precisão:** 5-10% menor que GPT-4 em casos complexos
2. **Hardware:** Requer GPU decente (custo inicial)
3. **Latência:** Pode ser mais lento que APIs cloud otimizadas
4. **Contexto:** Janela de contexto menor (4k-32k vs 128k do GPT-4)

### **Quando NÃO usar LLM Local**

- ❌ Documentos complexos demais (manuscritos ilegíveis)
- ❌ Precisão crítica (decisões médicas automatizadas)
- ❌ Hardware limitado (sem GPU ou <8GB VRAM)
- ❌ Volume muito alto (cloud pode ser mais eficiente)

### **Quando SIM usar LLM Local**

- ✅ Dados sensíveis (LGPD/HIPAA compliance)
- ✅ Infraestrutura própria (data centers)
- ✅ Volume previsível (custo fixo)
- ✅ Baixa latência de rede (edge computing)

---

## 🚀 Roadmap de Implementação

### **Fase 1 - MVP (2 semanas)**
- [ ] Setup Ollama + Qwen2.5:14b
- [ ] Implementar DocumentParserAgent
- [ ] Implementar ExtractionAgent (básico)
- [ ] API REST simples
- [ ] Testes com 10 documentos reais

### **Fase 2 - Validação (1 semana)**
- [ ] Implementar ValidationAgent
- [ ] Implementar NormalizationAgent
- [ ] Testes de precisão (50+ documentos)
- [ ] Benchmarking vs GPT-4

### **Fase 3 - Produção (2 semanas)**
- [ ] Fallback cloud (híbrido)
- [ ] Cache de resultados
- [ ] Observabilidade (logs, métricas)
- [ ] Deploy em ambiente de produção
- [ ] Documentação completa

### **Fase 4 - Melhorias (contínuo)**
- [ ] Fine-tuning do modelo local (se necessário)
- [ ] Dashboard de análise
- [ ] Integração com sistemas externos
- [ ] Machine Learning para pré-classificação

---

## 📚 Recursos Úteis

### **Documentação**
- [Ollama Docs](https://ollama.ai/docs)
- [Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/)
- [Qwen2.5 Model Card](https://huggingface.co/Qwen/Qwen2.5-14B-Instruct)

### **Ferramentas**
- [Ollama Web UI](https://github.com/open-webui/open-webui) - Interface gráfica
- [LM Studio](https://lmstudio.ai/) - Alternativa ao Ollama
- [Tesseract OCR](https://github.com/tesseract-ocr/tesseract) - OCR local gratuito

### **Comunidades**
- [r/LocalLLaMA](https://reddit.com/r/LocalLLaMA)
- [Ollama Discord](https://discord.gg/ollama)
- [.NET AI Community](https://discord.gg/dotnet)

---

## ✅ Conclusão

Para o caso de uso de **extração de exames clínicos**, a arquitetura MAF com **Ollama + Qwen2.5:14b** oferece:

### **Vantagens:**
- ✅ **Privacidade total** (dados não saem do servidor)
- ✅ **Custo zero** por documento processado
- ✅ **Controle total** sobre o modelo e pipeline
- ✅ **Offline-first** (não depende de internet)
- ✅ **Escalável** (adicionar GPUs conforme necessário)

### **Trade-offs:**
- ⚠️ **Precisão 5-10% menor** que GPT-4
- ⚠️ **Setup inicial mais complexo**
- ⚠️ **Hardware dedicado necessário**
- ⚠️ **Manutenção** (updates de modelos)

### **Recomendação Final:**

**Use abordagem híbrida:**
1. **Ollama como primário** (custo zero, privacidade)
2. **GPT-4 como fallback** (casos complexos/falhas)
3. **Monitorar precisão** (comparar resultados periodicamente)
4. **Iterar e melhorar** (ajustar prompts, testar modelos novos)

---

**Última atualização:** 29/01/2026  
**Versão:** 1.0  
**Status:** Pronto para implementação 🚀
