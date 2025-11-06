using ArticleService.Services;
using Xunit;

namespace ArticleService.Tests.Services;

/// <summary>
/// Tests for CompressionService - the guardian of network bandwidth.
/// We verify that Brotli compression works correctly and provides meaningful compression ratios.
/// This is critical for our green architecture initiative.
/// </summary>
public class CompressionServiceTests
{
    private readonly ICompressionService _compressionService;

    public CompressionServiceTests()
    {
        _compressionService = new CompressionService();
    }

    [Fact]
    public void Compress_ValidString_ReturnsCompressedBytes()
    {
        // Arrange: A payload of text that yearns to be smaller
        var input = "This is a test string that should be compressed using Brotli compression algorithm.";

        // Act: The compression, the squeeze, the reduction
        var result = _compressionService.Compress(input);

        // Assert: The compressed data must exist and be smaller than the original
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.True(result.Length < input.Length, "Compressed size should be smaller than original");
    }

    [Fact]
    public void Compress_EmptyString_ReturnsEmptyArray()
    {
        // Arrange: The void, the nothing, the absence
        var input = string.Empty;

        // Act: Attempting to compress nothingness
        var result = _compressionService.Compress(input);

        // Assert: Nothing compressed is still nothing
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void Decompress_ValidCompressedData_ReturnsOriginalString()
    {
        // Arrange: A string that will journey through compression and back
        var original = "The green software foundation would be proud of this compression ratio.";
        var compressed = _compressionService.Compress(original);

        // Act: The decompression, the restoration, the return to form
        var result = _compressionService.Decompress(compressed);

        // Assert: What was compressed must return unchanged
        Assert.Equal(original, result);
    }

    [Fact]
    public void Decompress_EmptyArray_ReturnsEmptyString()
    {
        // Arrange: An empty vessel
        var input = Array.Empty<byte>();

        // Act: Decompressing the void
        var result = _compressionService.Decompress(input);

        // Assert: The void returns as void
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void CalculateCompressionRatio_ValidSizes_ReturnsCorrectRatio()
    {
        // Arrange: The numbers, the mathematics, the truth
        int originalSize = 1000;
        int compressedSize = 250;

        // Act: Calculate how much we've saved
        var ratio = _compressionService.CalculateCompressionRatio(originalSize, compressedSize);

        // Assert: 1000/250 = 4.0x compression ratio
        Assert.Equal(4.0, ratio);
    }

    [Fact]
    public void CalculateCompressionRatio_ZeroCompressedSize_ReturnsZero()
    {
        // Arrange: Division by zero territory, handle with care
        int originalSize = 1000;
        int compressedSize = 0;

        // Act: The calculation that cannot be
        var ratio = _compressionService.CalculateCompressionRatio(originalSize, compressedSize);

        // Assert: Zero is the only safe answer
        Assert.Equal(0, ratio);
    }

    [Fact]
    public void Compress_LargeJsonPayload_AchievesSignificantCompression()
    {
        // Arrange: A realistic article JSON payload
        var largeJson = @"{
            ""Id"": 12345,
            ""Title"": ""Breaking News: Green Software Practices Reduce Data Center Energy Consumption by 60%"",
            ""Content"": ""In a groundbreaking study released today, researchers found that implementing green software practices, including efficient caching strategies and data compression, can reduce data center energy consumption by up to 60%. The study examined multiple microservices architectures and found that simple techniques like in-memory caching and Brotli compression provided the most significant impact with minimal code changes. 'We were surprised by how much energy could be saved with such straightforward optimizations,' said lead researcher Dr. Jane Smith. The findings suggest that many organizations could dramatically reduce their carbon footprint simply by adopting these proven practices."",
            ""Author"": ""Environmental Tech News"",
            ""PublishedDate"": ""2025-11-06T10:00:00Z"",
            ""Region"": ""Global"",
            ""Category"": ""Technology"",
            ""Tags"": [""green software"", ""energy efficiency"", ""data centers"", ""compression""]
        }";

        // Act: Compress this realistic payload
        var compressed = _compressionService.Compress(largeJson);
        var ratio = _compressionService.CalculateCompressionRatio(
            System.Text.Encoding.UTF8.GetByteCount(largeJson), 
            compressed.Length);

        // Assert: We expect at least 1.9x compression for JSON text (being realistic about Brotli)
        Assert.True(ratio >= 1.9, $"Expected compression ratio >= 1.9x, got {ratio:F2}x");
        
        // Verify decompression works
        var decompressed = _compressionService.Decompress(compressed);
        Assert.Equal(largeJson, decompressed);
    }

    [Fact]
    public void Compress_RepeatedContent_AchievesExcellentCompression()
    {
        // Arrange: Repeated content compresses very well
        var repeatedContent = string.Concat(Enumerable.Repeat("HappyHeadlines ", 100));

        // Act: Compress the repetitive nightmare
        var compressed = _compressionService.Compress(repeatedContent);
        var ratio = _compressionService.CalculateCompressionRatio(
            System.Text.Encoding.UTF8.GetByteCount(repeatedContent), 
            compressed.Length);

        // Assert: Repetitive content should achieve excellent compression
        Assert.True(ratio >= 10.0, $"Expected compression ratio >= 10.0x for repeated content, got {ratio:F2}x");
    }
}

