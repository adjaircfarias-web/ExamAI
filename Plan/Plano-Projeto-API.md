# 📋 Plano de Projeto: API de Extração de Exames Médicos

**Projeto:** MedicalExamExtractor API  
**Data de Criação:** 29/01/2026  
**Versão:** 1.1  
**Autor:** Adjair Farias (com Clawdex 🔍)

---

## 📖 Visão Geral

Sistema API REST para extração automática de dados estruturados de laudos médicos em múltiplos formatos (PDF, Word, Excel), utilizando AI Agents com LLM local (Ollama), armazenamento em PostgreSQL, e endpoints para consulta dos resultados.

### **Objetivos**

1. ✅ Receber uploads de documentos médicos (PDF, DOCX, XLSX)
2. ✅ Extrair dados estruturados usando AI Agents
3. ✅ Armazenar em PostgreSQL normalizado
4. ✅ Fornecer endpoints REST para consulta
5. ✅ Manter privacidade (LLM local, dados não saem do servidor)

### **Escopo**

| Incluído | Não Incluído (v1) |
|----------|-------------------|
| Upload de PDF/Word/Excel | Autenticação/Autorização |
| Extração com Ollama (local) | Dashboard web |
| Armazenamento PostgreSQL | Integração HL7/FHIR |
| Endpoints de consulta | OCR para manuscritos |
| Logs e observabilidade | Machine Learning avançado |

---

## 🏗️ Arquitetura do Sistema

```
┌─────────────────────────────────────────────────────────┐
│                     Cliente (Postman/App)               │
└───────────────────────┬─────────────────────────────────┘
                        │ HTTP/REST
                        ▼
┌─────────────────────────────────────────────────────────┐
│                  API Gateway (.NET 10)                  │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │  Upload      │  │  Query       │  │  Health      │  │
│  │  Controller  │  │  Controller  │  │  Controller  │  │
│  └──────┬───────┘  └───────┬──────┘  └──────────────┘  │
└─────────┼──────────────────┼─────────────────────────────┘
          │                  │
          ▼                  ▼
┌─────────────────────────────────────────────────────────┐
│              Application Layer (Services)               │
│  ┌────────────────────────────────────────────────────┐ │
│  │         MedicalExamPipeline (Orquestrador)         │ │
│  └──┬──────┬──────────┬───────────┬──────────┬────────┘ │
│     │      │          │           │          │          │
│  ┌──▼──┐┌──▼───┐  ┌───▼────┐  ┌──▼────┐  ┌──▼────┐    │
│  │Doc  ││OCR   │  │Extract │  │Validat││Norm   │    │
│  │Parse││Agent │  │Agent   │  │Agent  ││Agent  │    │
│  └─────┘└──────┘  └───┬────┘  └───────┘  └───────┘    │
└─────────────────────────┼─────────────────────────────────┘
                          │
                    ┌─────▼──────┐
                    │   Ollama   │
                    │(Llama 3.1) │
                    └────────────┘
          │
          ▼
┌─────────────────────────────────────────────────────────┐
│              Data Layer (Repository)                    │
│  ┌──────────────────────────────────────────────────┐  │
│  │         ExamRepository (EF Core)                 │  │
│  └───────────────────┬──────────────────────────────┘  │
└────────────────────────┼─────────────────────────────────┘
                         │
                         ▼
                  ┌──────────────┐
                  │  PostgreSQL  │
                  │   Database   │
                  └──────────────┘
```

---

## 🤖 Integração com Ollama 3.1 Local - Guia Completo

### **1. O que é Ollama?**

Ollama é uma ferramenta que permite rodar **Large Language Models (LLMs) localmente** no seu computador, sem necessidade de APIs externas. Ideal para:

- ✅ **Privacidade total** (dados não saem do servidor)
- ✅ **Custo zero** por inferência
- ✅ **Baixa latência** (sem round-trip para cloud)
- ✅ **Offline-first** (funciona sem internet)

---

### **2. Instalação do Ollama**

#### **Windows**

```powershell
# Opção 1: Instalador oficial
# Baixar de: https://ollama.com/download/windows
# Executar o instalador

# Opção 2: Winget
winget install Ollama.Ollama

# Verificar instalação
ollama --version
# Output esperado: ollama version 0.x.x
```

#### **Linux**

```bash
# Instalação via script oficial
curl -fsSL https://ollama.com/install.sh | sh

# Verificar
ollama --version
```

#### **macOS**

```bash
# Baixar de: https://ollama.com/download/mac
# Ou via Homebrew
brew install ollama

# Verificar
ollama --version
```

---

### **3. Download e Setup de Modelos**

#### **Llama 3.1 - Variantes Disponíveis**

```bash
# Llama 3.1 8B (Recomendado para começar)
# Requer: ~5GB VRAM, ~8GB RAM
ollama pull llama3.1:8b

# Llama 3.1 70B (Melhor qualidade)
# Requer: ~40GB VRAM, ~64GB RAM
ollama pull llama3.1:70b

# Llama 3.1 8B Instruct (Otimizado para instruções)
ollama pull llama3.1:8b-instruct

# Verificar modelos instalados
ollama list
```

**Output esperado:**
```
NAME                ID              SIZE    MODIFIED
llama3.1:8b         abc123def456    4.7 GB  2 minutes ago
```

#### **Testar o Modelo**

```bash
# Modo interativo
ollama run llama3.1:8b

>>> Olá, você entende português?
Sim, eu entendo português! Como posso ajudá-lo?

>>> /bye  # Para sair
```

#### **Verificar API REST**

```bash
# Ollama expõe API REST em http://localhost:11434
curl http://localhost:11434/api/tags

# Response:
{
  "models": [
    {
      "name": "llama3.1:8b",
      "size": 4661224448,
      "digest": "abc123...",
      "modified_at": "2026-01-29T12:00:00Z"
    }
  ]
}
```

