namespace LlamaLogic.TuningArchive;

sealed class ResourceConfiguration
{
    public static int? GetDefaultPriority(string fileName, bool isDelta)
    {
        if (fileName.StartsWith("Strings_", StringComparison.OrdinalIgnoreCase))
            return isDelta ? -9 : -20;
        if (fileName.StartsWith("SimulationPreload", StringComparison.OrdinalIgnoreCase))
            return isDelta ? -14 : -15;
        if (fileName.StartsWith("SimulationDeltaBuild", StringComparison.OrdinalIgnoreCase))
            return -20;
        if (fileName.StartsWith("SimulationFullBuild", StringComparison.OrdinalIgnoreCase))
            return -30;
        if (fileName.StartsWith("ClientDeltaBuild", StringComparison.OrdinalIgnoreCase))
            return -20;
        if (fileName.StartsWith("ClientFullBuild", StringComparison.OrdinalIgnoreCase))
            return -30;
        return null;
    }

    public static ResourceConfiguration? Read(string path)
    {
        if (!File.Exists(path))
            return null;
        var rules = new List<(Regex pattern, int priority)>();
        var priority = 0;
        var selectDepth = 0;
        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;
            var separator = line.IndexOfAny([' ', '\t']);
            var directive = separator < 0 ? line : line[..separator];
            var argument = separator < 0 ? string.Empty : line[separator..].Trim();
            if (directive.Equals("Select", StringComparison.OrdinalIgnoreCase))
                ++selectDepth;
            else if (directive.Equals("End", StringComparison.OrdinalIgnoreCase))
                selectDepth = Math.Max(0, selectDepth - 1);
            else if (selectDepth > 0)
                continue;
            else if (directive.Equals("Priority", StringComparison.OrdinalIgnoreCase) && int.TryParse(argument, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var newPriority))
                priority = newPriority;
            else if (directive.Equals("PackedFile", StringComparison.OrdinalIgnoreCase) && argument.Length > 0)
                rules.Add((new Regex($"^{Regex.Escape(argument).Replace(@"\*", ".*", StringComparison.Ordinal).Replace(@"\?", ".", StringComparison.Ordinal)}$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase), priority));
        }
        return new(rules);
    }

    ResourceConfiguration(IReadOnlyList<(Regex pattern, int priority)> rules) =>
        this.rules = rules;

    readonly IReadOnlyList<(Regex pattern, int priority)> rules;

    public int? GetPriority(string fileName)
    {
        foreach (var (pattern, priority) in rules)
            if (pattern.IsMatch(fileName))
                return priority;
        return null;
    }
}