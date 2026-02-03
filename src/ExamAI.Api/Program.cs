using ExamAI.Application.Agents;
using ExamAI.Application.Pipelines;
using ExamAI.Domain.Interfaces;
using ExamAI.Infrastructure.Data;
using ExamAI.Infrastructure.Parsers;
using ExamAI.Infrastructure.Repositories;
using ExamAI.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

// ===================================================
// Configure Database (PostgreSQL + EF Core)
// ===================================================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ===================================================
// Configure Ollama (LLM Local)
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
        var client = new OllamaChatClient(new Uri(ollamaUrl), model);
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
// Configure Document Parsers
// ===================================================
builder.Services.AddScoped<PdfParser>();
builder.Services.AddScoped<WordParser>();
builder.Services.AddScoped<ExcelParser>();

// Registrar múltiplos parsers
builder.Services.AddScoped<IEnumerable<IDocumentParser>>(sp => new IDocumentParser[]
{
    sp.GetRequiredService<PdfParser>(),
    sp.GetRequiredService<WordParser>(),
    sp.GetRequiredService<ExcelParser>()
});

// Registrar Agents
builder.Services.AddScoped<DocumentParserAgent>();
builder.Services.AddScoped<ExtractionAgent>();
builder.Services.AddScoped<ValidationAgent>();
builder.Services.AddScoped<NormalizationAgent>();

// Registrar Pipeline
builder.Services.AddScoped<MedicalExamPipeline>();

// Registrar Repositories
builder.Services.AddScoped<ExamRepository>();

// Registrar Services
builder.Services.AddScoped<DocumentHashService>();

// ===================================================
// Configure HTTP Client Factory
// ===================================================
builder.Services.AddHttpClient();

// ===================================================
// Configure OpenAPI/Swagger
// ===================================================
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// ===================================================
// Configure HTTP Pipeline
// ===================================================
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

// ===================================================
// Health Checks
// ===================================================

// Health check geral
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
}))
.WithName("HealthCheck")
.WithTags("Health");

// Health check do Ollama
app.MapGet("/health/ollama", async (ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("Testing Ollama connection...");

        // Testar conexão HTTP direta com Ollama
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(5);

        var response = await httpClient.GetAsync("http://localhost:11434/api/tags");
        
        if (response.IsSuccessStatusCode)
        {
            logger.LogInformation("Ollama is healthy");
            return Results.Ok(new
            {
                status = "healthy",
                service = "Ollama",
                url = "http://localhost:11434",
                model = "llama3.1:8b",
                timestamp = DateTime.UtcNow
            });
        }
        else
        {
            logger.LogWarning("Ollama returned status code: {StatusCode}", response.StatusCode);
            return Results.Json(new
            {
                status = "unhealthy",
                error = $"Ollama returned status code: {response.StatusCode}"
            }, statusCode: 503);
        }
    }
    catch (HttpRequestException ex)
    {
        logger.LogError(ex, "Ollama connection failed");
        return Results.Json(new
        {
            status = "unhealthy",
            error = "Cannot connect to Ollama service. Is Ollama running?",
            details = ex.Message
        }, statusCode: 503);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Ollama health check failed");
        return Results.Json(new
        {
            status = "unhealthy",
            error = ex.Message
        }, statusCode: 500);
    }
})
.WithName("OllamaHealthCheck")
.WithTags("Health");

// Health check do PostgreSQL
app.MapGet("/health/database", async (AppDbContext dbContext, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("Testing database connection...");

        var canConnect = await dbContext.Database.CanConnectAsync();

        if (canConnect)
        {
            logger.LogInformation("Database is healthy");
            return Results.Ok(new
            {
                status = "healthy",
                database = "PostgreSQL",
                timestamp = DateTime.UtcNow
            });
        }
        else
        {
            logger.LogWarning("Cannot connect to database");
            return Results.Json(new
            {
                status = "unhealthy",
                error = "Cannot connect to database"
            }, statusCode: 503);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database health check failed");
        return Results.Json(new
        {
            status = "unhealthy",
            error = ex.Message
        }, statusCode: 500);
    }
})
.WithName("DatabaseHealthCheck")
.WithTags("Health");