---

### **4. Integração no .NET 10**

#### **Pacotes NuGet**

```bash
# Adicionar pacote oficial Microsoft.Extensions.AI
dotnet add package Microsoft.Extensions.AI --prerelease
dotnet add package Microsoft.Extensions.AI.Ollama --prerelease

# Verificar versões instaladas
dotnet list package
```

#### **appsettings.json**

```json
{
  "Ollama": {
    "Url": "http://localhost:11434",
    "Model": "llama3.1:8b",
    "Temperature": 0.1,
    "MaxTokens": 4096,
    "TimeoutSeconds": 60
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=medicalexams;Username=postgres;Password=postgres123"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Extensions.AI": "Debug"
    }
  }
}
```

#### **Configuração no Program.cs**

```csharp
using Microsoft.Extensions.AI;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ===================================================
// Configurar Serilog (Logging)
// ===================================================
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/api-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ===================================================
// Configurar Ollama IChatClient
// ===================================================
builder.Services.AddSingleton<IChatClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<Program>>();
    
    var ollamaUrl = config["Ollama:Url"] ?? "http://localhost:11434";
    var model = config["Ollama:Model"] ?? "llama3.1:8b";
    
    logger.LogInformation("Configuring Ollama client: {Url}, Model: {Model}", ollamaUrl, model);

    try
    {
        var client = new OllamaChatClient(new Uri(ollamaUrl), model)
            .AsBuilder()
            .UseLogging(sp.GetRequiredService<ILoggerFactory>())
            .Build();

        logger.LogInformation("Ollama client configured successfully");
        return client;
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to configure Ollama client");
        throw;
    }
});

// ===================================================
// Registrar Agents e Services
// ===================================================
builder.Services.AddScoped<DocumentParserAgent>();
builder.Services.AddScoped<ExtractionAgent>();
builder.Services.AddScoped<ValidationAgent>();
builder.Services.AddScoped<NormalizationAgent>();
builder.Services.AddScoped<MedicalExamPipeline>();

// ===================================================
// Database (PostgreSQL + EF Core)
// ===================================================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IExamRepository, ExamRepository>();

// ===================================================
// Controllers e Swagger
// ===================================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "Medical Exam Extractor API", 
        Version = "v1",
        Description = "API para extração automática de dados de exames médicos"
    });
});

var app = builder.Build();

// ===================================================
// Middleware
// ===================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// ===================================================
// Health Check do Ollama
// ===================================================
app.MapGet("/health/ollama", async (IChatClient chatClient) =>
{
    try
    {
        var response = await chatClient.CompleteAsync("ping", new ChatOptions
        {
            MaxTokens = 10,
            Temperature = 0
        });
        
        return Results.Ok(new 
        { 
            status = "healthy", 
            model = "llama3.1:8b",
            response = response.Message.Text
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Ollama Unhealthy",
            detail: ex.Message,
            statusCode: 503
        );
    }
});

app.Run();
```

---

### **5. Implementação do ExtractionAgent**

```csharp
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace MedicalExamExtractor.Application.Agents;

public class ExtractionAgent
{
    private readonly IChatClient _llm;
    private readonly ILogger<ExtractionAgent> _logger;
    private readonly IConfiguration _config;

    public ExtractionAgent(
        IChatClient llm, 
        ILogger<ExtractionAgent> logger,
        IConfiguration config)
    {
        _llm = llm;
        _logger = logger;
        _config = config;
    }

    public async Task<ExamResult> ExtractStructuredDataAsync(
        string documentText,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting extraction from document ({Length} chars)", 
            documentText.Length);

        var systemPrompt = BuildSystemPrompt();
        var userPrompt = BuildUserPrompt(documentText);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, userPrompt)
        };

        var options = new ChatOptions
        {
            Temperature = _config.GetValue<float>("Ollama:Temperature", 0.1f),
            MaxTokens = _config.GetValue<int>("Ollama:MaxTokens", 4096),
            StopSequences = new[] { "=== FIM ===" }
        };

        try
        {
            _logger.LogDebug("Calling Ollama with {MessageCount} messages", messages.Count);
            
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var response = await _llm.CompleteAsync(messages, options, cancellationToken);
            sw.Stop();
            
            _logger.LogInformation("Ollama responded in {ElapsedMs}ms", sw.ElapsedMilliseconds);
            _logger.LogDebug("Raw response: {Response}", response.Message.Text);

            var jsonText = CleanJsonResponse(response.Message.Text);
            
            var result = JsonSerializer.Deserialize<ExamResult>(jsonText,
                new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });

            if (result == null)
                throw new InvalidOperationException("Deserialization returned null");

            _logger.LogInformation("Successfully extracted {ExamCount} exams", 
                result.Exames?.Count ?? 0);

            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse JSON from LLM response");
            throw new InvalidOperationException("LLM returned invalid JSON", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Extraction failed");
            throw;
        }
    }

    private string BuildSystemPrompt()
    {
        return @"
Você é um especialista em análise de exames clínicos brasileiros.
Sua tarefa é extrair TODOS os resultados de exames médicos de documentos e retornar em formato JSON estruturado.

REGRAS OBRIGATÓRIAS:
1. Normalize nomes de exames: 'Col. Total' → 'Colesterol Total', 'Glicemia Jejum' → 'Glicemia em Jejum'
2. Extraia valores numéricos com suas unidades (mg/dL, g/dL, %, etc.)
3. Identifique valores de referência quando disponíveis (mín/máx)
4. Classifique status baseado em referências: 'normal', 'baixo', 'alto', 'crítico'
5. SEMPRE retorne JSON válido (sem markdown, sem código)
6. Se não encontrar dados, retorne arrays vazios (nunca null)
7. Datas no formato ISO 8601: yyyy-MM-dd
8. Valores numéricos como números (não strings)

TIPOS DE EXAMES COMUNS:
- Lipidograma: Colesterol Total, HDL, LDL, VLDL, Triglicerídeos
- Glicemia: Glicemia em Jejum, Hemoglobina Glicada (HbA1c)
- Hemograma: Hemácias, Leucócitos, Plaquetas, Hemoglobina, Hematócrito
- Função Renal: Ureia, Creatinina, Ácido Úrico
- Função Hepática: TGO (AST), TGP (ALT), Gama GT, Bilirrubinas

FORMATO DE SAÍDA (JSON puro):
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
      ""valor"": 200.5,
      ""unidade"": ""mg/dL"",
      ""referencia_min"": 0,
      ""referencia_max"": 200,
      ""status"": ""normal"",
      ""observacoes"": null
    }
  ]
}
".Trim();
    }

    private string BuildUserPrompt(string documentText)
    {
        return $@"
Analise este laudo médico e extraia os dados estruturados:

=== INÍCIO DO DOCUMENTO ===
{documentText}
=== FIM DO DOCUMENTO ===

IMPORTANTE: Retorne APENAS o JSON (sem ```json, sem explicações).
".Trim();
    }

    private static string CleanJsonResponse(string response)
    {
        // Remove markdown code blocks se presentes
        response = response.Trim();
        
        if (response.StartsWith("```json"))
            response = response["```json".Length..];
        else if (response.StartsWith("```"))
            response = response[3..];
        
        if (response.EndsWith("```"))
            response = response[..^3];
        
        return response.Trim();
    }
}

