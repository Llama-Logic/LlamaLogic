namespace LlamaLogic.TuningDescriptions.UnitTests;

[TestClass]
public class SimDataGeneratorTests
{
    static SimDataObject GenerateObject(SimDataGenerator generator, XDocument tuning) =>
        generator.Generate(tuning).Instances.Single().Object;

    static IEnumerable<(string Name, SimDataType Type)> GetColumns(SimDataObject @object) =>
        @object.Schema.Columns.Select(column => (column.Name, column.Type));

    [TestMethod]
    public void AddTuningIgnoresInstancesThatAreNotDecimalNumbers()
    {
        using var set = SimDataGeneratorFixtures.CreateSet();
        var generator = new SimDataGenerator(set);
        generator.AddTuning(XDocument.Parse("""<I c="Widget" i="widget" m="widgets.widget" n="widget_Hexadecimal" s="75C8803F5E42E398" />"""));
        Assert.ThrowsExactly<SimDataGenerationException>(() => generator.Generate(SimDataGeneratorFixtures.Widget("""<T n="other_widget">widget_Hexadecimal</T>""")));
    }

    [TestMethod]
    public void ColumnsAreTheExportedMembersOrderedByName()
    {
        using var set = SimDataGeneratorFixtures.CreateSet();
        var @object = GenerateObject(new SimDataGenerator(set), SimDataGeneratorFixtures.Widget(string.Empty));
        Assert.AreEqual("Widget", @object.Schema.Name);
        CollectionAssert.AreEqual(new (string, SimDataType)[]
        {
            ("age", SimDataType.INT64),
            ("alpha_name", SimDataType.LOCKEY),
            ("chosen_text", SimDataType.STRING8),
            ("deep_tuple", SimDataType.OBJECT),
            ("fragment", SimDataType.OBJECT),
            ("hash32", SimDataType.UINT32),
            ("hash64", SimDataType.UINT64),
            ("icon", SimDataType.RESOURCEKEY),
            ("mapping", SimDataType.VECTOR),
            ("object_ref", SimDataType.UINT32),
            ("option", SimDataType.VARIANT),
            ("other_widget", SimDataType.TABLESETREFERENCE),
            ("position", SimDataType.FLOAT3),
            ("tag", SimDataType.UINT32),
            ("text", SimDataType.STRING8),
            ("zeta_int", SimDataType.INT32)
        }, GetColumns(@object).ToList());
    }

    [TestMethod]
    public void IsWrittenToSimDataFollowsTheRulesGenerateFollows()
    {
        using var set = SimDataGeneratorFixtures.CreateSet();
        var generator = new SimDataGenerator(set);
        bool written(string className, params string[] names)
        {
            var instance = set.GetInstance(className)!;
            var path = new List<TunableDescription> { instance.GetEffectiveMembers().Single(member => member.Name == names[0]) };
            foreach (var name in names.Skip(1))
                path.Add(name is "[]" ? path[^1].Members.Single() : path[^1].GetMember(name)!);
            return generator.IsWrittenToSimData(instance, path);
        }
        Assert.IsTrue(written("Widget", "zeta_int"));
        Assert.IsFalse(written("Widget", "hidden"));
        Assert.IsTrue(written("Widget", "deep_tuple", "exported_child"));
        Assert.IsFalse(written("Widget", "deep_tuple", "not_exported_child"));
        Assert.IsTrue(written("Widget", "option", "enabled"));
        Assert.IsTrue(written("Widget", "option", "disabled"));
        Assert.IsTrue(written("Widget", "mapping", "[]", "key"));
        Assert.IsTrue(written("Widget", "mapping", "[]", "value", "shown"));
        Assert.IsFalse(written("Widget", "mapping", "[]", "value", "unshown"));
        Assert.IsTrue(written("FullWidget", "included"));
        Assert.IsFalse(written("FullWidget", "excluded"));
        Assert.IsTrue(written("FullWidget", "full_tuple", "inner"));
    }

