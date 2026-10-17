namespace LlamaLogic.Packages.Models.World;

/// <summary>
/// Represents a <see cref="ResourceType.WorldObjectData"/> resource: the objects, routing footprints and locators of a world's lots (🔓)
/// </summary>
/// <remarks>
/// A world file is a package (see <see cref="DataBasePackedFile"/>); this models one of the resources in it, not the file.
/// Versions 7 through 10 are supported: 10 is the version of every world file the game currently ships, and 7 that of older ones.
/// Version 8 added each object's <see cref="WorldObjectDataObject.Snow"/> flag, version 9 each lot's <see cref="WorldObjectDataLot.StairLocators"/> and version 10 each object's <see cref="WorldObjectDataObject.Flags"/>.
/// </remarks>
public sealed class WorldObjectDataModel :
    Model,
    IModel<WorldObjectDataModel>
{
    const uint minimumVersion = 7;
    const uint maximumVersion = 10;
    static readonly ImmutableHashSet<ResourceType> supportedTypes =
    [
        ResourceType.WorldObjectData
    ];

    /// <inheritdoc/>
    public static new ISet<ResourceType> SupportedTypes =>
        supportedTypes;

    /// <inheritdoc/>
    /// <exception cref="InvalidDataException">The data is not a well-formed world object data resource of version 7 through 10</exception>
    public static WorldObjectDataModel Decode(ReadOnlyMemory<byte> data)
    {
        var span = data.Span;
        try
        {
            var position = 0;
            var version = span.ReadAndAdvancePosition<uint>(ref position);
            if (version is < minimumVersion or > maximumVersion)
                throw new InvalidDataException($"Version {version} is not supported, expected {minimumVersion} through {maximumVersion}");
            var model = new WorldObjectDataModel { Version = version };
            var lotCount = span.ReadAndAdvancePosition<uint>(ref position);
            for (var l = 0U; l < lotCount; ++l)
            {
                var lot = new WorldObjectDataLot
                {
                    LotId = span.ReadAndAdvancePosition<uint>(ref position),
                    FootprintKey = ReadTypeGroupInstance(span, ref position)
                };
                ReadList(span, ref position, lot.Objects, (ReadOnlySpan<byte> s, ref int p) => WorldObjectDataObject.Read(s, ref p, version));
                ReadList(span, ref position, lot.FootprintObjects, (ReadOnlySpan<byte> s, ref int p) => WorldObjectDataObject.Read(s, ref p, version));
                ReadList(span, ref position, lot.SimLocators, ReadLocator);
                if (version >= 9)
                    ReadList(span, ref position, lot.StairLocators, WorldObjectDataStairLocator.Read);
                model.Lots.Add(lot);
            }
            if (position != span.Length)
                throw new InvalidDataException($"{span.Length - position} bytes follow the last lot");
            return model;
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new InvalidDataException("The data ends before the last lot does", ex);
        }
    }

    /// <inheritdoc/>
    public static new string? GetName(Stream stream) =>
        null;

    /// <inheritdoc/>
    public static new Task<string?> GetNameAsync(Stream stream, CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);

    static WorldObjectDataLocator ReadLocator(ReadOnlySpan<byte> span, ref int position)
    {
        var locator = new WorldObjectDataLocator();
        WorldObjectDataLocator.Read(span, ref position, locator);
        return locator;
    }

    delegate T ItemReader<T>(ReadOnlySpan<byte> span, ref int position);

    static void ReadList<T>(ReadOnlySpan<byte> span, ref int position, Collection<T> list, ItemReader<T> read)
    {
        var count = span.ReadAndAdvancePosition<uint>(ref position);
        if (count > (uint)(span.Length - position))
            throw new InvalidDataException($"A list of {count} items cannot fit in the {span.Length - position} remaining bytes");
        for (var i = 0U; i < count; ++i)
            list.Add(read(span, ref position));
    }

    internal static ResourceKey ReadTypeGroupInstance(ReadOnlySpan<byte> span, ref int position)
    {
        var type = span.ReadAndAdvancePosition<uint>(ref position);
        var group = span.ReadAndAdvancePosition<uint>(ref position);
        var instance = span.ReadAndAdvancePosition<ulong>(ref position);
        return new((ResourceType)type, group, instance);
    }

    internal static void WriteTypeGroupInstance(ArrayBufferWriter<byte> writer, ResourceKey key)
    {
        var type = (uint)key.Type;
        writer.Write(ref type);
        var group = key.Group;
        writer.Write(ref group);
        var instance = key.FullInstance;
        writer.Write(ref instance);
    }

    static void WriteList<T>(ArrayBufferWriter<byte> writer, Collection<T> list, uint version)
        where T : WorldObjectDataLocator
    {
        var count = (uint)list.Count;
        writer.Write(ref count);
        foreach (var item in list)
            item.Write(writer, version);
    }

    /// <summary>
    /// Gets/sets the version of the resource, which determines which values <see cref="Encode"/> writes
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not 7 through 10</exception>
    public uint Version
    {
        get => version;
        set
        {
            if (value is < minimumVersion or > maximumVersion)
                throw new ArgumentOutOfRangeException(nameof(value), value, $"The version must be {minimumVersion} through {maximumVersion}");
            version = value;
        }
    }

    uint version = maximumVersion;

    /// <summary>
    /// Gets the lots
    /// </summary>
    public Collection<WorldObjectDataLot> Lots { get; } = [];

    /// <inheritdoc/>
    public override string? ResourceName =>
        null;

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">A lot has <see cref="WorldObjectDataLot.StairLocators"/> but <see cref="Version"/> is less than 9, which cannot store them</exception>
    public override ReadOnlyMemory<byte> Encode()
    {
        if (version < 9 && Lots.Any(lot => lot.StairLocators.Count > 0))
            throw new InvalidOperationException($"Version {version} cannot store stair locators");
        var writer = new ArrayBufferWriter<byte>();
        var encodedVersion = version;
        writer.Write(ref encodedVersion);
        var lotCount = (uint)Lots.Count;
        writer.Write(ref lotCount);
        foreach (var lot in Lots)
        {
            var lotId = lot.LotId;
            writer.Write(ref lotId);
            WriteTypeGroupInstance(writer, lot.FootprintKey);
            WriteList(writer, lot.Objects, version);
            WriteList(writer, lot.FootprintObjects, version);
            WriteList(writer, lot.SimLocators, version);
            if (version >= 9)
                WriteList(writer, lot.StairLocators, version);
        }
        return writer.WrittenMemory;
    }
}