// ===================================================
// DTOs
// ===================================================

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

### **6. Configuração Avançada do Ollama**

#### **Modelfile Customizado (Opcional)**

Se quiser ajustar parâmetros do modelo:

```dockerfile
# Criar arquivo: Modelfile
FROM llama3.1:8b

# Temperature (0.0 = determinístico, 1.0 = criativo)
PARAMETER temperature 0.1

# Top-p (nucleus sampling)
PARAMETER top_p 0.9

# Top-k
PARAMETER top_k 40

# Repeat penalty (evitar repetições)
PARAMETER repeat_penalty 1.1

# Contexto máximo (tokens)
PARAMETER num_ctx 4096

# System prompt padrão
SYSTEM """
Você é um assistente especializado em análise de documentos médicos.
Sempre retorne respostas em formato JSON estruturado.
"""
```

**Criar modelo customizado:**
```bash
ollama create medical-llama -f Modelfile
ollama run medical-llama
```

**Usar no .NET:**
```csharp
var client = new OllamaChatClient(new Uri(ollamaUrl), "medical-llama");
```

---

### **7. Monitoramento e Troubleshooting**

#### **Logs do Ollama**

```bash
# Windows: logs em
%USERPROFILE%\.ollama\logs\server.log

# Linux/Mac:
~/.ollama/logs/server.log

# Ver logs em tempo real
tail -f ~/.ollama/logs/server.log
```

#### **Métricas de Performance**

```csharp
public class OllamaMetricsMiddleware
{
    private readonly IChatClient _llm;
    private readonly ILogger<OllamaMetricsMiddleware> _logger;

    public async Task<ChatCompletion> CallWithMetricsAsync(
        List<ChatMessage> messages,
        ChatOptions options)
    {
        var sw = Stopwatch.StartNew();
        var inputTokens = EstimateTokens(messages);

        try
        {
            var response = await _llm.CompleteAsync(messages, options);
            sw.Stop();

            var outputTokens = EstimateTokens(response.Message.Text);
            var tokensPerSecond = (inputTokens + outputTokens) / (sw.ElapsedMilliseconds / 1000.0);

            _logger.LogInformation(
                "Ollama metrics: Duration={DurationMs}ms, " +
                "InputTokens={InputTokens}, OutputTokens={OutputTokens}, " +
                "Speed={Speed:F1} tokens/s",
                sw.ElapsedMilliseconds,
                inputTokens,
                outputTokens,
                tokensPerSecond
            );

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Ollama call failed after {DurationMs}ms", 
                sw.ElapsedMilliseconds);
            throw;
        }
    }

    private int EstimateTokens(string text) => text.Length / 4; // Rough estimate
    private int EstimateTokens(List<ChatMessage> messages) =>
        messages.Sum(m => EstimateTokens(m.Text ?? ""));
}
```

#### **Health Check Avançado**

```csharp
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IChatClient _llm;
    private readonly ILogger<HealthController> _logger;

    [HttpGet("ollama")]
    public async Task<IActionResult> CheckOllama()
    {
        var sw = Stopwatch.StartNew();
        
        try
        {
            var response = await _llm.CompleteAsync(
                "Responda apenas: OK",
                new ChatOptions
                {
                    MaxTokens = 10,
                    Temperature = 0
                }
            );
            sw.Stop();

            return Ok(new
            {
                status = "healthy",
                model = "llama3.1:8b",
                responseTimeMs = sw.ElapsedMilliseconds,
                response = response.Message.Text
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Ollama connection failed");
            return StatusCode(503, new
            {
                status = "unhealthy",
                error = "Cannot connect to Ollama service",
                details = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama health check failed");
            return StatusCode(500, new
            {
                status = "unhealthy",
                error = ex.Message
            });
        }
    }
}
```

---

### **8. Troubleshooting Comum**

