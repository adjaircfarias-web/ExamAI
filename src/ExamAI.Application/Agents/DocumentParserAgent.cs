using ExamAI.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ExamAI.Application.Agents;

/// <summary>
/// Agent responsável por orquestrar o parsing de documentos.
/// Detecta o tipo de arquivo e chama o parser apropriado.
/// </summary>
public class DocumentParserAgent
{
    private readonly IEnumerable<IDocumentParser> _parsers;
    private readonly ILogger<DocumentParserAgent> _logger;

    public DocumentParserAgent(
        IEnumerable<IDocumentParser> parsers,
        ILogger<DocumentParserAgent> logger)
    {
        _parsers = parsers ?? throw new ArgumentNullException(nameof(parsers));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Extrai texto de um documento, detectando automaticamente o parser correto
    /// </summary>
    /// <param name="fileStream">Stream do arquivo</param>
    /// <param name="fileName">Nome do arquivo (usado para detectar extensão)</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Texto extraído do documento</returns>
    /// <exception cref="NotSupportedException">Lançado quando o formato não é suportado</exception>
    public async Task<string> ExtractTextAsync(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        if (fileStream == null)
            throw new ArgumentNullException(nameof(fileStream));

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be empty", nameof(fileName));

        // Extrair extensão do arquivo
        var fileExtension = Path.GetExtension(fileName);
        
        if (string.IsNullOrWhiteSpace(fileExtension))
        {
            _logger.LogError("File '{FileName}' has no extension", fileName);
            throw new NotSupportedException($"Cannot determine file type: '{fileName}' has no extension");
        }

        _logger.LogInformation(
            "Processing document '{FileName}' with extension '{Extension}'",
            fileName,
            fileExtension);

        // Buscar parser que suporta a extensão
        var parser = _parsers.FirstOrDefault(p => p.SupportsFileType(fileExtension));

        if (parser == null)
        {
            var supportedFormats = string.Join(", ", GetSupportedFormats());
            _logger.LogError(
                "No parser found for file type '{Extension}'. Supported formats: {SupportedFormats}",
                fileExtension,
                supportedFormats);

            throw new NotSupportedException(
                $"File type '{fileExtension}' is not supported. Supported formats: {supportedFormats}");
        }

        _logger.LogInformation(
            "Using parser '{ParserType}' for file '{FileName}'",
            parser.GetType().Name,
            fileName);

        try
        {
            var extractedText = await parser.ExtractTextAsync(
                fileStream,
                fileExtension,
                cancellationToken);

            _logger.LogInformation(
                "Successfully extracted {CharCount} characters from '{FileName}'",
                extractedText?.Length ?? 0,
                fileName);

            return extractedText ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to extract text from '{FileName}' using parser '{ParserType}'",
                fileName,
                parser.GetType().Name);
            throw;
        }
    }

    /// <summary>
    /// Retorna a lista de formatos de arquivo suportados
    /// </summary>
    /// <returns>Lista de extensões suportadas (ex: .pdf, .docx, .xlsx)</returns>
    public IEnumerable<string> GetSupportedFormats()
    {
        var formats = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Extensões comuns para testar
        var testExtensions = new[] { ".pdf", ".docx", ".xlsx", ".xls", ".doc", ".txt" };

        foreach (var extension in testExtensions)
        {
            if (_parsers.Any(p => p.SupportsFileType(extension)))
            {
                formats.Add(extension);
            }
        }

        return formats.OrderBy(f => f);
    }

    /// <summary>
    /// Verifica se um formato de arquivo é suportado
    /// </summary>
    /// <param name="fileName">Nome do arquivo ou extensão</param>
    /// <returns>True se suportado, False caso contrário</returns>
    public bool IsFormatSupported(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        var extension = fileName.Contains('.')
            ? Path.GetExtension(fileName)
            : fileName;

        return _parsers.Any(p => p.SupportsFileType(extension));
    }
}
