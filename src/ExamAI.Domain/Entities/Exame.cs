namespace ExamAI.Domain.Entities;

public class Exame
{
    public Guid Id { get; set; }
    public Guid DocumentoId { get; set; }
    public int? TipoExameId { get; set; }
    public DateTime? DataColeta { get; set; }
    public string? MedicoSolicitante { get; set; }
    public string? Laboratorio { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Documento Documento { get; set; } = null!;
    public TipoExame? TipoExame { get; set; }
    public ICollection<ResultadoExame> Resultados { get; set; } = new List<ResultadoExame>();
}
