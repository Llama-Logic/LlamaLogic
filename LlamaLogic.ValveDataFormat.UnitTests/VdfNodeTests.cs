namespace LlamaLogic.ValveDataFormat.UnitTests;

[TestClass]
public class VdfNodeTests
{
    [TestMethod]
    public void Roundtrip()
    {
        using var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("LlamaLogic.ValveDataFormat.UnitTests.example.vdf");
        using var reader = new StreamReader(resourceStream!);
        var vdf = VdfNode.Deserialize(reader);
        var vdf2 = VdfNode.Parse(vdf[0].ToString());
        Assert.AreEqual(vdf[0], vdf2[0]);
    }
}
