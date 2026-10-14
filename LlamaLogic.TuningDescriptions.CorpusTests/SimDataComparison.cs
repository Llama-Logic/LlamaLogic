namespace LlamaLogic.TuningDescriptions.CorpusTests;

static class SimDataComparison
{
    public static (string Path, string Difference)? Compare(SimDataModel generated, SimDataModel game)
    {
        if (!generated.Instances.Select(instance => instance.Name).SequenceEqual(game.Instances.Select(instance => instance.Name)))
            return (string.Empty, $"instances {string.Join(", ", generated.Instances.Select(instance => instance.Name))} vs {string.Join(", ", game.Instances.Select(instance => instance.Name))}");
        for (var i = 0; i < generated.Instances.Count; ++i)
            if (CompareValues(generated.Instances[i].Object, game.Instances[i].Object, generated.Instances[i].Name) is { } difference)
                return difference;
        return null;
    }

    static (string Path, string Difference)? CompareValues(object? generated, object? game, string path)
    {
        switch (generated)
        {
            case null:
                return game is null or SimDataVector { Count: 0 } ? null : (path, $"null vs {game}");
            case SimDataObject generatedObject:
                if (game is not SimDataObject gameObject)
                    return (path, $"object vs {game}");
                if (generatedObject.Schema.Name != gameObject.Schema.Name)
                    return (path, $"schema {generatedObject.Schema.Name} vs {gameObject.Schema.Name}");
                var generatedColumns = generatedObject.Schema.Columns.Select(column => (column.Name, column.Type)).ToList();
                var gameColumns = gameObject.Schema.Columns.Select(column => (column.Name, column.Type)).ToList();
                if (!generatedColumns.SequenceEqual(gameColumns))
                    return ($"{path}{{{generatedObject.Schema.Name}}}", $"columns {string.Join(", ", generatedColumns)} vs {string.Join(", ", gameColumns)}");
                for (var i = 0; i < generatedColumns.Count; ++i)
                    if (CompareValues(generatedObject[i], gameObject[i], $"{path}.{generatedColumns[i].Name}") is { } difference)
                        return difference;
                return null;
            case SimDataVector generatedVector:
                if (game is null && generatedVector.Count == 0)
                    return null;
                if (game is not SimDataVector gameVector)
                    return (path, $"vector vs {game}");
                if (generatedVector.Count != gameVector.Count)
                    return (path, $"{generatedVector.Count} elements vs {gameVector.Count}");
                for (var i = 0; i < generatedVector.Count; ++i)
                    if (CompareValues(generatedVector[i], gameVector[i], $"{path}[{i}]") is { } difference)
                    {
                        var unmatched = gameVector.ToList();
                        foreach (var element in generatedVector)
                        {
                            var match = unmatched.FindIndex(candidate => CompareValues(element, candidate, path) is null);
                            if (match < 0)
                                return difference;
                            unmatched.RemoveAt(match);
                        }
                        return (path, OrderOnly);
                    }
                return null;
            case SimDataVariant generatedVariant:
                if (game is not SimDataVariant gameVariant)
                    return (path, $"variant vs {game}");
                return generatedVariant.TypeHash != gameVariant.TypeHash
                    ? (path, $"variant {generatedVariant.TypeHash:x8} vs {gameVariant.TypeHash:x8}")
                    : CompareValues(generatedVariant.Value, gameVariant.Value, $"{path}<{generatedVariant.TypeHash:x8}>");
            case float generatedSingle:
                return game is float gameSingle && BitConverter.SingleToInt32Bits(generatedSingle) == BitConverter.SingleToInt32Bits(gameSingle) ? null : (path, $"{generatedSingle} vs {game}");
            default:
                return Equals(generated, game) ? null : (path, $"{generated} vs {game}");
        }
    }

    public const string OrderOnly = "same elements in a different order";
}
