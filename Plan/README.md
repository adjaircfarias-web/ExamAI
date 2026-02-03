# 🤖 AI Agents no .NET 10 - Guia Completo

**Data:** 01/02/2026  
**Autor:** Clawdex 🔍

---

## 🤖 O que são Agents no .NET?

**Agents** são abstrações de alto nível introduzidas no **`Microsoft.Extensions.AI`** (preview no .NET 9, evoluindo no .NET 10) que encapsulam **comportamentos inteligentes** usando modelos de IA (LLMs, embeddings, etc.).

Em vez de você chamar diretamente APIs de IA (OpenAI, Azure OpenAI, Ollama, etc.), você trabalha com **interfaces padronizadas** que podem:
- Chamar modelos de IA
- Usar ferramentas (function calling)
- Manter contexto/memória
- Orquestrar múltiplos passos
- Pipeline de middlewares

---

## 🎯 Diferença: API Direta vs Agent

### ❌ **Sem Agent (API direta)**
```csharp
// Você lida com detalhes de implementação
var client = new OpenAIClient(apiKey);
var response = await client.GetChatCompletionsAsync(
    "gpt-4",
    new ChatMessage[] {
        new ChatMessage(ChatRole.System, "Você é um assistente útil"),
        new ChatMessage(ChatRole.User, "Qual é a capital do Brasil?")
    }
);
```

### ✅ **Com Agent (abstração)**
```csharp
// Interface padronizada, troca de provider sem mudar código
IChatClient agent = new OpenAIChatClient(apiKey, "gpt-4");

var response = await agent.CompleteAsync("Qual é a capital do Brasil?");
```

---

## 📦 Principais Interfaces

### 1. **`IChatClient`** (Chat/Conversação)
```csharp
public interface IChatClient
{
    Task<ChatCompletion> CompleteAsync(
        IList<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default);
}
```

**Uso:**
```csharp
IChatClient agent = new AzureOpenAIChatClient(endpoint, apiKey, "gpt-4");

var messages = new List<ChatMessage>
{
    new(ChatRole.System, "Você é um especialista em Event Sourcing"),
    new(ChatRole.User, "Explique CQRS em 2 linhas")
};

var response = await agent.CompleteAsync(messages);
Console.WriteLine(response.Message.Text);
// "CQRS separa operações de leitura (queries) e escrita (commands)..."
```

---

### 2. **`IEmbeddingGenerator`** (Embeddings/Vetores)
```csharp
public interface IEmbeddingGenerator<TInput, TEmbedding>
{
    Task<GeneratedEmbeddings<TEmbedding>> GenerateAsync(
        IEnumerable<TInput> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default);
}
```

**Uso:**
```csharp
IEmbeddingGenerator<string, Embedding<float>> embedder = 
    new OpenAIEmbeddingGenerator(apiKey, "text-embedding-3-small");

var embeddings = await embedder.GenerateAsync(new[] {
    "Event Sourcing é um padrão arquitetural",
    "CQRS separa leitura e escrita"
});

// Calcular similaridade entre textos (cosine similarity)
var similarity = CosineSimilarity(embeddings[0], embeddings[1]);
```

---

## 🔧 Function Calling (Ferramentas)

Agents podem **chamar funções C#** automaticamente quando precisarem!

```csharp
// Definir função que o Agent pode chamar
[Description("Busca informações sobre um pagamento")]
string GetPaymentInfo(Guid paymentId)
{
    // Simular busca no banco
    return $"Pagamento {paymentId}: Status=Processed, Amount=100.50 USD";
}

// Configurar Agent com função disponível
var agent = new OpenAIChatClient(apiKey, "gpt-4")
    .AsBuilder()
    .UseFunctionInvocation()  // Middleware para function calling
    .Build();

var options = new ChatOptions
{
    Tools = [AIFunctionFactory.Create(GetPaymentInfo)]
};

var response = await agent.CompleteAsync(
    "Qual o status do pagamento 123e4567-e89b-12d3-a456-426614174000?",
    options
);

// Agent detecta que precisa da função, chama automaticamente GetPaymentInfo()
// e responde: "O pagamento está Processed com valor de 100.50 USD"
```

