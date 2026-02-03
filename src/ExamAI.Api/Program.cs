using ExamAI.Application.Agents;
using ExamAI.Domain.Interfaces;
using ExamAI.Infrastructure.Data;
using ExamAI.Infrastructure.Parsers;
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

// Registrar DocumentParserAgent (orquestrador)
builder.Services.AddScoped<DocumentParserAgent>();

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

app.Run();
