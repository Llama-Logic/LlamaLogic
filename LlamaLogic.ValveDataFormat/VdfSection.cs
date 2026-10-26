namespace LlamaLogic.ValveDataFormat;

/// <summary>
/// Represents a section in a VDF document
/// </summary>
public sealed class VdfSection :
    VdfNode,
    IEquatable<VdfSection>
{
    /// <inheritdoc/>
    public static bool operator ==(VdfSection? left, VdfSection? right) =>
        left?.Equals(right) ?? left is null == right is null;

    /// <inheritdoc/>
    public static bool operator !=(VdfSection? left, VdfSection? right) =>
        !(left == right);

    /// <summary>
    /// Gets the list of <see cref="VdfNode"/> in the <see cref="VdfSection"/>
    /// </summary>
    public IList<VdfNode> Nodes { get; } = [];

    /// <summary>
    /// Gets/sets the comment following the opening character ('{') of the section
    /// </summary>
    public string? OpeningTrailingComment { get; set; }

    /// <summary>
    /// Gets/sets <see cref="VdfKeyValuePair.Value"/> of the <see cref="VdfKeyValuePair"/> at the specified <paramref name="index"/> of the section
    /// </summary>
    public object this[int index]
    {
        get => Nodes.OfType<VdfKeyValuePair>().ElementAt(index).Value;
        set => Nodes.OfType<VdfKeyValuePair>().ElementAt(index).Value = value;
    }

    /// <summary>
    /// Gets/sets <see cref="VdfKeyValuePair.Value"/> of the <see cref="VdfKeyValuePair"/> with the specified <paramref name="key"/>
    /// </summary>
    public object this[string key]
    {
        get => Nodes.OfType<VdfKeyValuePair>().First(kvp => kvp.Key == key).Value;
        set => Nodes.OfType<VdfKeyValuePair>().First(kvp => kvp.Key == key).Value = value;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is VdfSection vdfSection
        && Equals(vdfSection);

    /// <inheritdoc/>
    public override bool Equals(VdfNode? other) =>
        other is VdfSection vdfSection
        && Equals(vdfSection);

    /// <inheritdoc/>
    public bool Equals(VdfSection? other)
    {
        if (other is null
            || !base.Equals(other)
            || Nodes.Count != other.Nodes.Count)
            return false;
        for (var i = 0; i < Nodes.Count; ++i)
            if (!Nodes[i].Equals(other.Nodes[i]))
                return false;
        return true;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(typeof(VdfSection));
        hashCode.Add(OpeningTrailingComment);
        foreach (var node in Nodes)
            hashCode.Add(node.GetHashCode());
        hashCode.Add(base.GetHashCode());
        return hashCode.ToHashCode();
    }

    internal override void Serialize(StreamWriter writer, int depth)
    {
        for (var i = 0; i < depth; ++i)
            writer.Write('\t');
        writer.Write('{');
        if (OpeningTrailingComment is { } openingTrailingComment)
            writer.WriteLine($"\t\t// {openingTrailingComment}");
        else
            writer.WriteLine();
        ++depth;
        foreach (var node in Nodes)
            node.Serialize(writer, depth);
        --depth;
        for (var i = 0; i < depth; ++i)
            writer.Write('\t');
        writer.Write('}');
        if (TrailingComment is { } trailingComment)
            writer.WriteLine($"\t\t// {trailingComment}");
        else
            writer.WriteLine();
    }

    internal override async ValueTask SerializeAsync(StreamWriter writer, int depth)
    {
        for (var i = 0; i < depth; ++i)
            await writer.WriteAsync('\t').ConfigureAwait(false);
        await writer.WriteAsync('{').ConfigureAwait(false);
        if (OpeningTrailingComment is { } openingTrailingComment)
            await writer.WriteLineAsync($"\t\t// {openingTrailingComment}").ConfigureAwait(false);
        else
            await writer.WriteLineAsync().ConfigureAwait(false);
        ++depth;
        foreach (var node in Nodes)
            await node.SerializeAsync(writer, depth).ConfigureAwait(false);
        --depth;
        for (var i = 0; i < depth; ++i)
            await writer.WriteAsync('\t').ConfigureAwait(false);
        await writer.WriteAsync('}').ConfigureAwait(false);
        if (TrailingComment is { } trailingComment)
            await writer.WriteLineAsync($"\t\t// {trailingComment}").ConfigureAwait(false);
        else
            await writer.WriteLineAsync().ConfigureAwait(false);
    }
}