**Fluxo:**
1. User pergunta sobre pagamento
2. Agent decide chamar `GetPaymentInfo()`
3. Função executa e retorna dados
4. Agent usa resultado para responder

---

## 🧩 Pipeline de Middlewares

Você pode adicionar **comportamentos** ao Agent:

```csharp
IChatClient agent = new OpenAIChatClient(apiKey, "gpt-4")
    .AsBuilder()
    .UseLogging()               // Log de requests/responses
    .UseOpenTelemetry()         // Métricas
    .UseFunctionInvocation()    // Function calling
    .UseRateLimiting()          // Rate limiting
    .Build();
```

**Middlewares comuns:**
- `UseLogging()` - Logging automático
- `UseFunctionInvocation()` - Function calling
- `UseOpenTelemetry()` - Observabilidade
- `UseDistributedCache()` - Cache de respostas
- Custom - Você pode criar seus próprios!

---

## 💡 Exemplo Prático: Agent no Payment System

Imagina um **Agent que ajuda com análise de pagamentos**:

```csharp
public class PaymentAnalysisAgent
{
    private readonly IChatClient _agent;
    private readonly IAggregateRepository<Payment> _repository;

    public PaymentAnalysisAgent(
        IChatClient agent,
        IAggregateRepository<Payment> repository)
    {
        _agent = agent;
        _repository = repository;
    }

    // Função que o Agent pode chamar
    [Description("Busca detalhes de um pagamento pelo ID")]
    async Task<string> GetPaymentDetails(Guid paymentId)
    {
        var payment = await _repository.GetByIdAsync(paymentId);
        if (payment == null) return "Pagamento não encontrado";

        return $@"
            PaymentId: {payment.PaymentId}
            Status: {payment.Status}
            Amount: {payment.Amount} {payment.Currency}
            InitiatedAt: {payment.InitiatedAt}
            ProcessedAt: {payment.ProcessedAt}
        ";
    }

    [Description("Analisa padrões de fraude em um pagamento")]
    bool CheckFraudPattern(Guid paymentId, decimal amount)
    {
        // Lógica de análise de fraude
        return amount > 10000; // Simplificado
    }

    public async Task<string> AnalyzeAsync(string userQuery)
    {
        var options = new ChatOptions
        {
            Tools = [
                AIFunctionFactory.Create(GetPaymentDetails),
                AIFunctionFactory.Create(CheckFraudPattern)
            ]
        };

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, @"
                Você é um assistente de análise de pagamentos.
                Use as ferramentas disponíveis para responder perguntas.
            "),
            new(ChatRole.User, userQuery)
        };

        var response = await _agent.CompleteAsync(messages, options);
        return response.Message.Text;
    }
}

// Uso:
var agent = new PaymentAnalysisAgent(chatClient, repository);

var result = await agent.AnalyzeAsync(
    "O pagamento 123e4567-e89b-12d3-a456-426614174000 pode ser fraude?"
);

// Agent vai:
// 1. Chamar GetPaymentDetails() para ver o valor
// 2. Chamar CheckFraudPattern() com os dados
// 3. Responder: "Sim, o pagamento de 15.000 USD excede o limite..."
```

---

## 🔄 Multi-Turn Conversations (Memória)

Agents mantêm contexto entre mensagens:

```csharp
var agent = new OpenAIChatClient(apiKey, "gpt-4");
var conversation = new List<ChatMessage>
{
    new(ChatRole.System, "Você é um especialista em Event Sourcing")
};

// Turn 1
conversation.Add(new(ChatRole.User, "O que é Event Sourcing?"));
var response1 = await agent.CompleteAsync(conversation);
conversation.Add(response1.Message);

// Turn 2 (Agent lembra do contexto anterior)
conversation.Add(new(ChatRole.User, "E quais as vantagens disso?"));
var response2 = await agent.CompleteAsync(conversation);
conversation.Add(response2.Message);

// Turn 3
conversation.Add(new(ChatRole.User, "Dá um exemplo?"));
var response3 = await agent.CompleteAsync(conversation);
```

---

## 🚀 Providers Suportados

O `Microsoft.Extensions.AI` tem abstrações, mas cada provider tem sua implementação:

