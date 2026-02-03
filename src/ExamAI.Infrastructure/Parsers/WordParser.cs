using ExamAI.Domain.Interfaces;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using System.Text;

namespace ExamAI.Infrastructure.Parsers;

/// <summary>
/// Parser para arquivos Word (.docx) usando DocumentFormat.OpenXml
/// </summary>
public class WordParser : IDocumentParser
{
    private readonly ILogger<WordParser> _logger;

    public WordParser(ILogger<WordParser> logger)
    {
        _logger = logger;
    }

    public bool SupportsFileType(string fileType)
    {
        return fileType.Equals(".docx", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<string> ExtractTextAsync(
        Stream fileStream,
        string fileType,
        CancellationToken cancellationToken = default)
    {
        if (!SupportsFileType(fileType))
        {
            throw new NotSupportedException($"File type '{fileType}' is not supported by WordParser");
        }

        if (fileStream == null || fileStream.Length == 0)
        {
            throw new ArgumentException("File stream is empty or null", nameof(fileStream));
        }

        try
        {
            _logger.LogInformation("Starting Word document text extraction, size: {Size} bytes", fileStream.Length);

            // OpenXml precisa de um stream que suporte seek e read
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
                fileStream.Position = 0;
            }

            var extractedText = new StringBuilder();

            // Abrir o documento Word
            using (var wordDocument = WordprocessingDocument.Open(streamToUse, false))
            {
                if (wordDocument.MainDocumentPart == null)
                {
                    _logger.LogWarning("Document has no MainDocumentPart");
                    return "AVISO: Documento Word não possui conteúdo principal.";
                }

                var body = wordDocument.MainDocumentPart.Document?.Body;

                if (body == null)
                {
                    _logger.LogWarning("Document body is null");
                    return "AVISO: Documento Word não possui corpo de texto.";
                }

                // Extrair texto de todos os parágrafos
                var paragraphs = body.Elements<Paragraph>();
                var paragraphCount = 0;

                foreach (var paragraph in paragraphs)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var paragraphText = ExtractParagraphText(paragraph);

                    if (!string.IsNullOrWhiteSpace(paragraphText))
                    {
                        extractedText.AppendLine(paragraphText);
                        paragraphCount++;
                    }
                }

                _logger.LogInformation("Extracted text from {ParagraphCount} paragraphs", paragraphCount);

                // Extrair texto de tabelas (se houver)
                var tables = body.Elements<Table>();
                var tableCount = 0;

                foreach (var table in tables)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    extractedText.AppendLine("\n--- TABELA ---");
                    ExtractTableText(table, extractedText);
                    extractedText.AppendLine("--- FIM TABELA ---\n");
                    tableCount++;
                }

                if (tableCount > 0)
                {
                    _logger.LogInformation("Extracted text from {TableCount} tables", tableCount);
                }
            }

            var result = extractedText.ToString();
            _logger.LogInformation("Word document text extraction completed, total chars: {CharCount}", result.Length);

            if (string.IsNullOrWhiteSpace(result))
            {
                _logger.LogWarning("No text extracted from Word document");
                return "AVISO: Nenhum texto foi extraído do documento Word. O documento pode estar vazio.";
            }

            return result;
        }
        catch (OpenXmlPackageException ex)
        {
            _logger.LogError(ex, "Word document is corrupted or invalid");
            throw new InvalidOperationException("The Word document is corrupted or invalid", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract text from Word document");
            throw new InvalidOperationException("Failed to extract text from Word document", ex);
        }
    }

    /// <summary>
    /// Extrai texto de um parágrafo
    /// </summary>
    private string ExtractParagraphText(Paragraph paragraph)
    {
        var textBuilder = new StringBuilder();

        foreach (var run in paragraph.Elements<Run>())
        {
            foreach (var text in run.Elements<Text>())
            {
                textBuilder.Append(text.Text);
            }
        }

        return textBuilder.ToString();
    }

    /// <summary>
    /// Extrai texto de uma tabela
    /// </summary>
    private void ExtractTableText(Table table, StringBuilder output)
    {
        foreach (var row in table.Elements<TableRow>())
        {
            var rowText = new List<string>();

            foreach (var cell in row.Elements<TableCell>())
            {
                var cellText = new StringBuilder();

                foreach (var paragraph in cell.Elements<Paragraph>())
                {
                    var paragraphText = ExtractParagraphText(paragraph);
                    if (!string.IsNullOrWhiteSpace(paragraphText))
                    {
                        cellText.Append(paragraphText);
                        cellText.Append(" ");
                    }
                }

                rowText.Add(cellText.ToString().Trim());
            }

            // Juntar células com separador de tabulação
            output.AppendLine(string.Join("\t", rowText));
        }
    }
}