| Problema | Causa | Solução |
|----------|-------|---------|
| **Connection refused** | Ollama não está rodando | `ollama serve` ou reiniciar serviço |
| **Model not found** | Modelo não baixado | `ollama pull llama3.1:8b` |
| **Out of memory** | GPU/RAM insuficiente | Usar modelo menor (3B) ou aumentar hardware |
| **Slow inference** | CPU-only mode | Instalar drivers CUDA/ROCm para usar GPU |
| **Invalid JSON** | Prompt mal formatado | Ajustar system prompt, adicionar exemplos |
| **Timeout** | Documento muito grande | Aumentar `TimeoutSeconds`, chunking de texto |

#### **Verificar se GPU está sendo usada**

```bash
# NVIDIA
nvidia-smi

# Ver uso durante inferência
watch -n 1 nvidia-smi

# Ollama detecta automaticamente GPUs
ollama run llama3.1:8b
# Deve mostrar: "using GPU: NVIDIA GeForce RTX..."
```

#### **Forçar CPU-only (se GPU não funcionar)**

```bash
# Windows
$env:OLLAMA_NUM_GPU=0
ollama serve

# Linux/Mac
OLLAMA_NUM_GPU=0 ollama serve
```

---

### **9. Comparativo de Modelos**

| Modelo | Tamanho | VRAM | Precisão | Velocidade | Português |
|--------|---------|------|----------|------------|-----------|
| **llama3.1:8b** | 4.7 GB | ~6 GB | ★★★★☆ | ★★★★★ | ★★★★☆ |
| **llama3.1:70b** | 40 GB | ~42 GB | ★★★★★ | ★★☆☆☆ | ★★★★★ |
| **qwen2.5:14b** | 8.5 GB | ~10 GB | ★★★★★ | ★★★★☆ | ★★★★★ |
| **mistral:7b** | 4.1 GB | ~5 GB | ★★★☆☆ | ★★★★★ | ★★★☆☆ |
| **phi3:medium** | 7.6 GB | ~9 GB | ★★★★☆ | ★★★★☆ | ★★★☆☆ |

**Recomendação para o projeto:**
- **Desenvolvimento/Testes:** `llama3.1:8b` (rápido, bom equilíbrio)
- **Produção (qualidade):** `qwen2.5:14b` (melhor para português + JSON)
- **Produção (velocidade):** `llama3.1:8b-instruct`

---

### **10. Otimizações de Performance**

#### **Cache de Respostas**

```csharp
public class CachedExtractionAgent
{
    private readonly ExtractionAgent _agent;
    private readonly IMemoryCache _cache;

    public async Task<ExamResult> ExtractWithCacheAsync(string documentText)
    {
        var hash = ComputeSha256(documentText);
        
        if (_cache.TryGetValue(hash, out ExamResult? cached))
        {
            _logger.LogInformation("Cache HIT for document {Hash}", hash);
            return cached!;
        }

        _logger.LogInformation("Cache MISS for document {Hash}", hash);
        var result = await _agent.ExtractStructuredDataAsync(documentText);
        
        _cache.Set(hash, result, TimeSpan.FromHours(24));
        return result;
    }

    private string ComputeSha256(string text)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
}
```

#### **Processamento em Lote**

```csharp
public async Task<List<ExamResult>> ExtractBatchAsync(
    List<string> documents,
    int maxParallel = 3)
{
    var semaphore = new SemaphoreSlim(maxParallel);
    var tasks = documents.Select(async doc =>
    {
        await semaphore.WaitAsync();
        try
        {
            return await _agent.ExtractStructuredDataAsync(doc);
        }
        finally
        {
            semaphore.Release();
        }
    });

    return (await Task.WhenAll(tasks)).ToList();
}
```

---

### **11. Fallback para Cloud (Híbrido)**

```csharp
public class HybridExtractionAgent
{
    private readonly IChatClient _localLlm;   // Ollama
    private readonly IChatClient _cloudLlm;   // OpenAI GPT-4
    private readonly ILogger _logger;

    public async Task<ExamResult> ExtractAsync(
        string text,
        bool forceCloud = false)
    {
        if (forceCloud)
        {
            _logger.LogInformation("Using cloud LLM (forced)");
            return await ExtractWithLlmAsync(_cloudLlm, text);
        }

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
        // Lógica compartilhada de extração
        var response = await llm.CompleteAsync(BuildPrompt(text));
        return ParseResponse(response);
    }
}
```

---

### **12. Docker Compose com Ollama**

```yaml
version: '3.8'

services:
  api:
    build: .
    ports:
      - "5000:8080"
    environment:
      - Ollama__Url=http://ollama:11434
      - Ollama__Model=llama3.1:8b
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=medicalexams;Username=postgres;Password=postgres123
    depends_on:
      - postgres
      - ollama

  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: medicalexams
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres123
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

  ollama:
    image: ollama/ollama:latest
    ports:
      - "11434:11434"
    volumes:
      - ollama_data:/root/.ollama
    # Para usar GPU no Docker (NVIDIA)
    deploy:
      resources:
        reservations:
          devices:
            - driver: nvidia
              count: 1
              capabilities: [gpu]
    command: serve

volumes:
  postgres_data:
  ollama_data:
```

**Inicializar:**
```bash
docker-compose up -d

# Ver logs do Ollama
docker-compose logs -f ollama

# Testar
curl http://localhost:11434/api/tags
```

---

## 🗄️ Modelagem do Banco de Dados (PostgreSQL)

### **Diagrama ER (Entidade-Relacionamento)**

