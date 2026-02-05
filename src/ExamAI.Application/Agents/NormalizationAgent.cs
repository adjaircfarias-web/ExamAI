using ExamAI.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace ExamAI.Application.Agents;

/// <summary>
/// Agent responsável por normalizar dados extraídos (nomenclatura, unidades)
/// </summary>
public class NormalizationAgent
{
    private readonly ILogger<NormalizationAgent> _logger;

    // Dicionário de normalizações de nomes de exames
    private static readonly Dictionary<string, string> ExamNormalizationMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Colesterol
        { "Col. Total", "Colesterol Total" },
        { "Col Total", "Colesterol Total" },
        { "Colesterol", "Colesterol Total" },
        { "HDL", "Colesterol HDL" },
        { "LDL", "Colesterol LDL" },
        { "VLDL", "Colesterol VLDL" },
        
        // Glicemia
        { "Glicemia Jejum", "Glicemia em Jejum" },
        { "Glicose", "Glicemia em Jejum" },
        { "Glicose Jejum", "Glicemia em Jejum" },
        
        // Enzimas hepáticas
        { "TGO", "TGO (AST)" },
        { "AST", "TGO (AST)" },
        { "TGP", "TGP (ALT)" },
        { "ALT", "TGP (ALT)" },
        
        // Hemograma
        { "Hemácias", "Hemácias (Eritrócitos)" },
        { "Eritrócitos", "Hemácias (Eritrócitos)" },
        { "Leucócitos", "Leucócitos (Glóbulos Brancos)" },
        { "Glóbulos Brancos", "Leucócitos (Glóbulos Brancos)" },
        { "Plaquetas", "Plaquetas (Trombócitos)" },
        { "Trombócitos", "Plaquetas (Trombócitos)" },
        { "Hemoglobina", "Hemoglobina (Hb)" },
        { "Hb", "Hemoglobina (Hb)" },
        { "Hematócrito", "Hematócrito (Ht)" },
        { "Ht", "Hematócrito (Ht)" },
        
        // Triglicerídeos
        { "Triglicerídeos", "Triglicerídeos (Triglicérides)" },
        { "Triglicérides", "Triglicerídeos (Triglicérides)" },
        
        // Ureia e Creatinina
        { "Ureia", "Ureia (Nitrogênio Ureico)" },
        { "Creatinina", "Creatinina Sérica" },
        
        // TSH e T4
        { "TSH", "TSH (Hormônio Tireoestimulante)" },
        { "T4", "T4 Livre (Tiroxina)" },
        { "T4 Livre", "T4 Livre (Tiroxina)" }
    };

    public NormalizationAgent(ILogger<NormalizationAgent> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Normaliza os dados extraídos (nomes de exames, unidades)
    /// </summary>
    public Task<ExamExtractionResult> NormalizeAsync(
        ExamExtractionResult extractionResult,
        CancellationToken cancellationToken = default)
    {
        if (extractionResult == null)
            throw new ArgumentNullException(nameof(extractionResult));

        _logger.LogInformation("Starting normalization of {ExamCount} exams", extractionResult.Exams?.Count ?? 0);

        if (extractionResult.Exams == null || extractionResult.Exams.Count == 0)
        {
            _logger.LogWarning("No exams to normalize");
            return Task.FromResult(extractionResult);
        }

        int normalizedCount = 0;

        foreach (var exam in extractionResult.Exams)
        {
            var originalType = exam.Type;

            // 1. Normalizar nome do exame
            var normalizedType = NormalizeExamName(exam.Type);
            if (normalizedType != originalType)
            {
                _logger.LogDebug("Normalized: '{Original}' → '{Normalized}'", originalType, normalizedType);
                exam.Type = normalizedType;
                normalizedCount++;
            }

            // 2. Normalizar unidade (limpeza básica)
            if (!string.IsNullOrWhiteSpace(exam.Unit))
            {
                exam.Unit = exam.Unit.Trim();
            }

            // 3. Normalizar status (lowercase)
            if (!string.IsNullOrWhiteSpace(exam.Status))
            {
                exam.Status = exam.Status.ToLower().Trim();
            }
        }

        _logger.LogInformation(
            "Normalization completed: {NormalizedCount} names normalized",
            normalizedCount);

        return Task.FromResult(extractionResult);
    }

    /// <summary>
    /// Normaliza o nome de um exame usando o dicionário de mapeamento
    /// </summary>
    private string NormalizeExamName(string examName)
    {
        if (string.IsNullOrWhiteSpace(examName))
            return examName;

        // Tentar match direto
        if (ExamNormalizationMap.TryGetValue(examName, out var normalized))
        {
            return normalized;
        }

        // Tentar match parcial (contém)
        foreach (var kvp in ExamNormalizationMap)
        {
            if (examName.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
            {
                return kvp.Value;
            }
        }

        // Não encontrado, retornar original com trim
        return examName.Trim();
    }
}
