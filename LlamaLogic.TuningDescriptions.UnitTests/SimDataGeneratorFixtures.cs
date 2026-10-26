namespace LlamaLogic.TuningDescriptions.UnitTests;

static class SimDataGeneratorFixtures
{
    public static readonly IReadOnlyDictionary<string, string> Documents = new Dictionary<string, string>
    {
        ["Widgets/Descriptions/Widget.tdesc"] =
            """
            <?xml version="1.0" encoding="iso-8859-1"?>

            <TuningRoot>
                <Instance instance_type="widget" class="Widget" module="widgets.widget" muid="1000000000000001" path="Widgets\Descriptions">
                    <Tunable type="int" name="zeta_int" class="Tunable" default="3" muid="1000000000000002" export_modes="client_binary" />
                    <Tunable type="int" name="alpha_name" class="TunableLocalizedString" default="0x0" muid="1000000000000003" export_modes="client_binary,server_binary,server_xml" />
                    <Tunable type="int" name="hidden" class="Tunable" default="1" muid="1000000000000004" />
                    <TunableTuple name="deep_tuple" class="WidgetDeep" muid="1000000000000005">
                        <Tunable type="float" name="exported_child" class="Tunable" default="1.5" muid="1000000000000006" export_modes="client_binary" />
                        <Tunable type="int" name="not_exported_child" class="Tunable" default="2" muid="1000000000000007" />
                    </TunableTuple>
                    <Tunable type="str" name="hash64" class="TunableStringHash64" default="" muid="1000000000000008" allow_none="True" binary_type="hashcode64" export_modes="client_binary" />
                    <Tunable type="str" name="hash32" class="TunableStringHash32" default="" muid="1000000000000009" binary_type="hashcode32" export_modes="client_binary" />
                    <Tunable type="str" name="text" class="Tunable" default="None" muid="100000000000000A" allow_none="True" export_modes="client_binary" />
                    <Tunable type="str" name="chosen_text" class="Tunable" default="None" muid="100000000000000B" allow_none="True" choice_query="anything" export_modes="client_binary" />
                    <Tunable type="widget" name="other_widget" class="TunableReference" muid="100000000000000C" allow_none="True" export_modes="client_binary" />
                    <Tunable type="object" name="object_ref" class="TunableReference" muid="100000000000000D" allow_none="True" export_modes="client_binary" />
                    <Tunable type="ResourceKey" name="icon" class="TunableIcon" default="None" muid="100000000000000E" allow_none="True" export_modes="client_binary" />
                    <TunableVariant type="None" name="option" class="OptionalTunable" default="disabled" muid="100000000000000F" export_modes="client_binary">
                        <Tunable name="disabled" class="TunableExistance" muid="1000000000000010" />
                        <Tunable type="int" name="enabled" class="Tunable" default="9" muid="1000000000000011" />
                    </TunableVariant>
                    <TunableTuple name="position" class="TunableVector3" muid="1000000000000012" export_modes="client_binary">
                        <Tunable type="float" name="x" class="Tunable" default="1" muid="1000000000000013" />
                        <Tunable type="float" name="y" class="Tunable" default="None" muid="1000000000000014" />
                        <Tunable type="float" name="z" class="Tunable" default="3" muid="1000000000000015" />
                    </TunableTuple>
                    <TunableEnum type="Age" name="age" class="TunableEnumEntry" default="ADULT" muid="1000000000000016" static_entries="sims-sim_info_types.Age" export_modes="client_binary" />
                    <TunableEnum type="Tag" name="tag" class="TunableEnumEntry" default="INVALID" muid="1000000000000017" static_entries="tag.Tag" dynamic_entries="tag.Tag._elements" binary_type="uint32" export_modes="client_binary" />
                    <TunableList name="mapping" class="TunableMapping" muid="1000000000000018" mapping_key="key" mapping_value="value" mapping_class="WidgetMapping" export_modes="client_binary">
                        <TunableTuple class="TunableTuple" muid="1000000000000019">
                            <TunableEnum type="Age" name="key" class="TunableEnumEntry" default="ADULT" muid="100000000000001A" static_entries="sims-sim_info_types.Age" />
                            <TunableTuple name="value" class="WidgetMappingValue" muid="100000000000001B" export_modes="server_xml">
                                <Tunable type="int" name="shown" class="Tunable" default="4" muid="100000000000001C" export_modes="client_binary" />
                                <Tunable type="int" name="unshown" class="Tunable" default="5" muid="100000000000001D" />
                            </TunableTuple>
                        </TunableTuple>
                    </TunableList>
                    <TdescFragTag name="fragment" class="WidgetFragment" muid="100000000000001E" export_modes="client_binary" />
                </Instance>
            </TuningRoot>
            """,
        ["Widgets/Descriptions/SubWidget.tdesc"] =
            """
            <?xml version="1.0" encoding="iso-8859-1"?>

            <TuningRoot>
                <Instance instance_type="widget" class="SubWidget" module="widgets.sub_widget" muid="2000000000000001" parents="Widget" path="Widgets\Descriptions">
                    <Deleted name="zeta_int" muid="1000000000000002" />
                </Instance>
            </TuningRoot>
            """,
        ["Widgets/Descriptions/FullWidget.tdesc"] =
            """
            <?xml version="1.0" encoding="iso-8859-1"?>

            <TuningRoot>
                <Instance instance_type="widget" class="FullWidget" module="widgets.full_widget" muid="3000000000000001" export_modes="client_binary" path="Widgets\Descriptions">
                    <Tunable type="int" name="included" class="Tunable" default="1" muid="3000000000000002" />
                    <Tunable type="int" name="excluded" class="Tunable" default="2" muid="3000000000000003" export_modes="server_xml" />
                    <TunableTuple name="full_tuple" class="FullTuple" muid="3000000000000004">
                        <Tunable type="int" name="inner" class="Tunable" default="3" muid="3000000000000005" />
                    </TunableTuple>
                </Instance>
            </TuningRoot>
            """,
        ["Widgets/Descriptions/Client_Engine.tdesc"] =
            """
            <?xml version="1.0" encoding="iso-8859-1"?>

            <TuningRoot>
                <Instance instance_type="engine" class="Client_Engine" module="engine.engine" muid="4000000000000001" path="Widgets\Descriptions">
                    <Tunable type="int" name="alpha" class="Tunable" default="1" muid="4000000000000002" export_modes="client_binary" />
                    <Tunable type="bool" name="beta" class="Tunable" default="True" muid="4000000000000003" export_modes="client_binary" />
                </Instance>
            </TuningRoot>
            """,
        ["Widgets/Descriptions/OrderBase.tdesc"] =
            """
            <?xml version="1.0" encoding="iso-8859-1"?>

            <TuningRoot>
                <Instance instance_type="order" class="OrderBase" module="order.order_base" muid="5000000000000001" path="Widgets\Descriptions">
                    <Tunable type="int" name="a" class="Tunable" default="1" muid="5000000000000002" />
                    <Tunable type="int" name="b" class="Tunable" default="2" muid="5000000000000003" />
                    <Tunable type="int" name="c" class="Tunable" default="3" muid="5000000000000004" />
                </Instance>
            </TuningRoot>
            """,
        ["Widgets/Descriptions/OrderMiddle.tdesc"] =
            """
            <?xml version="1.0" encoding="iso-8859-1"?>

            <TuningRoot>
                <Instance instance_type="order" class="OrderMiddle" module="order.order_middle" muid="6000000000000001" parents="OrderBase" path="Widgets\Descriptions">
                    <Deleted name="b" muid="5000000000000003" />
                </Instance>
            </TuningRoot>
            """,
        ["Widgets/Descriptions/OrderLeaf.tdesc"] =
            """
            <?xml version="1.0" encoding="iso-8859-1"?>

            <TuningRoot>
                <Instance instance_type="order" class="OrderLeaf" module="order.order_leaf" muid="7000000000000001" parents="OrderMiddle" path="Widgets\Descriptions">
                    <Tunable type="int" name="b" class="Tunable" default="4" muid="7000000000000002" />
                </Instance>
            </TuningRoot>
            """,
        ["Tuning/sims-sim_info_types.tdesc"] =
            """
            <?xml version="1.0" encoding="iso-8859-1"?>

            <TuningRoot>
                <Module name="sims.sim_info_types" muid="8000000000000001">
                    <Class name="Age" muid="8000000000000002" enum_bit_flag="True" enum_locked="True">
                        <EnumItem name="CHILD" enum_value="8" muid="8000000000000003" />
                        <EnumItem name="ADULT" enum_value="32" muid="8000000000000004" />
                    </Class>
                    <Class name="SimInfoTypesTuning" muid="8000000000000005">
                        <Tunable type="int" name="MAXIMUM_AGE" class="Tunable" default="100" muid="8000000000000006" export_modes="client_binary" />
                        <Tunable type="int" name="SERVER_ONLY" class="Tunable" default="7" muid="8000000000000007" />
                    </Class>
                </Module>
            </TuningRoot>
            """,
        ["Tuning/tag.tdesc"] =
            """
            <?xml version="1.0" encoding="iso-8859-1"?>

            <TuningRoot>
                <Module name="tag" muid="9000000000000001">
                    <Class name="Tag" muid="9000000000000002" enum_bit_flag="False" enum_locked="True">
                        <EnumItem name="INVALID" enum_value="0" muid="9000000000000003" />
                        <TunableList name="_elements" class="TunableList" muid="9000000000000004" export_modes="client_binary">
                            <Tunable type="str" class="Tunable" default="None" muid="9000000000000005" />
                        </TunableList>
                    </Class>
                </Module>
            </TuningRoot>
            """,
        ["TdescFrag/WidgetFragment.tdescfrag"] =
            """
            <?xml version="1.0" encoding="iso-8859-1"?>

            <TuningRoot>
                <TdescFrag muid="A000000000000001">
                    <TunableTuple class="WidgetFragment" muid="A000000000000002">
                        <Tunable type="int" name="from_fragment" class="Tunable" default="6" muid="A000000000000003" />
                    </TunableTuple>
                </TdescFrag>
            </TuningRoot>
            """
    };

    public const string TagModuleTuning =
        """
        <?xml version="1.0" encoding="utf-8"?>
        <M n="tag" s="15608553923092786359">
          <C n="Tag">
            <L n="_elements">
              <T ev="5">Fancy</T>
              <T ev="9">Plain</T>
            </L>
          </C>
        </M>
        """;

    public static TuningDescriptionSet CreateSet() =>
        TuningDescriptionSet.FromZip(new MemoryStream(TuningDescriptionFixtures.BuildZip(CompressionType.LZMA, Documents)));

    public static XDocument Widget(string members, string className = "Widget", string name = "widget_Test", ulong instance = 12345) =>
        XDocument.Parse(
            $"""
            <?xml version="1.0" encoding="utf-8"?>
            <I c="{className}" i="widget" m="widgets.widget" n="{name}" s="{instance}">
            {members}
            </I>
            """);
}
