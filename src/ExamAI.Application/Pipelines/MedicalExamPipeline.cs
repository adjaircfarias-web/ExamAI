using System.Diagnostics;
using ExamAI.Application.Agents;
using ExamAI.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace ExamAI.Application.Pipelines;

/// <summary>
/// Pipeline completo para processar documentos médicos
/// Orquestra: Parse → Extract → Validate → Normalize
/// </summary>
public class MedicalExamPipeline
{
    private readonly DocumentParserAgent _parserAgent;
    private readonly ExtractionAgent _extractionAgent;
    private readonly ValidationAgent _validationAgent;
    private readonly NormalizationAgent _normalizationAgent;
    private readonly ILogger<MedicalExamPipeline> _logger;

    public MedicalExamPipeline(
        DocumentParserAgent parserAgent,
        ExtractionAgent extractionAgent,
        ValidationAgent validationAgent,
        NormalizationAgent normalizationAgent,
        ILogger<MedicalExamPipeline> logger)
    {
        _parserAgent = parserAgent ?? throw new ArgumentNullException(nameof(parserAgent));
        _extractionAgent = extractionAgent ?? throw new ArgumentNullException(nameof(extractionAgent));
        _validationAgent = validationAgent ?? throw new ArgumentNullException(nameof(validationAgent));
        _normalizationAgent = normalizationAgent ?? throw new ArgumentNullException(nameof(normalizationAgent));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processa um documento médico do início ao fim
    /// </summary>
    /// <param name="fileStream">Stream do arquivo</param>
    /// <param name="fileName">Nome do arquivo (para detectar tipo)</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resultado completo do processamento</returns>
    public async Task<PipelineResult> ProcessAsync(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var result = new PipelineResult
        {
            FileName = fileName,
            Stats = new ProcessingStats
            {
                StartedAt = DateTime.UtcNow
            }
        };

        try
        {
            _logger.LogInformation(
                "Starting pipeline for document: {FileName}",
                fileName);

            // Passo 1: Parse do documento (extrair texto bruto)
            var parseStopwatch = Stopwatch.StartNew();
            string extractedText;
            
            try
            {
                _logger.LogDebug("Step 1/4: Parsing document...");
                extractedText = await _parserAgent.ExtractTextAsync(fileStream, fileName, cancellationToken);
                parseStopwatch.Stop();
                
                result.ExtractedTextLength = extractedText.Length;
                result.Stats.StepDurations["1_Parse"] = parseStopwatch.Elapsed;
                
                _logger.LogInformation(
                    "Step 1/4 completed: Extracted {CharCount} characters in {Duration}ms",
                    extractedText.Length,
                    parseStopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed at Step 1/4: Document parsing");
                result.Success = false;
                result.ErrorMessage = $"Parse error: {ex.Message}";
                result.Stats.CompletedAt = DateTime.UtcNow;
                return result;
            }

            // Passo 2: Extração com IA (estruturar dados)
            var extractStopwatch = Stopwatch.StartNew();
            ExamExtractionResult structuredData;
            
            try
            {
                _logger.LogDebug("Step 2/4: Extracting structured data with LLM...");
                structuredData = await _extractionAgent.ExtractAsync(extractedText, cancellationToken);
                extractStopwatch.Stop();
                
                result.Stats.StepDurations["2_Extract"] = extractStopwatch.Elapsed;
                result.Stats.ExtractedExams = structuredData.Exams?.Count ?? 0;
                
                _logger.LogInformation(
                    "Step 2/4 completed: Extracted {ExamCount} exams in {Duration}ms",
                    result.Stats.ExtractedExams,
                    extractStopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed at Step 2/4: Data extraction");
                result.Success = false;
                result.ErrorMessage = $"Extraction error: {ex.Message}";
                result.Stats.CompletedAt = DateTime.UtcNow;
                return result;
            }

            // Passo 3: Validação (verificar consistência)
            var validateStopwatch = Stopwatch.StartNew();
            ValidationResult validationResult;
            
            try
            {
                _logger.LogDebug("Step 3/4: Validating extracted data...");
                validationResult = _validationAgent.Validate(structuredData);
                validateStopwatch.Stop();
                
                result.Validation = validationResult;
                result.Stats.StepDurations["3_Validate"] = validateStopwatch.Elapsed;
                result.Stats.ValidationWarnings = validationResult.Warnings.Count;
                
                _logger.LogInformation(
                    "Step 3/4 completed: {WarningCount} warnings found in {Duration}ms",
                    result.Stats.ValidationWarnings,
                    validateStopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed at Step 3/4: Data validation");
                result.Success = false;
                result.ErrorMessage = $"Validation error: {ex.Message}";
                result.Stats.CompletedAt = DateTime.UtcNow;
                return result;
            }

            // Passo 4: Normalização (padronizar nomenclatura)
            var normalizeStopwatch = Stopwatch.StartNew();
            
            try
            {
                _logger.LogDebug("Step 4/4: Normalizing data...");
                structuredData = await _normalizationAgent.NormalizeAsync(structuredData, cancellationToken);
                normalizeStopwatch.Stop();
                
                result.Data = structuredData;
                result.Stats.StepDurations["4_Normalize"] = normalizeStopwatch.Elapsed;
                result.Stats.NormalizedExams = structuredData.Exams?.Count ?? 0;
                
                _logger.LogInformation(
                    "Step 4/4 completed: {ExamCount} exams normalized in {Duration}ms",
                    result.Stats.NormalizedExams,
                    normalizeStopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed at Step 4/4: Data normalization");
                result.Success = false;
                result.ErrorMessage = $"Normalization error: {ex.Message}";
                result.Stats.CompletedAt = DateTime.UtcNow;
                return result;
            }

            // Pipeline concluído com sucesso!
            result.Success = true;
            result.Stats.CompletedAt = DateTime.UtcNow;

            _logger.LogInformation(
                "Pipeline completed successfully for {FileName}: {ExamCount} exams processed in {Duration}ms (Parse: {ParseMs}ms, Extract: {ExtractMs}ms, Validate: {ValidateMs}ms, Normalize: {NormalizeMs}ms)",
                fileName,
                result.Stats.ExtractedExams,
                result.Stats.Duration.TotalMilliseconds,
                result.Stats.StepDurations["1_Parse"].TotalMilliseconds,
                result.Stats.StepDurations["2_Extract"].TotalMilliseconds,
                result.Stats.StepDurations["3_Validate"].TotalMilliseconds,
                result.Stats.StepDurations["4_Normalize"].TotalMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in pipeline for {FileName}", fileName);
            result.Success = false;
            result.ErrorMessage = $"Unexpected error: {ex.Message}";
            result.Stats.CompletedAt = DateTime.UtcNow;
            return result;
        }
    }
}
