namespace LlamaLogic.Architecture;

/// <summary>
/// Represents the <see cref="LotArchitectureSectionType.TerrainPaint"/> section: the terrain paint layers, each a paint and a mask of where it is applied (🔓)
/// </summary>
/// <remarks>
/// <para>The game's reader and writer keep two sets of layers, <see cref="LayerSet0"/> and <see cref="LayerSet1"/>; what distinguishes them has not been established.
/// The headers of the layers (key and use count) come first, set by set, then <see cref="MaskSizeX"/> and <see cref="MaskSizeZ"/>, then the masks, in the same order.</para>
/// <para>Versions 2 through 4 are supported, and the game writes version 4; the lots examined hold versions 2 and 4.
/// Version 2 has only <see cref="LayerSet1"/>, followed by <see cref="Unknown1"/>, and its layers have no <see cref="TerrainPaintLayer.HasMask"/>.
/// Version 3 stores <see cref="LayerSet1"/> before <see cref="LayerSet0"/>; version 4 stores <see cref="LayerSet0"/> first.</para>
/// <para>The game writes no keys of this section into the <see cref="ResourceManifestSection"/>, which it saves earlier.</para>
/// </remarks>
public sealed class TerrainPaintSection :
    LotArchitectureSection
{
    const uint minimumVersion = 2;
    const uint maximumVersion = 4;

    internal static TerrainPaintSection Read(ref SectionReader reader, SectionContext context)
    {
        var section = new TerrainPaintSection { Version = reader.ReadVersion(minimumVersion, maximumVersion) };
        var sets = section.GetStoredSets();
        foreach (var set in sets)
        {
            var count = reader.ReadCount(reader.ReadUInt32() * 20L) / 20;
            for (var i = 0; i < count; ++i)
                set.Add(new TerrainPaintLayer
                {
                    Key = reader.ReadKeyTypeGroupInstance(),
                    UseCount = reader.ReadUInt32()
                });
        }
        if (section.Version < 3)
            section.Unknown1 = reader.ReadUInt32();
        section.MaskSizeX = reader.ReadInt32();
        section.MaskSizeZ = reader.ReadInt32();
        if (section.MaskSizeX < 0 || section.MaskSizeZ < 0)
            throw reader.Invalid($"the mask size {section.MaskSizeX} × {section.MaskSizeZ} is negative");
        if (context.HasDimensions && (section.MaskSizeX != context.RequireSizeX(reader.Type) * 4 + 1 || section.MaskSizeZ != context.RequireSizeZ(reader.Type) * 4 + 1))
            throw reader.Invalid($"the masks are {section.MaskSizeX} × {section.MaskSizeZ}, but the lot needs {context.RequireSizeX(reader.Type) * 4 + 1} × {context.RequireSizeZ(reader.Type) * 4 + 1}");
        var maskLength = (long)section.MaskSizeX * section.MaskSizeZ;
        foreach (var set in sets)
            foreach (var layer in set)
            {
                if (section.Version > 2)
                    layer.HasMask = reader.ReadBoolean();
                layer.SetMask(section.MaskSizeX, section.MaskSizeZ, reader.ReadBytes(reader.ReadCount(maskLength)));
            }
        return section;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TerrainPaintSection"/> class
    /// </summary>
    public TerrainPaintSection() :
        base(LotArchitectureSectionType.TerrainPaint)
    {
    }

    uint version = maximumVersion;

    /// <summary>
    /// Gets the first set of layers, which version 2 does not have
    /// </summary>
    /// <remarks>
    /// When its caller passes a flag of unknown meaning, the game's reader replaces the key of this set's first layer with default key 0x1E of the lot's style.
    /// It holds 0 or 1 layers in the lots examined.
    /// Sculpting the terrain with the raise tool in game added a layer to this set (in-game test).
    /// </remarks>
    public Collection<TerrainPaintLayer> LayerSet0 { get; } = [];

    /// <summary>
    /// Gets the second set of layers, the only one in version 2
    /// </summary>
    public Collection<TerrainPaintLayer> LayerSet1 { get; } = [];

    /// <summary>
    /// Gets/sets the number of mask values along the lot's local X axis
    /// </summary>
    /// <remarks>
    /// 4 × <see cref="LotSizeSection.SizeX"/> + 1 in every lot examined, which this library requires, on decoding and on encoding, when it knows the lot's dimensions; the game itself reads only what fits its own mask.
    /// </remarks>
    public int MaskSizeX { get; set; }

    /// <summary>
    /// Gets/sets the number of mask values along the lot's local Z axis
    /// </summary>
    /// <remarks>
    /// 4 × <see cref="LotSizeSection.SizeZ"/> + 1 in every lot examined, which this library requires, on decoding and on encoding, when it knows the lot's dimensions.
    /// </remarks>
    public int MaskSizeZ { get; set; }

    /// <summary>
    /// Gets/sets a value of unknown meaning, written in version 2 only
    /// </summary>
    /// <remarks>
    /// The game reads and discards it; it equals the number of layers in every lot examined.
    /// </remarks>
    public uint Unknown1 { get; set; }

    /// <summary>
    /// Gets/sets the version of the section, which decides its layout
    /// </summary>
    /// <remarks>
    /// Encoding throws an <see cref="InvalidOperationException"/> when a field the version lacks holds a value other than its default, or a collection the version lacks is not empty.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The version is not 2 through 4</exception>
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

    Collection<TerrainPaintLayer>[] GetStoredSets() =>
        version switch
        {
            < 3 => [LayerSet1],
            3 => [LayerSet1, LayerSet0],
            _ => [LayerSet0, LayerSet1]
        };

    internal override void Write(SectionWriter writer, SectionContext context)
    {
        if (version < 3)
        {
            writer.RequireEmpty(LayerSet0.Count, nameof(LayerSet0), version);
            foreach (var layer in LayerSet1)
                writer.RequireDefault(layer.HasMask, true, $"the {nameof(TerrainPaintLayer.HasMask)} of a layer", version);
        }
        else
            writer.RequireDefault(Unknown1, 0U, nameof(Unknown1), version);
        if (context.HasDimensions && (MaskSizeX != context.RequireSizeX(Type) * 4 + 1 || MaskSizeZ != context.RequireSizeZ(Type) * 4 + 1))
            throw writer.Invalid($"the masks are {MaskSizeX} × {MaskSizeZ}, but the lot needs {context.RequireSizeX(Type) * 4 + 1} × {context.RequireSizeZ(Type) * 4 + 1}");
        var sets = GetStoredSets();
        writer.Write(version);
        foreach (var set in sets)
        {
            writer.WriteCount(set.Count);
            foreach (var layer in set)
            {
                if (layer.SizeX != MaskSizeX || layer.SizeZ != MaskSizeZ)
                    throw writer.Invalid($"a layer's mask is {layer.SizeX} × {layer.SizeZ}, but the section's masks are {MaskSizeX} × {MaskSizeZ}");
                writer.WriteKeyTypeGroupInstance(layer.Key);
                writer.Write(layer.UseCount);
            }
        }
        if (version < 3)
            writer.Write(Unknown1);
        writer.Write(MaskSizeX);
        writer.Write(MaskSizeZ);
        foreach (var set in sets)
            foreach (var layer in set)
            {
                if (version > 2)
                    writer.Write(layer.HasMask);
                writer.Write(layer.Mask);
            }
    }
}
