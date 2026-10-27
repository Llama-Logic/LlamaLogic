namespace LlamaLogic.TuningArchive.UnitTests;

static class TuningArchiveFixtures
{
    public const string AnimationStateMachineFileName = "S4_02D5DF13_00000000_8B12A7C35F0E4D61_llamalogic_Sit.xml";
    public const ulong PatchedAnimationStateMachineInstance = 0x8B12A7C35F0E4D62;
    public const ulong SitAnimationStateMachineInstance = 0x8B12A7C35F0E4D61;
    public const ulong UnreadableAnimationStateMachineInstance = 0x8B12A7C35F0E4D63;
    public const ulong BuffGladInstance = 202;
    public const ulong BuffHappyInstance = 200;
    public const ulong BuffSadInstance = 201;
    public const ulong MissingBuffInstance = 999;
    public const ulong MoodHappyInstance = 100;
    public const uint BaseGameStringKey = 0x00001001;
    public const uint DeltaOnlyStringKey = 0x00001002;
    public const uint InstalledOnlyStringKey = 0x00001003;
    public const uint PatchedStringKey = 0x00001004;
    public const string SimDataFileName = "545AC67A!00000000!0000000000001234.Thing.SimData.xml";
    public const ulong DeletedIconInstance = 0xA2;
    public const ulong IconInstance = 0xA1;
    public const ulong PngIconInstance = 0xA4;
    public const ulong SimDataIconInstance = 0xA3;
    public const ulong SoundInstance = 0xB1;
    public const ulong SoundParentInstance = 0xB2;
    public const ulong SoundSampleInstance = 0xD1;
    public const ulong SoundStreamInstance = 0xD2;
    public const ulong UninstalledBuffInstance = 210;
    public const ulong UnreferencedIconInstance = 0xA9;
    public const ulong VideoInstance = 0xC1;
    public const string VoiceSilenceName = "vo_test_silence_12sec";
    public const string VoiceSongName = "vo_test_song_skillhigh_30sec";

    public static readonly IReadOnlyDictionary<string, string> MediaDescriptions = new Dictionary<string, string>
    {
        ["Buffs/Descriptions/Buff.tdesc"] =
            """
            <?xml version="1.0" encoding="iso-8859-1"?>

            <TuningRoot>
                <Instance instance_type="buff" class="Buff" module="buffs.buff" muid="7F0C39A1D1E2B3C4" description="A buff." instance_needs_reference="True" instance_subclasses_only="False" path="Buffs\Descriptions" use_guid_for_reference="True">
                    <Tunable type="ResourceKey" name="icon" class="TunableIcon" default="None" display="Icon" muid="1111111111111111" Deprecated="False" filter="0" group="UI" />
                    <Tunable type="ResourceKey" name="large_icon" class="TunableResourceKey" default="None" display="Large Icon" muid="2222222222222222" Deprecated="False" filter="0" group="UI" />
                    <Tunable type="ResourceKey" name="png_icon" class="TunableIcon" default="None" display="PNG Icon" muid="3333333333333333" Deprecated="False" filter="0" group="UI" />
                    <Tunable type="ResourceKey" name="audio" class="TunableResourceKey" default="None" display="Audio" muid="4444444444444444" Deprecated="False" filter="0" group="Audio" />
                    <Tunable type="ResourceKey" name="video" class="TunableResourceKey" default="None" display="Video" muid="5555555555555555" Deprecated="False" filter="0" group="UI" />
                </Instance>
            </TuningRoot>
            """
    };