```bash
# OpenAI
dotnet add package Microsoft.Extensions.AI.OpenAI

# Azure OpenAI
dotnet add package Microsoft.Extensions.AI.AzureAIInference

# Ollama (local)
dotnet add package Microsoft.Extensions.AI.Ollama

# Semantic Kernel (integração)
dotnet add package Microsoft.SemanticKernel
```

**Troca de provider sem mudar código:**
```csharp
// OpenAI
IChatClient agent = new OpenAIChatClient(apiKey, "gpt-4");

// OU Azure
IChatClient agent = new AzureAIChatClient(endpoint, credential, "gpt-4");

// OU Ollama (local, grátis!)
IChatClient agent = new OllamaChatClient("http://localhost:11434", "llama2");

// Código que usa o agent não muda!
var response = await agent.CompleteAsync("Olá!");
```

---

## 🎓 Quando Usar Agents?

### ✅ **Use quando:**
- Precisa trocar providers de IA facilmente
- Quer function calling/tool use
- Precisa de pipeline de middlewares
- Quer abstrair complexidade da IA
- Multi-turn conversations
- Embeddings/RAG (Retrieval Augmented Generation)

### ❌ **Não use quando:**
- Apenas 1 chamada simples de IA
- Provider específico com features únicas
- Precisa de controle total sobre requests
- Performance extrema (abstração adiciona overhead)

---

## 📚 Recursos

### Documentação Oficial
- https://learn.microsoft.com/dotnet/ai/
- https://github.com/dotnet/extensions
- https://github.com/microsoft/semantic-kernel

### Instalação
```bash
# Instalar preview
dotnet add package Microsoft.Extensions.AI --prerelease
dotnet add package Microsoft.Extensions.AI.OpenAI --prerelease
```

---

## 🎯 Casos de Uso no Payment System

### 1. **Análise de Fraude com IA**
- Agent analisa padrões de pagamentos
- Chama funções para buscar histórico
- Decide se é suspeito

### 2. **Assistente de Suporte**
- "Por que meu pagamento falhou?"
- Agent busca eventos, analisa e explica

### 3. **Business Intelligence**
- "Qual a tendência de pagamentos esta semana?"
- Agent acessa MongoDB, analisa e responde

### 4. **Análise de Logs**
- Agent lê eventos do Event Store
- Identifica padrões anômalos
- Sugere otimizações

---

## 🔥 Exemplo Completo: Agent de Análise

```csharp
// Program.cs
builder.Services.AddSingleton<IChatClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var apiKey = config["OpenAI:ApiKey"];
    
    return new OpenAIChatClient(apiKey, "gpt-4")
        .AsBuilder()
        .UseLogging(sp.GetRequiredService<ILoggerFactory>())
        .UseFunctionInvocation()
        .Build();
});

builder.Services.AddScoped<PaymentAnalysisAgent>();

// Controller
[ApiController]
[Route("api/[controller]")]
public class AnalysisController : ControllerBase
{
    private readonly PaymentAnalysisAgent _agent;

    public AnalysisController(PaymentAnalysisAgent agent)
    {
        _agent = agent;
    }

    [HttpPost("analyze")]
    public async Task<IActionResult> Analyze([FromBody] string question)
    {
        var result = await _agent.AnalyzeAsync(question);
        return Ok(new { answer = result });
    }
}
```

**Uso:**
```bash
POST /api/analysis/analyze
{
  "question": "Quantos pagamentos foram processados hoje?"
}

Response:
{
  "answer": "Hoje foram processados 127 pagamentos, totalizando $15,342.50 USD."
}
```

---

## 💭 Conclusão

AI Agents no .NET 10 são uma **abstração poderosa** que:
- ✅ Simplifica integração com modelos de IA
- ✅ Permite trocar providers facilmente
- ✅ Adiciona function calling de forma natural
- ✅ Suporta pipelines de middlewares
- ✅ Mantém contexto em conversações

Para projetos que precisam de **inteligência adaptativa** e **análise complexa**, Agents são uma excelente escolha!

---

**Última atualização:** 01/02/2026  
**Versão:** 1.0
