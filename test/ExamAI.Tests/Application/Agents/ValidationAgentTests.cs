using ExamAI.Application.Agents;
using ExamAI.Application.DTOs;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace ExamAI.Tests.Application.Agents;

public class ValidationAgentTests
{
    private readonly ValidationAgent _sut;
    private readonly Mock<ILogger<ValidationAgent>> _loggerMock;

    public ValidationAgentTests()
    {
        _loggerMock = new Mock<ILogger<ValidationAgent>>();
        _sut = new ValidationAgent(_loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WhenLoggerIsNull_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new ValidationAgent(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region Validate - Argument Validation Tests

    [Fact]
    public void Validate_WhenExtractionResultIsNull_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => _sut.Validate(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("extractionResult");
    }

    [Fact]
    public void Validate_WhenPacienteIsNull_ShouldAddWarning()
    {
        // Arrange
        var result = new ExamExtractionResult
        {
            Paciente = null,
            Exames = new List<ExameInfo>()
        };

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().ContainSingle(w => w.Field == "paciente");
    }

    [Fact]
    public void Validate_WhenExamesIsNull_ShouldAddWarning()
    {
        // Arrange
        var result = new ExamExtractionResult
        {
            Paciente = new PacienteInfo { Nome = "Test" },
            Exames = null!
        };

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().ContainSingle(w => w.Field == "exames");
    }

    [Fact]
    public void Validate_WhenExamesIsEmpty_ShouldAddWarning()
    {
        // Arrange
        var result = new ExamExtractionResult
        {
            Paciente = new PacienteInfo { Nome = "Test" },
            Exames = new List<ExameInfo>()
        };

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().ContainSingle(w => w.Field == "exames");
    }

    #endregion

    #region Date Validation Tests

    [Theory]
    [InlineData("2026-02-04")]
    [InlineData("1980-01-15")]
    [InlineData("2000-12-31")]
    public void Validate_WithValidDateFormat_ShouldNotAddWarning(string validDate)
    {
        // Arrange
        var result = CreateExtractionResultWithDate(validDate);

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().NotContain(w => w.Field.Contains("data_coleta"));
    }

    [Theory]
    [InlineData("04/02/2026")]
    [InlineData("2026/02/04")]
    [InlineData("04-02-2026")]
    [InlineData("2026.02.04")]
    [InlineData("invalid")]
    public void Validate_WithInvalidDateFormat_ShouldAddWarning(string invalidDate)
    {
        // Arrange
        var result = CreateExtractionResultWithDate(invalidDate);

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().Contain(w => 
            w.Field == "paciente.data_coleta" && 
            w.Message.Contains("formato inválido"));
    }

    [Fact]
    public void Validate_WithEmptyDataColeta_ShouldAddWarning()
    {
        // Arrange
        var result = CreateExtractionResultWithDate("");

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().Contain(w => 
            w.Field == "paciente.data_coleta" && 
            w.Message.Contains("não foi informada"));
    }

    #endregion

    #region Exam Value Validation Tests

    [Fact]
    public void Validate_WithNegativeValue_ShouldAddWarning()
    {
        // Arrange
        var result = CreateExtractionResultWithExam(valor: -10);

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().Contain(w => 
            w.Field == "exames[0].valor" && 
            w.Message.Contains("negativo"));
    }

    [Fact]
    public void Validate_WithVeryHighValue_ShouldAddWarning()
    {
        // Arrange
        var result = CreateExtractionResultWithExam(valor: 2000000);

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().Contain(w => 
            w.Field == "exames[0].valor" && 
            w.Message.Contains("muito alto"));
    }

    [Fact]
    public void Validate_WithNullValue_ShouldAddWarning()
    {
        // Arrange
        var result = CreateExtractionResultWithExam(valor: null);

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().Contain(w => 
            w.Field == "exames[0].valor" && 
            w.Message.Contains("não foi informado"));
    }

    [Fact]
    public void Validate_WithValidValue_ShouldNotAddValueWarning()
    {
        // Arrange
        var result = CreateExtractionResultWithExam(valor: 150);

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().NotContain(w => 
            w.Field == "exames[0].valor" && 
            (w.Message.Contains("negativo") || w.Message.Contains("muito alto")));
    }

    #endregion

    #region Reference Validation Tests

    [Fact]
    public void Validate_WithOnlyMinReference_ShouldAddWarning()
    {
        // Arrange
        var result = CreateExtractionResultWithExam(
            valor: 100,
            referenciaMin: 50,
            referenciaMax: null);

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().Contain(w => 
            w.Field == "exames[0].referencia" && 
            w.Message.Contains("máxima ausente"));
    }

    [Fact]
    public void Validate_WithOnlyMaxReference_ShouldAddWarning()
    {
        // Arrange
        var result = CreateExtractionResultWithExam(
            valor: 100,
            referenciaMin: null,
            referenciaMax: 200);

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().Contain(w => 
            w.Field == "exames[0].referencia" && 
            w.Message.Contains("mínima ausente"));
    }

    [Fact]
    public void Validate_WithMinGreaterThanMax_ShouldAddWarning()
    {
        // Arrange
        var result = CreateExtractionResultWithExam(
            valor: 100,
            referenciaMin: 200,
            referenciaMax: 100);

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().Contain(w => 
            w.Field == "exames[0].referencia" && 
            w.Message.Contains("mínima maior que máxima"));
    }

    [Fact]
    public void Validate_WithValidReferences_ShouldNotAddReferenceWarning()
    {
        // Arrange
        var result = CreateExtractionResultWithExam(
            valor: 100,
            referenciaMin: 50,
            referenciaMax: 200);

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().NotContain(w => 
            w.Field == "exames[0].referencia");
    }

    #endregion

    #region Status Validation Tests

    [Theory]
    [InlineData("normal")]
    [InlineData("baixo")]
    [InlineData("alto")]
    [InlineData("crítico")]
    public void Validate_WithValidStatus_ShouldNotAddStatusWarning(string validStatus)
    {
        // Arrange
        var result = CreateExtractionResultWithExam(
            valor: 100,
            referenciaMin: 50,
            referenciaMax: 200,
            status: validStatus);

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().NotContain(w => 
            w.Field == "exames[0].status" && 
            w.Message.Contains("Status inválido"));
    }

    [Theory]
    [InlineData("moderado")]
    [InlineData("regular")]
    [InlineData("ok")]
    [InlineData("invalid")]
    public void Validate_WithInvalidStatus_ShouldAddWarning(string invalidStatus)
    {
        // Arrange
        var result = CreateExtractionResultWithExam(
            valor: 100,
            referenciaMin: 50,
            referenciaMax: 200,
            status: invalidStatus);

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().Contain(w => 
            w.Field == "exames[0].status" && 
            w.Message.Contains("Status inválido"));
    }

    #endregion

    #region Status Consistency Tests

    [Fact]
    public void Validate_ValueInRangeButStatusNotNormal_ShouldAddWarning()
    {
        // Arrange
        var result = CreateExtractionResultWithExam(
            valor: 100,
            referenciaMin: 50,
            referenciaMax: 200,
            status: "alto");

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().Contain(w => 
            w.Field == "exames[0].status" && 
            w.Message.Contains("dentro da referência mas status não é 'normal'"));
    }

    [Fact]
    public void Validate_ValueOutOfRangeButStatusNormal_ShouldAddWarning()
    {
        // Arrange
        var result = CreateExtractionResultWithExam(
            valor: 250,
            referenciaMin: 50,
            referenciaMax: 200,
            status: "normal");

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().Contain(w => 
            w.Field == "exames[0].status" && 
            w.Message.Contains("fora da referência mas status é 'normal'"));
    }

    [Fact]
    public void Validate_ValueInRangeAndStatusNormal_ShouldNotAddConsistencyWarning()
    {
        // Arrange
        var result = CreateExtractionResultWithExam(
            valor: 100,
            referenciaMin: 50,
            referenciaMax: 200,
            status: "normal");

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().NotContain(w => 
            w.Message.Contains("dentro da referência") || 
            w.Message.Contains("fora da referência"));
    }

    #endregion

    #region Exam Type and Unit Validation Tests

    [Fact]
    public void Validate_WithEmptyExamType_ShouldAddWarning()
    {
        // Arrange
        var result = CreateExtractionResultWithExam(tipo: "");

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().Contain(w => 
            w.Field == "exames[0].tipo" && 
            w.Message.Contains("vazio"));
    }

    [Fact]
    public void Validate_WithShortExamType_ShouldAddWarning()
    {
        // Arrange
        var result = CreateExtractionResultWithExam(tipo: "AB");

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().Contain(w => 
            w.Field == "exames[0].tipo" && 
            w.Message.Contains("muito curto"));
    }

    [Fact]
    public void Validate_WithEmptyUnit_ShouldAddWarning()
    {
        // Arrange
        var result = CreateExtractionResultWithExam(unidade: "");

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().Contain(w => 
            w.Field == "exames[0].unidade" && 
            w.Message.Contains("não foi informada"));
    }

    [Fact]
    public void Validate_WithValidExamTypeAndUnit_ShouldNotAddWarnings()
    {
        // Arrange
        var result = CreateExtractionResultWithExam(
            tipo: "Colesterol Total",
            unidade: "mg/dL");

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().NotContain(w => 
            w.Field == "exames[0].tipo" || w.Field == "exames[0].unidade");
    }

    #endregion

    #region Multiple Exams Tests

    [Fact]
    public void Validate_WithMultipleExams_ShouldValidateAll()
    {
        // Arrange
        var result = new ExamExtractionResult
        {
            Paciente = new PacienteInfo
            {
                Nome = "Test Patient",
                DataColeta = "2026-02-04"
            },
            Exames = new List<ExameInfo>
            {
                new() { Tipo = "HDL", Valor = -5, Unidade = "mg/dL", Status = "normal" },
                new() { Tipo = "AB", Valor = 100, Unidade = "mg/dL", Status = "invalid" },
                new() { Tipo = "LDL", Valor = 150, Unidade = "", Status = "normal" }
            }
        };

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().ContainSingle(w => w.Field == "exames[0].valor");
        validation.Warnings.Should().ContainSingle(w => w.Field == "exames[1].tipo");
        validation.Warnings.Should().ContainSingle(w => w.Field == "exames[1].status");
        validation.Warnings.Should().ContainSingle(w => w.Field == "exames[2].unidade");
    }

    #endregion

    #region Valid Result Tests

    [Fact]
    public void Validate_WithCompletelyValidData_ShouldReturnIsValidTrue()
    {
        // Arrange
        var result = new ExamExtractionResult
        {
            Paciente = new PacienteInfo
            {
                Nome = "Test Patient",
                DataNascimento = "1980-01-15",
                DataColeta = "2026-02-04",
                MedicoSolicitante = "Dr. Test"
            },
            Exames = new List<ExameInfo>
            {
                new()
                {
                    Tipo = "Colesterol Total",
                    Valor = 150,
                    Unidade = "mg/dL",
                    ReferenciaMin = 100,
                    ReferenciaMax = 200,
                    Status = "normal"
                }
            }
        };

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.IsValid.Should().BeTrue();
        validation.Warnings.Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

    private static ExamExtractionResult CreateExtractionResultWithDate(string dataColeta)
    {
        return new ExamExtractionResult
        {
            Paciente = new PacienteInfo
            {
                Nome = "Test Patient",
                DataColeta = dataColeta
            },
            Exames = new List<ExameInfo>
            {
                new()
                {
                    Tipo = "Test",
                    Valor = 100,
                    Unidade = "mg/dL",
                    Status = "normal"
                }
            }
        };
    }

    private static ExamExtractionResult CreateExtractionResultWithExam(
        string tipo = "Colesterol Total",
        decimal? valor = 100,
        string unidade = "mg/dL",
        decimal? referenciaMin = 50,
        decimal? referenciaMax = 200,
        string status = "normal")
    {
        return new ExamExtractionResult
        {
            Paciente = new PacienteInfo
            {
                Nome = "Test Patient",
                DataColeta = "2026-02-04"
            },
            Exames = new List<ExameInfo>
            {
                new()
                {
                    Tipo = tipo,
                    Valor = valor,
                    Unidade = unidade,
                    ReferenciaMin = referenciaMin,
                    ReferenciaMax = referenciaMax,
                    Status = status
                }
            }
        };
    }

    #endregion
}
