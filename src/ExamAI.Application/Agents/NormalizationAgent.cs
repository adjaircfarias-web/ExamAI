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
    private static readonly Dictionary<string, string> ExameNormalizationMap = new(StringComparer.OrdinalIgnoreCase)
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

        _logger.LogInformation("Starting normalization of {ExameCount} exames", extractionResult.Exames?.Count ?? 0);

        if (extractionResult.Exames == null || extractionResult.Exames.Count == 0)
        {
            _logger.LogWarning("No exames to normalize");
            return Task.FromResult(extractionResult);
        }

        int normalizedCount = 0;

        foreach (var exame in extractionResult.Exames)
        {
            var originalTipo = exame.Tipo;

            // 1. Normalizar nome do exame
            var normalizedTipo = NormalizeExameName(exame.Tipo);
            if (normalizedTipo != originalTipo)
            {
                _logger.LogDebug("Normalized: '{Original}' → '{Normalized}'", originalTipo, normalizedTipo);
                exame.Tipo = normalizedTipo;
                normalizedCount++;
            }

            // 2. Normalizar unidade (limpeza básica)
            if (!string.IsNullOrWhiteSpace(exame.Unidade))
            {
                exame.Unidade = exame.Unidade.Trim();
            }

            // 3. Normalizar status (lowercase)
            if (!string.IsNullOrWhiteSpace(exame.Status))
            {
                exame.Status = exame.Status.ToLower().Trim();
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
    private string NormalizeExameName(string exameName)
    {
        if (string.IsNullOrWhiteSpace(exameName))
            return exameName;

        // Tentar match direto
        if (ExameNormalizationMap.TryGetValue(exameName, out var normalized))
        {
            return normalized;
        }

        // Tentar match parcial (contém)
        foreach (var kvp in ExameNormalizationMap)
        {
            if (exameName.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
            {
                return kvp.Value;
            }
        }

        // Não encontrado, retornar original com trim
        return exameName.Trim();
    }

    /// <summary>
    /// Converte unidades (opcional, implementação futura)
    /// </summary>
    public string? ConvertUnit(decimal value, string fromUnit, string toUnit)
    {
        // TODO: Implementar conversões de unidades se necessário
        // Exemplo: mg/dL → mmol/L para glicose
        _logger.LogDebug("Unit conversion not implemented yet: {Value} {FromUnit} → {ToUnit}",
            value, fromUnit, toUnit);

        return null;
    }
}
