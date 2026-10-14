namespace LlamaLogic.Packages.Models.SimData;

/// <summary>
/// Represents the schema of a <see cref="SimDataObject"/> (🔒)
/// </summary>
/// <remarks>
/// The order of <see cref="Columns"/> is the order in which the columns' values are laid out in each row.
/// For schemas the game reads with a fixed layout (e.g. <c>Client_*</c> and <c>Renderer.*</c> schemas), this order must match the game's, which is why <see cref="SimDataModel.Decode(ReadOnlyMemory{byte})"/> preserves it.
/// The game derives a schema's hash from its name and layout (see <see cref="ComputedHash"/>), so changing the columns or their order changes the hash the game expects.
/// </remarks>
public sealed class SimDataSchema
{
    /// <summary>
    /// Initializes a new <see cref="SimDataSchema"/>
    /// </summary>
    /// <param name="name">The name of the schema</param>
    /// <param name="hash">The hash identifying the schema</param>
    /// <param name="columns">The columns of the schema, in layout order</param>
    /// <exception cref="ArgumentException"><paramref name="columns"/> is empty, contains <see langword="null"/>, or contains two columns with the same name</exception>
    public SimDataSchema(string name, uint hash, IEnumerable<SimDataColumn> columns)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(columns);
        var columnArray = columns.ToImmutableArray();
        if (columnArray.IsEmpty)
            throw new ArgumentException("A schema must have at least one column", nameof(columns));
        if (columnArray.Any(column => column is null))
            throw new ArgumentException("A schema cannot have null columns", nameof(columns));
        var columnIndices = new Dictionary<string, int>();
        var offsets = new uint[columnArray.Length];
        var offset = 0U;
        var maximumAlignment = 1U;
        for (var i = 0; i < columnArray.Length; ++i)
        {
            var column = columnArray[i];
            if (!columnIndices.TryAdd(column.Name, i))
                throw new ArgumentException($"A schema cannot have two columns named {column.Name}", nameof(columns));
            var alignment = column.Type.GetAlignment();
            offset = (offset + alignment - 1) & ~(alignment - 1);
            offsets[i] = offset;
            offset += column.Type.GetSize();
            maximumAlignment = Math.Max(maximumAlignment, alignment);
        }
        Name = name;
        Hash = hash;
        Columns = columnArray;
        this.columnIndices = columnIndices;
        Offsets = [.. offsets];
        Size = (offset + maximumAlignment - 1) & ~(maximumAlignment - 1);
        var computedHash = SimDataUtilities.GetNameHash(name) + (Size << 16) + (uint)columnArray.Length;
        for (var i = 0; i < columnArray.Length; ++i)
            computedHash += SimDataUtilities.GetNameHash(columnArray[i].Name) ^ ((uint)columnArray[i].Type << 16) ^ (offsets[i] << 8);
        ComputedHash = computedHash;
    }

    /// <summary>
    /// Initializes a new <see cref="SimDataSchema"/> whose <see cref="Hash"/> is its <see cref="ComputedHash"/>
    /// </summary>
    /// <param name="name">The name of the schema</param>
    /// <param name="columns">The columns of the schema, in layout order</param>
    /// <exception cref="ArgumentException"><paramref name="columns"/> is empty, contains <see langword="null"/>, or contains two columns with the same name</exception>
    public SimDataSchema(string name, IEnumerable<SimDataColumn> columns) :
        this(name, 0, columns) =>
        Hash = ComputedHash;

    readonly Dictionary<string, int> columnIndices;

    /// <summary>
    /// Gets the columns of the schema, in layout order
    /// </summary>
    public IReadOnlyList<SimDataColumn> Columns { get; }

    /// <summary>
    /// Gets the hash the game derives from the schema's <see cref="Name"/> and the layout of its <see cref="Columns"/>
    /// </summary>
    /// <remarks>
    /// The game computes this as the sum of the <see cref="Fnv32"/> hash of the lowercased name, the row size shifted left 16 bits, the number of columns, and, for each column, the <see cref="Fnv32"/> hash of its lowercased name XOR its type shifted left 16 bits XOR its offset in the row shifted left 8 bits.
    /// Column flags do not contribute.
    /// </remarks>
    public uint ComputedHash { get; }

    /// <summary>
    /// Gets the hash identifying the schema
    /// </summary>
    public uint Hash { get; }

    /// <summary>
    /// Gets the name of the schema
    /// </summary>
    public string Name { get; }

    internal ImmutableArray<uint> Offsets { get; }

    internal uint Size { get; }

    internal bool DefinitionEquals(SimDataSchema other) =>
        ReferenceEquals(this, other)
        || Name == other.Name
        && Hash == other.Hash
        && Columns.Count == other.Columns.Count
        && Columns.Zip(other.Columns).All(t => t.First.Name == t.Second.Name && t.First.Type == t.Second.Type && t.First.Flags == t.Second.Flags);

    /// <summary>
    /// Gets the index of the column with the specified <paramref name="name"/>, or <see langword="null"/> if there is no such column
    /// </summary>
    public int? GetColumnIndex(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return columnIndices.TryGetValue(name, out var index) ? index : null;
    }
}
