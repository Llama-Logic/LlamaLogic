namespace LlamaLogic.TuningArchive;

/// <summary>
/// Compares SimData with SimData generated from its tuning, learns which columns the generator reproduces, and fixes SimData by the tuning descriptions
/// </summary>
static class TuningSimDataRepair
{
    internal sealed record Classification(TuningPackageAddedSimDataProblems Problems, IReadOnlyList<string> MissingColumns, IReadOnlyList<string> MistypedColumns, IReadOnlyList<string> ExtraColumns, IReadOnlyList<string> MismatchedHashes, string? FirstDifferingColumn);

    sealed class Walk(Func<string, bool> isSet)
    {
        public string? FirstDifference { get; set; }

        public Func<string, bool> IsSet { get; } = isSet;

        public HashSet<(SimDataObject, SimDataObject)> Visited { get; } = new(new PairComparer());
    }

    sealed class PairComparer :
        IEqualityComparer<(SimDataObject, SimDataObject)>
    {
        public bool Equals((SimDataObject, SimDataObject) x, (SimDataObject, SimDataObject) y) =>
            ReferenceEquals(x.Item1, y.Item1) && ReferenceEquals(x.Item2, y.Item2);

        public int GetHashCode((SimDataObject, SimDataObject) obj) =>
            HashCode.Combine(RuntimeHelpers.GetHashCode(obj.Item1), RuntimeHelpers.GetHashCode(obj.Item2));
    }

    static bool PrimitivesEqual(object? a, object? b) =>
        (a is null or string { Length: 0 } && b is null or string { Length: 0 }) || Equals(a, b);

    static bool IsStructured(object? value) =>
        value is SimDataObject or SimDataVector or SimDataVariant;

    static bool AreBothEmptyLists(object? a, object? b) =>
        (a is SimDataVector || b is SimDataVector) && a is null or SimDataVector { Count: 0 } && b is null or SimDataVector { Count: 0 };

    static bool HaveSameItems(SimDataVector a, SimDataVector b)
    {
        if (a.Count != b.Count || a.Any(item => item is null || IsStructured(item)) || b.Any(item => item is null || IsStructured(item)))
            return false;
        var counts = new Dictionary<object, int>();
        foreach (var item in a)
            counts[item!] = counts.GetValueOrDefault(item!) + 1;
        foreach (var item in b)
        {
            if (counts.GetValueOrDefault(item!) is not > 0)
                return false;
            --counts[item!];
        }
        return true;
    }

    static Dictionary<string, SimDataSchema> GetSchemasByName(SimDataModel model)
    {
        var byName = new Dictionary<string, SimDataSchema>(StringComparer.Ordinal);
        foreach (var schema in model.Schemas)
            byName.TryAdd(schema.Name, schema);
        return byName;
    }

    static Dictionary<string, SimDataInstance> GetInstancesByName(SimDataModel model)
    {
        var byName = new Dictionary<string, SimDataInstance>(StringComparer.Ordinal);
        foreach (var instance in model.Instances)
            byName.TryAdd(instance.Name ?? string.Empty, instance);
        return byName;
    }

    static SimDataColumn? FindColumn(SimDataSchema schema, string name) =>
        schema.GetColumnIndex(name) is { } index ? schema.Columns[index] : null;

    static string GetKey(string schema, string column) =>
        $"{schema}.{column}";

