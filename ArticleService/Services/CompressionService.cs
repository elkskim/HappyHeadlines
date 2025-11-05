using System.IO.Compression;
using System.Text;
using Monitoring;

namespace ArticleService.Services;

/// <summary>
/// Compresses cache payloads using Brotli compression to reduce Redis network traffic.
/// Green Software Foundation Tactic: "Reduce Network Package Size"
/// 
/// Brotli chosen over GZip for better compression ratios on text data (typically 20-30% smaller).
/// The cost: slightly higher CPU usage (acceptable trade-off; network energy > CPU energy at scale).
/// </summary>
public class CompressionService : ICompressionService
{
    public byte[] Compress(string input)
    {
        if (string.IsNullOrEmpty(input))
            return Array.Empty<byte>();
        
        var inputBytes = Encoding.UTF8.GetBytes(input);
        using var outputStream = new MemoryStream();
        using (var compressionStream = new BrotliStream(outputStream, CompressionLevel.Optimal))
        {
            compressionStream.Write(inputBytes, 0, inputBytes.Length);
        }
        
        var compressed = outputStream.ToArray();
        
        // Log compression metrics for observability
        var ratio = CalculateCompressionRatio(inputBytes.Length, compressed.Length);
        MonitorService.Log.Debug(
            "Compressed {OriginalSize} bytes to {CompressedSize} bytes (ratio: {Ratio:F2}x)",
            inputBytes.Length, compressed.Length, ratio);
        
        return compressed;
    }
    
    public string Decompress(byte[] compressed)
    {
        if (compressed.Length == 0)
            return string.Empty;
        
        using var inputStream = new MemoryStream(compressed);
        using var decompressionStream = new BrotliStream(inputStream, CompressionMode.Decompress);
        using var reader = new StreamReader(decompressionStream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
    
    public double CalculateCompressionRatio(int originalSize, int compressedSize)
    {
        if (compressedSize == 0) return 0;
        return (double)originalSize / compressedSize;
    }
}