// ===================================================
// Test Endpoints (Parsers)
// ===================================================

// Endpoint para testar DocumentParserAgent
app.MapPost("/test/parse-document", async (
    IFormFile file,
    DocumentParserAgent parserAgent,
    ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("Parsing document: {FileName} ({Size} bytes)", file.FileName, file.Length);

        using var stream = file.OpenReadStream();
        var extractedText = await parserAgent.ExtractTextAsync(stream, file.FileName);

        return Results.Ok(new
        {
            success = true,
            fileName = file.FileName,
            fileSize = file.Length,
            extractedChars = extractedText.Length,
            extractedText = extractedText,
            supportedFormats = parserAgent.GetSupportedFormats()
        });
    }
    catch (NotSupportedException ex)
    {
        logger.LogWarning(ex, "File format not supported: {FileName}", file.FileName);
        return Results.BadRequest(new
        {
            success = false,
            error = ex.Message,
            supportedFormats = parserAgent.GetSupportedFormats()
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to parse document: {FileName}", file.FileName);
        return Results.Json(new
        {
            success = false,
            error = ex.Message
        }, statusCode: 500);
    }
})
.WithName("TestDocumentParsing")
.WithTags("Testing")
.DisableAntiforgery();

// Endpoint para listar formatos suportados
app.MapGet("/test/supported-formats", (DocumentParserAgent parserAgent) =>
{
    var formats = parserAgent.GetSupportedFormats();
    return Results.Ok(new
    {
        supportedFormats = formats,
        count = formats.Count()
    });
})
.WithName("GetSupportedFormats")
.WithTags("Testing");

// Endpoint para testar extração com IA (parse + extract)
app.MapPost("/test/extract-full", async (
    IFormFile file,
    DocumentParserAgent parserAgent,
    ExtractionAgent extractionAgent,
    ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("Full extraction test: {FileName} ({Size} bytes)", file.FileName, file.Length);

        // Passo 1: Parse do documento
        using var stream = file.OpenReadStream();
        var extractedText = await parserAgent.ExtractTextAsync(stream, file.FileName);

        logger.LogInformation("Text extracted, now sending to LLM...");

        // Passo 2: Extração com IA
        var structuredData = await extractionAgent.ExtractAsync(extractedText);

        return Results.Ok(new
        {
            success = true,
            fileName = file.FileName,
            fileSize = file.Length,
            extractedTextChars = extractedText.Length,
            structuredData = structuredData
        });
    }
    catch (NotSupportedException ex)
    {
        logger.LogWarning(ex, "File format not supported: {FileName}", file.FileName);
        return Results.BadRequest(new
        {
            success = false,
            error = ex.Message
        });
    }
    catch (InvalidOperationException ex)
    {
        logger.LogError(ex, "LLM extraction failed: {FileName}", file.FileName);
        return Results.Json(new
        {
            success = false,
            error = ex.Message
        }, statusCode: 500);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error during full extraction: {FileName}", file.FileName);
        return Results.Json(new
        {
            success = false,
            error = ex.Message
        }, statusCode: 500);
    }
})
.WithName("TestFullExtraction")
.WithTags("Testing")
.DisableAntiforgery();

