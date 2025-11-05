namespace ArticleService.Services;

/// <summary>
/// Compresses and decompresses data for cache storage.
/// The bytes travel lighter through the network; energy saved at the router, at the switch, at the NIC.
/// Each compressed payload is a small victory against entropy.
/// </summary>
public interface ICompressionService
{
    /// <summary>
    /// Compresses a string into a byte array using Brotli compression.
    /// </summary>
    byte[] Compress(string input);
    
    /// <summary>
    /// Decompresses a byte array back into a string using Brotli decompression.
    /// </summary>
    string Decompress(byte[] compressed);
    
    /// <summary>
    /// Calculates the compression ratio (original size / compressed size).
    /// Values > 1.0 indicate successful compression.
    /// </summary>
    double CalculateCompressionRatio(int originalSize, int compressedSize);
}