    /// <summary>
    /// Compares the specified <paramref name="simData"/> with the SimData <paramref name="generated"/> from its tuning, the items of columns that <paramref name="isSet"/> in any order
    /// </summary>
    internal static Classification Classify(SimDataModel simData, SimDataModel generated, Func<string, bool> isSet)
    {
        var missing = new List<string>();
        var mistyped = new List<string>();
        var extra = new List<string>();
        var mismatchedHashes = new List<string>();
        var expectedSchemas = GetSchemasByName(generated);
        foreach (var schema in simData.Schemas)
        {
            if (schema.Hash != schema.ComputedHash)
                mismatchedHashes.Add(schema.Name);
            if (!expectedSchemas.TryGetValue(schema.Name, out var expected))
                continue;
            foreach (var column in expected.Columns)
                if (FindColumn(schema, column.Name) is not { } existing)
                    missing.Add(GetKey(schema.Name, column.Name));
                else if (existing.Type != column.Type)
                    mistyped.Add(GetKey(schema.Name, column.Name));
            foreach (var column in schema.Columns)
                if (FindColumn(expected, column.Name) is null)
                    extra.Add(GetKey(schema.Name, column.Name));
        }
        var walk = new Walk(isSet);
        var counterparts = GetInstancesByName(generated);
        if (simData.Instances.Count != generated.Instances.Count)
            walk.FirstDifference = "the instances";
        foreach (var instance in simData.Instances)
        {
            if (walk.FirstDifference is not null)
                break;
            if (counterparts.TryGetValue(instance.Name ?? string.Empty, out var counterpart))
                CompareObjects(instance.Object, counterpart.Object, walk);
            else
                walk.FirstDifference = $"the instance {instance.Name}";
        }
        var problems = TuningPackageAddedSimDataProblems.None;
        if (missing.Count > 0)
            problems |= TuningPackageAddedSimDataProblems.MissingColumns;
        if (mistyped.Count > 0)
            problems |= TuningPackageAddedSimDataProblems.MistypedColumns;
        if (extra.Count > 0)
            problems |= TuningPackageAddedSimDataProblems.ExtraColumns;
        if (mismatchedHashes.Count > 0)
            problems |= TuningPackageAddedSimDataProblems.MismatchedHashes;
        if (walk.FirstDifference is not null)
            problems |= TuningPackageAddedSimDataProblems.Values;
        return new(problems, missing, mistyped, extra, mismatchedHashes, walk.FirstDifference);
    }

    static void CompareObjects(SimDataObject simData, SimDataObject generated, Walk walk)
    {
        if (!walk.Visited.Add((simData, generated)))
            return;
        foreach (var column in simData.Schema.Columns)
        {
            if (walk.FirstDifference is not null)
                return;
            if (FindColumn(generated.Schema, column.Name) is not { } other || other.Type != column.Type)
                continue;
            CompareValues(simData[column.Name], generated[column.Name], GetKey(simData.Schema.Name, column.Name), walk);
        }
    }

    static void CompareValues(object? simData, object? generated, string key, Walk walk)
    {
        if (AreBothEmptyLists(simData, generated))
            return;
        switch (simData)
        {
            case SimDataObject simDataObject when generated is SimDataObject generatedObject:
                if (simDataObject.Schema.Name != generatedObject.Schema.Name)
                    walk.FirstDifference ??= key;
                else
                    CompareObjects(simDataObject, generatedObject, walk);
                return;
            case SimDataVector simDataVector when generated is SimDataVector generatedVector:
                if (walk.IsSet(key) && HaveSameItems(simDataVector, generatedVector))
                    return;
                if (simDataVector.Count != generatedVector.Count)
                {
                    walk.FirstDifference ??= key;
                    return;
                }
                for (var index = 0; index < simDataVector.Count && walk.FirstDifference is null; ++index)
                    CompareValues(simDataVector[index], generatedVector[index], key, walk);
                return;
            case SimDataVariant simDataVariant when generated is SimDataVariant generatedVariant:
                if (simDataVariant.TypeHash != generatedVariant.TypeHash)
                    walk.FirstDifference ??= key;
                else
                    CompareValues(simDataVariant.Value, generatedVariant.Value, key, walk);
                return;
        }
        if (IsStructured(simData) || IsStructured(generated) || !PrimitivesEqual(simData, generated))
            walk.FirstDifference ??= key;
    }

