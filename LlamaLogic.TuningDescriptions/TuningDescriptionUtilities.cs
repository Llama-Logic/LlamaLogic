namespace LlamaLogic.TuningDescriptions;

static class TuningDescriptionUtilities
{
    public static ImmutableDictionary<string, string> GetAttributes(XElement element) =>
        element.Attributes().ToImmutableDictionary(attribute => attribute.Name.LocalName, attribute => attribute.Value, StringComparer.Ordinal);

    public static bool? ParseBoolean(string? value) =>
        value is null ? null : string.Equals(value, "True", StringComparison.OrdinalIgnoreCase);

    public static TuningDescriptionExportModes? ParseExportModes(string? value)
    {
        if (value is null)
            return null;
        var exportModes = TuningDescriptionExportModes.None;
        foreach (var token in value.Split(','))
            exportModes |= token.Trim() switch
            {
                "client_binary" => TuningDescriptionExportModes.ClientBinary,
                "server_binary" => TuningDescriptionExportModes.ServerBinary,
                "server_xml" => TuningDescriptionExportModes.ServerXml,
                _ => TuningDescriptionExportModes.None
            };
        return exportModes;
    }

    public static ulong? ParseMuid(string? value) =>
        value is not null && ulong.TryParse(value, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var muid) ? muid : null;

    public static ImmutableArray<IReadOnlyList<TuningDescriptionAction>> ParseVersionHistory(XElement? versionHistory) =>
        versionHistory is null
        ? []
        : [.. versionHistory.Elements("ActionList").Select(actionList => (IReadOnlyList<TuningDescriptionAction>)actionList.Elements("Action").Select(action => new TuningDescriptionAction(action)).ToImmutableArray())];
}
