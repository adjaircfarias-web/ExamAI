using System.Net.Http.Json;
using System.Text.Json;
using ExamAI.Application.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExamAI.Application.Agents;

/// <summary>
/// Agent responsável por extrair dados estruturados de documentos médicos usando LLM (Ollama)
/// </summary>
public class ExtractionAgent
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ExtractionAgent> _logger;
    private readonly string _ollamaUrl;
    private readonly string _model;
    private const int MaxRetries = 1;

    public ExtractionAgent(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ExtractionAgent> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _ollamaUrl = configuration["Ollama:Url"] ?? "http://localhost:11434";
        _model = configuration["Ollama:Model"] ?? "Llama3.1:latest";
        
        _logger.LogInformation("ExtractionAgent configured with Ollama URL: {Url}, Model: {Model}", _ollamaUrl, _model);
    }

    /// <summary>
    /// Extrai dados estruturados de um texto de documento médico
    /// </summary>
    /// <param name="documentText">Texto bruto extraído do documento</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Dados estruturados extraídos</returns>
    public async Task<ExamExtractionResult> ExtractAsync(
        string documentText,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(documentText))
            throw new ArgumentException("Document text cannot be empty", nameof(documentText));

        _logger.LogInformation("Starting extraction from document text ({CharCount} chars)", documentText.Length);

        var systemPrompt = GetSystemPrompt();
        var userPrompt = GetUserPrompt(documentText);

        _logger.LogInformation("Document text being sent to LLM ({CharCount} chars): {Text}", 
            documentText.Length,
            documentText.Length > 300 ? documentText.Substring(0, 300) + "..." : documentText);

        // Tentar extração com retry
        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                _logger.LogDebug("Extraction attempt {Attempt}/{MaxAttempts}", attempt + 1, MaxRetries + 1);

                // Chamar Ollama via HTTP
                using var httpClient = _httpClientFactory.CreateClient("OllamaClient");
                
                var ollamaRequest = new
                {
                    model = _model,
                    prompt = $"{systemPrompt}\n\n{userPrompt}",
                    stream = false,
                    options = new
                    {
                        temperature = 0.1,
                        num_predict = 4096
                    }
                };

                var response = await httpClient.PostAsJsonAsync(
                    $"{_ollamaUrl}/api/generate",
                    ollamaRequest,
                    cancellationToken);

                response.EnsureSuccessStatusCode();

                var ollamaResponse = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(
                    cancellationToken: cancellationToken);

                var responseText = ollamaResponse?.Response ?? string.Empty;

                _logger.LogInformation("LLM raw response ({CharCount} chars): {Response}", 
                    responseText.Length, 
                    responseText.Length > 500 ? responseText.Substring(0, 500) + "..." : responseText);

                // Extrair JSON da resposta (pode vir com markdown code block)
                var jsonText = ExtractJsonFromResponse(responseText);

                // Parse JSON
                var result = ParseExtractionResult(jsonText);

                _logger.LogInformation(
                    "Extraction successful: {ExamCount} exams found, patient: {PatientName}",
                    result.Exams.Count,
                    result.Patient?.Name ?? "N/A");

                return result;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to parse JSON on attempt {Attempt}/{MaxAttempts}",
                    attempt + 1,
                    MaxRetries + 1);

                if (attempt >= MaxRetries)
                {
                    _logger.LogError("Max retries reached, extraction failed");
                    throw new InvalidOperationException(
                        "Failed to parse LLM response after retries. The response might be malformed.",
                        ex);
                }

                // Aguardar antes de tentar novamente
                await Task.Delay(500, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during extraction on attempt {Attempt}", attempt + 1);

                if (attempt >= MaxRetries)
                    throw;

                await Task.Delay(500, cancellationToken);
            }
        }

        // Fallback (não deve chegar aqui)
        throw new InvalidOperationException("Extraction failed after all retries");
    }

    /// <summary>
    /// Retorna o system prompt otimizado para extração de exames médicos
    /// </summary>
    private string GetSystemPrompt()
    {
        return @"Você é um assistente especializado em extrair informações de documentos médicos.

Sua tarefa é analisar o texto de um exame médico e extrair as seguintes informações em formato JSON:

1. Informações do paciente (use as chaves exatas em inglês):
   - patient: objeto com dados do paciente
     - name: nome completo do paciente
     - birthDate: data de nascimento (formato: YYYY-MM-DD, ou null se não encontrado)
     - collectionDate: data de coleta do exame (formato: YYYY-MM-DD)
     - requestingPhysician: nome do médico que solicitou o exame

2. Lista de exames realizados (use as chaves exatas em inglês):
   - exams: array de objetos, cada um com:
     - type: nome do exame/parâmetro (ex: ""Colesterol Total"", ""Glicemia"", ""Hemoglobina"")
     - value: valor numérico do resultado (apenas número, sem unidade)
     - unit: unidade de medida (ex: ""mg/dL"", ""g/dL"", ""%"")
     - referenceMin: valor mínimo da faixa de referência (número ou null)
     - referenceMax: valor máximo da faixa de referência (número ou null)
     - status: interpretação do resultado (""normal"", ""baixo"", ""alto"", ""crítico"", ou null)
     - observations: qualquer observação adicional (ou null)

IMPORTANTE:
- Retorne APENAS o JSON válido, sem texto adicional
- Use as chaves EXATAMENTE como especificado acima (em inglês)
- Use null quando a informação não estiver disponível
- Para datas, use formato YYYY-MM-DD
- Para valores numéricos, use ponto como separador decimal (ex: 5.2, não 5,2)
- Para status, analise se o valor está dentro, abaixo ou acima da faixa de referência
- Se houver múltiplos exames, inclua todos na lista
- O JSON deve ter exatamente esta estrutura:

{
  ""patient"": {
    ""name"": ""string ou null"",
    ""birthDate"": ""YYYY-MM-DD ou null"",
    ""collectionDate"": ""YYYY-MM-DD"",
    ""requestingPhysician"": ""string ou null""
  },
  ""exams"": [
    {
      ""type"": ""string"",
      ""value"": número.ou.null,
      ""unit"": ""string ou null"",
      ""referenceMin"": número.ou.null,
      ""referenceMax"": número.ou.null,
      ""status"": ""normal|baixo|alto|crítico ou null"",
      ""observations"": ""string ou null""
    }
  ]
}";
    }

    /// <summary>
    /// Retorna o user prompt com o texto do documento
    /// </summary>
    private string GetUserPrompt(string documentText)
    {
        return $@"Analise o seguinte texto de um documento médico e extraia as informações em formato JSON:

```
{documentText}
```

Retorne apenas o JSON estruturado conforme as instruções.";
    }

    /// <summary>
    /// Extrai JSON de uma resposta que pode conter markdown code blocks
    /// </summary>
    private string ExtractJsonFromResponse(string response)
    {
        // Remover markdown code blocks se presente (```json ... ```)
        var jsonStart = response.IndexOf('{');
        var jsonEnd = response.LastIndexOf('}');

        if (jsonStart >= 0 && jsonEnd > jsonStart)
        {
            var jsonText = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
            _logger.LogDebug("Extracted JSON from response ({Length} chars)", jsonText.Length);
            return jsonText;
        }

        _logger.LogWarning("Could not find JSON delimiters in response, using full text");
        return response;
    }

    /// <summary>
    /// Faz parse do JSON retornado pelo LLM
    /// </summary>
    private ExamExtractionResult ParseExtractionResult(string jsonText)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        try
        {
            var result = JsonSerializer.Deserialize<ExamExtractionResult>(jsonText, options);

            if (result == null)
            {
                _logger.LogError("Deserialization returned null");
                throw new JsonException("Failed to deserialize JSON: result is null");
            }

            _logger.LogDebug(
                "Successfully parsed JSON: {ExamCount} exams, patient: {PatientName}",
                result.Exams?.Count ?? 0,
                result.Patient?.Name ?? "N/A");

            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse JSON: {JsonText}", jsonText);
            throw;
        }
    }
}

/// <summary>
/// Resposta da API do Ollama (endpoint /api/generate)
/// </summary>
internal class OllamaGenerateResponse
{
    public string? Model { get; set; }
    public string? Response { get; set; }
    public bool? Done { get; set; }
}
