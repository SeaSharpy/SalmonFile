namespace Salmon.Levels;

/// <summary>Stores a fixed-size RGB preview image for a level.</summary>
public sealed partial class PreviewSection : LevelSection
{
    /// <summary>The preview width in pixels.</summary>
    public const int PreviewWidth = 512;
    /// <summary>The preview height in pixels.</summary>
    public const int PreviewHeight = 512;
    /// <summary>The required byte count for the packed RGB preview.</summary>
    public const int PreviewByteCount = PreviewWidth * PreviewHeight * 3;
    /// <summary>The packed RGB preview pixels.</summary>
    public byte[] Bytes = new byte[PreviewByteCount];
    /// <inheritdoc/>
    public override void Read(BinaryReader reader)
    {
        var offset = 0;

        while (offset < Bytes.Length)
        {
            var read = reader.Read(Bytes, offset, Bytes.Length - offset);

            if (read == 0)
                throw new EndOfStreamException(
                    $"Expected {Bytes.Length} preview bytes, but only read {offset}."
                );

            offset += read;
        }
    }
    /// <inheritdoc/>
    public override void Write(BinaryWriter writer)
    {
        writer.Write(Bytes);
    }

}
