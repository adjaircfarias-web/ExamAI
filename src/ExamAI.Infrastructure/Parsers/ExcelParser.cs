using ExamAI.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using System.Text;

namespace ExamAI.Infrastructure.Parsers;

/// <summary>
/// Parser para arquivos Excel (.xlsx) usando EPPlus
/// </summary>
public class ExcelParser : IDocumentParser
{
    private readonly ILogger<ExcelParser> _logger;

    public ExcelParser(ILogger<ExcelParser> logger)
    {
        _logger = logger;
        
        // EPPlus 8+ requer uma licença comercial para uso empresarial
        // Para desenvolvimento/testes, não requer configuração adicional
        // Veja: https://www.epplussoftware.com/en/Home/LgplToPolyform
    }

    public bool SupportsFileType(string fileType)
    {
        return fileType.Equals(".xlsx", StringComparison.OrdinalIgnoreCase) ||
               fileType.Equals(".xls", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<string> ExtractTextAsync(
        Stream fileStream, 
        string fileType, 
        CancellationToken cancellationToken = default)
    {
        if (!SupportsFileType(fileType))
        {
            throw new NotSupportedException($"File type '{fileType}' is not supported by ExcelParser");
        }

        if (fileStream == null || fileStream.Length == 0)
        {
            throw new ArgumentException("File stream is empty or null", nameof(fileStream));
        }

        try
        {
            _logger.LogInformation("Starting Excel text extraction, size: {Size} bytes", fileStream.Length);

            // EPPlus precisa de um stream que suporte seek
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

            using (var package = new ExcelPackage(streamToUse))
            {
                var worksheets = package.Workbook.Worksheets;
                _logger.LogInformation("Excel has {WorksheetCount} worksheets", worksheets.Count);

                if (worksheets.Count == 0)
                {
                    _logger.LogWarning("No worksheets found in Excel file");
                    return "AVISO: Nenhuma planilha encontrada no arquivo Excel.";
                }

                for (int worksheetIndex = 0; worksheetIndex < worksheets.Count; worksheetIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var worksheet = worksheets[worksheetIndex];
                    
                    try
                    {
                        extractedText.AppendLine($"=== Planilha: {worksheet.Name} ===");
                        extractedText.AppendLine();

                        var dimension = worksheet.Dimension;
                        
                        if (dimension == null || dimension.Rows == 0 || dimension.Columns == 0)
                        {
                            _logger.LogDebug("Worksheet '{WorksheetName}' is empty", worksheet.Name);
                            extractedText.AppendLine("(Planilha vazia)");
                            extractedText.AppendLine();
                            continue;
                        }

                        int startRow = dimension.Start.Row;
                        int endRow = dimension.End.Row;
                        int startCol = dimension.Start.Column;
                        int endCol = dimension.End.Column;

                        _logger.LogDebug(
                            "Processing worksheet '{WorksheetName}': {Rows} rows x {Cols} columns", 
                            worksheet.Name, 
                            endRow - startRow + 1, 
                            endCol - startCol + 1);

                        // Extrair dados em formato tabular
                        for (int row = startRow; row <= endRow; row++)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            var rowValues = new List<string>();
                            bool hasContent = false;

                            for (int col = startCol; col <= endCol; col++)
                            {
                                var cell = worksheet.Cells[row, col];
                                var cellValue = cell.Value?.ToString() ?? "";
                                
                                if (!string.IsNullOrWhiteSpace(cellValue))
                                {
                                    hasContent = true;
                                }
                                
                                rowValues.Add(cellValue);
                            }

                            // Só adicionar linha se tiver conteúdo
                            if (hasContent)
                            {
                                // Formato: Coluna A | Coluna B | Coluna C
                                extractedText.AppendLine(string.Join(" | ", rowValues));
                            }
                        }

                        extractedText.AppendLine();
                        
                        _logger.LogDebug(
                            "Extracted {CellCount} cells from worksheet '{WorksheetName}'", 
                            (endRow - startRow + 1) * (endCol - startCol + 1), 
                            worksheet.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex, 
                            "Failed to extract data from worksheet '{WorksheetName}', skipping", 
                            worksheet.Name);
                        extractedText.AppendLine($"(Erro ao processar planilha: {worksheet.Name})");
                        extractedText.AppendLine();
                    }
                }
            }

            var result = extractedText.ToString();
            _logger.LogInformation("Excel text extraction completed, total chars: {CharCount}", result.Length);

            if (string.IsNullOrWhiteSpace(result))
            {
                _logger.LogWarning("No text extracted from Excel file");
                return "AVISO: Nenhum dado foi extraído do arquivo Excel. Todas as planilhas estão vazias.";
            }

            return result;
        }
        catch (InvalidDataException ex)
        {
            _logger.LogError(ex, "Excel file is corrupted or invalid");
            throw new InvalidOperationException("The Excel file is corrupted or invalid", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract text from Excel");
            throw new InvalidOperationException("Failed to extract text from Excel", ex);
        }
    }
}