// Endpoint pipeline completo: parse + extract + validate + normalize
app.MapPost("/test/full-pipeline", async (
    IFormFile file,
    DocumentParserAgent parserAgent,
    ExtractionAgent extractionAgent,
    ValidationAgent validationAgent,
    NormalizationAgent normalizationAgent,
    ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("Full pipeline: {FileName} ({Size} bytes)", file.FileName, file.Length);

        // Passo 1: Parse
        using var stream = file.OpenReadStream();
        var extractedText = await parserAgent.ExtractTextAsync(stream, file.FileName);

        // Passo 2: Extract
        var structuredData = await extractionAgent.ExtractAsync(extractedText);

        // Passo 3: Validate
        var validationResult = validationAgent.Validate(structuredData);

        // Passo 4: Normalize
        var normalizedData = await normalizationAgent.NormalizeAsync(structuredData);

        return Results.Ok(new
        {
            success = true,
            fileName = file.FileName,
            fileSize = file.Length,
            extractedTextChars = extractedText.Length,
            structuredData = normalizedData,
            validation = new
            {
                isValid = validationResult.IsValid,
                warningCount = validationResult.Warnings.Count,
                warnings = validationResult.Warnings
            }
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error in full pipeline: {FileName}", file.FileName);
        return Results.Json(new
        {
            success = false,
            error = ex.Message
        }, statusCode: 500);
    }
})
.WithName("TestFullPipeline")
.WithTags("Testing")
.DisableAntiforgery();

// Endpoint completo: parse + extract + validate
app.MapPost("/test/extract-validate", async (
    IFormFile file,
    DocumentParserAgent parserAgent,
    ExtractionAgent extractionAgent,
    ValidationAgent validationAgent,
    ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("Full pipeline test: {FileName} ({Size} bytes)", file.FileName, file.Length);

        // Passo 1: Parse
        using var stream = file.OpenReadStream();
        var extractedText = await parserAgent.ExtractTextAsync(stream, file.FileName);

        // Passo 2: Extract
        var structuredData = await extractionAgent.ExtractAsync(extractedText);

        // Passo 3: Validate
        var validationResult = validationAgent.Validate(structuredData);

        return Results.Ok(new
        {
            success = true,
            fileName = file.FileName,
            fileSize = file.Length,
            extractedTextChars = extractedText.Length,
            structuredData = structuredData,
            validation = new
            {
                isValid = validationResult.IsValid,
                warningCount = validationResult.Warnings.Count,
                warnings = validationResult.Warnings
            }
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error in extract-validate pipeline: {FileName}", file.FileName);
        return Results.Json(new
        {
            success = false,
            error = ex.Message
        }, statusCode: 500);
    }
})
.WithName("TestExtractAndValidate")
.WithTags("Testing")
.DisableAntiforgery();

// Endpoint completo: processar + salvar no banco (com detecção de duplicatas)
app.MapPost("/api/process-and-save", async (
    IFormFile file,
    MedicalExamPipeline pipeline,
    ExamRepository repository,
    DocumentHashService hashService,
    ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("Processing and saving exam: {FileName} ({Size} bytes)", 
            file.FileName, file.Length);

        // 1. Calcular hash SHA256 do arquivo
        using var hashStream = file.OpenReadStream();
        var fileHash = await hashService.ComputeSha256Async(hashStream);
        
        logger.LogInformation("File hash: {Hash}", fileHash);

        // 2. Verificar se documento já foi processado (duplicata)
        var existingDocumento = await repository.FindDocumentoByHashAsync(fileHash);
        
        if (existingDocumento != null)
        {
            logger.LogInformation(
                "Duplicate document found! Hash: {Hash}, Documento ID: {DocumentoId}",
                fileHash,
                existingDocumento.Id);

            // Retornar resultado existente sem reprocessar
            return Results.Ok(new
            {
                success = true,
                duplicate = true,
                documentoId = existingDocumento.Id,
                pacienteId = existingDocumento.PacienteId,
                fileName = existingDocumento.NomeArquivo,
                message = "Document already processed. Returning cached result.",
                status = existingDocumento.StatusProcessamento,
                processedAt = existingDocumento.DataUpload,
                exames = existingDocumento.Exames.Select(e => new
                {
                    id = e.Id,
                    tipo = e.TipoExame.Nome,
                    dataColeta = e.DataColeta,
                    resultadosCount = e.Resultados.Count
                })
            });
        }

        // 3. Criar documento no banco
        var documento = new ExamAI.Domain.Entities.Documento
        {
            Id = Guid.NewGuid(),
            NomeArquivo = file.FileName,
            TipoArquivo = Path.GetExtension(file.FileName),
            TamanhoBytes = file.Length,
            HashSha256 = fileHash,
            StatusProcessamento = "processing",
            DataUpload = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ExamAI.Infrastructure.Data.AppDbContext>();
        dbContext.Documentos.Add(documento);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Created documento ID: {DocumentoId}", documento.Id);

        // 4. Processar documento
        using var stream = file.OpenReadStream();
        var result = await pipeline.ProcessAsync(stream, file.FileName);

        if (!result.Success)
        {
            // Atualizar status para failed
            documento.StatusProcessamento = "failed";
            documento.ErroProcessamento = result.ErrorMessage;
            await dbContext.SaveChangesAsync();

            return Results.Json(new
            {
                success = false,
                error = result.ErrorMessage,
                documentoId = documento.Id
            }, statusCode: 500);
        }

        // 5. Salvar resultado no banco
        var pacienteId = await repository.SaveExamAsync(result, documento.Id);

        logger.LogInformation("Saved exam result for paciente ID: {PacienteId}", pacienteId);

        return Results.Ok(new
        {
            success = true,
            duplicate = false,
            documentoId = documento.Id,
            pacienteId = pacienteId,
            fileName = result.FileName,
            fileHash = fileHash,
            data = result.Data,
            validation = new
            {
                isValid = result.Validation?.IsValid ?? true,
                warningCount = result.Validation?.Warnings.Count ?? 0,
                warnings = result.Validation?.Warnings
            },
            stats = new
            {
                duration = result.Stats.Duration.TotalMilliseconds,
                examesExtracted = result.Stats.ExamesExtracted,
                validationWarnings = result.Stats.ValidationWarnings
            }
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error processing and saving exam: {FileName}", file.FileName);
        return Results.Json(new
        {
            success = false,
            error = ex.Message
        }, statusCode: 500);
    }
})
.WithName("ProcessAndSaveExam")
.WithTags("Exams")
.DisableAntiforgery();

// Endpoint usando MedicalExamPipeline (recomendado)
app.MapPost("/api/process-exam", async (
    IFormFile file,
    MedicalExamPipeline pipeline,
    ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("Processing exam document: {FileName} ({Size} bytes)", 
            file.FileName, file.Length);

        using var stream = file.OpenReadStream();
        var result = await pipeline.ProcessAsync(stream, file.FileName);

        if (!result.Success)
        {
            return Results.Json(new
            {
                success = false,
                error = result.ErrorMessage,
                fileName = result.FileName,
                stats = result.Stats
            }, statusCode: 500);
        }

        return Results.Ok(new
        {
            success = true,
            fileName = result.FileName,
            fileSize = result.FileSize,
            data = result.Data,
            validation = new
            {
                isValid = result.Validation?.IsValid ?? true,
                warningCount = result.Validation?.Warnings.Count ?? 0,
                warnings = result.Validation?.Warnings
            },
            stats = new
            {
                duration = result.Stats.Duration.TotalMilliseconds,
                examesExtracted = result.Stats.ExamesExtracted,
                validationWarnings = result.Stats.ValidationWarnings,
                stepDurations = result.Stats.StepDurations.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.TotalMilliseconds)
            }
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error processing exam: {FileName}", file.FileName);
        return Results.Json(new
        {
            success = false,
            error = ex.Message
        }, statusCode: 500);
    }
})
.WithName("ProcessExamDocument")
.WithTags("Exams")
.DisableAntiforgery();

// Endpoint para buscar exames por CPF
app.MapGet("/api/exams/paciente/{cpf}", async (
    string cpf,
    ExamRepository repository,
    ILogger<Program> logger,
    DateTime? dataInicio = null,
    DateTime? dataFim = null,
    string? tipoExame = null) =>
{
    try
    {
        logger.LogInformation("Searching exames for CPF: {CPF}", cpf);

        var exames = await repository.GetExamsByPacienteAsync(
            cpf, dataInicio, dataFim, tipoExame);

        if (exames.Count == 0)
        {
            return Results.NotFound(new
            {
                success = false,
                message = "No exams found for this CPF"
            });
        }

        var paciente = exames.First().Documento.Paciente;

        return Results.Ok(new
        {
            success = true,
            paciente = new
            {
                id = paciente.Id,
                nome = paciente.Nome,
                cpf = paciente.Cpf,
                dataNascimento = paciente.DataNascimento
            },
            exames = exames.Select(e => new
            {
                id = e.Id,
                tipo = e.TipoExame.Nome,
                categoria = e.TipoExame.Categoria,
                dataColeta = e.DataColeta,
                medicoSolicitante = e.MedicoSolicitante,
                resultados = e.Resultados.Select(r => new
                {
                    parametro = r.Parametro,
                    valor = r.ValorNumerico,
                    unidade = r.Unidade,
                    referenciaMin = r.ReferenciaMin,
                    referenciaMax = r.ReferenciaMax,
                    status = r.Status,
                    observacoes = r.Observacoes
                })
            }),
            total = exames.Count
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error searching exams for CPF: {CPF}", cpf);
        return Results.Json(new
        {
            success = false,
            error = ex.Message
        }, statusCode: 500);
    }
})
.WithName("GetExamsByPaciente")
.WithTags("Exams");

// Endpoint para buscar exame específico
app.MapGet("/api/exams/{exameId:guid}", async (
    Guid exameId,
    ExamRepository repository,
    ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("Searching exame with ID: {ExameId}", exameId);

        var exame = await repository.GetExamByIdAsync(exameId);

        if (exame == null)
        {
            return Results.NotFound(new
            {
                success = false,
                message = "Exam not found"
            });
        }

        return Results.Ok(new
        {
            success = true,
            id = exame.Id,
            tipo = exame.TipoExame.Nome,
            categoria = exame.TipoExame.Categoria,
            dataColeta = exame.DataColeta,
            medicoSolicitante = exame.MedicoSolicitante,
            paciente = new
            {
                id = exame.Documento.Paciente.Id,
                nome = exame.Documento.Paciente.Nome,
                cpf = exame.Documento.Paciente.Cpf
            },
            resultados = exame.Resultados.Select(r => new
            {
                id = r.Id,
                parametro = r.Parametro,
                valor = r.ValorNumerico,
                unidade = r.Unidade,
                referenciaMin = r.ReferenciaMin,
                referenciaMax = r.ReferenciaMax,
                status = r.Status,
                observacoes = r.Observacoes
            })
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error searching exame with ID: {ExameId}", exameId);
        return Results.Json(new
        {
            success = false,
            error = ex.Message
        }, statusCode: 500);
    }
})
.WithName("GetExamById")
.WithTags("Exams");

// Endpoint para testar apenas a extração (texto → JSON)
app.MapPost("/test/extract-from-text", async (
    ExtractionAgent extractionAgent,
    ILogger<Program> logger,
    HttpRequest request) =>
{
    try
    {
        // Ler texto do body
        using var reader = new StreamReader(request.Body);
        var documentText = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(documentText))
        {
            return Results.BadRequest(new
            {
                success = false,
                error = "Request body is empty"
            });
        }

        logger.LogInformation("Extracting from text ({CharCount} chars)", documentText.Length);

        var structuredData = await extractionAgent.ExtractAsync(documentText);

        return Results.Ok(new
        {
            success = true,
            inputChars = documentText.Length,
            structuredData = structuredData
        });
    }
    catch (InvalidOperationException ex)
    {
        logger.LogError(ex, "LLM extraction failed");
        return Results.Json(new
        {
            success = false,
            error = ex.Message
        }, statusCode: 500);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error during extraction");
        return Results.Json(new
        {
            success = false,
            error = ex.Message
        }, statusCode: 500);
    }
})
.WithName("TestExtractionFromText")
.WithTags("Testing");

app.Run();
