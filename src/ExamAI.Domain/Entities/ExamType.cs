namespace ExamAI.Domain.Entities;

public class ExamType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public ICollection<Exam> Exams { get; set; } = new List<Exam>();
}
