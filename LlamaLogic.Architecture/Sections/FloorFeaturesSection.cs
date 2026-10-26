namespace LlamaLogic.Architecture;

/// <summary>
/// Represents the <see cref="LotArchitectureSectionType.FloorFeatures"/> section: the floor feature layers (🔓)
/// </summary>
/// <remarks>
/// <para>The game's floor feature manager reads and writes the section and exposes it to Python as <c>get_floor_feature</c>, <c>set_floor_feature</c>, <c>has_floor_feature</c>, <c>list_floor_features</c> and <c>scan_floor_features</c>.
/// Each layer is stored as its type, the byte length of its payload and the payload; the game skips the payload of a layer type above 2, which this library does not support: a section holding one is kept undecoded.</para>
/// <para>Versions 0 and 1 are supported, as the game's reader accepts, and the game writes version 1, the only one in the lots examined.
/// The section needs the lot's dimensions.</para>
/// </remarks>
public sealed class FloorFeaturesSection :
    LotArchitectureSection
{
    const uint minimumVersion = 0;
    const uint maximumVersion = 1;
    const uint maximumLayerVersion = 1;
    const sbyte endOfLevels = sbyte.MinValue;

    internal static FloorFeaturesSection Read(ref SectionReader reader, SectionContext context)
    {
        var sizeX = context.RequireSizeX(reader.Type);
        var sizeZ = context.RequireSizeZ(reader.Type);
        var section = new FloorFeaturesSection { Version = reader.ReadVersion(minimumVersion, maximumVersion) };
        var layerCount = reader.ReadByte();
        for (var i = 0; i < layerCount; ++i)
        {
            var layerType = reader.ReadByte();
            if (layerType > FloorFeatureLayer.MaximumType)
                throw reader.UnsupportedVersion($"floor feature layer type {layerType} is not supported, expected 0 through {FloorFeatureLayer.MaximumType}");
            var length = reader.ReadUInt32();
            var start = reader.Position;
            var layer = new FloorFeatureLayer
            {
                Type = layerType,
                Version = reader.ReadVersion(0, maximumLayerVersion)
            };
            var (levelSizeX, levelSizeZ) = FloorFeatureLayer.GetLevelSize(layerType, sizeX, sizeZ);
            while (true)
            {
                var levelIndex = reader.ReadSByte();
                if (levelIndex == endOfLevels)
                    break;
                layer.Levels.Add(new FloorFeatureLevel(levelSizeX, levelSizeZ, reader.ReadBytes(levelSizeX * levelSizeZ)) { Level = levelIndex });
            }
            if (reader.Position - start != length)
                throw reader.Invalid($"the floor feature layer is {reader.Position - start} bytes long, but its length is {length}");
            section.Layers.Add(layer);
        }
        return section;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FloorFeaturesSection"/> class
    /// </summary>
    public FloorFeaturesSection() :
        base(LotArchitectureSectionType.FloorFeatures)
    {
    }

    uint version = maximumVersion;

    /// <summary>
    /// Gets the layers
    /// </summary>
    /// <remarks>
    /// The lots examined hold layer type 0 alone, types 0 and 1, or types 0, 1 and 2, in that order.
    /// </remarks>
    public Collection<FloorFeatureLayer> Layers { get; } = [];

    /// <summary>
    /// Gets/sets the version of the section
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The version is not 0 or 1</exception>
    public uint Version
    {
        get => version;
        set
        {
            if (value > maximumVersion)
                throw new ArgumentOutOfRangeException(nameof(value), value, $"The version must be {minimumVersion} through {maximumVersion}");
            version = value;
        }
    }

    internal override void Write(SectionWriter writer, SectionContext context)
    {
        var sizeX = context.RequireSizeX(Type);
        var sizeZ = context.RequireSizeZ(Type);
        if (Layers.Count > byte.MaxValue)
            throw writer.Invalid($"there are {Layers.Count} layers, more than the {byte.MaxValue} the section can hold");
        writer.Write(version);
        writer.Write((byte)Layers.Count);
        foreach (var layer in Layers)
        {
            if (layer.Type > FloorFeatureLayer.MaximumType)
                throw writer.Invalid($"floor feature layer type {layer.Type} is not supported, expected 0 through {FloorFeatureLayer.MaximumType}");
            if (layer.Version > maximumLayerVersion)
                throw writer.Invalid($"floor feature layer version {layer.Version} is not supported, expected 0 through {maximumLayerVersion}");
            var (levelSizeX, levelSizeZ) = FloorFeatureLayer.GetLevelSize(layer.Type, sizeX, sizeZ);
            writer.Write(layer.Type);
            var lengthPosition = writer.Position;
            writer.Write(0);
            var start = writer.Position;
            writer.Write(layer.Version);
            foreach (var level in layer.Levels)
            {
                if (level.SizeX != levelSizeX || level.SizeZ != levelSizeZ)
                    throw writer.Invalid($"a level of floor feature layer type {layer.Type} is {level.SizeX} × {level.SizeZ}, but it must be {levelSizeX} × {levelSizeZ}");
                writer.Write(level.Level);
                writer.Write(level.Values);
            }
            writer.Write(endOfLevels);
            writer.PatchInt32(lengthPosition, writer.Position - start);
        }
    }
}
