namespace ExamAI.Domain.Entities;

public class Exam
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int? ExamTypeId { get; set; }
    public DateTime? CollectionDate { get; set; }
    public string? RequestingPhysician { get; set; }
    public string? Laboratory { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Document Document { get; set; } = null!;
    public ExamType? ExamType { get; set; }
    public ICollection<ExamResult> Results { get; set; } = new List<ExamResult>();
}
