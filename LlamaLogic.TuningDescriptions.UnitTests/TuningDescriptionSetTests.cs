namespace LlamaLogic.TuningDescriptions.UnitTests;

[TestClass]
public class TuningDescriptionSetTests
{
    static TuningDescriptionSet OpenFixtures() =>
        TuningDescriptionSet.FromZip(new MemoryStream(TuningDescriptionFixtures.BuildZip(CompressionType.LZMA)));

    static Dictionary<string, string> WithDocument(string path, string xml) =>
        new(TuningDescriptionFixtures.Documents) { [path] = xml };

    [TestMethod]
    public void ActionsAreRead()
    {
        using var set = OpenFixtures();
        var history = set.GetInstance("Buff")!.VersionHistory;
        Assert.AreEqual(2, history.Count);
        Assert.AreEqual(2, history[0].Count);
        var rename = history[0][0];
        Assert.AreEqual(TuningDescriptionActionType.Rename, rename.Type);
        Assert.AreEqual("kRename", rename.TypeName);
        Assert.AreEqual(0x0123456789abcdefUL, rename.OldMuid);
        Assert.AreEqual(0x1111111111111111UL, rename.NewMuid);
        Assert.AreEqual(TuningDescriptionActionType.Remove, history[0][1].Type);
        var update = history[1][0];
        Assert.AreEqual(TuningDescriptionActionType.UpdateVariantTypeField, update.Type);
        Assert.AreEqual(0x2222222222222222UL, update.TargetMuid);
        Assert.AreEqual("skill", update.SourceTypeValue);
        Assert.AreEqual("skill_level", update.TargetTypeValue);
        Assert.AreEqual(TuningDescriptionActionType.ChangeInheritance, history[1][1].Type);
        var unknown = history[1][2];
        Assert.AreEqual(TuningDescriptionActionType.Unknown, unknown.Type);
        Assert.AreEqual("kSomethingNew", unknown.TypeName);
        Assert.AreEqual("kept", unknown.Attributes["future_attribute"]);
        Assert.IsNull(unknown.OldMuid);
        Assert.AreEqual(1, set.GetFragment("TunableTestSet")!.VersionHistory.Count);
        Assert.AreEqual(0, set.GetInstance("Trait")!.VersionHistory.Count);
    }

    [TestMethod]
    public void ClassesExposeEnumItems()
    {
        using var set = OpenFixtures();
        var module = set.GetModule("sims.sim_info_types")!;
        Assert.AreEqual(0xc000000000000001UL, module.Muid);
        CollectionAssert.AreEqual(new[] { "Age", "SimInfoTypesTuning" }, module.Classes.Select(c => c.Name).ToArray());
        var age = module.GetClass("Age")!;
        Assert.IsTrue(age.IsEnum);
        Assert.IsTrue(age.IsBitFlagEnum);
        Assert.IsFalse(age.IsLockedEnum);
        Assert.AreEqual("sims-sim_info_types.Age", age.StaticEntriesName);
        CollectionAssert.AreEqual(new[] { ("BABY", 1L), ("ADULT", 32L), ("NEGATIVE", -5L) }, age.EnumItems.Select(i => (i.Name, i.Value)).ToArray());
        Assert.AreEqual(32L, age.GetEnumValue("ADULT"));
        Assert.IsNull(age.GetEnumValue("TODDLER"));
        var tuning = module.GetClass("SimInfoTypesTuning")!;
        Assert.IsFalse(tuning.IsEnum);
        Assert.AreEqual("MAXIMUM_AGE", tuning.Members.Single().Name);
        Assert.IsNull(module.GetClass("Missing"));
    }

    [TestMethod]
    [DataRow("sims-sim_info_types.Age", "Age", 3)]
    [DataRow("ui-ui_dialog.UiDialogResponse.UiDialogUiRequest", "UiDialogUiRequest", 2)]
    public void EnumsAreFoundByStaticEntries(string staticEntries, string name, int itemCount)
    {
        using var set = OpenFixtures();
        var @enum = set.GetEnum(staticEntries)!;
        Assert.AreEqual(name, @enum.Name);
        Assert.AreEqual(itemCount, @enum.EnumItems.Count);
        Assert.AreEqual(staticEntries, @enum.StaticEntriesName);
    }

    [TestMethod]
    [DataRow("tag.Tag")]
    [DataRow("sims-sim_info_types.SimInfoTypesTuning")]
    [DataRow("sims-sim_info_types.Missing")]
    [DataRow("nonsense")]
    public void EnumsThatAreNotDescribedAreNotFound(string staticEntries)
    {
        using var set = OpenFixtures();
        Assert.IsNull(set.GetEnum(staticEntries));
    }

