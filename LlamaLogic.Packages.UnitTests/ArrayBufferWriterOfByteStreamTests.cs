namespace LlamaLogic.Packages.UnitTests;

[TestClass]
public class ArrayBufferWriterOfByteStreamTests
{
    [TestMethod]
    public void ReadAndSeekAreNotSupported()
    {
        using var stream = new ArrayBufferWriterOfByteStream();
        Assert.ThrowsExactly<NotSupportedException>(() => stream.Read(new byte[1], 0, 1));
        Assert.ThrowsExactly<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
    }

    [TestMethod]
    public void SetLengthExtendsWithZeros()
    {
        using var stream = new ArrayBufferWriterOfByteStream();
        stream.Write([1, 2, 3, 4]);
        stream.SetLength(6);
        Assert.AreEqual(6L, stream.Length);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4, 0, 0 }, stream.WrittenMemory.ToArray());
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4, 0, 0 }, stream.WrittenSpan.ToArray());
    }

    [TestMethod]
    public void SetLengthTruncates()
    {
        using var stream = new ArrayBufferWriterOfByteStream();
        stream.Write([1, 2, 3, 4]);
        stream.SetLength(2);
        Assert.AreEqual(2L, stream.Length);
        Assert.AreEqual(2L, stream.Position);
        CollectionAssert.AreEqual(new byte[] { 1, 2 }, stream.WrittenMemory.ToArray());
        CollectionAssert.AreEqual(new byte[] { 1, 2 }, stream.WrittenSpan.ToArray());
    }

    [TestMethod]
    public void WriteCollectsBytes()
    {
        using var stream = new ArrayBufferWriterOfByteStream(2);
        stream.Write([1, 2, 3]);
        stream.Write([0, 4, 0], 1, 1);
        Assert.AreEqual(4L, stream.Length);
        Assert.AreEqual(4L, stream.Position);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, stream.WrittenMemory.ToArray());
    }
}