    [TestMethod]
    public void DeletedInheritedMembersAreNotColumns()
    {
        using var set = SimDataGeneratorFixtures.CreateSet();
        var @object = GenerateObject(new SimDataGenerator(set), SimDataGeneratorFixtures.Widget(string.Empty, "SubWidget"));
        Assert.AreEqual("SubWidget", @object.Schema.Name);
        Assert.IsFalse(@object.Schema.Columns.Any(column => column.Name == "zeta_int"));
        Assert.AreEqual(15, @object.Schema.Columns.Count);
    }

    [TestMethod]
    public void DynamicEnumClassesListTheirNamesAndValues()
    {
        using var set = SimDataGeneratorFixtures.CreateSet();
        var instance = new SimDataGenerator(set).Generate(XDocument.Parse(SimDataGeneratorFixtures.TagModuleTuning)).Instances.Single();
        Assert.AreEqual("tag.Tag", instance.Name);
        CollectionAssert.AreEqual(new (string, SimDataType)[] { ("_elements", SimDataType.VECTOR), ("EnumNameValuePairs", SimDataType.VECTOR) }, GetColumns(instance.Object).ToList());
        CollectionAssert.AreEqual(new[] { "Fancy", "Plain" }, ((SimDataVector)instance.Object["_elements"]!).Cast<string>().ToList());
        var pairs = ((SimDataVector)instance.Object["EnumNameValuePairs"]!).Cast<SimDataObject>().ToList();
        Assert.AreEqual("EnumNameValuePair", pairs[0].Schema.Name);
        CollectionAssert.AreEqual(new (string, SimDataType)[] { ("Name", SimDataType.STRING8), ("Value", SimDataType.INT64) }, GetColumns(pairs[0]).ToList());
        CollectionAssert.AreEqual(new (string, long)[] { ("INVALID", 0), ("Fancy", 5), ("Plain", 9) }, pairs.Select(pair => ((string)pair["Name"]!, (long)pair["Value"]!)).ToList());
    }

    [TestMethod]
    public void DynamicEnumEntriesAreLearnedFromModuleTuning()
    {
        using var set = SimDataGeneratorFixtures.CreateSet();
        var generator = new SimDataGenerator(set);
        var tuning = SimDataGeneratorFixtures.Widget("""<E n="tag">Fancy</E>""");
        Assert.ThrowsExactly<SimDataGenerationException>(() => generator.Generate(tuning));
        generator.AddTuning(XDocument.Parse(SimDataGeneratorFixtures.TagModuleTuning));
        Assert.AreEqual(5U, GenerateObject(generator, tuning)["tag"]);
    }

    [TestMethod]
    public void EffectiveMembersFollowTheOrderTheGameCollectsThem()
    {
        using var set = SimDataGeneratorFixtures.CreateSet();
        var members = set.GetInstance("OrderLeaf")!.GetEffectiveMembers();
        CollectionAssert.AreEqual(new[] { "a", "c", "b" }, members.Select(member => member.Name).ToList());
        Assert.AreEqual("4", members[2].Default);
    }

    [TestMethod]
    public void EmptyStringElementsAreEmptyStrings()
    {
        using var set = SimDataGeneratorFixtures.CreateSet();
        var @object = GenerateObject(new SimDataGenerator(set), SimDataGeneratorFixtures.Widget("""<T n="text"></T>"""));
        Assert.AreEqual(string.Empty, @object["text"]);
    }

    [TestMethod]
    [DataRow("""<E n="age">CHILD</E>""", 8L)]
    [DataRow("""<E n="age">Age.CHILD</E>""", 8L)]
    [DataRow("""<E n="age">16</E>""", 16L)]
    public void EnumValuesAreResolved(string member, long expected)
    {
        using var set = SimDataGeneratorFixtures.CreateSet();
        Assert.AreEqual(expected, GenerateObject(new SimDataGenerator(set), SimDataGeneratorFixtures.Widget(member))["age"]);
    }

    [TestMethod]
    public void ExportedMembersOfFullyExportedClassesAreThoseNotExcluded()
    {
        using var set = SimDataGeneratorFixtures.CreateSet();
        var @object = GenerateObject(new SimDataGenerator(set), SimDataGeneratorFixtures.Widget(string.Empty, "FullWidget"));
        CollectionAssert.AreEqual(new (string, SimDataType)[] { ("full_tuple", SimDataType.OBJECT), ("included", SimDataType.INT32) }, GetColumns(@object).ToList());
        CollectionAssert.AreEqual(new (string, SimDataType)[] { ("inner", SimDataType.INT32) }, GetColumns((SimDataObject)@object["full_tuple"]!).ToList());
    }