```
┌─────────────────────┐
│     pacientes       │
├─────────────────────┤
│ id (PK)             │
│ nome                │
│ data_nascimento     │
│ cpf (UNIQUE)        │
│ created_at          │
│ updated_at          │
└──────────┬──────────┘
           │
           │ 1:N
           │
┌──────────▼──────────────────┐
│     documentos              │
├─────────────────────────────┤
│ id (PK)                     │
│ paciente_id (FK)            │
│ nome_arquivo                │
│ tipo_arquivo                │
│ tamanho_bytes               │
│ hash_sha256                 │
│ data_upload                 │
│ status_processamento        │
│ erro_processamento          │
│ created_at                  │
└──────────┬──────────────────┘
           │
           │ 1:N
           │
┌──────────▼──────────────────┐
│     exames                  │
├─────────────────────────────┤
│ id (PK)                     │
│ documento_id (FK)           │
│ tipo_exame_id (FK)          │
│ data_coleta                 │
│ medico_solicitante          │
│ laboratorio                 │
│ created_at                  │
└──────────┬──────────────────┘
           │
           │ 1:N
           │
┌──────────▼──────────────────┐
│   resultados_exame          │
├─────────────────────────────┤
│ id (PK)                     │
│ exame_id (FK)               │
│ parametro                   │
│ valor_numerico              │
│ valor_texto                 │
│ unidade                     │
│ referencia_min              │
│ referencia_max              │
│ status                      │
│ observacoes                 │
│ created_at                  │
└─────────────────────────────┘

┌─────────────────────┐
│   tipos_exame       │
├─────────────────────┤
│ id (PK)             │
│ nome                │
│ descricao           │
│ categoria           │
│ created_at          │
└─────────────────────┘
```

### **Scripts SQL de Criação**

```sql
-- Tabela de Pacientes
CREATE TABLE pacientes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    nome VARCHAR(255) NOT NULL,
    data_nascimento DATE,
    cpf VARCHAR(11) UNIQUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_pacientes_cpf ON pacientes(cpf);
CREATE INDEX idx_pacientes_nome ON pacientes(nome);

-- Tabela de Documentos Carregados
CREATE TABLE documentos (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    paciente_id UUID REFERENCES pacientes(id) ON DELETE CASCADE,
    nome_arquivo VARCHAR(500) NOT NULL,
    tipo_arquivo VARCHAR(50) NOT NULL,
    tamanho_bytes BIGINT NOT NULL,
    hash_sha256 VARCHAR(64) NOT NULL,
    data_upload TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    status_processamento VARCHAR(50) NOT NULL DEFAULT 'pending',
    erro_processamento TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_documentos_paciente ON documentos(paciente_id);
CREATE INDEX idx_documentos_hash ON documentos(hash_sha256);
CREATE INDEX idx_documentos_status ON documentos(status_processamento);

-- Tabela de Tipos de Exame (Catálogo)
CREATE TABLE tipos_exame (
    id SERIAL PRIMARY KEY,
    nome VARCHAR(255) NOT NULL UNIQUE,
    descricao TEXT,
    categoria VARCHAR(100),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_tipos_exame_categoria ON tipos_exame(categoria);

-- Inserir tipos comuns
INSERT INTO tipos_exame (nome, categoria) VALUES
    ('Hemograma Completo', 'Hematologia'),
    ('Glicemia', 'Bioquímica'),
    ('Colesterol Total', 'Lipidograma'),
    ('HDL', 'Lipidograma'),
    ('LDL', 'Lipidograma'),
    ('Triglicerídeos', 'Lipidograma'),
    ('Ureia', 'Função Renal'),
    ('Creatinina', 'Função Renal'),
    ('TGO/AST', 'Função Hepática'),
    ('TGP/ALT', 'Função Hepática');

-- Tabela de Exames
CREATE TABLE exames (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    documento_id UUID NOT NULL REFERENCES documentos(id) ON DELETE CASCADE,
    tipo_exame_id INTEGER REFERENCES tipos_exame(id),
    data_coleta DATE,
    medico_solicitante VARCHAR(255),
    laboratorio VARCHAR(255),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_exames_documento ON exames(documento_id);
CREATE INDEX idx_exames_tipo ON exames(tipo_exame_id);
CREATE INDEX idx_exames_data_coleta ON exames(data_coleta);

-- Tabela de Resultados de Exame
CREATE TABLE resultados_exame (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    exame_id UUID NOT NULL REFERENCES exames(id) ON DELETE CASCADE,
    parametro VARCHAR(255) NOT NULL,
    valor_numerico DECIMAL(18, 4),
    valor_texto TEXT,
    unidade VARCHAR(50),
    referencia_min DECIMAL(18, 4),
    referencia_max DECIMAL(18, 4),
    status VARCHAR(50),
    observacoes TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_resultados_exame ON resultados_exame(exame_id);
CREATE INDEX idx_resultados_parametro ON resultados_exame(parametro);
CREATE INDEX idx_resultados_status ON resultados_exame(status);

-- Trigger para atualizar updated_at
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER update_pacientes_updated_at
    BEFORE UPDATE ON pacientes
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();
```

---

## 📁 Estrutura do Projeto

