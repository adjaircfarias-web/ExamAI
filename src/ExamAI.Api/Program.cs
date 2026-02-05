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
builder.Services.AddHttpClient();

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
                model = "llama3.1:70b",
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

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ExamAI.Infrastructure.Data.AppDbContext>();

        // 3. Criar documento no banco (sem paciente inicialmente)
        var documento = new ExamAI.Domain.Entities.Documento
        {
            Id = Guid.NewGuid(),
            NomeArquivo = file.FileName,
            TipoArquivo = Path.GetExtension(file.FileName),
            TamanhoBytes = file.Length,
            HashSha256 = fileHash,
            StatusProcessamento = "processing",
            DataUpload = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            PacienteId = null // Será definido após processamento
        };

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

        // 5. Salvar resultado no banco (cria/busca paciente internamente)
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

// Reprocessar Documento Falhado
app.MapPost("/api/exams/reprocess/{documentoId}", async (
    Guid documentoId,
    MedicalExamPipeline pipeline,
    ExamRepository repository,
    ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("Reprocessing documento: {DocumentoId}", documentoId);

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ExamAI.Infrastructure.Data.AppDbContext>();

        // Buscar documento
        var documento = await dbContext.Documentos
            .Include(d => d.Paciente)
            .FirstOrDefaultAsync(d => d.Id == documentoId);

        if (documento == null)
        {
            return Results.NotFound(new { success = false, error = "Document not found" });
        }

        logger.LogInformation("Found documento: {FileName}, Current status: {Status}", 
            documento.NomeArquivo, documento.StatusProcessamento);

        // Atualizar status para processing
        documento.StatusProcessamento = "processing";
        documento.ErroProcessamento = null;
        await dbContext.SaveChangesAsync();

        // Nota: Como não temos o arquivo original em disco, 
        // não podemos reprocessar completamente
        // Esta é uma limitação - precisaria armazenar o arquivo em disco ou blob storage

        return Results.Ok(new
        {
            success = false,
            error = "Cannot reprocess: original file not stored. Please re-upload the document.",
            documentoId = documento.Id,
            fileName = documento.NomeArquivo,
            suggestion = "Use DELETE /api/exams/{documentoId} to remove failed document, then upload again"
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error reprocessing documento: {DocumentoId}", documentoId);
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
app.MapDelete("/api/exams/{documentoId}", async (
    Guid documentoId,
    ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("Deleting documento: {DocumentoId}", documentoId);

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ExamAI.Infrastructure.Data.AppDbContext>();

        var documento = await dbContext.Documentos
            .Include(d => d.Exames)
                .ThenInclude(e => e.Resultados)
            .FirstOrDefaultAsync(d => d.Id == documentoId);

        if (documento == null)
        {
            return Results.NotFound(new { success = false, error = "Document not found" });
        }

        // Deletar em cascata (devido ao relacionamento configurado)
        dbContext.Documentos.Remove(documento);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Deleted documento: {DocumentoId}, File: {FileName}", 
            documentoId, documento.NomeArquivo);

        return Results.Ok(new
        {
            success = true,
            message = "Document deleted successfully",
            documentoId = documentoId,
            fileName = documento.NomeArquivo
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error deleting documento: {DocumentoId}", documentoId);
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
