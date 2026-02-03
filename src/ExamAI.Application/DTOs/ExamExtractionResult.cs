namespace ExamAI.Application.DTOs;

/// <summary>
/// Resultado completo da extração de um documento médico
/// </summary>
public class ExamExtractionResult
{
    public PacienteInfo? Paciente { get; set; }
    public List<ExameInfo> Exames { get; set; } = new();
}

/// <summary>
/// Informações do paciente extraídas do documento
/// </summary>
public class PacienteInfo
{
    public string? Nome { get; set; }
    public string? DataNascimento { get; set; }
    public string? DataColeta { get; set; }
    public string? MedicoSolicitante { get; set; }
}

/// <summary>
/// Informações de um exame extraído
/// </summary>
public class ExameInfo
{
    public string Tipo { get; set; } = string.Empty;
    public decimal? Valor { get; set; }
    public string? Unidade { get; set; }
    public decimal? ReferenciaMin { get; set; }
    public decimal? ReferenciaMax { get; set; }
    public string? Status { get; set; }
    public string? Observacoes { get; set; }
}