```
MedicalExamExtractor/
│
├── src/
│   ├── MedicalExamExtractor.Api/
│   │   ├── Controllers/
│   │   │   ├── ExamsController.cs
│   │   │   ├── PacientesController.cs
│   │   │   └── HealthController.cs
│   │   ├── Middleware/
│   │   │   ├── ErrorHandlingMiddleware.cs
│   │   │   └── RequestLoggingMiddleware.cs
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── appsettings.Development.json
│   │
│   ├── MedicalExamExtractor.Application/
│   │   ├── Services/
│   │   │   ├── MedicalExamPipeline.cs
│   │   │   └── ExamQueryService.cs
│   │   ├── Agents/
│   │   │   ├── DocumentParserAgent.cs
│   │   │   ├── OcrAgent.cs
│   │   │   ├── ExtractionAgent.cs
│   │   │   ├── ValidationAgent.cs
│   │   │   └── NormalizationAgent.cs
│   │   └── DTOs/
│   │       ├── ExamUploadDto.cs
│   │       ├── ExamResultDto.cs
│   │       └── QueryResultDto.cs
│   │
│   ├── MedicalExamExtractor.Domain/
│   │   ├── Entities/
│   │   │   ├── Paciente.cs
│   │   │   ├── Documento.cs
│   │   │   ├── Exame.cs
│   │   │   ├── ResultadoExame.cs
│   │   │   └── TipoExame.cs
│   │   ├── ValueObjects/
│   │   │   └── Cpf.cs
│   │   └── Interfaces/
│   │       └── IExamRepository.cs
│   │
│   └── MedicalExamExtractor.Infrastructure/
│       ├── Data/
│       │   ├── AppDbContext.cs
│       │   └── Migrations/
│       ├── Repositories/
│       │   └── ExamRepository.cs
│       └── Parsers/
│           ├── PdfParser.cs
│           ├── WordParser.cs
│           └── ExcelParser.cs
│
├── tests/
│   ├── MedicalExamExtractor.Tests/
│   │   ├── Integration/
│   │   └── Unit/
│   └── MedicalExamExtractor.Api.Tests/
│
├── docker/
│   ├── Dockerfile
│   └── docker-compose.yml
│
├── docs/
│   ├── API.md
│   ├── DATABASE.md
│   └── DEPLOYMENT.md
│
├── .gitignore
├── README.md
└── MedicalExamExtractor.sln
```

---

## 🔌 Endpoints da API

### **1. Upload de Documentos**

#### `POST /api/exams/upload`

**Request:**
```http
POST /api/exams/upload
Content-Type: multipart/form-data

{
  "file": [binary],
  "cpf": "12345678900",
  "nomePaciente": "João Silva" (opcional)
}
```

**Response (200 OK):**
```json
{
  "documentoId": "550e8400-e29b-41d4-a716-446655440000",
  "pacienteId": "660e8400-e29b-41d4-a716-446655440001",
  "status": "processing",
  "message": "Documento recebido e em processamento"
}
```

**Response (202 Accepted):**
```json
{
  "documentoId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "queued",
  "estimatedCompletionTime": "2026-01-29T15:30:00Z"
}
```

---

### **2. Status do Processamento**

#### `GET /api/exams/status/{documentoId}`

**Response (200 OK):**
```json
{
  "documentoId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "completed",
  "processedAt": "2026-01-29T15:25:00Z",
  "examesExtraidos": 5,
  "erros": []
}
```

**Status possíveis:**
- `pending` - Na fila
- `processing` - Em processamento
- `completed` - Concluído com sucesso
- `failed` - Falhou (ver campo `erros`)

---

### **3. Buscar Exames de um Paciente**

#### `GET /api/exams/paciente/{cpf}`

**Query Parameters:**
- `dataInicio` (opcional): yyyy-MM-dd
- `dataFim` (opcional): yyyy-MM-dd
- `tipoExame` (opcional): nome do tipo de exame

**Response (200 OK):**
```json
{
  "paciente": {
    "id": "660e8400-e29b-41d4-a716-446655440001",
    "nome": "João Silva",
    "cpf": "12345678900",
    "dataNascimento": "1980-05-15"
  },
  "exames": [
    {
      "id": "770e8400-e29b-41d4-a716-446655440002",
      "tipo": "Lipidograma",
      "dataColeta": "2026-01-28",
      "documentoId": "550e8400-e29b-41d4-a716-446655440000",
      "resultados": [
        {
          "parametro": "Colesterol Total",
          "valor": 210,
          "unidade": "mg/dL",
          "referenciaMin": 0,
          "referenciaMax": 200,
          "status": "alto"
        },
        {
          "parametro": "HDL",
          "valor": 45,
          "unidade": "mg/dL",
          "referenciaMin": 40,
          "referenciaMax": 60,
          "status": "normal"
        }
      ]
    }
  ],
  "total": 1
}
```

---

### **4. Buscar Resultado Específico**

#### `GET /api/exams/{exameId}`

**Response (200 OK):**
```json
{
  "id": "770e8400-e29b-41d4-a716-446655440002",
  "tipo": "Lipidograma",
  "dataColeta": "2026-01-28",
  "medicoSolicitante": "Dra. Maria Santos",
  "laboratorio": "Lab Central",
  "paciente": {
    "id": "660e8400-e29b-41d4-a716-446655440001",
    "nome": "João Silva",
    "cpf": "12345678900"
  },
  "resultados": [
    {
      "id": "880e8400-e29b-41d4-a716-446655440003",
      "parametro": "Colesterol Total",
      "valorNumerico": 210,
      "unidade": "mg/dL",
      "referenciaMin": 0,
      "referenciaMax": 200,
      "status": "alto",
      "observacoes": null
    }
  ]
}
```

---

### **5. Análise Temporal (Tendências)**

#### `GET /api/exams/paciente/{cpf}/tendencia`

**Query Parameters:**
- `parametro`: nome do parâmetro (ex: "Colesterol Total")
- `meses`: quantidade de meses retroativos (default: 12)

**Response (200 OK):**
```json
{
  "paciente": {
    "id": "660e8400-e29b-41d4-a716-446655440001",
    "nome": "João Silva"
  },
  "parametro": "Colesterol Total",
  "unidade": "mg/dL",
  "referencia": {
    "min": 0,
    "max": 200
  },
  "serie": [
    {
      "data": "2025-07-15",
      "valor": 195,
      "status": "normal"
    },
    {
      "data": "2025-10-20",
      "valor": 205,
      "status": "alto"
    },
    {
      "data": "2026-01-28",
      "valor": 210,
      "status": "alto"
    }
  ],
  "tendencia": "crescente",
  "variacao": "+7.7%",
  "analise": "Colesterol em tendência crescente nos últimos 6 meses. Recomenda-se acompanhamento médico."
}
```

