namespace LlamaLogic.Architecture;

/// <summary>
/// Represents the <see cref="LotArchitectureSectionType.Ponds"/> section: the lot's ponds and the tile quarters each one covers (🔓)
/// </summary>
/// <remarks>
/// Versions 0 through 3 are supported, as the game's reader accepts, and the game writes version 3, the only one in the lots examined.
/// Versions 0 and 1 hold no ponds; <see cref="PondCell.Unknown1"/> and <see cref="Pond.Unknown2"/> exist from version 3.
/// </remarks>
public sealed class PondsSection :
    LotArchitectureSection
{
    const uint minimumVersion = 0;
    const uint maximumVersion = 3;

    internal static PondsSection Read(ref SectionReader reader, SectionContext context)
    {
        var section = new PondsSection { Version = reader.ReadVersion(minimumVersion, maximumVersion) };
        if (section.Version < 2)
            return section;
        var pondCount = reader.ReadUInt16();
        for (var i = 0; i < pondCount; ++i)
        {
            var pond = new Pond
            {
                Id = reader.ReadUInt16(),
                WaterLevel = reader.ReadInt32(),
                FloorPatternKey = reader.ReadKeyTypeGroupInstance()
            };
            var cellCount = reader.ReadCount(reader.ReadUInt16() * (section.Version > 2 ? 5L : 4L)) / (section.Version > 2 ? 5 : 4);
            for (var j = 0; j < cellCount; ++j)
            {
                var cell = new PondCell
                {
                    X = reader.ReadByte(),
                    Z = reader.ReadByte(),
                    QuarterMask = reader.ReadByte(),
                    Level = reader.ReadSByte()
                };
                if (section.Version > 2)
                    cell.Unknown1 = reader.ReadByte();
                pond.Cells.Add(cell);
            }
            if (section.Version > 2)
            {
                var valueCount = reader.ReadCount(reader.ReadUInt16() * 2L) / 2;
                for (var j = 0; j < valueCount; ++j)
                    pond.Unknown2.Add(reader.ReadUInt16());
            }
            section.Ponds.Add(pond);
        }
        return section;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PondsSection"/> class
    /// </summary>
    public PondsSection() :
        base(LotArchitectureSectionType.Ponds)
    {
    }

    uint version = maximumVersion;

    /// <summary>
    /// Gets the ponds
    /// </summary>
    /// <remarks>
    /// The section holds ponds from version 2; in versions 0 and 1 this collection must be empty.
    /// </remarks>
    public Collection<Pond> Ponds { get; } = [];

    /// <summary>
    /// Gets/sets the version of the section, which decides its layout
    /// </summary>
    /// <remarks>
    /// Encoding throws an <see cref="InvalidOperationException"/> when a field the version lacks holds a value other than its default, or a collection the version lacks is not empty.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The version is not 0 through 3</exception>
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
        writer.Write(version);
        if (version < 2)
        {
            if (Ponds.Count > 0)
                throw writer.Invalid($"version {version} cannot hold ponds");
            return;
        }
        if (Ponds.Count > ushort.MaxValue)
            throw writer.Invalid($"there are {Ponds.Count} ponds, more than the {ushort.MaxValue} the section can hold");
        writer.Write((ushort)Ponds.Count);
        foreach (var pond in Ponds)
        {
            if (pond.Cells.Count > ushort.MaxValue)
                throw writer.Invalid($"pond {pond.Id} has {pond.Cells.Count} cells, more than the {ushort.MaxValue} a pond can hold");
            if (version < 3)
            {
                writer.RequireEmpty(pond.Unknown2.Count, $"the {nameof(Pond.Unknown2)} of pond {pond.Id}", version);
                foreach (var cell in pond.Cells)
                    writer.RequireDefault(cell.Unknown1, PondCell.DefaultUnknown1, $"the {nameof(PondCell.Unknown1)} of a cell of pond {pond.Id}", version);
            }
            writer.Write(pond.Id);
            writer.Write(pond.WaterLevel);
            writer.WriteKeyTypeGroupInstance(pond.FloorPatternKey);
            writer.Write((ushort)pond.Cells.Count);
            foreach (var cell in pond.Cells)
            {
                writer.Write(cell.X);
                writer.Write(cell.Z);
                writer.Write(cell.QuarterMask);
                writer.Write(cell.Level);
                if (version > 2)
                    writer.Write(cell.Unknown1);
            }
            if (version > 2)
            {
                if (pond.Unknown2.Count > ushort.MaxValue)
                    throw writer.Invalid($"pond {pond.Id} has {pond.Unknown2.Count} values in its list, more than the {ushort.MaxValue} it can hold");
                writer.Write((ushort)pond.Unknown2.Count);
                foreach (var value in pond.Unknown2)
                    writer.Write(value);
            }
        }
    }
}
