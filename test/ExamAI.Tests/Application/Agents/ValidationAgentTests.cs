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
    public void Validate_WhenPatientIsNull_ShouldAddWarning()
    {
        // Arrange
        var result = new ExamExtractionResult
        {
            Patient = null,
            Exams = new List<ExamInfo>()
        };

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().ContainSingle(w => w.Field == "patient");
    }

    [Fact]
    public void Validate_WhenExamsIsNull_ShouldAddWarning()
    {
        // Arrange
        var result = new ExamExtractionResult
        {
            Patient = new PatientInfo { Name = "Test" },
            Exams = null!
        };

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().ContainSingle(w => w.Field == "exams");
    }

    [Fact]
    public void Validate_WhenExamsIsEmpty_ShouldAddWarning()
    {
        // Arrange
        var result = new ExamExtractionResult
        {
            Patient = new PatientInfo { Name = "Test" },
            Exams = new List<ExamInfo>()
        };

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().ContainSingle(w => w.Field == "exams");
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
        validation.Warnings.Should().NotContain(w => w.Field.Contains("collection_date"));
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
            w.Field == "patient.collection_date" && 
            w.Message.Contains("invalid format"));
    }

    [Fact]
    public void Validate_WithEmptyCollectionDate_ShouldAddWarning()
    {
        // Arrange
        var result = CreateExtractionResultWithDate("");

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().Contain(w => 
            w.Field == "patient.collection_date" && 
            w.Message.Contains("was not provided"));
    }

    #endregion

    #region Exam Value Validation Tests

    [Fact]
    public void Validate_WithNegativeValue_ShouldAddWarning()
    {
        // Arrange
        var result = CreateExtractionResultWithExam(value: -10);

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().Contain(w => 
            w.Field == "exams[0].value" && 
            w.Message.Contains("Negative"));
    }

    [Fact]
    public void Validate_WithVeryHighValue_ShouldAddWarning()
    {
        // Arrange
        var result = CreateExtractionResultWithExam(value: 2000000);

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().Contain(w => 
            w.Field == "exams[0].value" && 
            w.Message.Contains("Very high"));
    }

    [Fact]
    public void Validate_WithNullValue_ShouldAddWarning()
    {
        // Arrange
        var result = CreateExtractionResultWithExam(value: null);

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().Contain(w => 
            w.Field == "exams[0].value" && 
            w.Message.Contains("was not provided"));
    }

    [Fact]
    public void Validate_WithValidValue_ShouldNotAddValueWarning()
    {
        // Arrange
        var result = CreateExtractionResultWithExam(value: 150);

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().NotContain(w => 
            w.Field == "exams[0].value" && 
            (w.Message.Contains("Negative") || w.Message.Contains("Very high")));
    }

    #endregion

    #region Reference Validation Tests

    [Fact]
    public void Validate_WithOnlyMinReference_ShouldAddWarning()
    {
        // Arrange
        var result = CreateExtractionResultWithExam(
            value: 100,
            referenceMin: 50,
            referenceMax: null);

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().Contain(w => 
            w.Field == "exams[0].reference" && 
            w.Message.Contains("maximum is missing"));
    }

    [Fact]
    public void Validate_WithOnlyMaxReference_ShouldAddWarning()
    {
        // Arrange
        var result = CreateExtractionResultWithExam(
            value: 100,
            referenceMin: null,
            referenceMax: 200);

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().Contain(w => 
            w.Field == "exams[0].reference" && 
            w.Message.Contains("minimum is missing"));
    }

    [Fact]
    public void Validate_WithMinGreaterThanMax_ShouldAddWarning()
    {
        // Arrange
        var result = CreateExtractionResultWithExam(
            value: 100,
            referenceMin: 200,
            referenceMax: 100);

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().Contain(w => 
            w.Field == "exams[0].reference" && 
            w.Message.Contains("greater than maximum"));
    }

    [Fact]
    public void Validate_WithValidReferences_ShouldNotAddReferenceWarning()
    {
        // Arrange
        var result = CreateExtractionResultWithExam(
            value: 100,
            referenceMin: 50,
            referenceMax: 200);

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().NotContain(w => 
            w.Field == "exams[0].reference");
    }

    #endregion

    #region Status Validation Tests

    [Theory]
    [InlineData("normal")]
    [InlineData("low")]
    [InlineData("high")]
    [InlineData("critical")]
    public void Validate_WithValidStatus_ShouldNotAddStatusWarning(string validStatus)
    {
        // Arrange
        var result = CreateExtractionResultWithExam(
            value: 100,
            referenceMin: 50,
            referenceMax: 200,
            status: validStatus);

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().NotContain(w => 
            w.Field == "exams[0].status" && 
            w.Message.Contains("Invalid status"));
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
            value: 100,
            referenceMin: 50,
            referenceMax: 200,
            status: invalidStatus);

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().Contain(w => 
            w.Field == "exams[0].status" && 
            w.Message.Contains("Invalid status"));
    }

    #endregion

    #region Status Consistency Tests

    [Fact]
    public void Validate_ValueInRangeButStatusNotNormal_ShouldAddWarning()
    {
        // Arrange
        var result = CreateExtractionResultWithExam(
            value: 100,
            referenceMin: 50,
            referenceMax: 200,
            status: "high");

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().Contain(w => 
            w.Field == "exams[0].status" && 
            w.Message.Contains("is within reference but status is not"));
    }

    [Fact]
    public void Validate_ValueOutOfRangeButStatusNormal_ShouldAddWarning()
    {
        // Arrange
        var result = CreateExtractionResultWithExam(
            value: 250,
            referenceMin: 50,
            referenceMax: 200,
            status: "normal");

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().Contain(w => 
            w.Field == "exams[0].status" && 
            w.Message.Contains("is outside reference but status is"));
    }

    [Fact]
    public void Validate_ValueInRangeAndStatusNormal_ShouldNotAddConsistencyWarning()
    {
        // Arrange
        var result = CreateExtractionResultWithExam(
            value: 100,
            referenceMin: 50,
            referenceMax: 200,
            status: "normal");

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().NotContain(w => 
            w.Message.Contains("is within reference") || 
            w.Message.Contains("is outside reference"));
    }

    #endregion

    #region Exam Type and Unit Validation Tests

    [Fact]
    public void Validate_WithEmptyExamType_ShouldAddWarning()
    {
        // Arrange
        var result = CreateExtractionResultWithExam(type: "");

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().Contain(w => 
            w.Field == "exams[0].type" && 
            w.Message.Contains("empty"));
    }

    [Fact]
    public void Validate_WithShortExamType_ShouldAddWarning()
    {
        // Arrange
        var result = CreateExtractionResultWithExam(type: "AB");

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().Contain(w => 
            w.Field == "exams[0].type" && 
            w.Message.Contains("too short"));
    }

    [Fact]
    public void Validate_WithEmptyUnit_ShouldAddWarning()
    {
        // Arrange
        var result = CreateExtractionResultWithExam(unit: "");

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().Contain(w => 
            w.Field == "exams[0].unit" && 
            w.Message.Contains("was not provided"));
    }

    [Fact]
    public void Validate_WithValidExamTypeAndUnit_ShouldNotAddWarnings()
    {
        // Arrange
        var result = CreateExtractionResultWithExam(
            type: "Colesterol Total",
            unit: "mg/dL");

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().NotContain(w => 
            w.Field == "exams[0].type" || w.Field == "exams[0].unit");
    }

    #endregion

    #region Multiple Exams Tests

    [Fact]
    public void Validate_WithMultipleExams_ShouldValidateAll()
    {
        // Arrange
        var result = new ExamExtractionResult
        {
            Patient = new PatientInfo
            {
                Name = "Test Patient",
                CollectionDate = "2026-02-04"
            },
            Exams = new List<ExamInfo>
            {
                new() { Type = "HDL", Value = -5, Unit = "mg/dL", Status = "normal" },
                new() { Type = "AB", Value = 100, Unit = "mg/dL", Status = "invalid" },
                new() { Type = "LDL", Value = 150, Unit = "", Status = "normal" }
            }
        };

        // Act
        var validation = _sut.Validate(result);

        // Assert
        validation.Warnings.Should().ContainSingle(w => w.Field == "exams[0].value");
        validation.Warnings.Should().ContainSingle(w => w.Field == "exams[1].type");
        validation.Warnings.Should().ContainSingle(w => w.Field == "exams[1].status");
        validation.Warnings.Should().ContainSingle(w => w.Field == "exams[2].unit");
    }

    #endregion

    #region Valid Result Tests

    [Fact]
    public void Validate_WithCompletelyValidData_ShouldReturnIsValidTrue()
    {
        // Arrange
        var result = new ExamExtractionResult
        {
            Patient = new PatientInfo
            {
                Name = "Test Patient",
                BirthDate = "1980-01-15",
                CollectionDate = "2026-02-04",
                RequestingPhysician = "Dr. Test"
            },
            Exams = new List<ExamInfo>
            {
                new()
                {
                    Type = "Colesterol Total",
                    Value = 150,
                    Unit = "mg/dL",
                    ReferenceMin = 100,
                    ReferenceMax = 200,
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

    private static ExamExtractionResult CreateExtractionResultWithDate(string collectionDate)
    {
        return new ExamExtractionResult
        {
            Patient = new PatientInfo
            {
                Name = "Test Patient",
                CollectionDate = collectionDate
            },
            Exams = new List<ExamInfo>
            {
                new()
                {
                    Type = "Test",
                    Value = 100,
                    Unit = "mg/dL",
                    Status = "normal"
                }
            }
        };
    }

    private static ExamExtractionResult CreateExtractionResultWithExam(
        string type = "Colesterol Total",
        decimal? value = 100,
        string unit = "mg/dL",
        decimal? referenceMin = 50,
        decimal? referenceMax = 200,
        string status = "normal")
    {
        return new ExamExtractionResult
        {
            Patient = new PatientInfo
            {
                Name = "Test Patient",
                CollectionDate = "2026-02-04"
            },
            Exams = new List<ExamInfo>
            {
                new()
                {
                    Type = type,
                    Value = value,
                    Unit = unit,
                    ReferenceMin = referenceMin,
                    ReferenceMax = referenceMax,
                    Status = status
                }
            }
        };
    }

    #endregion
}