---

### **6. Health Check**

#### `GET /api/health`

**Response (200 OK):**
```json
{
  "status": "healthy",
  "timestamp": "2026-01-29T15:30:00Z",
  "services": {
    "database": "healthy",
    "ollama": "healthy"
  },
  "version": "1.0.0"
}
```

---

## 🛠️ Tecnologias e Pacotes NuGet

### **Framework e Runtime**
- .NET 10 (ASP.NET Core)
- C# 13

### **Banco de Dados**
```bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.EntityFrameworkCore.Tools
```

### **AI/LLM**
```bash
dotnet add package Microsoft.Extensions.AI --prerelease
dotnet add package Microsoft.Extensions.AI.Ollama --prerelease
```

### **Document Parsing**
```bash
# PDF
dotnet add package itext7

# Word
dotnet add package DocumentFormat.OpenXml

# Excel
dotnet add package EPPlus
```

### **Logging e Observabilidade**
```bash
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
dotnet add package Serilog.Sinks.Seq
```

### **Validação**
```bash
dotnet add package FluentValidation.AspNetCore
```

### **Testes**
```bash
dotnet add package xUnit
dotnet add package Moq
dotnet add package FluentAssertions
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Testcontainers
```

---

## 🔄 Pipeline de Processamento

```
1. Upload
   ↓
2. Validação (tamanho, formato, duplicata)
   ↓
3. Persistência (arquivo + metadados)
   ↓
4. Identificação/Criação do Paciente
   ↓
5. DocumentParserAgent (extrair texto)
   ↓
6. ExtractionAgent (Ollama → JSON)
   ↓
7. ValidationAgent (validar dados)
   ↓
8. NormalizationAgent (normalizar)
   ↓
9. Persistência no PostgreSQL
   ↓
10. Atualizar status do documento
```

### **Fluxo de Erro**
```
Erro em qualquer etapa
   ↓
Registrar log detalhado
   ↓
Atualizar documento.status = 'failed'
   ↓
Salvar erro_processamento
   ↓
Retornar 500 ou 422 (conforme caso)
```

---

## 📅 Cronograma de Implementação

### **Sprint 1 - Setup e Infraestrutura (1 semana)**

**Dias 1-2:**
- [ ] Criar solution e projetos (.Api, .Application, .Domain, .Infrastructure)
- [ ] Setup PostgreSQL (Docker ou local)
- [ ] Configurar EF Core + Migrations
- [ ] Criar scripts SQL (tabelas, índices, triggers)
- [ ] Executar primeira migration

**Dias 3-4:**
- [ ] Implementar entidades do domínio (Paciente, Documento, Exame, etc.)
- [ ] Implementar AppDbContext
- [ ] Criar repositórios base (IExamRepository, ExamRepository)
- [ ] Testes unitários das entidades

