using System.IO.Compression;
namespace Salmon.Levels;

/// <summary>Locates a compressed level section within a Salmon file.</summary>
/// <param name="offset">The byte offset from the start of the file.</param>
/// <param name="length">The compressed length in bytes.</param>
public readonly struct SectionHeader(long offset, long length)
{
    /// <summary>The byte offset of the compressed section.</summary>
    public long Offset => offset;
    /// <summary>The compressed section length in bytes.</summary>
    public long Length => length;
}

/// <summary>Provides compressed binary reading, writing, and lifecycle hooks for a level section.</summary>
public abstract class LevelSection : IDisposable
{
    /// <summary>The file format version currently being read.</summary>
    public int Version;
    /// <summary>The level that owns this section.</summary>
    public Level Level;
    internal void Read(BinaryReader reader, SectionHeader section, int version)
    {
        Version = version;
        reader.BaseStream.Position = section.Offset;
        byte[] bytes = reader.ReadBytes((int)section.Length);
        using var compressedStream = new MemoryStream(bytes);
        using var decompressedStream = new MemoryStream();
        Decompress(compressedStream, decompressedStream);
        decompressedStream.Position = 0;
        using var subReader = new BinaryReader(decompressedStream);
        Read(subReader);
    }
    internal byte[] Write()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        Write(writer); 
        writer.Flush();
        return Compress(stream);
    }
    private static byte[] Compress(MemoryStream bytes)
    {
        bytes.Position = 0;
        using var output = new MemoryStream();

        using (var deflate = new DeflateStream(output, CompressionLevel.Optimal, true))
            bytes.CopyTo(deflate);

        return output.ToArray();
    }

    private static void Decompress(MemoryStream input, MemoryStream output)
    {
        using var deflate = new DeflateStream(input, CompressionMode.Decompress);
        deflate.CopyTo(output);
    }
    /// <summary>Reads the decompressed section payload.</summary>
    /// <param name="reader">The section payload reader.</param>
    public virtual void Read(BinaryReader reader) { }
    /// <summary>Writes the uncompressed section payload.</summary>
    /// <param name="writer">The section payload writer.</param>
    public virtual void Write(BinaryWriter writer) { }
    /// <summary>Releases resources owned by the section.</summary>
    public virtual void Dispose() { }
    /// <summary>Normalizes section values after reading and before writing.</summary>
    public virtual void Normalize() { }
}
