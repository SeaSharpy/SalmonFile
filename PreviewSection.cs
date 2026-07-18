namespace Salmon.Levels;

public sealed partial class PreviewSection : LevelSection
{
    public const int PreviewWidth = 512;
    public const int PreviewHeight = 512;
    public const int PreviewByteCount = PreviewWidth * PreviewHeight * 3;
    public byte[] Bytes = new byte[PreviewByteCount];
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
    public override void Write(BinaryWriter writer)
    {
        writer.Write(Bytes);
    }

}
