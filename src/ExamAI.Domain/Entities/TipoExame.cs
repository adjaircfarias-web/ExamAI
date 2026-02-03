namespace ExamAI.Domain.Entities;

public class TipoExame
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string? Categoria { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public ICollection<Exame> Exames { get; set; } = new List<Exame>();
}
