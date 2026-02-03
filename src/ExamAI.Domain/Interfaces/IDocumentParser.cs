namespace ExamAI.Domain.Interfaces;

/// <summary>
/// Interface para parsers de documentos (PDF, Word, Excel, etc.)
/// </summary>
public interface IDocumentParser
{
    /// <summary>
    /// Extrai texto de um documento
    /// </summary>
    /// <param name="fileStream">Stream do arquivo</param>
    /// <param name="fileType">Tipo/extensão do arquivo (.pdf, .docx, .xlsx)</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Texto extraído do documento</returns>
    Task<string> ExtractTextAsync(
        Stream fileStream, 
        string fileType, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se o parser suporta o tipo de arquivo
    /// </summary>
    /// <param name="fileType">Tipo/extensão do arquivo</param>
    /// <returns>True se suporta, False caso contrário</returns>
    bool SupportsFileType(string fileType);
}
