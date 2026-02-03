using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace ExamAI.Infrastructure.Services;

/// <summary>
/// Serviço para calcular hash SHA256 de documentos
/// </summary>
public class DocumentHashService
{
    private readonly ILogger<DocumentHashService> _logger;

    public DocumentHashService(ILogger<DocumentHashService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Calcula SHA256 hash de um stream
    /// </summary>
    public async Task<string> ComputeSha256Async(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        if (!stream.CanRead)
            throw new ArgumentException("Stream must be readable", nameof(stream));

        _logger.LogDebug("Computing SHA256 hash for stream (length: {Length} bytes)", stream.Length);

        // Garantir que estamos no início do stream
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);

        // Resetar posição do stream para uso posterior
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

        _logger.LogDebug("SHA256 hash computed: {Hash}", hashString);

        return hashString;
    }

    /// <summary>
    /// Calcula SHA256 hash de um arquivo
    /// </summary>
    public async Task<string> ComputeSha256Async(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be empty", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException("File not found", filePath);

        _logger.LogDebug("Computing SHA256 hash for file: {FilePath}", filePath);

        using var fileStream = File.OpenRead(filePath);
        return await ComputeSha256Async(fileStream, cancellationToken);
    }
}
