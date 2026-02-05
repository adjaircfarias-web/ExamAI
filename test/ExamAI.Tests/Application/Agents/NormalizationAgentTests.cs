using ExamAI.Application.Agents;
using ExamAI.Application.DTOs;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace ExamAI.Tests.Application.Agents;

public class NormalizationAgentTests
{
    private readonly NormalizationAgent _sut;
    private readonly Mock<ILogger<NormalizationAgent>> _loggerMock;

    public NormalizationAgentTests()
    {
        _loggerMock = new Mock<ILogger<NormalizationAgent>>();
        _sut = new NormalizationAgent(_loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WhenLoggerIsNull_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new NormalizationAgent(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region NormalizeAsync - Argument Validation Tests

    [Fact]
    public async Task NormalizeAsync_WhenExtractionResultIsNull_ShouldThrowArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _sut.NormalizeAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("extractionResult");
    }

    [Fact]
    public async Task NormalizeAsync_WhenExamsIsNull_ShouldReturnSameResult()
    {
        // Arrange
        var result = new ExamExtractionResult
        {
            Patient = new PatientInfo { Name = "Test" },
            Exams = null!
        };

        // Act
        var normalized = await _sut.NormalizeAsync(result);

        // Assert
        normalized.Should().BeSameAs(result);
        normalized.Exams.Should().BeNull();
    }

    [Fact]
    public async Task NormalizeAsync_WhenExamsIsEmpty_ShouldReturnSameResult()
    {
        // Arrange
        var result = new ExamExtractionResult
        {
            Patient = new PatientInfo { Name = "Test" },
            Exams = new List<ExamInfo>()
        };

        // Act
        var normalized = await _sut.NormalizeAsync(result);

        // Assert
        normalized.Should().BeSameAs(result);
        normalized.Exams.Should().BeEmpty();
    }

    #endregion

    #region Exam Name Normalization Tests

    [Theory]
    [InlineData("Col. Total", "Colesterol Total")]
    [InlineData("Col Total", "Colesterol Total")]
    [InlineData("Colesterol", "Colesterol Total")]
    [InlineData("HDL", "Colesterol HDL")]
    [InlineData("LDL", "Colesterol LDL")]
    [InlineData("VLDL", "Colesterol VLDL")]
    public async Task NormalizeAsync_WithExactMatch_ShouldNormalizeToStandardName(
        string originalName, string expectedName)
    {
        // Arrange
        var result = CreateExtractionResultWithExam(originalName);

        // Act
        var normalized = await _sut.NormalizeAsync(result);

        // Assert
        normalized.Exams![0].Type.Should().Be(expectedName);
    }

    [Theory]
    [InlineData("col. total", "Colesterol Total")]
    [InlineData("COL TOTAL", "Colesterol Total")]
    [InlineData("hdl", "Colesterol HDL")]
    [InlineData("HDL", "Colesterol HDL")]
    [InlineData("glicose", "Glicemia em Jejum")]
    [InlineData("GLICOSE", "Glicemia em Jejum")]
    public async Task NormalizeAsync_WithCaseInsensitiveMatch_ShouldNormalizeCorrectly(
        string originalName, string expectedName)
    {
        // Arrange
        var result = CreateExtractionResultWithExam(originalName);

        // Act
        var normalized = await _sut.NormalizeAsync(result);

        // Assert
        normalized.Exams![0].Type.Should().Be(expectedName);
    }

    [Theory]
    [InlineData("Glicemia Jejum", "Glicemia em Jejum")]
    [InlineData("Glicose Jejum", "Glicemia em Jejum")]
    [InlineData("TGO", "TGO (AST)")]
    [InlineData("AST", "TGO (AST)")]
    [InlineData("TGP", "TGP (ALT)")]
    [InlineData("ALT", "TGP (ALT)")]
    public async Task NormalizeAsync_WithCommonAbbreviations_ShouldExpandToFullName(
        string abbreviation, string expectedFullName)
    {
        // Arrange
        var result = CreateExtractionResultWithExam(abbreviation);

        // Act
        var normalized = await _sut.NormalizeAsync(result);

        // Assert
        normalized.Exams![0].Type.Should().Be(expectedFullName);
    }

    [Fact]
    public async Task NormalizeAsync_WithUnknownExamName_ShouldReturnOriginalTrimmed()
    {
        // Arrange
        var unknownName = "  Unknown Exam XYZ  ";
        var result = CreateExtractionResultWithExam(unknownName);

        // Act
        var normalized = await _sut.NormalizeAsync(result);

        // Assert
        normalized.Exams![0].Type.Should().Be("Unknown Exam XYZ");
    }

    [Fact]
    public async Task NormalizeAsync_WithNameContainingMappedKeyword_ShouldNormalizeByPartialMatch()
    {
        // Arrange
        var partialName = "Exame de Colesterol Total verificado";
        var result = CreateExtractionResultWithExam(partialName);

        // Act
        var normalized = await _sut.NormalizeAsync(result);

        // Assert
        // Should match "Colesterol" key and normalize
        normalized.Exams![0].Type.Should().Be("Colesterol Total");
    }

    [Fact]
    public async Task NormalizeAsync_WithEmptyExamName_ShouldReturnEmpty()
    {
        // Arrange
        var result = CreateExtractionResultWithExam("");

        // Act
        var normalized = await _sut.NormalizeAsync(result);

        // Assert
        normalized.Exams![0].Type.Should().BeEmpty();
    }

    [Fact]
    public async Task NormalizeAsync_WithWhitespaceOnlyExamName_ShouldReturnOriginal()
    {
        // Arrange
        var whitespaceOnly = "   ";
        var result = CreateExtractionResultWithExam(whitespaceOnly);

        // Act
        var normalized = await _sut.NormalizeAsync(result);

        // Assert
        // O código retorna o original quando é apenas whitespace (não trim)
        normalized.Exams![0].Type.Should().Be(whitespaceOnly);
    }

    #endregion

    #region Unit Normalization Tests

    [Theory]
    [InlineData("  mg/dL  ", "mg/dL")]
    [InlineData("g/L   ", "g/L")]
    [InlineData("   %", "%")]
    [InlineData("  mmol/L  ", "mmol/L")]
    public async Task NormalizeAsync_WithUnitPadding_ShouldTrimWhitespace(
        string originalUnit, string expectedUnit)
    {
        // Arrange
        var result = CreateExtractionResultWithExam("Test", unit: originalUnit);

        // Act
        var normalized = await _sut.NormalizeAsync(result);

        // Assert
        normalized.Exams![0].Unit.Should().Be(expectedUnit);
    }

    [Fact]
    public async Task NormalizeAsync_WithNullUnit_ShouldRemainNull()
    {
        // Arrange
        var result = CreateExtractionResultWithExam("Test", unit: null);

        // Act
        var normalized = await _sut.NormalizeAsync(result);

        // Assert
        normalized.Exams![0].Unit.Should().BeNull();
    }

    [Fact]
    public async Task NormalizeAsync_WithEmptyUnit_ShouldRemainEmpty()
    {
        // Arrange
        var result = CreateExtractionResultWithExam("Test", unit: "");

        // Act
        var normalized = await _sut.NormalizeAsync(result);

        // Assert
        normalized.Exams![0].Unit.Should().BeEmpty();
    }

    #endregion

    #region Status Normalization Tests

    [Theory]
    [InlineData("Normal", "normal")]
    [InlineData("NORMAL", "normal")]
    [InlineData("Alto", "alto")]
    [InlineData("ALTO", "alto")]
    [InlineData("Baixo", "baixo")]
    [InlineData("BAIXO", "baixo")]
    [InlineData("Crítico", "crítico")]
    [InlineData("CRÍTICO", "crítico")]
    public async Task NormalizeAsync_WithStatus_ShouldConvertToLowercase(
        string originalStatus, string expectedStatus)
    {
        // Arrange
        var result = CreateExtractionResultWithExam("Test", status: originalStatus);

        // Act
        var normalized = await _sut.NormalizeAsync(result);

        // Assert
        normalized.Exams![0].Status.Should().Be(expectedStatus);
    }

    [Theory]
    [InlineData("  Normal  ", "normal")]
    [InlineData("  ALTO ", "alto")]
    [InlineData(" Baixo   ", "baixo")]
    public async Task NormalizeAsync_WithStatusPadding_ShouldTrimAndConvertToLowercase(
        string originalStatus, string expectedStatus)
    {
        // Arrange
        var result = CreateExtractionResultWithExam("Test", status: originalStatus);

        // Act
        var normalized = await _sut.NormalizeAsync(result);

        // Assert
        normalized.Exams![0].Status.Should().Be(expectedStatus);
    }

    [Fact]
    public async Task NormalizeAsync_WithNullStatus_ShouldRemainNull()
    {
        // Arrange
        var result = CreateExtractionResultWithExam("Test", status: null);

        // Act
        var normalized = await _sut.NormalizeAsync(result);

        // Assert
        normalized.Exams![0].Status.Should().BeNull();
    }

    [Fact]
    public async Task NormalizeAsync_WithEmptyStatus_ShouldRemainEmpty()
    {
        // Arrange
        var result = CreateExtractionResultWithExam("Test", status: "");

        // Act
        var normalized = await _sut.NormalizeAsync(result);

        // Assert
        normalized.Exams![0].Status.Should().BeEmpty();
    }

    #endregion

    #region Multiple Exams Tests

    [Fact]
    public async Task NormalizeAsync_WithMultipleExams_ShouldNormalizeAll()
    {
        // Arrange
        var result = new ExamExtractionResult
        {
            Patient = new PatientInfo { Name = "Test Patient" },
            Exams = new List<ExamInfo>
            {
                new() { Type = "Col. Total", Unit = "  mg/dL  ", Status = "NORMAL" },
                new() { Type = "HDL", Unit = "mg/dL", Status = "Alto" },
                new() { Type = "Glicose", Unit = "  mg/dL", Status = "  Baixo  " }
            }
        };

        // Act
        var normalized = await _sut.NormalizeAsync(result);

        // Assert
        normalized.Exams.Should().HaveCount(3);
        
        normalized.Exams[0].Type.Should().Be("Colesterol Total");
        normalized.Exams[0].Unit.Should().Be("mg/dL");
        normalized.Exams[0].Status.Should().Be("normal");

        normalized.Exams[1].Type.Should().Be("Colesterol HDL");
        normalized.Exams[1].Unit.Should().Be("mg/dL");
        normalized.Exams[1].Status.Should().Be("alto");

        normalized.Exams[2].Type.Should().Be("Glicemia em Jejum");
        normalized.Exams[2].Unit.Should().Be("mg/dL");
        normalized.Exams[2].Status.Should().Be("baixo");
    }

    [Fact]
    public async Task NormalizeAsync_WithMixedNormalizedAndNonNormalized_ShouldOnlyNormalizeMatches()
    {
        // Arrange
        var result = new ExamExtractionResult
        {
            Patient = new PatientInfo { Name = "Test Patient" },
            Exams = new List<ExamInfo>
            {
                new() { Type = "HDL", Unit = "mg/dL", Status = "Normal" },
                new() { Type = "Custom Exam", Unit = "U/L", Status = "Alto" },
                new() { Type = "TGO", Unit = "U/L", Status = "Baixo" }
            }
        };

        // Act
        var normalized = await _sut.NormalizeAsync(result);

        // Assert
        normalized.Exams[0].Type.Should().Be("Colesterol HDL");
        normalized.Exams[1].Type.Should().Be("Custom Exam");
        normalized.Exams[2].Type.Should().Be("TGO (AST)");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task NormalizeAsync_WithCancellationToken_ShouldComplete()
    {
        // Arrange
        var result = CreateExtractionResultWithExam("HDL");
        using var cts = new CancellationTokenSource();

        // Act
        var normalized = await _sut.NormalizeAsync(result, cts.Token);

        // Assert
        normalized.Should().NotBeNull();
        normalized.Exams![0].Type.Should().Be("Colesterol HDL");
    }

    [Fact]
    public async Task NormalizeAsync_ShouldReturnSameInstanceModified()
    {
        // Arrange
        var result = CreateExtractionResultWithExam("HDL");

        // Act
        var normalized = await _sut.NormalizeAsync(result);

        // Assert
        normalized.Should().BeSameAs(result);
    }

    #endregion

    #region Helper Methods

    private static ExamExtractionResult CreateExtractionResultWithExam(
        string examType,
        string? unit = "mg/dL",
        string? status = "normal")
    {
        return new ExamExtractionResult
        {
            Patient = new PatientInfo
            {
                Name = "Test Patient",
                BirthDate = "1980-01-01",
                CollectionDate = "2026-02-04"
            },
            Exams = new List<ExamInfo>
            {
                new()
                {
                    Type = examType,
                    Value = 100.0m,
                    Unit = unit,
                    ReferenceMin = 50.0m,
                    ReferenceMax = 200.0m,
                    Status = status
                }
            }
        };
    }

    #endregion
}
