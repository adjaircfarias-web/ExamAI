namespace ExamAI.Application.DTOs;

/// <summary>
/// Resultado completo da extração de um documento médico
/// </summary>
public class ExamExtractionResult
{
    public PatientInfo? Patient { get; set; }
    public List<ExamInfo> Exams { get; set; } = new();
}

/// <summary>
/// Informações do paciente extraídas do documento
/// </summary>
public class PatientInfo
{
    public string? Name { get; set; }
    public string? Cpf { get; set; }
    public string? BirthDate { get; set; }
    public string? CollectionDate { get; set; }
    public string? RequestingPhysician { get; set; }
}

/// <summary>
/// Informações de um exame extraído
/// </summary>
public class ExamInfo
{
    public string Type { get; set; } = string.Empty;
    public decimal? Value { get; set; }
    public string? Unit { get; set; }
    public decimal? ReferenceMin { get; set; }
    public decimal? ReferenceMax { get; set; }
    public string? Status { get; set; }
    public string? Observations { get; set; }
}
