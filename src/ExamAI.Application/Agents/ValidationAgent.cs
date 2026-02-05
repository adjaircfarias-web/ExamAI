using System.Text.RegularExpressions;
using ExamAI.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace ExamAI.Application.Agents;

/// <summary>
/// Agent responsável por validar dados extraídos do LLM
/// </summary>
public class ValidationAgent
{
    private readonly ILogger<ValidationAgent> _logger;
    private static readonly string[] ValidStatuses = { "normal", "low", "high", "critical" };

    public ValidationAgent(ILogger<ValidationAgent> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Valida os dados extraídos e retorna lista de warnings
    /// </summary>
    public ValidationResult Validate(ExamExtractionResult extractionResult)
    {
        if (extractionResult == null)
            throw new ArgumentNullException(nameof(extractionResult));

        var result = new ValidationResult();

        _logger.LogInformation("Starting validation of extraction result");

        // Validar dados do paciente
        if (extractionResult.Patient != null)
        {
            ValidatePatient(extractionResult.Patient, result);
        }
        else
        {
            result.AddWarning("patient", "No patient information was extracted");
        }

        // Validar exames
        if (extractionResult.Exams == null || extractionResult.Exams.Count == 0)
        {
            result.AddWarning("exams", "No exams were extracted from the document");
        }
        else
        {
            for (int i = 0; i < extractionResult.Exams.Count; i++)
            {
                ValidateExam(extractionResult.Exams[i], i, result);
            }
        }

        _logger.LogInformation(
            "Validation completed: {WarningCount} warnings found",
            result.Warnings.Count);

        if (result.Warnings.Count > 0)
        {
            foreach (var warning in result.Warnings)
            {
                _logger.LogWarning(
                    "Validation warning - Field: {Field}, Message: {Message}, Value: {Value}",
                    warning.Field,
                    warning.Message,
                    warning.CurrentValue ?? "null");
            }
        }

        return result;
    }

    private void ValidatePatient(PatientInfo patient, ValidationResult result)
    {
        // Validar nome
        if (string.IsNullOrWhiteSpace(patient.Name))
        {
            result.AddWarning("patient.name", "Patient name is empty");
        }
        else if (patient.Name.Length < 3)
        {
            result.AddWarning("patient.name", "Patient name is too short", patient.Name);
        }

        // Validar data de nascimento (formato)
        if (!string.IsNullOrWhiteSpace(patient.BirthDate))
        {
            if (!IsValidDate(patient.BirthDate))
            {
                result.AddWarning(
                    "patient.birth_date",
                    "Birth date in invalid format (expected: YYYY-MM-DD)",
                    patient.BirthDate);
            }
        }

        // Validar data de coleta (formato)
        if (!string.IsNullOrWhiteSpace(patient.CollectionDate))
        {
            if (!IsValidDate(patient.CollectionDate))
            {
                result.AddWarning(
                    "patient.collection_date",
                    "Collection date in invalid format (expected: YYYY-MM-DD)",
                    patient.CollectionDate);
            }
        }
        else
        {
            result.AddWarning("patient.collection_date", "Collection date was not provided");
        }

        // Validar médico solicitante
        if (string.IsNullOrWhiteSpace(patient.RequestingPhysician))
        {
            result.AddWarning("patient.requesting_physician", "Requesting physician was not provided");
        }
    }

    private void ValidateExam(ExamInfo exam, int index, ValidationResult result)
    {
        var prefix = $"exams[{index}]";

        // Validar tipo do exame
        if (string.IsNullOrWhiteSpace(exam.Type))
        {
            result.AddWarning($"{prefix}.type", "Exam type is empty");
        }
        else if (exam.Type.Length < 3)
        {
            result.AddWarning($"{prefix}.type", "Exam type is too short", exam.Type);
        }

        // Validar valor numérico
        if (exam.Value.HasValue)
        {
            if (exam.Value.Value < 0)
            {
                result.AddWarning(
                    $"{prefix}.value",
                    "Negative numeric value may be invalid",
                    exam.Value.Value.ToString());
            }

            if (exam.Value.Value > 1000000)
            {
                result.AddWarning(
                    $"{prefix}.value",
                    "Very high numeric value may be an extraction error",
                    exam.Value.Value.ToString());
            }
        }
        else
        {
            result.AddWarning($"{prefix}.value", "Exam value was not provided");
        }

        // Validar unidade
        if (string.IsNullOrWhiteSpace(exam.Unit))
        {
            result.AddWarning($"{prefix}.unit", "Measurement unit was not provided");
        }

        // Validar referências (se uma existe, a outra deveria existir)
        if (exam.ReferenceMin.HasValue && !exam.ReferenceMax.HasValue)
        {
            result.AddWarning(
                $"{prefix}.reference",
                "Minimum reference provided but maximum is missing");
        }

        if (!exam.ReferenceMin.HasValue && exam.ReferenceMax.HasValue)
        {
            result.AddWarning(
                $"{prefix}.reference",
                "Maximum reference provided but minimum is missing");
        }

        // Validar lógica das referências
        if (exam.ReferenceMin.HasValue && exam.ReferenceMax.HasValue)
        {
            if (exam.ReferenceMin.Value > exam.ReferenceMax.Value)
            {
                result.AddWarning(
                    $"{prefix}.reference",
                    "Minimum reference is greater than maximum",
                    $"min: {exam.ReferenceMin}, max: {exam.ReferenceMax}");
            }
        }

        // Validar status
        if (!string.IsNullOrWhiteSpace(exam.Status))
        {
            var normalizedStatus = exam.Status.ToLower().Trim();
            if (!ValidStatuses.Contains(normalizedStatus))
            {
                result.AddWarning(
                    $"{prefix}.status",
                    $"Invalid status (allowed: {string.Join(", ", ValidStatuses)})",
                    exam.Status);
            }
        }

        // Validar consistência: status vs valor vs referência
        if (exam.Value.HasValue && 
            exam.ReferenceMin.HasValue && 
            exam.ReferenceMax.HasValue &&
            !string.IsNullOrWhiteSpace(exam.Status))
        {
            var normalizedStatus = exam.Status.ToLower().Trim();
            var isInRange = exam.Value.Value >= exam.ReferenceMin.Value && 
                           exam.Value.Value <= exam.ReferenceMax.Value;

            if (isInRange && normalizedStatus != "normal")
            {
                result.AddWarning(
                    $"{prefix}.status",
                    "Value is within reference but status is not 'normal'",
                    $"value: {exam.Value}, status: {exam.Status}");
            }

            if (!isInRange && normalizedStatus == "normal")
            {
                result.AddWarning(
                    $"{prefix}.status",
                    "Value is outside reference but status is 'normal'",
                    $"value: {exam.Value}, status: {exam.Status}");
            }
        }
    }

    /// <summary>
    /// Valida formato de data YYYY-MM-DD
    /// </summary>
    private bool IsValidDate(string date)
    {
        if (string.IsNullOrWhiteSpace(date))
            return false;

        // Formato: YYYY-MM-DD
        var dateRegex = new Regex(@"^\d{4}-\d{2}-\d{2}$");
        if (!dateRegex.IsMatch(date))
            return false;

        // Tentar parsear
        return DateTime.TryParse(date, out _);
    }
}
