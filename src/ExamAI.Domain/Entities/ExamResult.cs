namespace ExamAI.Domain.Entities;

public class ExamResult
{
    public Guid Id { get; set; }
    public Guid ExamId { get; set; }
    public string Parameter { get; set; } = string.Empty;
    public decimal? NumericValue { get; set; }
    public string? TextValue { get; set; }
    public string? Unit { get; set; }
    public decimal? ReferenceMin { get; set; }
    public decimal? ReferenceMax { get; set; }
    public string? Status { get; set; } // normal, baixo, alto, crítico
    public string? Observations { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Exam Exam { get; set; } = null!;
}
