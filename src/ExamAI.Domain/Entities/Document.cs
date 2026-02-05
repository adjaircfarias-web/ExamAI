namespace ExamAI.Domain.Entities;

public class Document
{
    public Guid Id { get; set; }
    public Guid? PatientId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string HashSha256 { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; }
    public string ProcessingStatus { get; set; } = "pending"; // pending, processing, completed, failed
    public string? ProcessingError { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Patient? Patient { get; set; }
    public ICollection<Exam> Exams { get; set; } = new List<Exam>();
}
