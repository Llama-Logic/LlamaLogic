namespace LlamaLogic.TuningDescriptions.UnitTests;

static class TuningDescriptionFixtures
{
    public const string BuffPath = "Buffs/Descriptions/Buff.tdesc";
    public const string ClothingBuffPath = "Buffs/Descriptions/ClothingBuff.tdesc";
    public const string TraitPath = "Traits/Descriptions/Trait.tdesc";
    public const string SimInfoTypesPath = "Tuning/sims-sim_info_types.tdesc";
    public const string UiDialogPath = "Tuning/ui-ui_dialog.tdesc";
    public const string TestSetPath = "TdescFrag/TunableTestSet.tdescfrag";

    public static readonly IReadOnlyDictionary<string, string> Documents = new Dictionary<string, string>
    {
        [BuffPath] =
            """
            <?xml version="1.0" encoding="iso-8859-1"?>

            <TuningRoot>
                <Instance instance_type="buff" class="Buff" module="buffs.buff" muid="7F0C39A1D1E2B3C4" description="A buff." instance_needs_reference="True" instance_subclasses_only="False" path="Buffs\Descriptions" use_guid_for_reference="True">
                    <Tunable type="int" name="buff_name" class="TunableLocalizedString" default="0x0" display="Buff Name" muid="1111111111111111" description="The name." Deprecated="False" export_modes="client_binary,server_binary,server_xml" filter="0" group="UI" />
                    <Tunable type="str" name="cas_vfx" class="Tunable" default="None" display="Cas Vfx" muid="2222222222222222" Deprecated="False" export_modes="client_binary" filter="0" group="General" />
                    <Tunable type="ResourceKey" name="icon" class="TunableIcon" default="None" display="Icon" muid="3333333333333333" allow_none="True" resource_types="0x2f7d0004" Deprecated="False" export_modes="client_binary, server_binary" filter="0" group="UI" />
                    <Tunable type="mood" name="mood_type" class="TunableReference" display="Mood Type" muid="4444444444444444" allow_none="True" Deprecated="False" filter="0" group="General" />
                    <Tunable type="int" name="mood_weight" class="TunableRange" default="0" display="Mood Weight" muid="5555555555555555" min="0" max="None" Deprecated="False" filter="0" group="General" />
                    <Tunable type="str" name="hashed" class="Tunable" default="" display="Hashed" muid="62B6379E8E26299" binary_type="HashCode64" Deprecated="True" filter="0" group="General" />
                </Instance>
                <VersionHistory>
                    <ActionList>
                        <Action type="kRename" old_muid="0123456789ABCDEF" new_muid="1111111111111111" />
                        <Action type="kRemove" old_muid="FEDCBA9876543210" />
                    </ActionList>
                    <ActionList>
                        <Action type="kUpdateVariantTypeField" target_muid="2222222222222222" source_type_value="skill" target_type_value="skill_level" />
                        <Action type="kChangeInheritance" old_muid="AAAAAAAAAAAAAAAA" />
                        <Action type="kSomethingNew" future_attribute="kept" />
                    </ActionList>
                </VersionHistory>
            </TuningRoot>
            """,
        [ClothingBuffPath] =
            """
            <?xml version="1.0" encoding="iso-8859-1"?>

            <TuningRoot>
                <Instance instance_type="buff" class="ClothingBuff" module="buffs.clothing_buff" muid="8F0C39A1D1E2B3C4" parents="Buff, Trait" path="Buffs\Descriptions">
                    <Deleted name="mood_weight" muid="5555555555555555" />
                    <Tunable type="str" name="cas_vfx" class="Tunable" default="sparkle" display="Cas Vfx" muid="9999999999999999" Deprecated="False" export_modes="client_binary" filter="0" group="General" />
                    <TunableTuple name="appearance" class="TunableAppearance" display="Appearance" muid="A000000000000001" description="How it looks." Deprecated="False" filter="0" group="General" export_modes="client_binary">
                        <TunableEnum type="Age" name="age" class="TunableEnumEntry" default="ADULT" display="Age" muid="A000000000000002" static_entries="sims-sim_info_types.Age" Deprecated="False" filter="0" group="General" />
                        <TunableEnum type="Tag" name="tag" class="TunableEnumEntry" default="INVALID" display="Tag" muid="A000000000000003" static_entries="tag.Tag" dynamic_entries="tag.Tag._elements" invalid_entries="INVALID" binary_type="uint32" Deprecated="False" filter="0" group="General" />
                        <TunableVariant type="None" name="color" class="OptionalTunable" default="disabled" display="Color" muid="A000000000000004" Deprecated="False" filter="0" group="General">
                            <Tunable name="disabled" class="TunableExistance" muid="A000000000000005" />
                            <TunableTuple name="enabled" class="TunableColorRGBA" display="Enabled" muid="A000000000000006" Deprecated="False" filter="0" group="General">
                                <Tunable type="int" name="r" class="Tunable" default="255" display="R" muid="A000000000000007" Deprecated="False" filter="0" group="General" />
                            </TunableTuple>
                        </TunableVariant>
                        <TunableList name="tags_to_names" class="TunableMapping" display="Tags To Names" muid="A000000000000008" mapping_key="key" mapping_value="value" mapping_class="TagNameMapping" Deprecated="False" filter="0" group="General">
                            <TunableTuple class="TunableTuple" muid="A000000000000009" Deprecated="False" filter="0" group="General">
                                <TunableEnum type="Tag" name="key" class="TunableEnumEntry" default="INVALID" display="Key" muid="A00000000000000A" static_entries="tag.Tag" Deprecated="False" filter="0" group="General" />
                                <Tunable type="int" name="value" class="TunableLocalizedString" default="0x0" display="Value" muid="A00000000000000B" Deprecated="False" filter="0" group="General" />
                            </TunableTuple>
                        </TunableList>
                        <TunableList name="ages" class="TunableEnumSet" display="Ages" muid="A00000000000000C" unique_entries="true" Deprecated="False" filter="0" group="General">
                            <TunableEnum type="Age" class="TunableEnumEntry" default="ADULT" muid="A00000000000000D" static_entries="sims-sim_info_types.Age" Deprecated="False" filter="0" group="General" />
                        </TunableList>
                        <TdescFragTag name="tests" class="TunableTestSet" display="Tests" muid="A00000000000000E" description="Tests." filter="0" group="General" />
                        <FutureTunable name="future" class="TunableFromTheFuture" muid="A00000000000000F" shiny="True" />
                    </TunableTuple>
                </Instance>
            </TuningRoot>
            """,
        [TraitPath] =
            """
            <?xml version="1.0" encoding="iso-8859-1"?>

            <TuningRoot>
                <Instance instance_type="trait" class="Trait" module="traits.traits" muid="9F0C39A1D1E2B3C4" path="Traits\Descriptions">
                    <Tunable type="int" name="buff_name" class="Tunable" default="7" display="Buff Name" muid="B000000000000001" Deprecated="False" filter="0" group="General" />
                    <Tunable type="int" name="mood_weight" class="Tunable" default="1" display="Mood Weight" muid="B000000000000002" Deprecated="False" filter="0" group="General" />
                    <Tunable type="bool" name="trait_only" class="Tunable" default="False" display="Trait Only" muid="B000000000000003" Deprecated="False" filter="0" group="General" />
                </Instance>
            </TuningRoot>
            """,
        [SimInfoTypesPath] =
            """
            <?xml version="1.0" encoding="iso-8859-1"?>

            <TuningRoot>
                <Module name="sims.sim_info_types" muid="C000000000000001">
                    <Class name="Age" muid="C000000000000002" enum_bit_flag="True" enum_locked="False">
                        <EnumItem name="BABY" enum_value="1" muid="C000000000000003" />
                        <EnumItem name="ADULT" enum_value="32" muid="C000000000000004" />
                        <EnumItem name="NEGATIVE" enum_value="-5" muid="C000000000000005" />
                    </Class>
                    <Class name="SimInfoTypesTuning" muid="C000000000000006">
                        <Tunable type="int" name="MAXIMUM_AGE" class="Tunable" default="100" display="Maximum Age" muid="C000000000000007" Deprecated="False" export_modes="client_binary" filter="0" group="General" />
                    </Class>
                </Module>
            </TuningRoot>
            """,
        [UiDialogPath] =
            """
            <?xml version="1.0" encoding="iso-8859-1"?>

            <TuningRoot>
                <Module name="ui.ui_dialog" muid="D000000000000001">
                    <Class name="UiDialogResponse" muid="D000000000000002">
                        <Class name="UiDialogUiRequest" muid="D000000000000003" enum_bit_flag="False" enum_locked="True">
                            <EnumItem name="NO_REQUEST" enum_value="0" muid="D000000000000004" />
                            <EnumItem name="SHOW_LESSONS" enum_value="1" muid="D000000000000005" />
                        </Class>
                    </Class>
                </Module>
            </TuningRoot>
            """,
        [TestSetPath] =
            """
            <?xml version="1.0" encoding="iso-8859-1"?>

            <TuningRoot>
                <TdescFrag muid="E000000000000001">
                    <TunableList class="TunableTestSet" muid="E000000000000002" description="A list of test groups." Deprecated="False" filter="0" group="General">
                        <TunableList class="TunableList" muid="E000000000000003" Deprecated="False" filter="0" group="General">
                            <TunableVariant type="None" class="TunableTestVariant" default="none" muid="E000000000000004" Deprecated="False" filter="0" group="General">
                                <Tunable name="none" class="TunableExistance" muid="E000000000000005" />
                            </TunableVariant>
                        </TunableList>
                    </TunableList>
                </TdescFrag>
                <VersionHistory>
                    <ActionList>
                        <Action type="kRemove" old_muid="E00000000000000F" />
                    </ActionList>
                </VersionHistory>
            </TuningRoot>
            """
    };

    public static byte[] BuildZip(CompressionType compressionType, IReadOnlyDictionary<string, string>? documents = null)
    {
        using var stream = new MemoryStream();
        using (var writer = new ZipWriter(stream, new ZipWriterOptions(compressionType) { LeaveStreamOpen = true }))
            foreach (var (path, xml) in documents ?? Documents)
                writer.Write(path, new MemoryStream(Encoding.Latin1.GetBytes(xml)));
        return stream.ToArray();
    }

    public static string WriteDirectory(IReadOnlyDictionary<string, string>? documents = null)
    {
        var root = Path.Combine(Path.GetTempPath(), "LlamaLogic.TuningDescriptions.UnitTests", Guid.NewGuid().ToString("n"));
        foreach (var (path, xml) in documents ?? Documents)
        {
            var filePath = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.WriteAllBytes(filePath, Encoding.Latin1.GetBytes(xml));
        }
        return root;
    }
}
