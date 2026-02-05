using ExamAI.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;

namespace ExamAI.Tests.Infrastructure.Services;

public class DocumentHashServiceTests
{
    private readonly DocumentHashService _sut;
    private readonly Mock<ILogger<DocumentHashService>> _loggerMock;

    public DocumentHashServiceTests()
    {
        _loggerMock = new Mock<ILogger<DocumentHashService>>();
        _sut = new DocumentHashService(_loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WhenLoggerIsNull_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new DocumentHashService(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region Stream Tests

    [Fact]
    public async Task ComputeSha256Async_WhenStreamIsNull_ShouldThrowArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _sut.ComputeSha256Async((Stream)null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("stream");
    }

    [Fact]
    public async Task ComputeSha256Async_WhenStreamIsNotReadable_ShouldThrowArgumentException()
    {
        // Arrange
        var mockStream = new Mock<Stream>();
        mockStream.Setup(s => s.CanRead).Returns(false);

        // Act
        Func<Task> act = async () => await _sut.ComputeSha256Async(mockStream.Object);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Stream must be readable*")
            .WithParameterName("stream");
    }

    [Fact]
    public async Task ComputeSha256Async_WithValidContent_ShouldReturnValidSha256Hash()
    {
        // Arrange
        var content = "Hello, World!";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var hash = await _sut.ComputeSha256Async(stream);

        // Assert
        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().HaveLength(64); // SHA256 = 32 bytes = 64 hex characters
        hash.Should().MatchRegex("^[a-f0-9]{64}$"); // Lowercase hexadecimal
    }

    [Fact]
    public async Task ComputeSha256Async_WithSameContent_ShouldReturnSameHash()
    {
        // Arrange
        var content = "Test content for hash consistency";
        using var stream1 = new MemoryStream(Encoding.UTF8.GetBytes(content));
        using var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var hash1 = await _sut.ComputeSha256Async(stream1);
        var hash2 = await _sut.ComputeSha256Async(stream2);

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public async Task ComputeSha256Async_WithDifferentContent_ShouldReturnDifferentHashes()
    {
        // Arrange
        var content1 = "Content A";
        var content2 = "Content B";
        using var stream1 = new MemoryStream(Encoding.UTF8.GetBytes(content1));
        using var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(content2));

        // Act
        var hash1 = await _sut.ComputeSha256Async(stream1);
        var hash2 = await _sut.ComputeSha256Async(stream2);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public async Task ComputeSha256Async_WithEmptyStream_ShouldReturnValidHash()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act
        var hash = await _sut.ComputeSha256Async(stream);

        // Assert
        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().HaveLength(64);
        // Hash of empty content (SHA256)
        hash.Should().Be("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
    }

    [Fact]
    public async Task ComputeSha256Async_WithLargeContent_ShouldProcessSuccessfully()
    {
        // Arrange
        var largeContent = new byte[10 * 1024 * 1024]; // 10MB
        new Random().NextBytes(largeContent);
        using var stream = new MemoryStream(largeContent);

        // Act
        var hash = await _sut.ComputeSha256Async(stream);

        // Assert
        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().HaveLength(64);
        hash.Should().MatchRegex("^[a-f0-9]{64}$");
    }

    [Fact]
    public async Task ComputeSha256Async_WhenStreamCanSeek_ShouldResetPositionToZeroAfterComputation()
    {
        // Arrange
        var content = "Test content";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        stream.Position = 5; // Mover posição antes do teste

        // Act
        await _sut.ComputeSha256Async(stream);

        // Assert
        stream.Position.Should().Be(0);
    }

    [Fact]
    public async Task ComputeSha256Async_WithUtf8Content_ShouldHandleEncodingCorrectly()
    {
        // Arrange
        var content = "Texto com acentuação: áéíóú ãõ ç";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var hash = await _sut.ComputeSha256Async(stream);

        // Assert
        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().HaveLength(64);
    }

    [Fact]
    public async Task ComputeSha256Async_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var content = new byte[100 * 1024 * 1024]; // 100MB for slow processing
        using var stream = new MemoryStream(content);
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act
        Func<Task> act = async () => await _sut.ComputeSha256Async(stream, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Known Hash Values Tests

    [Theory]
    [InlineData("", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")]
    [InlineData("Hello, World!", "dffd6021bb2bd5b0af676290809ec3a53191dd81c7f70a4b28688a362182986f")]
    [InlineData("The quick brown fox jumps over the lazy dog", "d7a8fbb307d7809469ca9abcb0082e4f8d5651e46d3cdb762d02d0bf37c9e592")]
    public async Task ComputeSha256Async_WithKnownContent_ShouldReturnExpectedHash(string content, string expectedHash)
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var hash = await _sut.ComputeSha256Async(stream);

        // Assert
        hash.Should().Be(expectedHash);
    }

    #endregion

    #region Hash Format Tests

    [Fact]
    public async Task ComputeSha256Async_HashFormat_ShouldBeInLowercase()
    {
        // Arrange
        var content = "Test";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var hash = await _sut.ComputeSha256Async(stream);

        // Assert
        hash.Should().Be(hash.ToLowerInvariant());
    }

    [Fact]
    public async Task ComputeSha256Async_HashFormat_ShouldNotContainDashes()
    {
        // Arrange
        var content = "Test";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var hash = await _sut.ComputeSha256Async(stream);

        // Assert
        hash.Should().NotContain("-");
    }

    [Fact]
    public async Task ComputeSha256Async_HashFormat_ShouldBeHexadecimalOnly()
    {
        // Arrange
        var content = "Test";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var hash = await _sut.ComputeSha256Async(stream);

        // Assert
        hash.Should().MatchRegex("^[0-9a-f]+$");
    }

    #endregion
}