    public static readonly IReadOnlyDictionary<string, string> Descriptions = new Dictionary<string, string>
    {
        ["Buffs/Descriptions/Buff.tdesc"] =
            """
            <?xml version="1.0" encoding="iso-8859-1"?>

            <TuningRoot>
                <Instance instance_type="buff" class="Buff" module="buffs.buff" muid="7F0C39A1D1E2B3C4" description="A buff." instance_needs_reference="True" instance_subclasses_only="False" path="Buffs\Descriptions" use_guid_for_reference="True">
                    <Tunable type="int" name="buff_name" class="TunableLocalizedString" default="0x0" display="Buff Name" muid="1111111111111111" description="The name of the buff." Deprecated="False" filter="0" group="UI" />
                    <Tunable type="mood" name="mood_type" class="TunableReference" display="Mood Type" muid="2222222222222222" description="The mood the buff contributes to." allow_none="True" Deprecated="False" filter="0" group="General" />
                    <Tunable type="int" name="mood_weight" class="TunableRange" default="0" display="Mood Weight" muid="3333333333333333" min="0" max="None" Deprecated="False" filter="0" group="General" />
                </Instance>
            </TuningRoot>
            """,
        ["Buffs/Descriptions/Mood.tdesc"] =
            """
            <?xml version="1.0" encoding="iso-8859-1"?>

            <TuningRoot>
                <Instance instance_type="mood" class="Mood" module="buffs.mood" muid="8F0C39A1D1E2B3C4" description="A mood." instance_needs_reference="True" instance_subclasses_only="False" path="Buffs\Descriptions" use_guid_for_reference="True">
                    <TunableList name="buffs" class="TunableList" display="Buffs" muid="4444444444444444" Deprecated="False" filter="0" group="General">
                        <TunableTuple class="TunableTuple" muid="5555555555555555" Deprecated="False" filter="0" group="General">
                            <Tunable type="buff" name="buff_type" class="TunableReference" display="Buff Type" muid="6666666666666666" allow_none="False" Deprecated="False" filter="0" group="General" />
                            <TunableEnum type="Age" name="age" class="TunableEnumEntry" default="ADULT" display="Age" muid="7777777777777777" static_entries="sims-sim_info_types.Age" Deprecated="False" filter="0" group="General" />
                        </TunableTuple>
                    </TunableList>
                </Instance>
            </TuningRoot>
            """,
        ["Tuning/sims-sim_info_types.tdesc"] =
            """
            <?xml version="1.0" encoding="iso-8859-1"?>

            <TuningRoot>
                <Module name="sims.sim_info_types" muid="C000000000000001">
                    <Class name="Age" muid="C000000000000002" enum_bit_flag="True" enum_locked="False">
                        <EnumItem name="BABY" enum_value="1" muid="C000000000000003" />
                        <EnumItem name="ADULT" enum_value="32" muid="C000000000000004" />
                    </Class>
                </Module>
            </TuningRoot>
            """
    };

    public static readonly IReadOnlyDictionary<string, string> First = new Dictionary<string, string>
    {
        ["BG/Mood_Happy.xml"] = MoodHappy,
        ["BG/Buff_Happy.xml"] = BuffHappy(1),
        ["EP01/Buff_Sad.xml"] = BuffSad,
        ["Reference - All Strings.txt"] = "0x0000ABCD\tHappy\n0x0000ABCE\tSad\n"
    };

    public static readonly IReadOnlyDictionary<string, string> Second = new Dictionary<string, string>
    {
        ["EP02/Mood_Happy.xml"] = MoodHappy,
        ["BG/Buff_Happy.xml"] = BuffHappy(3),
        ["BG/Buff_Glad.xml"] = BuffGlad,
        ["Reference - All Strings.txt"] = "0x0000ABCD\tHappy!\n0x0000ABCF\tGlad\n"
    };

    const string BuffGlad =
        """
        <?xml version="1.0" encoding="utf-8"?>
        <I c="Buff" i="buff" m="buffs.buff" n="Buff_Glad" s="202">
          <T n="mood_type">100</T>
        </I>
        """;

    const string BuffSad =
        """
        <?xml version="1.0" encoding="utf-8"?>
        <I c="Buff" i="buff" m="buffs.buff" n="Buff_Sad" s="201">
          <T n="buff_name">0x0000ABCE</T>
          <T n="mood_type">Mood_Happy</T>
          <T n="mood_weight">2</T>
        </I>
        """;

    const string MoodHappy =
        """
        <?xml version="1.0" encoding="utf-8"?>
        <I c="Mood" i="mood" m="buffs.mood" n="Mood_Happy" s="100">
          <L n="buffs">
            <U>
              <T n="buff_type">200</T>
              <E n="age">ADULT</E>
            </U>
            <U>
              <T n="buff_type">999</T>
            </U>
          </L>
        </I>
        """;

    public static IReadOnlyList<string> Describe(IEnumerable<TuningArchiveChange> changes) =>
        [.. changes.Select(change => $"{change.Kind} {(change.New ?? change.Old)!.Name}").OrderBy(name => name, StringComparer.Ordinal)];

