namespace ExamAI.Application.DTOs;

/// <summary>
/// Resultado completo do processamento de um documento médico
/// </summary>
public class PipelineResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    
    // Informações do documento processado
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int ExtractedTextLength { get; set; }
    
    // Dados extraídos
    public ExamExtractionResult? Data { get; set; }
    
    // Validação
    public ValidationResult? Validation { get; set; }
    
    // Estatísticas do processamento
    public ProcessingStats Stats { get; set; } = new();
}

/// <summary>
/// Estatísticas de processamento do pipeline
/// </summary>
public class ProcessingStats
{
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan Duration => CompletedAt.HasValue 
        ? CompletedAt.Value - StartedAt 
        : TimeSpan.Zero;
    
    public int ExtractedExams { get; set; }
    public int NormalizedExams { get; set; }
    public int ValidationWarnings { get; set; }
    
    public Dictionary<string, TimeSpan> StepDurations { get; set; } = new();
}