**Dias 5-7:**
- [ ] Verificar Ollama configurado localmente
- [ ] Testar conectividade (curl http://localhost:11434)
- [ ] Configurar appsettings.json (connection strings, Ollama)
- [ ] Implementar HealthController
- [ ] Implementar logging (Serilog)
- [ ] Documentação inicial

---

### **Sprint 2 - Document Parsing (1 semana)**

**Dias 1-3:**
- [ ] Implementar IDocumentParser interface
- [ ] Implementar PdfParser (iText7)
- [ ] Implementar WordParser (OpenXml)
- [ ] Implementar ExcelParser (EPPlus)
- [ ] Testes unitários de cada parser

**Dias 4-5:**
- [ ] Implementar DocumentParserAgent
- [ ] Integrar parsers no agent
- [ ] Testes com 10 documentos reais (PDF, Word, Excel)
- [ ] Validar extração de texto

**Dias 6-7:**
- [ ] Implementar OcrAgent (para imagens, opcional v1)
- [ ] Tratamento de erros (arquivo corrompido, formato inválido)
- [ ] Documentar limitações

---

### **Sprint 3 - AI Extraction com Ollama (1,5 semanas)**

**Dias 1-2:**
- [ ] Verificar Ollama rodando (ollama list)
- [ ] Testar modelo manualmente (ollama run llama3.1:8b)
- [ ] Implementar health check do Ollama no .NET
- [ ] Configurar IChatClient no Program.cs

**Dias 3-5:**
- [ ] Implementar ExtractionAgent com Ollama
- [ ] Criar prompts estruturados (system + user)
- [ ] Integrar com IChatClient
- [ ] Parsing de JSON responses
- [ ] Tratamento de respostas malformadas
- [ ] Implementar retry logic

**Dias 6-8:**
- [ ] Implementar ValidationAgent
- [ ] Implementar NormalizationAgent
- [ ] Testes com 50 documentos reais
- [ ] Medir precisão e tempo de processamento
- [ ] Ajustar prompts baseado nos resultados

**Dias 9-10:**
- [ ] Implementar cache de respostas
- [ ] Implementar métricas de performance (tokens/s, latência)
- [ ] Documentar limitações encontradas
- [ ] Implementar fallback para cloud (opcional)

---

### **Sprint 4 - API Endpoints (1 semana)**

**Dias 1-3:**
- [ ] Implementar POST /api/exams/upload
- [ ] Validação de entrada (tamanho, formato, CPF)
- [ ] Hash de documentos (evitar duplicatas)
- [ ] Persistência de Paciente + Documento
- [ ] Chamar pipeline de processamento
- [ ] Retornar 202 Accepted

**Dias 4-5:**
- [ ] Implementar GET /api/exams/status/{documentoId}
- [ ] Implementar GET /api/exams/paciente/{cpf}
- [ ] Filtros (data, tipo de exame)
- [ ] Paginação (se necessário)

**Dias 6-7:**
- [ ] Implementar GET /api/exams/{exameId}
- [ ] Implementar endpoint de tendências (opcional v1)
- [ ] Testes de integração de todos endpoints
- [ ] Swagger/OpenAPI documentation

---

### **Sprint 5 - Testes e Refinamento (1 semana)**

**Dias 1-2:**
- [ ] Testes unitários (cobertura >80%)
- [ ] Testes de integração (Testcontainers + PostgreSQL)
- [ ] Testes de carga (quantos docs/minuto?)

**Dias 3-4:**
- [ ] Refatoração de código
- [ ] Code review
- [ ] Correção de bugs identificados
- [ ] Otimização de queries SQL

**Dias 5-7:**
- [ ] Documentação completa (README, API docs)
- [ ] Setup de CI/CD (opcional)
- [ ] Preparar ambiente de produção
- [ ] Deploy em staging

---

### **Sprint 6 - Deploy e Monitoramento (3 dias)**

**Dias 1-2:**
- [ ] Dockerfile + docker-compose
- [ ] Deploy em produção (ou staging final)
- [ ] Configurar logs centralizados (Seq, ELK, etc.)
- [ ] Configurar alertas (erros, latência)

**Dia 3:**
- [ ] Validação final com usuários
- [ ] Ajustes de última hora
- [ ] Retrospectiva do projeto
- [ ] **Go Live! 🚀**

---

## 📊 Métricas de Sucesso

| Métrica | Meta com Ollama Llama3.1:8b |
|---------|------------------------------|
| **Precisão de Extração** | >85% |
| **Tempo de Processamento** | <15s por documento |
| **Taxa de Sucesso** | >90% |
| **Custo por Documento** | $0 (local) |
| **Uptime** | >99% |
| **Cobertura de Testes** | >80% |

---

## 🔐 Segurança e Compliance

### **LGPD / Privacidade**
- ✅ **Dados processados 100% localmente** (Ollama não envia dados para fora)
- ✅ Criptografia em trânsito (HTTPS)
- ✅ Criptografia em repouso (PostgreSQL)
- ✅ Logs não contêm dados sensíveis (sanitização)
- ✅ **Compliance LGPD por design** (dados médicos nunca saem do servidor)
- ⚠️ Implementar autenticação/autorização (v2)
- ⚠️ Auditoria de acessos (v2)

### **Validações**
- Tamanho máximo de arquivo: 10MB
- Formatos permitidos: .pdf, .docx, .xlsx
- CPF validado (algoritmo)
- Rate limiting (v2)

---

## 🐳 Docker e Deploy

### **docker-compose.yml**

```yaml
version: '3.8'

services:
  api:
    build:
      context: .
      dockerfile: docker/Dockerfile
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=medicalexams;Username=postgres;Password=postgres123
      - Ollama__Url=http://host.docker.internal:11434
      - Ollama__Model=llama3.1:8b
    depends_on:
      - postgres
    volumes:
      - ./uploads:/app/uploads
    extra_hosts:
      - "host.docker.internal:host-gateway"

  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres123
      POSTGRES_DB: medicalexams
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./scripts/init.sql:/docker-entrypoint-initdb.d/init.sql

volumes:
  postgres_data:
```

**Nota:** Como o Ollama já está instalado localmente, a API se conecta via `host.docker.internal:11434`.

---

## 🚀 Próximos Passos (v2)

### **Funcionalidades Futuras**

1. **Autenticação e Autorização**
   - JWT tokens
   - Roles (admin, médico, paciente)
   - OAuth2 / OpenID Connect

2. **Dashboard Web**
   - Visualização de exames
   - Gráficos de tendências
   - Comparação temporal
   - Exportação de relatórios (PDF)

3. **Machine Learning**
   - Fine-tuning do modelo local
   - Classificação automática de exames
   - Detecção de anomalias

4. **Integrações**
   - HL7 / FHIR (sistemas hospitalares)
   - E-mail (alertas de valores críticos)
   - WhatsApp (notificações)

5. **Performance**
   - Cache (Redis)
   - Processamento assíncrono (RabbitMQ/Kafka)
   - CDN para uploads

6. **Auditoria e Compliance**
   - Log de todos os acessos
   - Trilha de auditoria completa
   - Relatórios de compliance LGPD

---

## 📚 Referências

- [Documentação .NET AI](https://learn.microsoft.com/dotnet/ai/)
- [Ollama Documentation](https://ollama.ai/docs)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [EF Core Documentation](https://learn.microsoft.com/ef/core/)
- [iText7 Documentation](https://itextpdf.com/products/itext-7)

---

## ✅ Checklist de Entrega

### **Mínimo Viável (MVP)**
- [ ] API REST funcionando
- [ ] Upload de PDF/Word/Excel
- [ ] **Ollama configurado e funcionando**
- [ ] **Extração com llama3.1:8b**
- [ ] Persistência em PostgreSQL
- [ ] Endpoints de consulta
- [ ] **Health check do Ollama**
- [ ] Testes (>70% cobertura)
- [ ] Documentação (README + Swagger)
- [ ] Docker Compose funcional

### **Desejável**
- [ ] CI/CD pipeline
- [ ] Logging centralizado
- [ ] Métricas e observabilidade
- [ ] Health checks avançados
- [ ] Análise de tendências

### **Futuro (v2)**
- [ ] Dashboard web
- [ ] Autenticação completa
- [ ] Integrações externas
- [ ] Fine-tuning do modelo

---

**Última atualização:** 29/01/2026  
**Versão:** 1.1 (com Ollama 3.1 local pré-configurado)  
**Status:** Pronto para kickoff 🚀