    public static IReadOnlyList<string> Describe(IEnumerable<TuningArchiveStringChange> changes) =>
        [.. changes.Select(change => $"{change.Kind} {change.Key:X8} {change.OldText} {change.NewText}").OrderBy(name => name, StringComparer.Ordinal)];

    public static string AnimationStateMachine(string name, int priority) =>
        $"""
        <ASM name="{name}" type="Jazz">
          <State name="entry" type="entry" />
          <State name="exit" type="exit" priority="{priority}" />
        </ASM>
        """;

    public static string WriteInstallationWithAnimationStateMachines(TemporaryDirectory directory, bool includeUnreadable = false)
    {
        var sit = new ResourceKey(ResourceType.AnimationStateMachine, 0, SitAnimationStateMachineInstance);
        var patched = new ResourceKey(ResourceType.AnimationStateMachine, 0, PatchedAnimationStateMachineInstance);
        directory.WriteFile("The Sims 4/Game/Bin/Default.ini", "[Version]\ngameversion = 1.0.1.1000\n");
        WritePackage(directory, "The Sims 4/Data/Simulation/SimulationFullBuild0.package", package =>
        {
            package.Set(sit, new AnimationStateMachineModel(XDocument.Parse(AnimationStateMachine("llamalogic_Sit", 1))));
            if (includeUnreadable)
                package.Set(new ResourceKey(ResourceType.AnimationStateMachine, 0, UnreadableAnimationStateMachineInstance), Encoding.UTF8.GetBytes("not XML at all"));
        });
        directory.WriteFile("The Sims 4/EP01/ResourceSimulation.cfg", "Priority -30\nPackedFile SimulationFullBuild*.package\n");
        WritePackage(directory, "The Sims 4/EP01/SimulationFullBuild0.package", package => package.Set(patched, new AnimationStateMachineModel(XDocument.Parse(AnimationStateMachine("llamalogic_Stand", 1)))));
        directory.WriteFile("The Sims 4/Delta/EP01/ResourceSimulation.cfg", "Priority -20\nPackedFile SimulationDeltaBuild*.package\n");
        WritePackage(directory, "The Sims 4/Delta/EP01/SimulationDeltaBuild0.package", package => package.Set(patched, new AnimationStateMachineModel(XDocument.Parse(AnimationStateMachine("llamalogic_Stand", 2)))));
        return directory.GetPath("The Sims 4");
    }

    public static string WriteInstallationWithSimData(TemporaryDirectory directory)
    {
        var key = new ResourceKey(ResourceType.SimData, 0, 0x0000000000001234);
        directory.WriteFile("The Sims 4/Game/Bin/Default.ini", "[Version]\ngameversion = 1.0.1.1000\n");
        WritePackage(directory, "The Sims 4/Data/Simulation/SimulationFullBuild0.package", package => package.Set(key, SimDataModel.FromXml(XDocument.Parse(SimData(0, 1)))));
        directory.WriteFile("The Sims 4/EP01/ResourceSimulation.cfg", "Priority -15\nPackedFile SimulationPreload.package\nPriority -30\nPackedFile SimulationFullBuild*.package\n");
        WritePackage(directory, "The Sims 4/EP01/SimulationPreload.package", package => package.Set(key, SimDataModel.FromXml(XDocument.Parse(SimData(0, 2)))));
        WritePackage(directory, "The Sims 4/EP01/SimulationFullBuild0.package");
        directory.WriteFile("The Sims 4/Delta/EP01/ResourceSimulation.cfg", "Priority -14\nPackedFile SimulationPreload.package\nPriority -20\nPackedFile SimulationDeltaBuild*.package\n");
        WritePackage(directory, "The Sims 4/Delta/EP01/SimulationDeltaBuild0.package", package => package.Set(key, ReadOnlyMemory<byte>.Empty, CompressionMode.SetDeletedFlag));
        return directory.GetPath("The Sims 4");
    }

