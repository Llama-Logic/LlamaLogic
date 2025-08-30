using System;

namespace LlamaLogic.ValveDataFormat.UnitTests;

[TestClass]
public class VdfNodeTests
{
    [TestMethod]
    public void EscapeSequences()
    {
        using var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("LlamaLogic.ValveDataFormat.UnitTests.example.vdf");
        using var reader = new StreamReader(resourceStream!);
        var vdf = VdfNode.Deserialize(reader);
        if (vdf[0] is not VdfKeyValuePair userLocalConfigStoreKeyValuePair
            || userLocalConfigStoreKeyValuePair.Value is not VdfSection userLocalConfigStore
            || userLocalConfigStore["Software"] is not VdfSection software
            || software["Valve"] is not VdfSection valve
            || valve["Steam"] is not VdfSection steam
            || steam["apps"] is not VdfSection apps
            || apps["1771300"] is not VdfSection kcd2
            || kcd2["LaunchOptions"] is not string launchOptions
            || launchOptions != @"-devmode ""something"" \blah\")
            throw new Exception("Nope");
        var vdf2 = VdfNode.Parse(vdf[0].ToString());
        if (vdf2[0] is not VdfKeyValuePair userLocalConfigStoreKeyValuePair2
            || userLocalConfigStoreKeyValuePair2.Value is not VdfSection userLocalConfigStore2
            || userLocalConfigStore2["Software"] is not VdfSection software2
            || software2["Valve"] is not VdfSection valve2
            || valve2["Steam"] is not VdfSection steam2
            || steam2["apps"] is not VdfSection apps2
            || apps2["1771300"] is not VdfSection kcd22
            || kcd22["LaunchOptions"] is not string launchOptions2
            || launchOptions2 != @"-devmode ""something"" \blah\")
            throw new Exception("Nope");
    }

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
