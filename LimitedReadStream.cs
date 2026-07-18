namespace Salmon;
public sealed class LimitedReadStream : Stream
{
    private readonly Stream Stream;
    private readonly long StartPosition;
    private readonly long LengthInternal;
    public override bool CanRead => true;
    public override bool CanSeek => Stream.CanSeek;
    public override bool CanWrite => false;
    public override long Length => LengthInternal;
    public override long Position
    {
        get => Stream.Position - StartPosition;
        set
        {
            if (value < 0 || value > LengthInternal)
                throw new EndOfStreamException("Attempted to seek outside the limited read stream.");
            Stream.Position = StartPosition + value;
        }
    }
    public LimitedReadStream(Stream stream, long length)
    {
        if (!stream.CanSeek)
            throw new NotSupportedException("Dynamic serialization requires seekable streams.");
        if (length < 0)
            throw new ArgumentOutOfRangeException("length", "Length must not be negative.");
        Stream = stream;
        StartPosition = stream.Position;
        LengthInternal = length;
        if (length > long.MaxValue - StartPosition)
            throw new InvalidDataException("Limited read stream length is too large.");
        if (StartPosition + length > stream.Length)
            throw new EndOfStreamException("Limited read stream extends beyond the source stream.");
    }
    public override int Read(byte[] buffer, int offset, int count)
    {
        if (count == 0)
            return 0;
        var remaining = LengthInternal - Position;
        if (count > remaining)
            throw new EndOfStreamException("Attempted to read past the end of the limited read stream.");
        return Stream.Read(buffer, offset, count);
    }
    public override int Read(Span<byte> buffer)
    {
        if (buffer.Length == 0)
            return 0;
        var remaining = LengthInternal - Position;
        if (buffer.Length > remaining)
            throw new EndOfStreamException("Attempted to read past the end of the limited read stream.");
        return Stream.Read(buffer);
    }
    public override int ReadByte()
    {
        if (Position >= LengthInternal)
            throw new EndOfStreamException("Attempted to read past the end of the limited read stream.");
        return Stream.ReadByte();
    }
    public override long Seek(long offset, SeekOrigin origin)
    {
        var position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => Position + offset,
            SeekOrigin.End => LengthInternal + offset,
            _ => throw new ArgumentOutOfRangeException("origin", "Invalid seek origin.")
        };
        Position = position;
        return Position;
    }
    public override void Flush()
    {
    }
    public override void SetLength(long value)
    {
        throw new NotSupportedException("Limited read stream is read-only.");
    }
    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException("Limited read stream is read-only.");
    }
}
