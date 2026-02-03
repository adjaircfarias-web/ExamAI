namespace ExamAI.Domain.Entities;

public class ResultadoExame
{
    public Guid Id { get; set; }
    public Guid ExameId { get; set; }
    public string Parametro { get; set; } = string.Empty;
    public decimal? ValorNumerico { get; set; }
    public string? ValorTexto { get; set; }
    public string? Unidade { get; set; }
    public decimal? ReferenciaMin { get; set; }
    public decimal? ReferenciaMax { get; set; }
    public string? Status { get; set; } // normal, baixo, alto, crítico
    public string? Observacoes { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Exame Exame { get; set; } = null!;
}
