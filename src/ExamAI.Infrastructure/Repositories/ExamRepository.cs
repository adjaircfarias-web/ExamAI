using ExamAI.Application.DTOs;
using ExamAI.Domain.Entities;
using ExamAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExamAI.Infrastructure.Repositories;

/// <summary>
/// Repositório para persistência de exames médicos
/// </summary>
public class ExamRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<ExamRepository> _logger;

    public ExamRepository(
        AppDbContext context,
        ILogger<ExamRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Salva o resultado de um exame processado (transação atômica)
    /// </summary>
    public async Task<Guid> SaveExamAsync(
        PipelineResult pipelineResult,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        if (pipelineResult == null)
            throw new ArgumentNullException(nameof(pipelineResult));

        if (pipelineResult.Data == null)
            throw new ArgumentException("PipelineResult.Data cannot be null", nameof(pipelineResult));

        _logger.LogInformation("Saving exam result for document ID: {DocumentId}", documentId);

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Buscar ou criar paciente
            var patient = await GetOrCreatePatientAsync(
                pipelineResult.Data.Patient,
                cancellationToken);

            _logger.LogDebug("Patient ID: {PatientId}", patient.Id);

            // 2. Atualizar documento com patient_id
            var document = await _context.Documents.FindAsync(new object[] { documentId }, cancellationToken);
            if (document == null)
            {
                throw new InvalidOperationException($"Document ID {documentId} not found");
            }

            document.PatientId = patient.Id;
            document.ProcessingStatus = "completed";

            // 3. Salvar exames
            var examIds = new List<Guid>();

            if (pipelineResult.Data.Exams != null && pipelineResult.Data.Exams.Count > 0)
            {
                foreach (var examInfo in pipelineResult.Data.Exams)
                {
                    // Buscar ou criar tipo de exame
                    var examType = await GetOrCreateExamTypeAsync(
                        examInfo.Type,
                        cancellationToken);

                    // Criar exame
                    var exam = new Exam
                    {
                        Id = Guid.NewGuid(),
                        DocumentId = documentId,
                        ExamTypeId = examType.Id,
                        CollectionDate = ParseDateOrDefault(pipelineResult.Data.Patient?.CollectionDate),
                        RequestingPhysician = pipelineResult.Data.Patient?.RequestingPhysician
                    };

                    _context.Exams.Add(exam);
                    examIds.Add(exam.Id);

                    // Criar resultado do exame
                    var result = new ExamResult
                    {
                        Id = Guid.NewGuid(),
                        ExamId = exam.Id,
                        Parameter = examInfo.Type,
                        NumericValue = examInfo.Value,
                        Unit = examInfo.Unit,
                        ReferenceMin = examInfo.ReferenceMin,
                        ReferenceMax = examInfo.ReferenceMax,
                        Status = examInfo.Status,
                        Observations = examInfo.Observations
                    };

                    _context.ExamResults.Add(result);

                    _logger.LogDebug(
                        "Created exam: {Type}, value: {Value} {Unit}",
                        examInfo.Type,
                        examInfo.Value,
                        examInfo.Unit);
                }
            }

            // 4. Salvar tudo
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully saved {ExamCount} exams for document {DocumentId}",
                examIds.Count,
                documentId);

            return patient.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save exam result for document {DocumentId}", documentId);
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Busca exames de um paciente por CPF
    /// </summary>
    public async Task<List<Exam>> GetExamsByPatientAsync(
        string cpf,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? examType = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            throw new ArgumentException("CPF cannot be empty", nameof(cpf));

        _logger.LogInformation("Searching exams for CPF: {CPF}", cpf);

        // Remover formatação do CPF
        cpf = cpf.Replace(".", "").Replace("-", "").Trim();

        var query = _context.Exams
            .Include(e => e.Document)
                .ThenInclude(d => d.Patient)
            .Include(e => e.ExamType)
            .Include(e => e.Results)
            .Where(e => e.Document.Patient != null && e.Document.Patient.Cpf == cpf);

        // Filtros opcionais
        if (startDate.HasValue)
        {
            query = query.Where(e => e.CollectionDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(e => e.CollectionDate <= endDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(examType))
        {
            query = query.Where(e => e.ExamType.Name.Contains(examType));
        }

        var exams = await query
            .OrderByDescending(e => e.CollectionDate)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {ExamCount} exams for CPF: {CPF}", exams.Count, cpf);

        return exams;
    }

    /// <summary>
    /// Verifica se um documento com o hash já existe
    /// </summary>
    public async Task<Document?> FindDocumentByHashAsync(
        string hashSha256,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(hashSha256))
            throw new ArgumentException("Hash cannot be empty", nameof(hashSha256));

        _logger.LogDebug("Searching for document with hash: {Hash}", hashSha256);

        var document = await _context.Documents
            .Include(d => d.Patient)
            .Include(d => d.Exams)
                .ThenInclude(e => e.ExamType)
            .Include(d => d.Exams)
                .ThenInclude(e => e.Results)
            .FirstOrDefaultAsync(d => d.HashSha256 == hashSha256, cancellationToken);

        if (document != null)
        {
            _logger.LogInformation(
                "Found existing document with hash {Hash}: ID {DocumentId}, Status {Status}",
                hashSha256,
                document.Id,
                document.ProcessingStatus);
        }

        return document;
    }

    /// <summary>
    /// Busca ou cria um paciente
    /// </summary>
    private async Task<Patient> GetOrCreatePatientAsync(
        PatientInfo? patientInfo,
        CancellationToken cancellationToken)
    {
        // Define nome padrão se não identificado
        var patientName = string.IsNullOrWhiteSpace(patientInfo?.Name) 
            ? "Unidentified patient" 
            : patientInfo.Name;

        // Tentar buscar paciente existente por nome (simplificado)
        // Em produção, seria melhor usar CPF se disponível
        var existingPatient = await _context.Patients
            .FirstOrDefaultAsync(
                p => p.Name != null && p.Name == patientName,
                cancellationToken);

        if (existingPatient != null)
        {
            _logger.LogDebug("Found existing patient: {Name}", patientName);
            return existingPatient;
        }

        // Criar novo paciente
        var newPatient = new Patient
        {
            Id = Guid.NewGuid(),
            Name = patientName,
            Cpf = null, // CPF não identificado ou não extraído
            BirthDate = ParseDateOrDefault(patientInfo?.BirthDate)
        };

        _context.Patients.Add(newPatient);

        _logger.LogDebug("Created new patient: {Name}", patientName);

        return newPatient;
    }

    /// <summary>
    /// Busca ou cria um tipo de exame
    /// </summary>
    private async Task<ExamType> GetOrCreateExamTypeAsync(
        string examName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(examName))
        {
            throw new ArgumentException("Exam name cannot be empty", nameof(examName));
        }

        // Buscar tipo existente (match exato ou parcial)
        var existingType = await _context.ExamTypes
            .FirstOrDefaultAsync(
                t => t.Name == examName || t.Name.Contains(examName),
                cancellationToken);

        if (existingType != null)
        {
            _logger.LogDebug("Found existing exam type: {Name}", existingType.Name);
            return existingType;
        }

        // Criar novo tipo de exame (categoria genérica)
        var newType = new ExamType
        {
            Name = examName,
            Category = "Others", // Categoria padrão para exames não mapeados
            CreatedAt = DateTime.UtcNow
        };

        _context.ExamTypes.Add(newType);
        
        // Salvar IMEDIATAMENTE para garantir que o ID existe antes de criar Exam
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Created new exam type: {Name} with ID: {Id}", examName, newType.Id);

        return newType;
    }

    /// <summary>
    /// Parse de data no formato YYYY-MM-DD
    /// </summary>
    private DateTime ParseDateOrDefault(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
        {
            return DateTime.UtcNow;
        }

        if (DateTime.TryParse(dateString, out var date))
        {
            // Garantir que a data seja UTC para PostgreSQL
            return DateTime.SpecifyKind(date, DateTimeKind.Utc);
        }

        _logger.LogWarning("Failed to parse date: {DateString}, using current date", dateString);
        return DateTime.UtcNow;
    }
}
