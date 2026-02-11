using ExamAI.Domain.Interfaces;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.Extensions.Logging;
using System.Text;

namespace ExamAI.Infrastructure.Parsers;

/// <summary>
/// Parser para arquivos PDF usando iText7 com múltiplas estratégias de extração
/// </summary>
public class PdfParser : IDocumentParser
{
    private readonly ILogger<PdfParser> _logger;

    public PdfParser(ILogger<PdfParser> logger)
    {
        _logger = logger;
    }

    public bool SupportsFileType(string fileType)
    {
        return fileType.Equals(".pdf", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<string> ExtractTextAsync(
        Stream fileStream, 
        string fileType, 
        CancellationToken cancellationToken = default)
    {
        if (!SupportsFileType(fileType))
        {
            throw new NotSupportedException($"File type '{fileType}' is not supported by PdfParser");
        }

        if (fileStream == null || fileStream.Length == 0)
        {
            throw new ArgumentException("File stream is empty or null", nameof(fileStream));
        }

        try
        {
            _logger.LogInformation("Starting PDF text extraction, size: {Size} bytes", fileStream.Length);

            // iText7 precisa de um stream que suporte seek
            // Se o stream não suportar, copiamos para um MemoryStream
            Stream streamToUse = fileStream;
            if (!fileStream.CanSeek)
            {
                _logger.LogDebug("Stream does not support seek, copying to MemoryStream");
                var memoryStream = new MemoryStream();
                await fileStream.CopyToAsync(memoryStream, cancellationToken);
                memoryStream.Position = 0;
                streamToUse = memoryStream;
            }
            else
            {
                // Garantir que estamos no início do stream
                fileStream.Position = 0;
            }

            var extractedText = new StringBuilder();
            var metadata = new StringBuilder();

            using (var pdfReader = new PdfReader(streamToUse))
            using (var pdfDocument = new PdfDocument(pdfReader))
            {
                var numberOfPages = pdfDocument.GetNumberOfPages();
                _logger.LogInformation("PDF has {PageCount} pages", numberOfPages);

                // Extrair metadados do PDF
                var pdfInfo = pdfDocument.GetDocumentInfo();
                if (!string.IsNullOrEmpty(pdfInfo.GetTitle()))
                    metadata.AppendLine($"Título: {pdfInfo.GetTitle()}");
                if (!string.IsNullOrEmpty(pdfInfo.GetAuthor()))
                    metadata.AppendLine($"Autor: {pdfInfo.GetAuthor()}");
                if (!string.IsNullOrEmpty(pdfInfo.GetSubject()))
                    metadata.AppendLine($"Assunto: {pdfInfo.GetSubject()}");
                
                if (metadata.Length > 0)
                {
                    extractedText.AppendLine("=== METADADOS DO DOCUMENTO ===");
                    extractedText.Append(metadata.ToString());
                    extractedText.AppendLine();
                }

                // Tentar extrair texto com múltiplas estratégias
                var totalChars = 0;
                
                for (int pageNumber = 1; pageNumber <= numberOfPages; pageNumber++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var page = pdfDocument.GetPage(pageNumber);
                        
                        // Estratégia 1: SimpleTextExtractionStrategy
                        var strategy1 = new SimpleTextExtractionStrategy();
                        var text1 = PdfTextExtractor.GetTextFromPage(page, strategy1);
                        
                        // Estratégia 2: LocationTextExtractionStrategy (melhor para layouts complexos)
                        var strategy2 = new LocationTextExtractionStrategy();
                        var text2 = PdfTextExtractor.GetTextFromPage(page, strategy2);
                        
                        // Usar o texto mais longo das duas estratégias
                        var bestText = text1?.Length > text2?.Length ? text1 : text2;
                        
                        if (!string.IsNullOrWhiteSpace(bestText))
                        {
                            extractedText.AppendLine($"--- PÁGINA {pageNumber} ---");
                            extractedText.AppendLine(bestText.Trim());
                            extractedText.AppendLine();
                            totalChars += bestText.Length;
                            
                            _logger.LogDebug("Extracted {CharCount} chars from page {PageNumber} using {Strategy}", 
                                bestText.Length, pageNumber, 
                                bestText == text1 ? "SimpleText" : "LocationText");
                        }
                        else
                        {
                            _logger.LogWarning("No text extracted from page {PageNumber} - might be image-based", pageNumber);
                            extractedText.AppendLine($"--- PÁGINA {pageNumber} (conteúdo em imagem) ---");
                            extractedText.AppendLine("[Esta página parece conter apenas imagens sem texto extraível]");
                            extractedText.AppendLine();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to extract text from page {PageNumber}, skipping", pageNumber);
                        extractedText.AppendLine($"--- PÁGINA {pageNumber} (erro na extração) ---");
                    }
                }

                _logger.LogInformation("PDF text extraction completed. Total chars extracted: {CharCount} from {PageCount} pages", 
                    totalChars, numberOfPages);
            }

            var result = extractedText.ToString();

            if (string.IsNullOrWhiteSpace(result) || result.Length < 50)
            {
                _logger.LogWarning("Very little or no text extracted from PDF ({CharCount} chars) - might be scanned/image-only", 
                    result?.Length ?? 0);
                return @"AVISO: Nenhum texto foi extraído do PDF. O documento pode ser uma imagem escaneada.

Para processar este documento, você pode:
1. Usar um software de OCR (Tesseract, Adobe Acrobat) para converter o PDF em texto
2. Digitar manualmente os dados do exame
3. Converter o PDF para imagem e usar uma API de OCR

Se este for um PDF comum e deveria conter texto, pode haver um problema com a codificação do documento.";
            }

            return result;
        }
        catch (iText.IO.Exceptions.IOException ex)
        {
            _logger.LogError(ex, "PDF file is corrupted or invalid");
            throw new InvalidOperationException("The PDF file is corrupted or invalid", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract text from PDF");
            throw new InvalidOperationException("Failed to extract text from PDF", ex);
        }
    }
}