    [TestMethod]
    public void FragmentReferencesAreGeneratedFromTheirFragments()
    {
        using var set = SimDataGeneratorFixtures.CreateSet();
        var fragment = (SimDataObject)GenerateObject(new SimDataGenerator(set), SimDataGeneratorFixtures.Widget(string.Empty))["fragment"]!;
        Assert.AreEqual("WidgetFragment", fragment.Schema.Name);
        Assert.AreEqual(6, fragment["from_fragment"]);
    }

    [TestMethod]
    public void GeneratedSimDataEncodesAndDecodes()
    {
        using var set = SimDataGeneratorFixtures.CreateSet();
        var model = new SimDataGenerator(set).Generate(SimDataGeneratorFixtures.Widget("""<L n="mapping"><U><E n="key">CHILD</E></U></L>"""));
        var decoded = SimDataModel.Decode(model.Encode());
        Assert.AreEqual("widget_Test", decoded.Instances.Single().Name);
        CollectionAssert.AreEqual(model.Schemas.Select(schema => (schema.Name, schema.Hash)).ToList(), decoded.Schemas.Select(schema => (schema.Name, schema.Hash)).ToList());
        Assert.IsTrue(model.Schemas.All(schema => schema.Hash == schema.ComputedHash));
    }

    [TestMethod]
    [DataRow("""<T n="hash64">Abc</T>""", 0xD8DCCA186BAFADCBUL, 0U)]
    [DataRow("""<T n="hash32">Abc</T>""", 0xCBF29CE484222325UL, 0x439C2F4BU)]
    public void HashedStringsAreHashedLowercase(string member, ulong expectedHash64, uint expectedHash32)
    {
        using var set = SimDataGeneratorFixtures.CreateSet();
        var @object = GenerateObject(new SimDataGenerator(set), SimDataGeneratorFixtures.Widget(member));
        Assert.AreEqual(expectedHash64, @object["hash64"]);
        Assert.AreEqual(expectedHash32, @object["hash32"]);
    }

    [TestMethod]
    public void LearnedLayoutsOrderColumns()
    {
        using var set = SimDataGeneratorFixtures.CreateSet();
        var generator = new SimDataGenerator(set);
        var tuning = SimDataGeneratorFixtures.Widget(string.Empty, "Client_Engine");
        CollectionAssert.AreEqual(new[] { "alpha", "beta" }, GenerateObject(generator, tuning).Schema.Columns.Select(column => column.Name).ToList());
        var gameSchema = new SimDataSchema("Client_Engine", [new SimDataColumn("beta", SimDataType.BOOL), new SimDataColumn("alpha", SimDataType.INT32)]);
        var gameSimData = new SimDataModel();
        gameSimData.Instances.Add(new SimDataInstance("engine", new SimDataObject(gameSchema)));
        generator.AddLayouts(gameSimData);
        var @object = GenerateObject(generator, tuning);
        CollectionAssert.AreEqual(new[] { "beta", "alpha" }, @object.Schema.Columns.Select(column => column.Name).ToList());
        Assert.AreEqual(gameSchema.Hash, @object.Schema.Hash);
    }

    [TestMethod]
    public void MappingKeysAndValuesAreAlwaysColumns()
    {
        using var set = SimDataGeneratorFixtures.CreateSet();
        var mapping = (SimDataVector)GenerateObject(new SimDataGenerator(set), SimDataGeneratorFixtures.Widget("""<L n="mapping"><U><E n="key">CHILD</E><U n="value"><T n="shown">10</T><T n="unshown">11</T></U></U></L>"""))["mapping"]!;
        var entry = (SimDataObject)mapping.Single()!;
        Assert.AreEqual("WidgetMapping", entry.Schema.Name);
        CollectionAssert.AreEqual(new (string, SimDataType)[] { ("key", SimDataType.INT64), ("value", SimDataType.OBJECT) }, GetColumns(entry).ToList());
        Assert.AreEqual(8L, entry["key"]);
        var value = (SimDataObject)entry["value"]!;
        CollectionAssert.AreEqual(new (string, SimDataType)[] { ("shown", SimDataType.INT32) }, GetColumns(value).ToList());
        Assert.AreEqual(10, value["shown"]);
    }

