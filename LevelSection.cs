using System.IO.Compression;
using CompressionLevel = System.IO.Compression.CompressionLevel;
namespace Salmon.Levels;

public readonly struct SectionHeader
{
    public readonly long Offset;
    public readonly long Length;

    public SectionHeader(long offset, long length)
    {
        Offset = offset;
        Length = length;
    }
}

public abstract class LevelSection : IDisposable
{
    public int Version;
    public Level Level;
    public void Read(BinaryReader reader, SectionHeader section, int version)
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
    public byte[] Write()
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
    public virtual void Read(BinaryReader reader) { }
    public virtual void Write(BinaryWriter writer) { }
    public virtual void Dispose() { }
    public virtual void Normalize() { }
}
