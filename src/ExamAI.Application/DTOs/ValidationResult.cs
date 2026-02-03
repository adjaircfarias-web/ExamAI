namespace ExamAI.Application.DTOs;

/// <summary>
/// Resultado da validação de dados extraídos
/// </summary>
public class ValidationResult
{
    public bool IsValid => Warnings.Count == 0;
    public List<ValidationWarning> Warnings { get; set; } = new();

    public void AddWarning(string field, string message, string? currentValue = null)
    {
        Warnings.Add(new ValidationWarning
        {
            Field = field,
            Message = message,
            CurrentValue = currentValue
        });
    }
}

/// <summary>
/// Aviso de validação (não bloqueia o processo)
/// </summary>
public class ValidationWarning
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? CurrentValue { get; set; }
}
