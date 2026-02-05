using ExamAI.Application.DTOs;
using ExamAI.Domain.Entities;
using ExamAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExamAI.Infrastructure.Repositories;

/// <summary>
/// Repositório para persistência de exames médicos
/// </summary>
public class ExamRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<ExamRepository> _logger;

    public ExamRepository(
        AppDbContext context,
        ILogger<ExamRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Salva o resultado de um exame processado (transação atômica)
    /// </summary>
    public async Task<Guid> SaveExamAsync(
        ExamResult examResult,
        Guid documentoId,
        CancellationToken cancellationToken = default)
    {
        if (examResult == null)
            throw new ArgumentNullException(nameof(examResult));

        if (examResult.Data == null)
            throw new ArgumentException("ExamResult.Data cannot be null", nameof(examResult));

        _logger.LogInformation("Saving exam result for documento ID: {DocumentoId}", documentoId);

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Buscar ou criar paciente
            var paciente = await GetOrCreatePacienteAsync(
                examResult.Data.Paciente,
                cancellationToken);

            _logger.LogDebug("Paciente ID: {PacienteId}", paciente.Id);

            // 2. Atualizar documento com paciente_id
            var documento = await _context.Documentos.FindAsync(new object[] { documentoId }, cancellationToken);
            if (documento == null)
            {
                throw new InvalidOperationException($"Documento ID {documentoId} not found");
            }

            documento.PacienteId = paciente.Id;
            documento.StatusProcessamento = "completed";

            // 3. Salvar exames
            var examIds = new List<Guid>();

            if (examResult.Data.Exames != null && examResult.Data.Exames.Count > 0)
            {
                foreach (var exameInfo in examResult.Data.Exames)
                {
                    // Buscar ou criar tipo de exame
                    var tipoExame = await GetOrCreateTipoExameAsync(
                        exameInfo.Tipo,
                        cancellationToken);

                    // Criar exame
                    var exame = new Exame
                    {
                        Id = Guid.NewGuid(),
                        DocumentoId = documentoId,
                        TipoExameId = tipoExame.Id,
                        DataColeta = ParseDateOrDefault(examResult.Data.Paciente?.DataColeta),
                        MedicoSolicitante = examResult.Data.Paciente?.MedicoSolicitante
                    };

                    _context.Exames.Add(exame);
                    examIds.Add(exame.Id);

                    // Criar resultado do exame
                    var resultado = new ResultadoExame
                    {
                        Id = Guid.NewGuid(),
                        ExameId = exame.Id,
                        Parametro = exameInfo.Tipo,
                        ValorNumerico = exameInfo.Valor,
                        Unidade = exameInfo.Unidade,
                        ReferenciaMin = exameInfo.ReferenciaMin,
                        ReferenciaMax = exameInfo.ReferenciaMax,
                        Status = exameInfo.Status,
                        Observacoes = exameInfo.Observacoes
                    };

                    _context.ResultadosExame.Add(resultado);

                    _logger.LogDebug(
                        "Created exame: {Tipo}, valor: {Valor} {Unidade}",
                        exameInfo.Tipo,
                        exameInfo.Valor,
                        exameInfo.Unidade);
                }
            }

            // 4. Salvar tudo
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully saved {ExameCount} exames for documento {DocumentoId}",
                examIds.Count,
                documentoId);

            return paciente.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save exam result for documento {DocumentoId}", documentoId);
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Busca exames de um paciente por CPF
    /// </summary>
    public async Task<List<Exame>> GetExamsByPacienteAsync(
        string cpf,
        DateTime? dataInicio = null,
        DateTime? dataFim = null,
        string? tipoExame = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            throw new ArgumentException("CPF cannot be empty", nameof(cpf));

        _logger.LogInformation("Searching exames for CPF: {CPF}", cpf);

        // Remover formatação do CPF
        cpf = cpf.Replace(".", "").Replace("-", "").Trim();

        var query = _context.Exames
            .Include(e => e.Documento)
                .ThenInclude(d => d.Paciente)
            .Include(e => e.TipoExame)
            .Include(e => e.Resultados)
            .Where(e => e.Documento.Paciente != null && e.Documento.Paciente.Cpf == cpf);

        // Filtros opcionais
        if (dataInicio.HasValue)
        {
            query = query.Where(e => e.DataColeta >= dataInicio.Value);
        }

        if (dataFim.HasValue)
        {
            query = query.Where(e => e.DataColeta <= dataFim.Value);
        }

        if (!string.IsNullOrWhiteSpace(tipoExame))
        {
            query = query.Where(e => e.TipoExame.Nome.Contains(tipoExame));
        }

        var exames = await query
            .OrderByDescending(e => e.DataColeta)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {ExameCount} exames for CPF: {CPF}", exames.Count, cpf);

        return exames;
    }

    /// <summary>
    /// Verifica se um documento com o hash já existe
    /// </summary>
    public async Task<Domain.Entities.Documento?> FindDocumentoByHashAsync(
        string hashSha256,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(hashSha256))
            throw new ArgumentException("Hash cannot be empty", nameof(hashSha256));

        _logger.LogDebug("Searching for documento with hash: {Hash}", hashSha256);

        var documento = await _context.Documentos
            .Include(d => d.Paciente)
            .Include(d => d.Exames)
                .ThenInclude(e => e.TipoExame)
            .Include(d => d.Exames)
                .ThenInclude(e => e.Resultados)
            .FirstOrDefaultAsync(d => d.HashSha256 == hashSha256, cancellationToken);

        if (documento != null)
        {
            _logger.LogInformation(
                "Found existing documento with hash {Hash}: ID {DocumentoId}, Status {Status}",
                hashSha256,
                documento.Id,
                documento.StatusProcessamento);
        }

        return documento;
    }

    /// <summary>
    /// Busca ou cria um paciente
    /// </summary>
    private async Task<Paciente> GetOrCreatePacienteAsync(
        PacienteInfo? pacienteInfo,
        CancellationToken cancellationToken)
    {
        // Define nome padrão se não identificado
        var nomePaciente = string.IsNullOrWhiteSpace(pacienteInfo?.Nome) 
            ? "Paciente não identificado" 
            : pacienteInfo.Nome;

        // Tentar buscar paciente existente por nome (simplificado)
        // Em produção, seria melhor usar CPF se disponível
        var pacienteExistente = await _context.Pacientes
            .FirstOrDefaultAsync(
                p => p.Nome != null && p.Nome == nomePaciente,
                cancellationToken);

        if (pacienteExistente != null)
        {
            _logger.LogDebug("Found existing paciente: {Nome}", nomePaciente);
            return pacienteExistente;
        }

        // Criar novo paciente
        var novoPaciente = new Paciente
        {
            Id = Guid.NewGuid(),
            Nome = nomePaciente,
            Cpf = null, // CPF não identificado ou não extraído
            DataNascimento = ParseDateOrDefault(pacienteInfo?.DataNascimento)
        };

        _context.Pacientes.Add(novoPaciente);

        _logger.LogDebug("Created new paciente: {Nome}", nomePaciente);

        return novoPaciente;
    }

    /// <summary>
    /// Busca ou cria um tipo de exame
    /// </summary>
    private async Task<TipoExame> GetOrCreateTipoExameAsync(
        string nomeExame,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(nomeExame))
        {
            throw new ArgumentException("Nome do exame cannot be empty", nameof(nomeExame));
        }

        // Buscar tipo existente (match exato ou parcial)
        var tipoExistente = await _context.TiposExame
            .FirstOrDefaultAsync(
                t => t.Nome == nomeExame || t.Nome.Contains(nomeExame),
                cancellationToken);

        if (tipoExistente != null)
        {
            _logger.LogDebug("Found existing tipo_exame: {Nome}", tipoExistente.Nome);
            return tipoExistente;
        }

        // Criar novo tipo de exame (categoria genérica)
        var novoTipo = new TipoExame
        {
            Nome = nomeExame,
            Categoria = "Outros", // Categoria padrão para exames não mapeados
            CreatedAt = DateTime.UtcNow
        };

        _context.TiposExame.Add(novoTipo);
        
        // Salvar IMEDIATAMENTE para garantir que o ID existe antes de criar Exame
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Created new tipo_exame: {Nome} with ID: {Id}", nomeExame, novoTipo.Id);

        return novoTipo;
    }

    /// <summary>
    /// Parse de data no formato YYYY-MM-DD
    /// </summary>
    private DateTime ParseDateOrDefault(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
        {
            return DateTime.UtcNow;
        }

        if (DateTime.TryParse(dateString, out var date))
        {
            return date;
        }

        _logger.LogWarning("Failed to parse date: {DateString}, using current date", dateString);
        return DateTime.UtcNow;
    }
}
