namespace LlamaLogic.TuningArchive;

/// <summary>
/// Represents a version of a resource in a <see cref="TuningArchiveFile"/>
/// </summary>
public sealed class TuningArchiveEntry
{
    internal TuningArchiveEntry(long resourceId, long versionId, TuningArchiveResourceKind kind, ResourceType type, uint group, ulong instance, string? name, string pack, string? @class, string? module, long blobId, long firstOrdinal, long? lastOrdinal)
    {
        ResourceId = resourceId;
        VersionId = versionId;
        Kind = kind;
        Type = type;
        Group = group;
        Instance = instance;
        Name = name;
        Pack = pack;
        Class = @class;
        Module = module;
        BlobId = blobId;
        FirstOrdinal = firstOrdinal;
        LastOrdinal = lastOrdinal;
    }

    internal long BlobId { get; }

    /// <summary>
    /// Gets the Python class of the tuning (its <c>c</c> attribute), if it is instance tuning
    /// </summary>
    public string? Class { get; }

    internal long FirstOrdinal { get; }

    /// <summary>
    /// Gets the group of the resource
    /// </summary>
    public uint Group { get; }

    /// <summary>
    /// Gets the instance of the resource
    /// </summary>
    public ulong Instance { get; }

    /// <summary>
    /// Gets the resource key of the resource
    /// </summary>
    public ResourceKey Key =>
        new(Type, Group, Instance);

    /// <summary>
    /// Gets the kind of the resource
    /// </summary>
    public TuningArchiveResourceKind Kind { get; }

    internal long? LastOrdinal { get; }

    /// <summary>
    /// Gets the Python module of the tuning (its <c>m</c> attribute, or the module of module tuning), if it has one
    /// </summary>
    public string? Module { get; }

    /// <summary>
    /// Gets the name of the resource (its tuning name, SimData instance name, or the path of a Python module), if it has one
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets the code of the pack the resource came from (<c>BG</c> for the base game)
    /// </summary>
    public string Pack { get; }

    internal long ResourceId { get; }

    /// <summary>
    /// Gets the type of the resource
    /// </summary>
    public ResourceType Type { get; }

    internal long VersionId { get; }

    /// <summary>
    /// Gets whether this version and the specified <paramref name="other"/> have exactly the same content
    /// </summary>
    public bool HasSameContentAs(TuningArchiveEntry other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return other.BlobId == BlobId;
    }

    /// <inheritdoc/>
    public override string ToString() =>
        $"{Key} {Name}";
}