    public static string WriteInstallationWithStrings(TemporaryDirectory directory)
    {
        directory.WriteFile("The Sims 4/Game/Bin/Default.ini", "[Version]\ngameversion = 1.0.1.1000\n");
        WritePackage(directory, "The Sims 4/Data/Simulation/SimulationFullBuild0.package");
        directory.WriteFile("The Sims 4/Data/Client/Resource.cfg", "Priority -30\nPackedFile Strings_*.package\n");
        WriteStringTablePackage(directory, "The Sims 4/Data/Client/Strings_ENG_US.package", 0x0000000000000001, (BaseGameStringKey, "Base game"));
        WritePackage(directory, "The Sims 4/EP01/SimulationFullBuild0.package");
        directory.WriteFile("The Sims 4/EP01/ResourceClient.cfg", "Priority -30\nPackedFile Strings_*.package\n");
        WriteStringTablePackage(directory, "The Sims 4/EP01/Strings_ENG_US.package", 0x0000000000000002, (BaseGameStringKey, string.Empty), (PatchedStringKey, "Old"), (InstalledOnlyStringKey, "Installed only"));
        directory.WriteFile("The Sims 4/Delta/EP01/ResourceClient.cfg", "Priority -9\nPackedFile Strings_*.package\n");
        WriteStringTablePackage(directory, "The Sims 4/Delta/EP01/Strings_ENG_US.package", 0x0000000000000002, (PatchedStringKey, "New"), (DeltaOnlyStringKey, "Delta only"));
        return directory.GetPath("The Sims 4");
    }

    public static byte[] AudioConfiguration(ulong parent, params ulong[] samples)
    {
        var content = new byte[14 + samples.Length * 8];
        BitConverter.TryWriteBytes(content.AsSpan(0, 2), (ushort)3);
        BitConverter.TryWriteBytes(content.AsSpan(2, 8), parent);
        BitConverter.TryWriteBytes(content.AsSpan(10, 4), (uint)samples.Length);
        for (var i = 0; i < samples.Length; ++i)
            BitConverter.TryWriteBytes(content.AsSpan(14 + i * 8, 8), samples[i]);
        return content;
    }

    public static byte[] Medium(string description) =>
        Encoding.UTF8.GetBytes(description);

    public static string WriteInstallationWithMedia(TemporaryDirectory directory, string root, int iconVersion = 1)
    {
        directory.WriteFile($"{root}/The Sims 4/Game/Bin/Default.ini", "[Version]\ngameversion = 1.0.1.1000\n");
        WritePackage(directory, $"{root}/The Sims 4/Data/Simulation/SimulationFullBuild0.package", package =>
        {
            package.Set(new ResourceKey(ResourceType.CombinedTuning, 0, 1), Encoding.UTF8.GetBytes
            (
                $"""
                <combined><g s="merged" /><R n="buff"><I c="Buff" i="buff" m="buffs.buff" n="Buff_Media" s="300"><T n="icon" p="InGame\UI\Icons\buff_media.png">2f7d0004:00000000:{IconInstance:x16}</T><T n="large_icon">2f7d0006:00000000:{DeletedIconInstance:x16}</T><T n="png_icon">2f7d0004:00000000:{PngIconInstance:x16}</T><T n="audio" p="InGame\Audio\buff_media.propx">39b2aa4a:00000000:{SoundInstance:x16}</T><T n="video">376840d7:00000000:{VideoInstance:x16}</T></I></R></combined>
                """
            ));
            package.Set(new ResourceKey(ResourceType.SimData, 0, 0x0000000000001234), SimDataModel.FromXml(XDocument.Parse
            (
                $"""
                <?xml version="1.0" encoding="utf-8"?>
                <SimData version="0x00000101">
                  <Instances>
                    <I name="Thing" schema="ThingSchema" type="Object">
                      <T name="Icon">00B2D882-00000000-{SimDataIconInstance:X16}</T>
                    </I>
                  </Instances>
                  <Schemas>
                    <Schema name="ThingSchema">
                      <Columns>
                        <Column flags="0x00000000" name="Icon" type="ResourceKey" />
                      </Columns>
                    </Schema>
                  </Schemas>
                </SimData>
                """
            )));
        });
        directory.WriteFile($"{root}/The Sims 4/Data/Client/Resource.cfg", "Priority -20\nPackedFile ClientDeltaBuild*.package\nPriority -30\nPackedFile ClientFullBuild*.package\n");
        WritePackage(directory, $"{root}/The Sims 4/Data/Client/ClientFullBuild0.package", package =>
        {
            package.Set(new ResourceKey(ResourceType.DSTImage, 0, IconInstance), Medium("icon, installed"));
            package.Set(new ResourceKey(ResourceType.DSTImage, 0, DeletedIconInstance), Medium("deleted icon"));
            package.Set(new ResourceKey(ResourceType.DSTImage, 0, SimDataIconInstance), Medium("SimData icon"));
            package.Set(new ResourceKey(ResourceType.DSTImage, 0, UnreferencedIconInstance), Medium("unreferenced icon"));
            package.Set(new ResourceKey(ResourceType.PNGImage, 0, PngIconInstance), Medium("PNG icon"));
            package.Set(new ResourceKey(ResourceType.AudioConfiguration, 0, SoundInstance), AudioConfiguration(SoundParentInstance, SoundSampleInstance));
            package.Set(new ResourceKey(ResourceType.AudioConfiguration, 0, SoundParentInstance), AudioConfiguration(0, SoundStreamInstance));
            package.Set(new ResourceKey(ResourceType.AudioVocals, 0x001407ec, SoundSampleInstance), Medium("sample"));
            package.Set(new ResourceKey(ResourceType.AudioVocals, 0x001407ec, SoundStreamInstance), Medium("stream header"));
            package.Set(new ResourceKey(ResourceType.AudioEffects, 0x001407ec, SoundStreamInstance), Medium("stream body"));
            package.Set(new ResourceKey(ResourceType.AVI, 0, VideoInstance), Medium("video"));
        });
        WritePackage(directory, $"{root}/The Sims 4/Data/Client/ClientDeltaBuild0.package", package =>
        {
            package.Set(new ResourceKey(ResourceType.DSTImage, 0, IconInstance), Medium($"icon, version {iconVersion}"));
            package.Set(new ResourceKey(ResourceType.DSTImage, 0, DeletedIconInstance), ReadOnlyMemory<byte>.Empty, CompressionMode.SetDeletedFlag);
        });
        return directory.GetPath($"{root}/The Sims 4");
    }