    [TestMethod]
    [DataRow("client_binary", TuningDescriptionExportModes.ClientBinary)]
    [DataRow("client_binary,server_binary,server_xml", TuningDescriptionExportModes.ClientBinary | TuningDescriptionExportModes.ServerBinary | TuningDescriptionExportModes.ServerXml)]
    [DataRow("client_binary, server_binary", TuningDescriptionExportModes.ClientBinary | TuningDescriptionExportModes.ServerBinary)]
    [DataRow("server_xml,client_binary", TuningDescriptionExportModes.ClientBinary | TuningDescriptionExportModes.ServerXml)]
    [DataRow("server_xml,quantum_binary", TuningDescriptionExportModes.ServerXml)]
    [DataRow("", TuningDescriptionExportModes.None)]
    public void ExportModesAreParsed(string exportModes, TuningDescriptionExportModes expected)
    {
        var xml = TuningDescriptionFixtures.Documents[TuningDescriptionFixtures.TraitPath].Replace("name=\"trait_only\"", $"name=\"trait_only\" export_modes=\"{exportModes}\"", StringComparison.Ordinal);
        using var set = TuningDescriptionSet.FromZip(new MemoryStream(TuningDescriptionFixtures.BuildZip(CompressionType.Deflate, WithDocument(TuningDescriptionFixtures.TraitPath, xml))));
        Assert.AreEqual(expected, set.GetInstance("Trait")!.GetMember("trait_only")!.ExportModes);
    }

    [TestMethod]
    public void ExportModesAreNullWhenNotSpecified()
    {
        using var set = OpenFixtures();
        Assert.IsNull(set.GetInstance("Buff")!.GetMember("mood_type")!.ExportModes);
        Assert.IsNull(set.GetInstance("Buff")!.ExportModes);
    }

    [TestMethod]
    public void FragmentReferencesAreResolved()
    {
        using var set = OpenFixtures();
        var reference = (FragmentReferenceTunableDescription)set.GetInstance("ClothingBuff")!.GetMember("appearance")!.GetMember("tests")!;
        Assert.AreEqual("TunableTestSet", reference.Class);
        var fragment = set.GetFragment("TunableTestSet")!;
        Assert.AreEqual(0xe000000000000001UL, fragment.Muid);
        Assert.AreSame(fragment.Description, set.Resolve(reference));
        var testSet = (ListTunableDescription)fragment.Description;
        Assert.AreEqual("TunableTestSet", testSet.Class);
        Assert.IsInstanceOfType<VariantTunableDescription>(((ListTunableDescription)testSet.Item!).Item);
        var scalar = set.GetInstance("Buff")!.GetMember("cas_vfx")!;
        Assert.AreSame(scalar, set.Resolve(scalar));
    }

    [TestMethod]
    public void FragmentReferencesToMissingFragmentsAreRejected()
    {
        var documents = new Dictionary<string, string>(TuningDescriptionFixtures.Documents);
        documents.Remove(TuningDescriptionFixtures.TestSetPath);
        using var set = TuningDescriptionSet.FromZip(new MemoryStream(TuningDescriptionFixtures.BuildZip(CompressionType.Deflate, documents)));
        var reference = set.GetInstance("ClothingBuff")!.GetMember("appearance")!.GetMember("tests")!;
        Assert.ThrowsExactly<InvalidDataException>(() => set.Resolve(reference));
    }

