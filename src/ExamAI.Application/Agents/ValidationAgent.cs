using System.Text.RegularExpressions;
using ExamAI.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace ExamAI.Application.Agents;

/// <summary>
/// Agent responsável por validar dados extraídos do LLM
/// </summary>
public class ValidationAgent
{
    private readonly ILogger<ValidationAgent> _logger;
    private static readonly string[] ValidStatuses = { "normal", "baixo", "alto", "crítico" };

    public ValidationAgent(ILogger<ValidationAgent> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Valida os dados extraídos e retorna lista de warnings
    /// </summary>
    public ValidationResult Validate(ExamExtractionResult extractionResult)
    {
        if (extractionResult == null)
            throw new ArgumentNullException(nameof(extractionResult));

        var result = new ValidationResult();

        _logger.LogInformation("Starting validation of extraction result");

        // Validar dados do paciente
        if (extractionResult.Paciente != null)
        {
            ValidatePaciente(extractionResult.Paciente, result);
        }
        else
        {
            result.AddWarning("paciente", "Nenhuma informação de paciente foi extraída");
        }

        // Validar exames
        if (extractionResult.Exames == null || extractionResult.Exames.Count == 0)
        {
            result.AddWarning("exames", "Nenhum exame foi extraído do documento");
        }
        else
        {
            for (int i = 0; i < extractionResult.Exames.Count; i++)
            {
                ValidateExame(extractionResult.Exames[i], i, result);
            }
        }

        _logger.LogInformation(
            "Validation completed: {WarningCount} warnings found",
            result.Warnings.Count);

        if (result.Warnings.Count > 0)
        {
            foreach (var warning in result.Warnings)
            {
                _logger.LogWarning(
                    "Validation warning - Field: {Field}, Message: {Message}, Value: {Value}",
                    warning.Field,
                    warning.Message,
                    warning.CurrentValue ?? "null");
            }
        }

        return result;
    }

    private void ValidatePaciente(PacienteInfo paciente, ValidationResult result)
    {
        // Validar nome
        if (string.IsNullOrWhiteSpace(paciente.Nome))
        {
            result.AddWarning("paciente.nome", "Nome do paciente está vazio");
        }
        else if (paciente.Nome.Length < 3)
        {
            result.AddWarning("paciente.nome", "Nome do paciente muito curto", paciente.Nome);
        }

        // Validar data de nascimento (formato)
        if (!string.IsNullOrWhiteSpace(paciente.DataNascimento))
        {
            if (!IsValidDate(paciente.DataNascimento))
            {
                result.AddWarning(
                    "paciente.data_nascimento",
                    "Data de nascimento em formato inválido (esperado: YYYY-MM-DD)",
                    paciente.DataNascimento);
            }
        }

        // Validar data de coleta (formato)
        if (!string.IsNullOrWhiteSpace(paciente.DataColeta))
        {
            if (!IsValidDate(paciente.DataColeta))
            {
                result.AddWarning(
                    "paciente.data_coleta",
                    "Data de coleta em formato inválido (esperado: YYYY-MM-DD)",
                    paciente.DataColeta);
            }
        }
        else
        {
            result.AddWarning("paciente.data_coleta", "Data de coleta não foi informada");
        }

        // Validar médico solicitante
        if (string.IsNullOrWhiteSpace(paciente.MedicoSolicitante))
        {
            result.AddWarning("paciente.medico_solicitante", "Médico solicitante não foi informado");
        }
    }

    private void ValidateExame(ExameInfo exame, int index, ValidationResult result)
    {
        var prefix = $"exames[{index}]";

        // Validar tipo do exame
        if (string.IsNullOrWhiteSpace(exame.Tipo))
        {
            result.AddWarning($"{prefix}.tipo", "Tipo do exame está vazio");
        }
        else if (exame.Tipo.Length < 3)
        {
            result.AddWarning($"{prefix}.tipo", "Tipo do exame muito curto", exame.Tipo);
        }

        // Validar valor numérico
        if (exame.Valor.HasValue)
        {
            if (exame.Valor.Value < 0)
            {
                result.AddWarning(
                    $"{prefix}.valor",
                    "Valor numérico negativo pode ser inválido",
                    exame.Valor.Value.ToString());
            }

            if (exame.Valor.Value > 1000000)
            {
                result.AddWarning(
                    $"{prefix}.valor",
                    "Valor numérico muito alto pode ser erro de extração",
                    exame.Valor.Value.ToString());
            }
        }
        else
        {
            result.AddWarning($"{prefix}.valor", "Valor do exame não foi informado");
        }

        // Validar unidade
        if (string.IsNullOrWhiteSpace(exame.Unidade))
        {
            result.AddWarning($"{prefix}.unidade", "Unidade de medida não foi informada");
        }

        // Validar referências (se uma existe, a outra deveria existir)
        if (exame.ReferenciaMin.HasValue && !exame.ReferenciaMax.HasValue)
        {
            result.AddWarning(
                $"{prefix}.referencia",
                "Referência mínima informada mas máxima ausente");
        }

        if (!exame.ReferenciaMin.HasValue && exame.ReferenciaMax.HasValue)
        {
            result.AddWarning(
                $"{prefix}.referencia",
                "Referência máxima informada mas mínima ausente");
        }

        // Validar lógica das referências
        if (exame.ReferenciaMin.HasValue && exame.ReferenciaMax.HasValue)
        {
            if (exame.ReferenciaMin.Value > exame.ReferenciaMax.Value)
            {
                result.AddWarning(
                    $"{prefix}.referencia",
                    "Referência mínima maior que máxima",
                    $"min: {exame.ReferenciaMin}, max: {exame.ReferenciaMax}");
            }
        }

        // Validar status
        if (!string.IsNullOrWhiteSpace(exame.Status))
        {
            var normalizedStatus = exame.Status.ToLower().Trim();
            if (!ValidStatuses.Contains(normalizedStatus))
            {
                result.AddWarning(
                    $"{prefix}.status",
                    $"Status inválido (permitidos: {string.Join(", ", ValidStatuses)})",
                    exame.Status);
            }
        }

        // Validar consistência: status vs valor vs referência
        if (exame.Valor.HasValue && 
            exame.ReferenciaMin.HasValue && 
            exame.ReferenciaMax.HasValue &&
            !string.IsNullOrWhiteSpace(exame.Status))
        {
            var normalizedStatus = exame.Status.ToLower().Trim();
            var isInRange = exame.Valor.Value >= exame.ReferenciaMin.Value && 
                           exame.Valor.Value <= exame.ReferenciaMax.Value;

            if (isInRange && normalizedStatus != "normal")
            {
                result.AddWarning(
                    $"{prefix}.status",
                    "Valor dentro da referência mas status não é 'normal'",
                    $"valor: {exame.Valor}, status: {exame.Status}");
            }

            if (!isInRange && normalizedStatus == "normal")
            {
                result.AddWarning(
                    $"{prefix}.status",
                    "Valor fora da referência mas status é 'normal'",
                    $"valor: {exame.Valor}, status: {exame.Status}");
            }
        }
    }

    /// <summary>
    /// Valida formato de data YYYY-MM-DD
    /// </summary>
    private bool IsValidDate(string date)
    {
        if (string.IsNullOrWhiteSpace(date))
            return false;

        // Formato: YYYY-MM-DD
        var dateRegex = new Regex(@"^\d{4}-\d{2}-\d{2}$");
        if (!dateRegex.IsMatch(date))
            return false;

        // Tentar parsear
        return DateTime.TryParse(date, out _);
    }

    /// <summary>
    /// Valida CPF (formato e dígitos verificadores)
    /// </summary>
    public bool IsValidCpf(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            return false;

        // Remover formatação
        cpf = Regex.Replace(cpf, @"[^\d]", "");

        // CPF deve ter 11 dígitos
        if (cpf.Length != 11)
            return false;

        // CPF não pode ter todos os dígitos iguais
        if (cpf.Distinct().Count() == 1)
            return false;

        // Validar dígitos verificadores
        var multiplicador1 = new[] { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
        var multiplicador2 = new[] { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

        var tempCpf = cpf.Substring(0, 9);
        var soma = 0;

        for (int i = 0; i < 9; i++)
            soma += int.Parse(tempCpf[i].ToString()) * multiplicador1[i];

        var resto = soma % 11;
        resto = resto < 2 ? 0 : 11 - resto;

        var digito = resto.ToString();
        tempCpf = tempCpf + digito;
        soma = 0;

        for (int i = 0; i < 10; i++)
            soma += int.Parse(tempCpf[i].ToString()) * multiplicador2[i];

        resto = soma % 11;
        resto = resto < 2 ? 0 : 11 - resto;

        digito = digito + resto.ToString();

        return cpf.EndsWith(digito);
    }
}