    /// <summary>
    /// Gets the columns of the instances of the specified <paramref name="a"/> whose values aren't those of the specified <paramref name="b"/>, the items of columns that <paramref name="isSet"/> in any order
    /// </summary>
    internal static HashSet<string> GetDifferingColumns(SimDataModel a, SimDataModel b, Func<string, bool> isSet)
    {
        var differing = new HashSet<string>(StringComparer.Ordinal);
        var counterparts = GetInstancesByName(b);
        foreach (var instance in a.Instances)
        {
            if (!counterparts.TryGetValue(instance.Name ?? string.Empty, out var counterpart))
                continue;
            var source = instance.Object;
            var other = counterpart.Object;
            foreach (var column in source.Schema.Columns.Select(column => column.Name).Union(other.Schema.Columns.Select(column => column.Name)))
            {
                var key = GetKey(source.Schema.Name, column);
                if (FindColumn(source.Schema, column) is not { } sourceColumn || FindColumn(other.Schema, column) is not { } otherColumn || sourceColumn.Type != otherColumn.Type)
                {
                    differing.Add(column);
                    continue;
                }
                var walk = new Walk(isSet);
                CompareValues(source[column], other[column], key, walk);
                if (walk.FirstDifference is not null)
                    differing.Add(column);
            }
        }
        return differing;
    }

    /// <summary>
    /// Gets the values of the columns of the instances of the specified <paramref name="simData"/> that aren't those of the SimData <paramref name="generated"/> from its tuning, as tuning would give them, for the columns whose tunables the tuning's <paramref name="root"/> leaves out and that <paramref name="isScalar"/>
    /// </summary>
    internal static IReadOnlyList<(string name, string value)> GetLeftOutTunables(SimDataModel simData, SimDataModel generated, XElement root, Func<string, bool> isScalar, Func<string, bool> isSet)
    {
        var tuned = root.Elements().Select(element => element.Attribute("n")?.Value).OfType<string>().ToHashSet(StringComparer.Ordinal);
        var counterparts = GetInstancesByName(generated);
        var leftOut = new List<(string name, string value)>();
        foreach (var name in GetDifferingColumns(simData, generated, isSet).OrderBy(name => name, StringComparer.Ordinal))
        {
            if (tuned.Contains(name) || !isScalar(name))
                continue;
            foreach (var instance in simData.Instances)
                if (counterparts.ContainsKey(instance.Name ?? string.Empty) && FindColumn(instance.Object.Schema, name) is { } column && FormatTunable(instance.Object[name], column.Type) is { } value)
                {
                    leftOut.Add((name, value));
                    break;
                }
        }
        return leftOut;
    }

