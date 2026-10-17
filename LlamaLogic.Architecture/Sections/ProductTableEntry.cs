namespace LlamaLogic.Architecture;

/// <summary>
/// Represents an entry of a <see cref="ProductTable"/>: a catalog product key and its id
/// </summary>
/// <remarks>
/// Layout: <see cref="Key"/> (instance, type, group), <see cref="Id"/>, <see cref="ReferenceCount"/>.
/// </remarks>
public sealed class ProductTableEntry
{
    internal static ProductTableEntry Read(ref SectionReader reader) =>
        new()
        {
            Key = reader.ReadKeyInstanceTypeGroup(),
            Id = reader.ReadUInt32(),
            ReferenceCount = reader.ReadUInt32()
        };

    /// <summary>
    /// Gets/sets the id by which the rest of the section refers to <see cref="Key"/>
    /// </summary>
    /// <remarks>
    /// Ids are unique within a table and never 0 in the lots examined; they run 1 through the entry count in 38,097 of the 68,278 non-empty tables examined (56%).
    /// </remarks>
    public uint Id { get; set; }

    /// <summary>
    /// Gets/sets the key of the catalog product
    /// </summary>
    public ResourceKey Key { get; set; }

    /// <summary>
    /// Gets/sets the reference count the game keeps for the key
    /// </summary>
    /// <remarks>
    /// The game adds to it each time the key is added to the table again.
    /// It does not always equal the number of references in the section: in 63,616 of the 63,742 non-empty block product tables examined every reference count is 1.
    /// </remarks>
    public uint ReferenceCount { get; set; }

    internal void Write(SectionWriter writer)
    {
        writer.WriteKeyInstanceTypeGroup(Key);
        writer.Write(Id);
        writer.Write(ReferenceCount);
    }
}