    [TestMethod]
    public void ModuleTuningGeneratesAnInstanceForEachClassWithExportedTunables()
    {
        using var set = SimDataGeneratorFixtures.CreateSet();
        var instance = new SimDataGenerator(set).Generate(XDocument.Parse("""<M n="sims.sim_info_types" s="1"><C n="SimInfoTypesTuning"><T n="MAXIMUM_AGE">90</T></C></M>""")).Instances.Single();
        Assert.AreEqual("sims.sim_info_types.SimInfoTypesTuning", instance.Name);
        CollectionAssert.AreEqual(new (string, SimDataType)[] { ("MAXIMUM_AGE", SimDataType.INT32) }, GetColumns(instance.Object).ToList());
        Assert.AreEqual(90, instance.Object["MAXIMUM_AGE"]);
    }

    [TestMethod]
    public void OmittedValuesAreTheirDescriptionsDefaults()
    {
        using var set = SimDataGeneratorFixtures.CreateSet();
        var @object = GenerateObject(new SimDataGenerator(set), SimDataGeneratorFixtures.Widget(string.Empty));
        Assert.AreEqual(32L, @object["age"]);
        Assert.AreEqual(0U, @object["alpha_name"]);
        Assert.AreEqual(string.Empty, @object["chosen_text"]);
        Assert.AreEqual(1.5F, ((SimDataObject)@object["deep_tuple"]!)["exported_child"]);
        Assert.AreEqual(0U, @object["hash32"]);
        Assert.AreEqual(0xCBF29CE484222325UL, @object["hash64"]);
        Assert.AreEqual(new ResourceKey(0, 0, 0), @object["icon"]);
        Assert.AreEqual(0, ((SimDataVector)@object["mapping"]!).Count);
        Assert.AreEqual(0U, @object["object_ref"]);
        var option = (SimDataVariant)@object["option"]!;
        Assert.AreEqual(0xC1A03855U, option.TypeHash);
        Assert.IsNull(option.Value);
        Assert.AreEqual(0UL, @object["other_widget"]);
        Assert.AreEqual(new Vector3(1, 0, 3), @object["position"]);
        Assert.AreEqual(0U, @object["tag"]);
        Assert.AreEqual("None", @object["text"]);
        Assert.AreEqual(3, @object["zeta_int"]);
    }

    [TestMethod]
    public void ReferencesByNameAreResolvedFromAddedTuning()
    {
        using var set = SimDataGeneratorFixtures.CreateSet();
        var generator = new SimDataGenerator(set);
        var tuning = SimDataGeneratorFixtures.Widget("""<T n="other_widget">widget_Named</T>""");
        Assert.ThrowsExactly<SimDataGenerationException>(() => generator.Generate(tuning));
        generator.AddTuning(SimDataGeneratorFixtures.Widget(string.Empty, name: "widget_Named", instance: 888));
        Assert.AreEqual(888UL, GenerateObject(generator, tuning)["other_widget"]);
    }

    [TestMethod]
    public void ReferencesToTuningAreTableSetReferencesAndOthersAreTruncated()
    {
        using var set = SimDataGeneratorFixtures.CreateSet();
        var @object = GenerateObject(new SimDataGenerator(set), SimDataGeneratorFixtures.Widget("""<T n="other_widget">4294967297</T><T n="object_ref">4294967297</T>"""));
        Assert.AreEqual(4294967297UL, @object["other_widget"]);
        Assert.AreEqual(1U, @object["object_ref"]);
    }