    public static ulong HashName(string name)
    {
        var hash = 0xcbf29ce484222325;
        foreach (var character in name.ToLowerInvariant())
        {
            hash *= 0x100000001b3UL;
            hash ^= (byte)character;
        }
        return hash;
    }

    public static string WriteInstallationWithVoices(TemporaryDirectory directory, string root)
    {
        directory.WriteFile($"{root}/The Sims 4/Game/Bin/Default.ini", "[Version]\ngameversion = 1.0.1.1000\n");
        WritePackage(directory, $"{root}/The Sims 4/Data/Simulation/SimulationFullBuild0.package", package =>
            package.Set(new ResourceKey(ResourceType.CombinedTuning, 0, 1), Encoding.UTF8.GetBytes
            (
                $"""
                <combined><g s="merged" /><R n="buff"><I c="Buff" i="buff" m="buffs.buff" n="Buff_Voices" s="301"><T n="audio" p="InEP03\Audio\VOX\fa\{VoiceSongName}.propx">39b2aa4a:00000000:{HashName(VoiceSongName):x16}</T></I><I c="Buff" i="buff" m="buffs.buff" n="Buff_Silent" s="302"><T n="audio" p="InEP03\Audio\VOX\fa\{VoiceSilenceName}.propx">39b2aa4a:00000000:{HashName(VoiceSilenceName):x16}</T></I></R></combined>
                """
            )));
        directory.WriteFile($"{root}/The Sims 4/Data/Client/Resource.cfg", "Priority -30\nPackedFile ClientFullBuild*.package\n");
        WritePackage(directory, $"{root}/The Sims 4/Data/Client/ClientFullBuild0.package", package =>
        {
            foreach (var (voiceCode, index) in new[] { "fa", "fc", "fd", "ma", "mb", "mc", "ca", "cb" }.Select((voiceCode, index) => (voiceCode, index)))
            {
                var sample = 0xE0UL + (ulong)index;
                package.Set(new ResourceKey(ResourceType.AudioConfiguration, 0, HashName($"{VoiceSongName}_{voiceCode}")), AudioConfiguration(0, sample));
                package.Set(new ResourceKey(ResourceType.AudioVocals, 0x001407ec, sample), Medium($"voice {voiceCode}"));
            }
        });
        return directory.GetPath($"{root}/The Sims 4");
    }

