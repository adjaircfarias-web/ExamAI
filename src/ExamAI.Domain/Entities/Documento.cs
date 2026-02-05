namespace ExamAI.Domain.Entities;

public class Documento
{
    public Guid Id { get; set; }
    public Guid? PacienteId { get; set; }
    public string NomeArquivo { get; set; } = string.Empty;
    public string TipoArquivo { get; set; } = string.Empty;
    public long TamanhoBytes { get; set; }
    public string HashSha256 { get; set; } = string.Empty;
    public DateTime DataUpload { get; set; }
    public string StatusProcessamento { get; set; } = "pending"; // pending, processing, completed, failed
    public string? ErroProcessamento { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Paciente? Paciente { get; set; }
    public ICollection<Exame> Exames { get; set; } = new List<Exame>();
}