    static string? FormatTunable(object? value, SimDataType type) =>
        value switch
        {
            _ when type is SimDataType.LOCKEY && value is uint key => $"0x{key:X8}",
            bool flag => flag ? "True" : "False",
            float number => number.ToString("R", CultureInfo.InvariantCulture),
            double number => number.ToString("R", CultureInfo.InvariantCulture),
            string text => text,
            ResourceKey key => key.ToString(),
            sbyte or byte or short or ushort or int or uint or long or ulong => ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture),
            _ => null
        };

    /// <summary>
    /// Adds the specified <paramref name="tunables"/> to the root of the specified tuning <paramref name="text"/>, each before the first of the root's tunables whose name sorts after it, keeping everything else as it was
    /// </summary>
    internal static string? AddTunables(string text, IReadOnlyList<(string name, string value)> tunables)
    {
        XDocument document;
        try
        {
            document = XDocument.Parse(text, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
        }
        catch (XmlException)
        {
            return null;
        }
        if (document.Root is not { } root)
            return null;
        var lineStarts = new List<int> { 0 };
        for (var index = 0; index < text.Length; ++index)
            if (text[index] == '\n')
                lineStarts.Add(index + 1);
        var newline = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        int? getOffset(XElement element)
        {
            var info = (IXmlLineInfo)element;
            if (!info.HasLineInfo())
                return null;
            var offset = lineStarts[info.LineNumber - 1] + info.LinePosition - 2;
            return offset >= 0 && offset < text.Length && text[offset] == '<' ? offset : null;
        }
        string? getIndentation(int offset)
        {
            var lineStart = offset == 0 ? 0 : text.LastIndexOf('\n', offset - 1) + 1;
            return lineStart > 0 && text[lineStart..offset].All(char.IsWhiteSpace) ? text[lineStart..offset] : null;
        }
        var children = root.Elements().ToList();
        var end = text.LastIndexOf("</", StringComparison.Ordinal);
        if (end < 0)
            return null;
        var insertions = new List<(int offset, string name, string text)>();
        foreach (var (name, value) in tunables)
        {
            var element = new XElement("T", new XAttribute("n", name), value).ToString(SaveOptions.DisableFormatting);
            if (children.FirstOrDefault(child => string.CompareOrdinal(child.Attribute("n")?.Value, name) > 0) is { } next)
            {
                if (getOffset(next) is not { } offset)
                    return null;
                insertions.Add((offset, name, getIndentation(offset) is { } indentation ? $"{element}{newline}{indentation}" : element));
                continue;
            }
            var childIndentation = children.Count > 0 && getOffset(children[0]) is { } first ? getIndentation(first) ?? string.Empty : string.Empty;
            if (getIndentation(end) is { } closingIndentation)
                insertions.Add((end - closingIndentation.Length, name, $"{childIndentation}{element}{newline}"));
            else
                insertions.Add((end, name, element));
        }
        var builder = new StringBuilder(text);
        foreach (var (offset, _, insertion) in insertions.OrderByDescending(insertion => insertion.offset).ThenByDescending(insertion => insertion.name, StringComparer.Ordinal))
            builder.Insert(offset, insertion);
        return builder.ToString();
    }

    /// <summary>
    /// Counts, in the specified <paramref name="observations"/>, which columns of the SimData <paramref name="generated"/> from the game's tuning have the values of the game's own <paramref name="simData"/>, the items of columns that <paramref name="isSet"/> in any order
    /// </summary>
    internal static void Observe(SimDataModel simData, SimDataModel generated, Dictionary<string, (int same, int different)> observations, Func<string, bool> isSet)
    {
        var walk = new Walk(isSet);
        var counterparts = GetInstancesByName(generated);
        foreach (var instance in simData.Instances)
            if (counterparts.TryGetValue(instance.Name ?? string.Empty, out var counterpart))
                ObserveObjects(instance.Object, counterpart.Object, observations, walk);
    }

    static void Mark(Dictionary<string, (int same, int different)> observations, string key, bool same)
    {
        var (sameCount, differentCount) = observations.GetValueOrDefault(key);
        observations[key] = same ? (sameCount + 1, differentCount) : (sameCount, differentCount + 1);
    }

    static void ObserveObjects(SimDataObject simData, SimDataObject generated, Dictionary<string, (int same, int different)> observations, Walk walk)
    {
        if (!walk.Visited.Add((simData, generated)))
            return;
        var schema = simData.Schema.Name;
        foreach (var column in simData.Schema.Columns)
        {
            var key = GetKey(schema, column.Name);
            if (FindColumn(generated.Schema, column.Name) is not { } other || other.Type != column.Type)
            {
                Mark(observations, key, false);
                continue;
            }
            Mark(observations, key, ObserveValues(simData[column.Name], generated[column.Name], key, observations, walk));
        }
        foreach (var column in generated.Schema.Columns)
            if (FindColumn(simData.Schema, column.Name) is null)
                Mark(observations, GetKey(schema, column.Name), false);
    }

    static bool ObserveValues(object? simData, object? generated, string key, Dictionary<string, (int same, int different)> observations, Walk walk)
    {
        if (AreBothEmptyLists(simData, generated))
            return true;
        switch (simData)
        {
            case SimDataObject simDataObject when generated is SimDataObject generatedObject:
                if (simDataObject.Schema.Name != generatedObject.Schema.Name)
                    return false;
                ObserveObjects(simDataObject, generatedObject, observations, walk);
                return true;
            case SimDataVector simDataVector when generated is SimDataVector generatedVector:
                if (walk.IsSet(key) && HaveSameItems(simDataVector, generatedVector))
                    return true;
                if (simDataVector.Count != generatedVector.Count)
                    return false;
                var same = true;
                for (var index = 0; index < simDataVector.Count; ++index)
                    same &= ObserveValues(simDataVector[index], generatedVector[index], key, observations, walk);
                return same;
            case SimDataVariant simDataVariant when generated is SimDataVariant generatedVariant:
                return simDataVariant.TypeHash == generatedVariant.TypeHash && ObserveValues(simDataVariant.Value, generatedVariant.Value, key, observations, walk);
        }
        return !IsStructured(simData) && !IsStructured(generated) && PrimitivesEqual(simData, generated);
    }

    static SimDataModel? TryCopy(SimDataModel simData, SimDataVersion version)
    {
        var original = simData.Version;
        try
        {
            simData.Version = version;
            return SimDataModel.Decode(simData.Encode());
        }
        catch (Exception exception) when (exception is InvalidOperationException or InvalidDataException or ArgumentException or NotSupportedException)
        {
            return null;
        }
        finally
        {
            simData.Version = original;
        }
    }

    /// <summary>
    /// Makes the specified <paramref name="fixes"/> to the specified <paramref name="simData"/>, taking values only from columns of the SimData <paramref name="generated"/> from its tuning that <paramref name="isReliable"/>
    /// </summary>
    /// <returns>The fixed SimData, or why it couldn't be fixed, and, if the fixes asked to rebuild it from its tuning but it couldn't be, why</returns>
    internal static (SimDataModel? simData, string? failure, string? rebuildFailure) Repair(SimDataModel simData, SimDataModel generated, TuningPackageAddedSimDataFixes fixes, Func<string, bool> isReliable)
    {
        string? rebuildFailure = null;
        if (fixes.HasFlag(TuningPackageAddedSimDataFixes.MatchTuning))
        {
            var unreliable = generated.Schemas.SelectMany(schema => schema.Columns.Select(column => GetKey(schema.Name, column.Name))).FirstOrDefault(key => !isReliable(key));
            if (unreliable is null)
            {
                if ((TryCopy(generated, simData.Version) ?? TryCopy(generated, generated.Version)) is { } rebuilt)
                    return (rebuilt, null, null);
                rebuildFailure = "SimData generated from it couldn't be written";
            }
            else
                rebuildFailure = $"{unreliable} can't be generated reliably from the tuning";
        }
        var expectedSchemas = GetSchemasByName(generated);
        var finals = new Dictionary<string, (SimDataSchema schema, HashSet<string> generatedColumns)>(StringComparer.Ordinal);
        foreach (var schema in simData.Schemas)
        {
            var generatedColumns = new HashSet<string>(StringComparer.Ordinal);
            if (!expectedSchemas.TryGetValue(schema.Name, out var expected))
            {
                finals[schema.Name] = (schema, generatedColumns);
                continue;
            }
            var columns = new List<SimDataColumn>();
            var changed = false;
            foreach (var column in expected.Columns)
            {
                var existing = FindColumn(schema, column.Name);
                if (existing is not null && existing.Type == column.Type)
                {
                    columns.Add(existing);
                    continue;
                }
                var key = GetKey(schema.Name, column.Name);
                if (existing is null && !fixes.HasFlag(TuningPackageAddedSimDataFixes.AddMissingColumns))
                    return (null, "adding missing columns isn't chosen", rebuildFailure);
                if (existing is not null && !fixes.HasFlag(TuningPackageAddedSimDataFixes.CorrectColumnTypes))
                    return (null, "correcting column types isn't chosen", rebuildFailure);
                if (!isReliable(key))
                    return (null, $"{key} can't be generated reliably from the tuning", rebuildFailure);
                columns.Add(column);
                generatedColumns.Add(column.Name);
                changed = true;
            }
            for (var index = 0; index < schema.Columns.Count; ++index)
            {
                var column = schema.Columns[index];
                if (FindColumn(expected, column.Name) is not null)
                    continue;
                if (fixes.HasFlag(TuningPackageAddedSimDataFixes.RemoveExtraColumns))
                {
                    changed = true;
                    continue;
                }
                var position = 0;
                for (var previous = index - 1; previous >= 0; --previous)
                {
                    var previousName = schema.Columns[previous].Name;
                    var found = columns.FindIndex(candidate => candidate.Name == previousName);
                    if (found >= 0)
                    {
                        position = found + 1;
                        break;
                    }
                }
                columns.Insert(position, column);
            }
            finals[schema.Name] = changed
                ? (new SimDataSchema(schema.Name, columns), generatedColumns)
                : (schema.Hash != schema.ComputedHash && fixes.HasFlag(TuningPackageAddedSimDataFixes.CorrectSchemaHashes) ? new SimDataSchema(schema.Name, schema.Columns) : schema, generatedColumns);
        }
        string? failure = null;
        var built = new Dictionary<SimDataObject, SimDataObject>(ReferenceEqualityComparer.Instance);
        var builtFromGenerated = new Dictionary<SimDataObject, SimDataObject>(ReferenceEqualityComparer.Instance);
        SimDataObject build(SimDataObject source, SimDataObject? counterpart)
        {
            if (built.TryGetValue(source, out var existing))
                return existing;
            var (schema, generatedColumns) = finals[source.Schema.Name];
            if (counterpart is not null && counterpart.Schema.Name != source.Schema.Name)
                counterpart = null;
            var target = new SimDataObject(schema);
            built[source] = target;
            foreach (var column in schema.Columns)
            {
                var key = GetKey(schema.Name, column.Name);
                if (generatedColumns.Contains(column.Name))
                {
                    if (counterpart is null)
                        failure ??= $"{key} couldn't be matched with SimData generated from the tuning";
                    else
                        target[column.Name] = buildGeneratedValue(counterpart[column.Name], key);
                    continue;
                }
                var counterpartValue = counterpart is not null && FindColumn(counterpart.Schema, column.Name) is { } other && other.Type == column.Type ? counterpart[column.Name] : null;
                target[column.Name] = buildValue(source[column.Name], counterpartValue);
            }
            return target;
        }
        object? buildValue(object? value, object? counterpart) =>
            value switch
            {
                SimDataObject source => build(source, counterpart as SimDataObject),
                SimDataVector vector => new SimDataVector(vector.ElementType, vector.Select((item, index) => buildValue(item, counterpart is SimDataVector other && other.Count == vector.Count ? other[index] : null)!)),
                SimDataVariant { Type: { } type } variant => new SimDataVariant(variant.TypeHash, type, buildValue(variant.Value, counterpart is SimDataVariant other && other.TypeHash == variant.TypeHash ? other.Value : null)!),
                _ => value
            };
        SimDataObject buildGenerated(SimDataObject source)
        {
            if (builtFromGenerated.TryGetValue(source, out var existing))
                return existing;
            var schema = finals.TryGetValue(source.Schema.Name, out var final) ? final.schema : source.Schema;
            var target = new SimDataObject(schema);
            builtFromGenerated[source] = target;
            foreach (var column in schema.Columns)
            {
                var key = GetKey(schema.Name, column.Name);
                if (FindColumn(source.Schema, column.Name) is not { } other || other.Type != column.Type)
                {
                    failure ??= $"{key} has no value in SimData generated from the tuning";
                    continue;
                }
                target[column.Name] = buildGeneratedValue(source[column.Name], key);
            }
            return target;
        }
        object? buildGeneratedValue(object? value, string key)
        {
            if (!isReliable(key))
                failure ??= $"{key} can't be generated reliably from the tuning";
            return value switch
            {
                SimDataObject source => buildGenerated(source),
                SimDataVector vector => new SimDataVector(vector.ElementType, vector.Select(item => buildGeneratedValue(item, key)!)),
                SimDataVariant { Type: { } type } variant => new SimDataVariant(variant.TypeHash, type, buildGeneratedValue(variant.Value, key)!),
                _ => value
            };
        }
        var result = new SimDataModel
        {
            Pack = simData.Pack,
            Version = simData.Version
        };
        var counterparts = GetInstancesByName(generated);
        foreach (var instance in simData.Instances)
            result.Instances.Add(new(instance.Name!, build(instance.Object, counterparts.TryGetValue(instance.Name ?? string.Empty, out var counterpart) ? counterpart.Object : null)));
        if (failure is not null)
            return (null, failure, rebuildFailure);
        try
        {
            return (SimDataModel.Decode(result.Encode()), null, rebuildFailure);
        }
        catch (Exception exception) when (exception is InvalidOperationException or InvalidDataException or ArgumentException or NotSupportedException)
        {
            return (null, $"the fixed SimData couldn't be written ({exception.Message})", rebuildFailure);
        }
    }
}