    [TestMethod]
    [DataRow("2f7d0004:00000000:00000000000000ab", 0x00B2D882U, 0U)]
    [DataRow("39b2aa4a:00000000:00000000000000ab", 0xFD04E3BEU, 0x001407ECU)]
    [DataRow("00b2d882:00000000:00000000000000ab", 0x00B2D882U, 0U)]
    public void ResourceKeysAreConvertedAsMaxisBuildConvertsThem(string key, uint expectedType, uint expectedGroup)
    {
        using var set = SimDataGeneratorFixtures.CreateSet();
        Assert.AreEqual(new ResourceKey((ResourceType)expectedType, expectedGroup, 0xAB), GenerateObject(new SimDataGenerator(set), SimDataGeneratorFixtures.Widget($"""<T n="icon">{key}</T>"""))["icon"]);
    }

    [TestMethod]
    public void ResourceKeyConversionsCanBeRemovedAndAdded()
    {
        using var set = SimDataGeneratorFixtures.CreateSet();
        var generator = new SimDataGenerator(set);
        var tuning = SimDataGeneratorFixtures.Widget("""<T n="icon">2f7d0004:00000000:00000000000000ab</T>""");
        generator.RemoveResourceKeyConversion(ResourceType.PNGImage, 0);
        Assert.AreEqual(new ResourceKey(ResourceType.PNGImage, 0, 0xAB), GenerateObject(generator, tuning)["icon"]);
        generator.AddResourceKeyConversion(ResourceType.PNGImage, 0, ResourceType.DSTImage, 7);
        Assert.AreEqual(new ResourceKey(ResourceType.DSTImage, 7, 0xAB), GenerateObject(generator, tuning)["icon"]);
    }

    [TestMethod]
    public void TuplesWithoutExportModesKeepOnlyTheirExportedMembers()
    {
        using var set = SimDataGeneratorFixtures.CreateSet();
        var deepTuple = (SimDataObject)GenerateObject(new SimDataGenerator(set), SimDataGeneratorFixtures.Widget(string.Empty))["deep_tuple"]!;
        Assert.AreEqual("WidgetDeep", deepTuple.Schema.Name);
        CollectionAssert.AreEqual(new (string, SimDataType)[] { ("exported_child", SimDataType.FLOAT) }, GetColumns(deepTuple).ToList());
    }

    [TestMethod]
    [DataRow("""<I c="Missing" n="missing" s="1" />""")]
    [DataRow("""<X />""")]
    [DataRow("""<I c="Widget" n="widget_Test" s="1"><E n="age">ANCIENT</E></I>""")]
    [DataRow("""<I c="Widget" n="widget_Test" s="1"><V n="option" t="sometimes" /></I>""")]
    [DataRow("""<M n="missing.module" s="1" />""")]
    [DataRow("""<I c="OrderBase" n="order_Test" s="1" />""")]
    [DataRow("""<I c="Widget" n="widget_Test" s="1"><T n="zeta_int">three</T></I>""")]
    [DataRow("""<I c="Widget" n="widget_Test" s="1"><T n="zeta_int">99999999999</T></I>""")]
    public void UngeneratableTuningIsRejected(string tuning)
    {
        using var set = SimDataGeneratorFixtures.CreateSet();
        Assert.ThrowsExactly<SimDataGenerationException>(() => new SimDataGenerator(set).Generate(XDocument.Parse(tuning)));
    }

    [TestMethod]
    public void VariantOptionsAreHashedLowercase()
    {
        using var set = SimDataGeneratorFixtures.CreateSet();
        var option = (SimDataVariant)GenerateObject(new SimDataGenerator(set), SimDataGeneratorFixtures.Widget("""<V n="option" t="enabled"><T n="enabled">12</T></V>"""))["option"]!;
        Assert.AreEqual(0xF8CBBF4CU, option.TypeHash);
        Assert.AreEqual(SimDataType.INT32, option.Type);
        Assert.AreEqual(12, option.Value);
    }

    [TestMethod]
    public void VectorComponentsDefaultIndividually()
    {
        using var set = SimDataGeneratorFixtures.CreateSet();
        Assert.AreEqual(new Vector3(7, 0, 3), GenerateObject(new SimDataGenerator(set), SimDataGeneratorFixtures.Widget("""<U n="position"><T n="x">7</T></U>"""))["position"]);
    }
}
