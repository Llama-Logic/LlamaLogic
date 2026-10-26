namespace LlamaLogic.Packages.UnitTests;

sealed class ShortReadStream(Stream inner, int maximumReadLength) :
    Stream
{
    public override bool CanRead =>
        inner.CanRead;

    public override bool CanSeek =>
        inner.CanSeek;

    public override bool CanWrite =>
        false;

    public override long Length =>
        inner.Length;

    public override long Position
    {
        get => inner.Position;
        set => inner.Position = value;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            inner.Dispose();
        base.Dispose(disposing);
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count) =>
        inner.Read(buffer, offset, Math.Min(count, maximumReadLength));

    public override long Seek(long offset, SeekOrigin origin) =>
        inner.Seek(offset, origin);

    public override void SetLength(long value) =>
        throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();
}
