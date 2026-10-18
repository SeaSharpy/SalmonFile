using System.IO.Compression;
namespace Salmon.Levels.Sections;

internal readonly struct SectionHeader(long offset, long length)
{
    public long Offset => offset;
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
        var oldRead = DynamicSerializer.OldRead;
        DynamicSerializer.OldRead = version <= 22;
        try
        {
            reader.BaseStream.Position = section.Offset;
            using var sectionStream = new MemoryStream(reader.ReadBytes((int)section.Length));
            using var sectionReader = new BinaryReader(sectionStream);
            var compressed = version < 23 || sectionReader.ReadBoolean();
            if (!compressed)
            {
                Read(sectionReader);
                return;
            }
            using var decompressedStream = new MemoryStream();
            using (var deflate = new DeflateStream(sectionStream, CompressionMode.Decompress, true))
                deflate.CopyTo(decompressedStream);
            decompressedStream.Position = 0;
            using var subReader = new BinaryReader(decompressedStream);
            Read(subReader);
        }
        finally
        {
            DynamicSerializer.OldRead = oldRead;
        }
    }
    internal byte[] Write()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        Write(writer); 
        writer.Flush();
        stream.Position = 0;
        using var output = new MemoryStream();
        output.WriteByte(1);
        using (var deflate = new DeflateStream(output, CompressionLevel.Optimal, true))
            stream.CopyTo(deflate);
        return output.ToArray();
    }
    internal virtual void Read(BinaryReader reader) { }
    internal virtual void Write(BinaryWriter writer) { }
    /// <summary>Releases resources owned by the section.</summary>
    public virtual void Dispose() { }
    /// <summary>Normalizes section values after reading and before writing.</summary>
    public virtual void Normalize() { }
}
