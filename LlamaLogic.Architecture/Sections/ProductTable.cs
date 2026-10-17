namespace LlamaLogic.Architecture;

/// <summary>
/// Represents a table of catalog product keys and the ids by which the rest of a section refers to them
/// </summary>
/// <remarks>
/// <para>The layout follows the game's reader and writer.
/// When the game adds a key, it gives a new key the lowest id from 1 up that is not in use, and adds the added amount to the <see cref="ProductTableEntry.ReferenceCount"/> of a key that is already present.
/// Id 0 stands for <see cref="DefaultKey"/>: the table returns 0 for that key and never adds it.</para>
/// <para>Layout: <see cref="DefaultKey"/> (instance, type, group), a <see cref="uint"/> count, then the <see cref="Entries"/>.</para>
/// </remarks>
public sealed class ProductTable
{
    /// <summary>
    /// Gets/sets the key that id 0 stands for
    /// </summary>
    /// <remarks>
    /// It is the null key in every lot examined.
    /// </remarks>
    public ResourceKey DefaultKey { get; set; }

    /// <summary>
    /// Gets the entries, in the order the game wrote them
    /// </summary>
    public Collection<ProductTableEntry> Entries { get; } = [];

    internal void Read(ref SectionReader reader)
    {
        DefaultKey = reader.ReadKeyInstanceTypeGroup();
        var count = reader.ReadCount(reader.ReadUInt32());
        Entries.Clear();
        for (var i = 0; i < count; ++i)
            Entries.Add(ProductTableEntry.Read(ref reader));
    }

    internal void Write(SectionWriter writer)
    {
        writer.WriteKeyInstanceTypeGroup(DefaultKey);
        writer.Write((uint)Entries.Count);
        foreach (var entry in Entries)
            entry.Write(writer);
    }
}
