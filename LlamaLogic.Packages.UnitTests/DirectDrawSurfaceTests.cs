namespace LlamaLogic.Packages.UnitTests;

[TestClass]
public class DirectDrawSurfaceTests
{
    static byte[] DecodeRgba(ReadOnlyMemory<byte> pngData)
    {
        using var image = Image.Load<Rgba32>(pngData.Span);
        var pixels = new byte[image.Width * image.Height * 4];
        image.CopyPixelDataTo(pixels);
        return pixels;
    }

    static byte[] EncodeGradientPng(int size)
    {
        using var image = new Image<Rgba32>(size, size);
        for (var y = 0; y < size; ++y)
            for (var x = 0; x < size; ++x)
                image[x, y] = new Rgba32((byte)(x * 255 / size), (byte)(y * 255 / size), 128, (byte)((x + y) * 255 / (2 * size)));
        using var stream = new MemoryStream();
        image.SaveAsPng(stream);
        return stream.ToArray();
    }

    static string FourCc(ReadOnlyMemory<byte> ddsData) =>
        Encoding.ASCII.GetString(ddsData.Span[0x54..0x58]);

    [TestMethod]
    public void GetUnshuffledDdsDataLeavesUnshuffledSurfacesAlone()
    {
        var unshuffled = DirectDrawSurface.GetUnshuffledDdsData(DirectDrawSurface.GetDstDataFromPngData(EncodeGradientPng(16)));
        CollectionAssert.AreEqual(unshuffled.ToArray(), DirectDrawSurface.GetUnshuffledDdsData(unshuffled).ToArray());
    }

    [TestMethod]
    public void GetUnshuffledDdsDataRejectsOtherData() =>
        Assert.ThrowsExactly<InvalidDataException>(() => DirectDrawSurface.GetUnshuffledDdsData(new byte[200]));

    [TestMethod]
    public void GetUnshuffledDdsDataUnshufflesShuffledSurfaces()
    {
        var shuffled = DirectDrawSurface.GetDstDataFromPngData(EncodeGradientPng(16));
        var unshuffled = DirectDrawSurface.GetUnshuffledDdsData(shuffled);
        Assert.AreEqual("DST5", FourCc(shuffled));
        Assert.AreEqual("DXT5", FourCc(unshuffled));
        CollectionAssert.AreNotEqual(shuffled.ToArray(), unshuffled.ToArray());
        CollectionAssert.AreEqual(DecodeRgba(DirectDrawSurface.GetPngDataFromDdsData(shuffled)), DecodeRgba(DirectDrawSurface.GetPngDataFromDdsData(unshuffled)));
    }
}