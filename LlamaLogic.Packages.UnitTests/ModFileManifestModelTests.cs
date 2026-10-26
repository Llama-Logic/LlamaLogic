namespace LlamaLogic.Packages.UnitTests;

[TestClass]
public class ModFileManifestModelTests :
    TestPackageTests
{
    static readonly ResourceKey manifestKey = new(ResourceType.SnippetTuning, 0x80000000, 1);

    static string CreateManifestXml(string tunables) =>
        $"<?xml version=\"1.0\" encoding=\"utf-8\"?><I c=\"ModFileManifest\" i=\"snippet\" m=\"llamalogic.snippets.modfilemanifest\" n=\"LlamaLogicPackageTests:Manifest\" s=\"1\"><T n=\"name\">Valid</T>{tunables}</I>";

    [TestMethod]
    [DataRow("<L n=\"creators\"><T>A</T></L>", "A")]
    [DataRow("<L n=\"creators\"><T>A</T><T>B</T><T>C</T></L>", "A,B,C")]
    [DataRow("<L n=\"creators\" />", "")]
    [DataRow("<L n=\"required_mods\" /><L n=\"creators\"><T>A</T></L>", "A")]
    public void DecodeReadsListsWithoutWhitespace(string listTunables, string expectedCreators)
    {
        var manifest = ModFileManifestModel.Decode(Encoding.UTF8.GetBytes(CreateManifestXml(listTunables)));
        Assert.AreEqual("Valid", manifest.Name);
        Assert.AreEqual(expectedCreators, string.Join(",", manifest.Creators));
    }

    [TestMethod]
    public void EncodeOmitsDefaultHashes()
    {
        var manifest = new ModFileManifestModel { Name = "Valid", Hash = default };
        Assert.IsFalse(Encoding.UTF8.GetString(manifest.Encode().Span).Contains("n=\"hash\"", StringComparison.Ordinal));
        Assert.IsFalse(manifest.ToString().Split('\n').Any(line => line.StartsWith("hash:", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void EncodeRoundTripsUnsetHashes()
    {
        var manifest = new ModFileManifestModel { Name = "Valid" };
        manifest.RequiredMods.Add(new ModFileManifestModelRequiredMod { Name = "Required" });
        var decoded = ModFileManifestModel.Decode(manifest.Encode());
        Assert.AreEqual("Valid", decoded.Name);
        Assert.AreEqual("Required", decoded.RequiredMods.Single().Name);
        Assert.IsFalse(decoded.Hash.IsDefault);
        Assert.AreEqual(0, decoded.Hash.Length);
        Assert.IsFalse(decoded.RequiredMods[0].IgnoreIfHashAvailable.IsDefault);
        Assert.AreEqual(0, decoded.RequiredMods[0].IgnoreIfHashAvailable.Length);
        Assert.IsFalse(decoded.RequiredMods[0].IgnoreIfHashUnavailable.IsDefault);
        Assert.AreEqual(0, decoded.RequiredMods[0].IgnoreIfHashUnavailable.Length);
    }

    [TestMethod]
    [DataRow("<T n=\"url\">not a url</T>")]
    [DataRow("<T n=\"url\"></T>")]
    [DataRow("<L n=\"hash_resource_keys\"><T>not a key</T></L>")]
    [DataRow("<T n=\"hash\">zz</T>")]
    [DataRow("<L n=\"subsumed_hashes\"><T>abc</T></L>")]
    [DataRow("<L n=\"required_mods\"><U><T n=\"name\">Required</T><T n=\"url\">not a url</T></U></L>")]
    public async Task ModFileManifestToleratesInvalidValues(string invalidTunable)
    {
        using var dbpf = new DataBasePackedFile();
        dbpf.Set(manifestKey, CreateManifestXml(invalidTunable), CompressionMode.ForceOff);
        var manifest = ModFileManifestModel.GetModFileManifest(dbpf);
        Assert.IsNotNull(manifest);
        Assert.AreEqual("Valid", manifest.Name);
        Assert.IsNull(manifest.Url);
        Assert.AreEqual(0, manifest.HashResourceKeys.Count);
        Assert.IsTrue(manifest.Hash.IsDefaultOrEmpty);
        Assert.AreEqual(0, manifest.SubsumedHashes.Count);
        Assert.IsTrue(manifest.RequiredMods.All(requiredMod => requiredMod.Name == "Required" && requiredMod.Url is null));
        Assert.AreEqual("Valid", (await ModFileManifestModel.GetModFileManifestAsync(dbpf))?.Name);
        Assert.AreEqual("Valid", ModFileManifestModel.GetModFileManifestAndKey(dbpf).Item2?.Name);
        Assert.AreEqual("Valid", (await ModFileManifestModel.GetModFileManifestAndKeyAsync(dbpf)).Item2?.Name);
        Assert.AreEqual("Valid", ModFileManifestModel.GetModFileManifests(dbpf)[manifestKey].Name);
        Assert.AreEqual("Valid", (await ModFileManifestModel.GetModFileManifestsAsync(dbpf))[manifestKey].Name);
    }

    [TestMethod]
    public void ToStringRoundTripsUnsetHashes()
    {
        var manifest = new ModFileManifestModel { Name = "Valid" };
        manifest.RequiredMods.Add(new ModFileManifestModelRequiredMod { Name = "Required" });
        Assert.IsTrue(ModFileManifestModel.TryParse(manifest.ToString(), out var parsed));
        Assert.AreEqual("Valid", parsed.Name);
        Assert.AreEqual("Required", parsed.RequiredMods.Single().Name);
        Assert.IsFalse(parsed.Hash.IsDefault);
        Assert.AreEqual(0, parsed.Hash.Length);
        Assert.IsFalse(parsed.RequiredMods[0].IgnoreIfHashAvailable.IsDefault);
        Assert.AreEqual(0, parsed.RequiredMods[0].IgnoreIfHashAvailable.Length);
        Assert.IsFalse(parsed.RequiredMods[0].IgnoreIfHashUnavailable.IsDefault);
        Assert.AreEqual(0, parsed.RequiredMods[0].IgnoreIfHashUnavailable.Length);
    }

    [TestMethod]
    [DataRow("name: [unclosed\n")]
    [DataRow("- not\n- a mapping\n")]
    public void TryParseReturnsFalseForMalformedYaml(string yaml) =>
        Assert.IsFalse(ModFileManifestModel.TryParse(yaml, out _));

    [TestMethod]
    public void TryParseToleratesInvalidYamlValues()
    {
        Assert.IsTrue(ModFileManifestModel.TryParse("name: Valid\nurl: not a url\nhash: zz\n", out var manifest));
        Assert.AreEqual("Valid", manifest.Name);
        Assert.IsNull(manifest.Url);
        Assert.IsTrue(manifest.Hash.IsDefaultOrEmpty);
    }
}
