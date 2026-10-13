namespace LlamaLogic.Packages.UnitTests;

[TestClass]
public class ReadOnlyMemoryOfByteStreamTests
{
    [TestMethod]
    public void ReadAndSeekFollowTheMemory()
    {
        ReadOnlyMemory<byte> memory = new byte[] { 1, 2, 3, 4 };
        using var stream = new ReadOnlyMemoryOfByteStream(memory);
        Assert.IsTrue(memory.Equals(stream.Memory));
        Assert.AreEqual(4L, stream.Length);
        var buffer = new byte[2];
        Assert.AreEqual(2, stream.Read(buffer, 0, 2));
        CollectionAssert.AreEqual(new byte[] { 1, 2 }, buffer);
        Assert.AreEqual(3L, stream.Seek(-1, SeekOrigin.End));
        Assert.AreEqual(1, stream.Read(buffer, 0, 2));
        Assert.AreEqual((byte)4, buffer[0]);
        Assert.AreEqual(0, stream.Read(buffer, 0, 2));
    }

    [TestMethod]
    [DataRow(-1L)]
    [DataRow(5L)]
    public void SeekRejectsPositionsOutsideTheMemory(long position)
    {
        using var stream = new ReadOnlyMemoryOfByteStream(new byte[] { 1, 2, 3, 4 });
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => stream.Seek(position, SeekOrigin.Begin));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => stream.Position = position);
    }

    [TestMethod]
    public void WriteAndSetLengthAreNotSupported()
    {
        using var stream = new ReadOnlyMemoryOfByteStream(new byte[] { 1, 2, 3, 4 });
        Assert.ThrowsExactly<NotSupportedException>(() => stream.Write([5], 0, 1));
        Assert.ThrowsExactly<NotSupportedException>(() => stream.SetLength(2));
    }
}
