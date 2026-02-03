using ExamAI.Domain.Interfaces;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.Extensions.Logging;
using System.Text;

namespace ExamAI.Infrastructure.Parsers;

/// <summary>
/// Parser para arquivos PDF usando iText7
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

            using (var pdfReader = new PdfReader(streamToUse))
            using (var pdfDocument = new PdfDocument(pdfReader))
            {
                var numberOfPages = pdfDocument.GetNumberOfPages();
                _logger.LogInformation("PDF has {PageCount} pages", numberOfPages);

                for (int pageNumber = 1; pageNumber <= numberOfPages; pageNumber++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var page = pdfDocument.GetPage(pageNumber);
                        var strategy = new SimpleTextExtractionStrategy();
                        var pageText = PdfTextExtractor.GetTextFromPage(page, strategy);

                        if (!string.IsNullOrWhiteSpace(pageText))
                        {
                            extractedText.AppendLine($"--- Página {pageNumber} ---");
                            extractedText.AppendLine(pageText);
                            extractedText.AppendLine();
                        }

                        _logger.LogDebug("Extracted {CharCount} chars from page {PageNumber}", 
                            pageText?.Length ?? 0, pageNumber);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to extract text from page {PageNumber}, skipping", pageNumber);
                        extractedText.AppendLine($"--- Página {pageNumber} (erro na extração) ---");
                    }
                }
            }

            var result = extractedText.ToString();
            _logger.LogInformation("PDF text extraction completed, total chars: {CharCount}", result.Length);

            if (string.IsNullOrWhiteSpace(result))
            {
                _logger.LogWarning("No text extracted from PDF - might be scanned/image-only");
                return "AVISO: Nenhum texto foi extraído do PDF. O documento pode ser uma imagem escaneada.";
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