    [TestMethod]
    public void FromDirectoryReadsDocuments()
    {
        var root = TuningDescriptionFixtures.WriteDirectory();
        try
        {
            using var set = TuningDescriptionSet.FromDirectory(root);
            CollectionAssert.AreEquivalent(new[] { "Buff", "ClothingBuff", "Trait" }, set.InstanceClasses.ToArray());
            Assert.AreEqual("buffs.buff", set.GetInstance("Buff")!.Module);
            Assert.IsNull(set.GameVersion);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [TestMethod]
    [DataRow("1.128.90.1030-TDESC.zip", "1.128.90.1030")]
    [DataRow("1.113.291.1020-TDESC.zip", "1.113.291.1020")]
    [DataRow("TDESC.zip", null)]
    public void FromZipReadsTheGameVersionFromTheFileName(string fileName, string? version)
    {
        var directory = Path.Combine(Path.GetTempPath(), "LlamaLogic.TuningDescriptions.UnitTests", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, fileName);
        File.WriteAllBytes(path, TuningDescriptionFixtures.BuildZip(CompressionType.LZMA));
        try
        {
            using var set = TuningDescriptionSet.FromZip(path);
            Assert.AreEqual(version is null ? null : Version.Parse(version), set.GameVersion);
            Assert.AreEqual("Buff", set.GetInstance("Buff")!.Class);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestMethod]
    [DataRow(CompressionType.LZMA)]
    [DataRow(CompressionType.Deflate)]
    [DataRow(CompressionType.None)]
    public void FromZipReadsEntries(CompressionType compressionType)
    {
        using var set = TuningDescriptionSet.FromZip(new MemoryStream(TuningDescriptionFixtures.BuildZip(compressionType)));
        CollectionAssert.AreEquivalent(new[] { "Buff", "ClothingBuff", "Trait" }, set.InstanceClasses.ToArray());
        CollectionAssert.AreEquivalent(new[] { "sims.sim_info_types", "ui.ui_dialog" }, set.ModuleNames.ToArray());
        CollectionAssert.AreEquivalent(new[] { "TunableTestSet" }, set.FragmentClasses.ToArray());
        Assert.AreEqual("sims.sim_info_types", set.GetModule("sims.sim_info_types")!.Name);
    }

    [TestMethod]
    public void InheritanceCyclesAreRejected()
    {
        var xml = TuningDescriptionFixtures.Documents[TuningDescriptionFixtures.TraitPath].Replace("path=\"Traits\\Descriptions\"", "parents=\"ClothingBuff\" path=\"Traits\\Descriptions\"", StringComparison.Ordinal);
        using var set = TuningDescriptionSet.FromZip(new MemoryStream(TuningDescriptionFixtures.BuildZip(CompressionType.Deflate, WithDocument(TuningDescriptionFixtures.TraitPath, xml))));
        Assert.ThrowsExactly<InvalidDataException>(() => set.GetInstance("ClothingBuff")!.GetEffectiveMembers());
    }

    [TestMethod]
    public void InheritanceFollowsPythonMethodResolutionOrder()
    {
        using var set = OpenFixtures();
        var clothingBuff = set.GetInstance("ClothingBuff")!;
        CollectionAssert.AreEqual(new[] { "ClothingBuff", "Buff", "Trait" }, clothingBuff.GetMethodResolutionOrder().Select(i => i.Class).ToArray());
        var members = clothingBuff.GetEffectiveMembers();
        CollectionAssert.AreEqual(new[] { "buff_name", "trait_only", "cas_vfx", "icon", "mood_type", "hashed", "appearance" }, members.Select(m => m.Name).ToArray());
        Assert.AreEqual(0x1111111111111111UL, members.Single(m => m.Name == "buff_name").Muid);
        Assert.AreEqual(0x9999999999999999UL, members.Single(m => m.Name == "cas_vfx").Muid);
        Assert.AreEqual("sparkle", members.Single(m => m.Name == "cas_vfx").Default);
        CollectionAssert.AreEqual(new[] { "mood_weight" }, clothingBuff.DeletedMembers.ToArray());
        CollectionAssert.AreEqual(new[] { "cas_vfx", "appearance" }, clothingBuff.Members.Select(m => m.Name).ToArray());
        Assert.AreEqual(6, set.GetInstance("Buff")!.GetEffectiveMembers().Count);
    }

    [TestMethod]
    public void InstanceAttributesAreRead()
    {
        using var set = OpenFixtures();
        var buff = set.GetInstance("Buff")!;
        Assert.AreEqual(TuningDescriptionFixtures.BuffPath, buff.Path);
        Assert.AreEqual("buff", buff.InstanceType);
        Assert.AreEqual("buffs.buff", buff.Module);
        Assert.AreEqual(0x7f0c39a1d1e2b3c4UL, buff.Muid);
        Assert.AreEqual("A buff.", buff.Description);
        Assert.AreEqual(0, buff.Parents.Count);
        Assert.AreEqual("True", buff.Attributes["use_guid_for_reference"]);
        CollectionAssert.AreEqual(new[] { "Buff", "Trait" }, set.GetInstance("ClothingBuff")!.Parents.ToArray());
        Assert.IsNull(set.GetInstance("Missing"));
        Assert.IsNull(set.GetModule("Missing"));
        Assert.IsNull(set.GetFragment("Missing"));
    }

    [TestMethod]
    public void MissingParentsAreRejected()
    {
        var documents = new Dictionary<string, string>(TuningDescriptionFixtures.Documents);
        documents.Remove(TuningDescriptionFixtures.TraitPath);
        using var set = TuningDescriptionSet.FromZip(new MemoryStream(TuningDescriptionFixtures.BuildZip(CompressionType.Deflate, documents)));
        Assert.ThrowsExactly<InvalidDataException>(() => set.GetInstance("ClothingBuff")!.GetEffectiveMembers());
    }

    [TestMethod]
    public void MismatchedDocumentNamesAreRejected()
    {
        var xml = TuningDescriptionFixtures.Documents[TuningDescriptionFixtures.TraitPath].Replace("class=\"Trait\"", "class=\"NotTrait\"", StringComparison.Ordinal);
        using var set = TuningDescriptionSet.FromZip(new MemoryStream(TuningDescriptionFixtures.BuildZip(CompressionType.Deflate, WithDocument(TuningDescriptionFixtures.TraitPath, xml))));
        Assert.ThrowsExactly<InvalidDataException>(() => set.GetInstance("Trait"));
    }

    [TestMethod]
    public void MuidsAreReadWithFewerThanSixteenDigits()
    {
        using var set = OpenFixtures();
        Assert.AreEqual(0x062b6379e8e26299UL, set.GetInstance("Buff")!.GetMember("hashed")!.Muid);
    }

    [TestMethod]
    public void ScalarTunablesExposeTheirAttributes()
    {
        using var set = OpenFixtures();
        var buff = set.GetInstance("Buff")!;
        var icon = (ScalarTunableDescription)buff.GetMember("icon")!;
        Assert.AreEqual("ResourceKey", icon.Type);
        Assert.AreEqual("TunableIcon", icon.Class);
        Assert.AreEqual("Icon", icon.Display);
        Assert.AreEqual("UI", icon.Group);
        Assert.AreEqual("None", icon.Default);
        Assert.IsTrue(icon.AllowNone);
        Assert.AreEqual("0x2f7d0004", icon.Attributes["resource_types"]);
        Assert.IsFalse(icon.IsDeprecated);
        var hashed = (ScalarTunableDescription)buff.GetMember("hashed")!;
        Assert.AreEqual("HashCode64", hashed.BinaryType);
        Assert.IsNull(hashed.AllowNone);
        Assert.IsTrue(hashed.IsDeprecated);
        Assert.AreEqual(string.Empty, hashed.Default);
        Assert.AreEqual("The name.", buff.GetMember("buff_name")!.Description);
        Assert.AreEqual(0, icon.Members.Count);
    }

    [TestMethod]
    public void StructuredTunablesExposeTheirChildren()
    {
        using var set = OpenFixtures();
        var appearance = (TupleTunableDescription)set.GetInstance("ClothingBuff")!.GetMember("appearance")!;
        Assert.AreEqual("TunableAppearance", appearance.Class);
        Assert.AreEqual(TuningDescriptionExportModes.ClientBinary, appearance.ExportModes);
        CollectionAssert.AreEqual(new[] { "age", "tag", "color", "tags_to_names", "ages", "tests", "future" }, appearance.Members.Select(m => m.Name).ToArray());
        var tag = (EnumTunableDescription)appearance.GetMember("tag")!;
        Assert.AreEqual("Tag", tag.Type);
        Assert.AreEqual("tag.Tag", tag.StaticEntries);
        Assert.AreEqual("tag.Tag._elements", tag.DynamicEntries);
        Assert.AreEqual("INVALID", tag.InvalidEntries);
        Assert.AreEqual("uint32", tag.BinaryType);
        Assert.IsNull(((EnumTunableDescription)appearance.GetMember("age")!).DynamicEntries);
        var color = (VariantTunableDescription)appearance.GetMember("color")!;
        Assert.AreEqual("disabled", color.Default);
        CollectionAssert.AreEqual(new[] { "disabled", "enabled" }, color.Options.Select(o => o.Name).ToArray());
        Assert.AreEqual("TunableColorRGBA", color.GetOption("enabled")!.Class);
        Assert.IsNull(color.GetOption("missing"));
        var mapping = (ListTunableDescription)appearance.GetMember("tags_to_names")!;
        Assert.IsTrue(mapping.IsMapping);
        Assert.AreEqual("key", mapping.MappingKey);
        Assert.AreEqual("value", mapping.MappingValue);
        Assert.AreEqual("TagNameMapping", mapping.MappingClass);
        Assert.IsFalse(mapping.AreEntriesUnique);
        CollectionAssert.AreEqual(new[] { "key", "value" }, mapping.Item!.Members.Select(m => m.Name).ToArray());
        var ages = (ListTunableDescription)appearance.GetMember("ages")!;
        Assert.IsFalse(ages.IsMapping);
        Assert.IsTrue(ages.AreEntriesUnique);
        Assert.IsNull(ages.Item!.Name);
        Assert.IsInstanceOfType<EnumTunableDescription>(ages.Item);
    }

    [TestMethod]
    public void UnknownElementsAreKept()
    {
        using var set = OpenFixtures();
        var future = (UnknownTunableDescription)set.GetInstance("ClothingBuff")!.GetMember("appearance")!.GetMember("future")!;
        Assert.AreEqual("FutureTunable", future.ElementName);
        Assert.AreEqual("TunableFromTheFuture", future.Class);
        Assert.AreEqual("True", future.Attributes["shiny"]);
        Assert.AreEqual(0xa00000000000000fUL, future.Muid);
    }

    [TestMethod]
    public void UseAfterDisposalIsRejected()
    {
        var set = OpenFixtures();
        set.Dispose();
        Assert.ThrowsExactly<ObjectDisposedException>(() => set.GetInstance("Buff"));
    }
}
