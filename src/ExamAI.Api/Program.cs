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
    var model = config["Ollama:Model"] ?? "llama3.1:70b";

    logger.LogInformation("Configuring Ollama client: {Url}, Model: {Model}", ollamaUrl, model);

    // Ollama client não valida conexão no construtor - apenas cria o cliente
    var client = new OllamaChatClient(new Uri(ollamaUrl), model);
    logger.LogInformation("Ollama client configured successfully");
    return client;
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
builder.Services.AddHttpClient("OllamaClient")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler())
    .ConfigureHttpClient((sp, client) =>
    {
        client.Timeout = TimeSpan.FromSeconds(300); // 5 minutos
    });

// Configurar HttpClient default também
builder.Services.AddHttpClient("DefaultClient")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler())
    .ConfigureHttpClient((sp, client) =>
    {
        client.Timeout = TimeSpan.FromSeconds(300); // 5 minutos
    });

// ===================================================
// Configure CORS
// ===================================================
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ===================================================
// Configure OpenAPI/Swagger
// ===================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

var app = builder.Build();

// ===================================================
// Configure HTTP Pipeline
// ===================================================
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Medical Exam Extractor API v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "ExamAI API Documentation";
});

// CORS deve vir ANTES de UseHttpsRedirection
app.UseCors();

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
app.MapGet("/health/ollama", async (ILogger<Program> logger, IConfiguration config) =>
{
    try
    {
        var ollamaUrl = config["Ollama:Url"] ?? "http://localhost:11434";
        var ollamaModel = config["Ollama:Model"] ?? "llama3.1:70b";
        
        logger.LogInformation("Testing Ollama connection at {Url}...", ollamaUrl);

        // Testar conexão HTTP direta com Ollama
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(5);

        var response = await httpClient.GetAsync($"{ollamaUrl}/api/tags");
        
        if (response.IsSuccessStatusCode)
        {
            logger.LogInformation("Ollama is healthy");
            return Results.Ok(new
            {
                status = "healthy",
                service = "Ollama",
                url = ollamaUrl,
                model = ollamaModel,
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
// Production Endpoints
// ===================================================

// Endpoint completo: processar + salvar no banco (com detecção de duplicatas)
app.MapPost("/api/process-and-save", async (
    IFormFile file,
    MedicalExamPipeline pipeline,
    ExamRepository repository,
    DocumentHashService hashService,
    AppDbContext dbContext,
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
        var existingDocument = await repository.FindDocumentByHashAsync(fileHash);
        
        if (existingDocument != null)
        {
            logger.LogInformation(
                "Duplicate document found! Hash: {Hash}, Document ID: {DocumentId}",
                fileHash,
                existingDocument.Id);

            // Retornar resultado existente sem reprocessar
            return Results.Ok(new
            {
                success = true,
                duplicate = true,
                documentId = existingDocument.Id,
                patientId = existingDocument.PatientId,
                fileName = existingDocument.FileName,
                message = "Document already processed. Returning cached result.",
                status = existingDocument.ProcessingStatus,
                processedAt = existingDocument.UploadDate,
                exams = existingDocument.Exams.Select(e => new
                {
                    id = e.Id,
                    type = e.ExamType.Name,
                    collectionDate = e.CollectionDate,
                    resultsCount = e.Results.Count
                })
            });
        }

        // 3. Criar documento no banco (sem paciente inicialmente)
        var document = new ExamAI.Domain.Entities.Document
        {
            Id = Guid.NewGuid(),
            FileName = file.FileName,
            FileType = Path.GetExtension(file.FileName),
            SizeBytes = file.Length,
            HashSha256 = fileHash,
            ProcessingStatus = "processing",
            UploadDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            PatientId = null // Será definido após processamento
        };

        dbContext.Documents.Add(document);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Created document ID: {DocumentId}", document.Id);

        // 4. Processar documento
        using var stream = file.OpenReadStream();
        var result = await pipeline.ProcessAsync(stream, file.FileName);

        if (!result.Success)
        {
            // Atualizar status para failed
            document.ProcessingStatus = "failed";
            document.ProcessingError = result.ErrorMessage;
            await dbContext.SaveChangesAsync();

            return Results.Json(new
            {
                success = false,
                error = result.ErrorMessage,
                documentId = document.Id
            }, statusCode: 500);
        }

        // 5. Salvar resultado no banco (cria/busca paciente internamente)
        var patientId = await repository.SaveExamAsync(result, document.Id);

        logger.LogInformation("Saved exam result for patient ID: {PatientId}", patientId);

        return Results.Ok(new
        {
            success = true,
            duplicate = false,
            documentId = document.Id,
            patientId = patientId,
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
                examsExtracted = result.Stats.ExtractedExams,
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

// GET /api/exams - Listar todos os exames
app.MapGet("/api/exams", async (
    AppDbContext dbContext,
    ILogger<Program> logger,
    int? page = 1,
    int? pageSize = 20,
    string? patientName = null) =>
{
    try
    {
        logger.LogInformation("Listing exams - Page: {Page}, PageSize: {PageSize}, Patient: {Patient}",
            page, pageSize, patientName);

        // Limitar pageSize maximo a 100
        var effectivePageSize = Math.Min(pageSize ?? 20, 100);

        // Base query
        var query = dbContext.Exams
            .AsNoTracking()
            .Include(e => e.Document)
                .ThenInclude(d => d.Patient)
            .Include(e => e.ExamType)
            .Include(e => e.Results)
            .AsQueryable();

        // Aplicar filtro por nome do paciente (parcial)
        if (!string.IsNullOrWhiteSpace(patientName))
        {
            query = query.Where(e => e.Document.Patient != null &&
                EF.Functions.ILike(e.Document.Patient.Name, $"%{patientName}%"));
        }

        // Contagem total
        var total = await query.CountAsync();

        // Ordenar por data de upload e aplicar paginacao
        var exams = await query
            .OrderByDescending(e => e.Document.UploadDate)
            .ThenByDescending(e => e.CreatedAt)
            .Skip((page.Value - 1) * effectivePageSize)
            .Take(effectivePageSize)
            .ToListAsync();

        // Mapear para DTO
        var examDtos = exams.Select(e => new
        {
            examId = e.Id,
            examType = e.ExamType?.Name ?? "Tipo nao identificado",
            category = e.ExamType?.Category ?? "Sem categoria",
            collectionDate = e.CollectionDate,
            requestingPhysician = e.RequestingPhysician,
            laboratory = e.Laboratory,
            uploadDate = e.Document.UploadDate,
            processingStatus = e.Document.ProcessingStatus,
            patient = e.Document.Patient == null ? null : new
            {
                id = e.Document.Patient.Id,
                name = e.Document.Patient.Name,
                cpf = e.Document.Patient.Cpf,
                birthDate = e.Document.Patient.BirthDate
            },
            document = new
            {
                id = e.Document.Id,
                fileName = e.Document.FileName
            },
            results = e.Results.Select(r => new
            {
                parameter = r.Parameter,
                value = r.NumericValue?.ToString() ?? r.TextValue,
                unit = r.Unit,
                referenceRange = r.ReferenceMin.HasValue && r.ReferenceMax.HasValue
                    ? $"{r.ReferenceMin}-{r.ReferenceMax}"
                    : null,
                status = r.Status
            }).ToList(),
            resultsCount = e.Results.Count
        }).ToList();

        var totalPages = (int)Math.Ceiling(total / (double)effectivePageSize);

        return Results.Ok(new
        {
            success = true,
            data = examDtos,
            pagination = new
            {
                page = page.Value,
                pageSize = effectivePageSize,
                total,
                totalPages
            }
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error listing exams");
        return Results.Json(new
        {
            success = false,
            error = "Erro ao listar exames",
            details = ex.Message
        }, statusCode: 500);
    }
})
.WithName("GetAllExams")
.WithTags("Exams")
.WithOpenApi(operation => new(operation)
{
    Summary = "Listar todos os exames",
    Description = "Retorna lista paginada de exames com dados do paciente e resultados"
});

// Endpoint para buscar exames por CPF
app.MapGet("/api/exams/patient/{cpf}", async (
    string cpf,
    ExamRepository repository,
    ILogger<Program> logger,
    DateTime? startDate = null,
    DateTime? endDate = null,
    string? examType = null) =>
{
    try
    {
        logger.LogInformation("Searching exams for CPF: {CPF}", cpf);

        var exams = await repository.GetExamsByPatientAsync(
            cpf, startDate, endDate, examType);

        if (exams.Count == 0)
        {
            return Results.NotFound(new
            {
                success = false,
                message = "No exams found for this CPF"
            });
        }

        var patient = exams.First().Document.Patient;

        return Results.Ok(new
        {
            success = true,
            patient = new
            {
                id = patient.Id,
                name = patient.Name,
                cpf = patient.Cpf,
                birthDate = patient.BirthDate
            },
            exams = exams.Select(e => new
            {
                id = e.Id,
                type = e.ExamType.Name,
                category = e.ExamType.Category,
                collectionDate = e.CollectionDate,
                requestingPhysician = e.RequestingPhysician,
                results = e.Results.Select(r => new
                {
                    parameter = r.Parameter,
                    value = r.NumericValue,
                    unit = r.Unit,
                    referenceMin = r.ReferenceMin,
                    referenceMax = r.ReferenceMax,
                    status = r.Status,
                    observations = r.Observations
                })
            }),
            total = exams.Count
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
.WithName("GetExamsByPatient")
.WithTags("Exams");

// Reprocessar Documento Falhado
app.MapPost("/api/exams/reprocess/{documentId}", async (
    Guid documentId,
    MedicalExamPipeline pipeline,
    ExamRepository repository,
    ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("Reprocessing document: {DocumentId}", documentId);

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ExamAI.Infrastructure.Data.AppDbContext>();

        // Buscar documento
        var document = await dbContext.Documents
            .Include(d => d.Patient)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null)
        {
            return Results.NotFound(new { success = false, error = "Document not found" });
        }

        logger.LogInformation("Found document: {FileName}, Current status: {Status}", 
            document.FileName, document.ProcessingStatus);

        // Atualizar status para processing
        document.ProcessingStatus = "processing";
        document.ProcessingError = null;
        await dbContext.SaveChangesAsync();

        // Nota: Como não temos o arquivo original em disco, 
        // não podemos reprocessar completamente
        // Esta é uma limitação - precisaria armazenar o arquivo em disco ou blob storage

        return Results.Ok(new
        {
            success = false,
            error = "Cannot reprocess: original file not stored. Please re-upload the document.",
            documentId = document.Id,
            fileName = document.FileName,
            suggestion = "Use DELETE /api/exams/{documentId} to remove failed document, then upload again"
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error reprocessing document: {DocumentId}", documentId);
        return Results.Json(new
        {
            success = false,
            error = ex.Message
        }, statusCode: 500);
    }
})
.WithName("ReprocessDocument")
.WithTags("Exams");

// Deletar Documento
app.MapDelete("/api/exams/{documentId}", async (
    Guid documentId,
    ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("Deleting document: {DocumentId}", documentId);

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ExamAI.Infrastructure.Data.AppDbContext>();

        var document = await dbContext.Documents
            .Include(d => d.Exams)
                .ThenInclude(e => e.Results)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null)
        {
            return Results.NotFound(new { success = false, error = "Document not found" });
        }

        // Deletar em cascata (devido ao relacionamento configurado)
        dbContext.Documents.Remove(document);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Deleted document: {DocumentId}, File: {FileName}", 
            documentId, document.FileName);

        return Results.Ok(new
        {
            success = true,
            message = "Document deleted successfully",
            documentId = documentId,
            fileName = document.FileName
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error deleting document: {DocumentId}", documentId);
        return Results.Json(new
        {
            success = false,
            error = ex.Message
        }, statusCode: 500);
    }
})
.WithName("DeleteDocument")
.WithTags("Exams");

app.Run();
