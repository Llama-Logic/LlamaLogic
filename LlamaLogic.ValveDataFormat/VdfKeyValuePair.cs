namespace LlamaLogic.ValveDataFormat;

/// <summary>
/// Represents a key-value pair in a VDF document
/// </summary>
public sealed class VdfKeyValuePair() :
    VdfNode,
    IEquatable<VdfKeyValuePair>
{
    /// <inheritdoc/>
    public static bool operator ==(VdfKeyValuePair? left, VdfKeyValuePair? right) =>
        left?.Equals(right) ?? left is null == right is null;

    /// <inheritdoc/>
    public static bool operator !=(VdfKeyValuePair? left, VdfKeyValuePair? right) =>
        !(left == right);

    /// <summary>
    /// Gets/sets the key of the key-value pair
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets/sets the comment following the key
    /// </summary>
    public string? KeyTrailingComment { get; set; }

    /// <summary>
    /// Gets/sets the value of the key-value pair
    /// </summary>
    public object Value { get; set; } = new();

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is VdfKeyValuePair vdfKeyValuePair
        && Equals(vdfKeyValuePair);

    /// <inheritdoc/>
    public override bool Equals(VdfNode? other) =>
        other is VdfKeyValuePair vdfKeyValuePair
        && Equals(vdfKeyValuePair);

    /// <inheritdoc/>
    public bool Equals(VdfKeyValuePair? other) =>
        other is not null
        && base.Equals(other)
        && Key.Equals(other.Key, StringComparison.OrdinalIgnoreCase)
        && Value.Equals(other.Value)
        && KeyTrailingComment is null == other.KeyTrailingComment is null
        && (KeyTrailingComment ?? string.Empty).Equals(other.KeyTrailingComment ?? string.Empty, StringComparison.Ordinal);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        HashCode.Combine
        (
            typeof(VdfKeyValuePair),
            Key,
            KeyTrailingComment,
            Value,
            base.GetHashCode()
        );

    internal override void Serialize(StringWriter writer, int depth)
    {
        for (var i = 0; i < depth; ++i)
            writer.Write('\t');
        writer.Write($"\"{Key.Replace(@"\", @"\\", StringComparison.Ordinal).Replace(@"""", @"\""", StringComparison.Ordinal)}\"");
        if (KeyTrailingComment is { } keyTrailingComment)
            writer.Write($"\t\t// {keyTrailingComment}");
        if (Value is VdfSection section)
        {
            if (TrailingComment is { } trailingComment)
                writer.WriteLine($"\t\t// {trailingComment}");
            else
                writer.WriteLine();
            section.Serialize(writer, depth);
        }
        else
        {
            if (KeyTrailingComment is not null)
            {
                writer.WriteLine();
                for (var i = 0; i < depth; ++i)
                    writer.Write('\t');
            }
            var value = Value?.ToString() ?? string.Empty;
            writer.Write($"\t\t\"{value.Replace(@"\", @"\\", StringComparison.Ordinal).Replace(@"""", @"\""", StringComparison.Ordinal)}\"");
            if (TrailingComment is { } trailingComment)
                writer.WriteLine($"\t\t// {trailingComment}");
            else
                writer.WriteLine();
        }
    }

    internal override async ValueTask SerializeAsync(StringWriter writer, int depth)
    {
        for (var i = 0; i < depth; ++i)
            await writer.WriteAsync('\t').ConfigureAwait(false);
        await writer.WriteAsync($"\"{Key}\"").ConfigureAwait(false);
        if (KeyTrailingComment is { } keyTrailingComment)
            await writer.WriteAsync($"\t\t// {keyTrailingComment}").ConfigureAwait(false);
        if (Value is VdfSection section)
        {
            if (TrailingComment is { } trailingComment)
                await writer.WriteLineAsync($"\t\t// {trailingComment}").ConfigureAwait(false);
            else
                await writer.WriteLineAsync().ConfigureAwait(false);
            await section.SerializeAsync(writer, depth).ConfigureAwait(false);
        }
        else
        {
            if (KeyTrailingComment is not null)
            {
                await writer.WriteLineAsync().ConfigureAwait(false);
                for (var i = 0; i < depth; ++i)
                    await writer.WriteAsync('\t').ConfigureAwait(false);
            }
            var value = Value?.ToString() ?? string.Empty;
            await writer.WriteAsync($"\t\t\"{value}\"").ConfigureAwait(false);
            if (TrailingComment is { } trailingComment)
                await writer.WriteLineAsync($"\t\t// {trailingComment}").ConfigureAwait(false);
            else
                await writer.WriteLineAsync().ConfigureAwait(false);
        }
    }
}