    public static string WriteInstallationWithUninstalledPack(TemporaryDirectory directory)
    {
        directory.WriteFile("The Sims 4/Game/Bin/Default.ini", "[Version]\ngameversion = 1.0.1.1000\n");
        WritePackage(directory, "The Sims 4/Data/Simulation/SimulationFullBuild0.package", package =>
            package.Set(new ResourceKey(ResourceType.CombinedTuning, 0, 1), Encoding.UTF8.GetBytes
            (
                $"""
                <combined><g s="merged" /><R n="mood"><I c="Mood" i="mood" m="buffs.mood" n="Mood_Happy" s="{MoodHappyInstance}"><L n="buffs"><U><T n="buff_type">{BuffHappyInstance}</T></U><U><T n="buff_type">{UninstalledBuffInstance}</T></U></L></I></R><R n="buff"><I c="Buff" i="buff" m="buffs.buff" n="Buff_Happy" s="{BuffHappyInstance}"><T n="mood_type">{MoodHappyInstance}</T></I></R></combined>
                """
            )));
        directory.WriteFile("The Sims 4/Delta/EP19/ResourceSimulation.cfg", "Priority -20\nPackedFile SimulationDeltaBuild*.package\n");
        WritePackage(directory, "The Sims 4/Delta/EP19/SimulationDeltaBuild0.package", package =>
            package.Set(new ResourceKey(ResourceType.CombinedTuning, 0, 1), Encoding.UTF8.GetBytes
            (
                $"""
                <combined><g s="merged" /><R n="buff"><I c="Buff" i="buff" m="buffs.buff" n="Buff_Patched" s="{UninstalledBuffInstance}"><T n="mood_type">{MoodHappyInstance}</T></I><I c="Buff" i="buff" m="buffs.buff" n="Buff_Happy_EP19" s="{BuffHappyInstance}"><T n="mood_type">{MoodHappyInstance}</T></I></R></combined>
                """
            )));
        return directory.GetPath("The Sims 4");
    }

    static void WritePackage(TemporaryDirectory directory, string relativePath, Action<DataBasePackedFile>? populate = null)
    {
        using var package = new DataBasePackedFile();
        populate?.Invoke(package);
        var path = directory.GetPath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        package.SaveAs(path);
    }

    static void WriteStringTablePackage(TemporaryDirectory directory, string relativePath, ulong instance, params (uint key, string text)[] strings) =>
        WritePackage(directory, relativePath, package =>
        {
            var table = new StringTableModel();
            foreach (var (key, text) in strings)
                table.Set(key, text);
            package.Set(new ResourceKey(ResourceType.StringTable, 0, instance), table);
        });

    public static string SimData(uint pack, int value) =>
        $"""
        <?xml version="1.0" encoding="utf-8"?>
        <SimData u="0x{pack:X8}" version="0x00000101">
          <Instances>
            <I name="Thing" schema="ThingSchema" type="Object">
              <T name="Value">{value}</T>
            </I>
          </Instances>
          <Schemas>
            <Schema name="ThingSchema">
              <Columns>
                <Column flags="0x00000000" name="Value" type="Int32" />
              </Columns>
            </Schema>
          </Schemas>
        </SimData>
        """;

    static string BuffHappy(int moodWeight) =>
        $"""
        <?xml version="1.0" encoding="utf-8"?>
        <!-- The happiest buff -->
        <I s="200" n="Buff_Happy" m="buffs.buff" i="buff" c="Buff">
            <T n="buff_name">0x0000ABCD</T>
            <T n="mood_type">100</T>
            <T n="mood_weight">{moodWeight}</T>
        </I>
        """;

    public static TuningDescriptionSet OpenDescriptions() =>
        TuningDescriptionSet.FromZip(new MemoryStream(WriteDescriptionsZip()));

    public static TuningDescriptionSet OpenDescriptions(IReadOnlyDictionary<string, string> descriptions) =>
        TuningDescriptionSet.FromZip(new MemoryStream(WriteDescriptionsZip(descriptions)));

    public static byte[] WriteDescriptionsZip() =>
        WriteDescriptionsZip(Descriptions);

    public static byte[] WriteDescriptionsZip(IReadOnlyDictionary<string, string> descriptions)
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
            foreach (var (path, xml) in descriptions)
            {
                using var entryStream = zip.CreateEntry(path).Open();
                var bytes = Encoding.Latin1.GetBytes(xml);
                entryStream.Write(bytes, 0, bytes.Length);
            }
        return stream.ToArray();
    }
}