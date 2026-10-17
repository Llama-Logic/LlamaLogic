namespace LlamaLogic.Architecture;

/// <summary>
/// Represents one roof of a <see cref="RoofsSection"/>
/// </summary>
/// <remarks>
/// <para>From version 10 of the section on, a roof holds an <see cref="InnerVersion"/>, the bounding box of its footprint, the four corners of its footprint, <see cref="Direction"/>, <see cref="Pitch"/>, <see cref="Curvature"/>, four more floats (inner version 3 and later), <see cref="BaseElevation"/>, four per-side bytes (one byte copied to all four before inner version 2) and <see cref="Flags"/>.
/// Before version 10, a roof holds only the bounding box (as two three-byte points), <see cref="Direction"/>, one per-side byte copied to all four, <see cref="Curvature"/>, <see cref="Pitch"/> and, in version 9, <see cref="BaseElevation"/>; the game derives the corners and computes the base elevation of older roofs while loading.</para>
/// <para>The game replaces each key whose catalog resource is missing or invalid with the default of its type while loading.</para>
/// </remarks>
public sealed class Roof
{
    const float defaultUnknown1 = 0.66F;
    const float defaultUnknown2 = 0.33F;
    const float defaultUnknown3 = -1F;
    const float defaultUnknown4 = 1F;
    const uint maximumInnerVersion = 3;

    internal static Roof Read(ref SectionReader reader, uint version)
    {
        var roof = new Roof { Id = reader.ReadUInt32() };
        if (version < 10)
        {
            roof.BoundsMinimum = ReadPoint(ref reader);
            roof.BoundsMaximum = ReadPoint(ref reader);
        }
        roof.RoofKey = reader.ReadKeyTypeGroupInstance();
        roof.FirstTrimKey = reader.ReadKeyTypeGroupInstance();
        roof.SecondTrimKey = reader.ReadKeyTypeGroupInstance();
        if (version > 5)
            roof.PatternKey = reader.ReadKeyTypeGroupInstance();
        if (version > 10)
            roof.WallKey = reader.ReadKeyTypeGroupInstance();
        if (version < 10)
        {
            roof.Direction = reader.ReadByte();
            roof.SetSides(reader.ReadByte());
            roof.Curvature = reader.ReadSingle();
            roof.Pitch = reader.ReadSingle();
            if (version > 8)
                roof.BaseElevation = reader.ReadSingle();
            return roof;
        }
        var innerVersion = reader.ReadUInt32();
        if (innerVersion > maximumInnerVersion)
            throw reader.UnsupportedVersion($"the inner version {innerVersion} of roof {roof.Id} is not supported, expected 0 through {maximumInnerVersion}");
        roof.InnerVersion = innerVersion;
        roof.BoundsMinimum = RoofPoint.FromPacked(reader.ReadUInt32());
        roof.BoundsMaximum = RoofPoint.FromPacked(reader.ReadUInt32());
        roof.Corner1 = RoofPoint.FromPacked(reader.ReadUInt32());
        roof.Corner2 = RoofPoint.FromPacked(reader.ReadUInt32());
        roof.Corner3 = RoofPoint.FromPacked(reader.ReadUInt32());
        roof.Corner4 = RoofPoint.FromPacked(reader.ReadUInt32());
        roof.Direction = reader.ReadByte();
        roof.Pitch = reader.ReadSingle();
        roof.Curvature = reader.ReadSingle();
        if (roof.InnerVersion > 2)
        {
            roof.Unknown1 = reader.ReadSingle();
            roof.Unknown2 = reader.ReadSingle();
            roof.Unknown3 = reader.ReadSingle();
            roof.Unknown4 = reader.ReadSingle();
        }
        roof.BaseElevation = reader.ReadSingle();
        if (roof.InnerVersion < 2)
            roof.SetSides(reader.ReadByte());
        else
        {
            roof.Unknown5 = reader.ReadByte();
            roof.Unknown6 = reader.ReadByte();
            roof.Unknown7 = reader.ReadByte();
            roof.Unknown8 = reader.ReadByte();
        }
        roof.Flags = reader.ReadUInt32();
        return roof;
    }

    static RoofPoint ReadPoint(ref SectionReader reader) =>
        new()
        {
            X = reader.ReadByte(),
            Z = reader.ReadByte(),
            Level = reader.ReadSByte()
        };

    static void WritePoint(SectionWriter writer, RoofPoint point)
    {
        writer.Write(point.X);
        writer.Write(point.Z);
        writer.Write(point.Level);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Roof"/> class
    /// </summary>
    public Roof()
    {
    }

    uint innerVersion = maximumInnerVersion;

    /// <summary>
    /// Gets/sets the elevation of the roof's base
    /// </summary>
    /// <remarks>
    /// Present in version 9 and from version 10 on.
    /// When it is outside the valid range, the game recomputes it while loading as the elevation of the roof's level plus the lot's <see cref="LotInfoSection.LevelElevationOffset"/> (levels 0 and up) or <see cref="LotInfoSection.NegativeLevelElevationOffset"/> (levels below 0).
    /// Raising the walls below a roof from 3 m to 5 m changed it from 3 to 5 (in-game test).
    /// </remarks>
    public float BaseElevation { get; set; }

    /// <summary>
    /// Gets/sets the maximum corner of the bounding box of the roof's footprint
    /// </summary>
    /// <remarks>
    /// It is at or above <see cref="BoundsMinimum"/> on both axes in every lot examined.
    /// </remarks>
    public RoofPoint BoundsMaximum { get; set; }

    /// <summary>
    /// Gets/sets the minimum corner of the bounding box of the roof's footprint
    /// </summary>
    public RoofPoint BoundsMinimum { get; set; }

    /// <summary>
    /// Gets/sets the first corner of the roof's footprint
    /// </summary>
    /// <remarks>
    /// Present from version 10 on.
    /// The game derives the four corners from the bounding box; with bit 0 of <see cref="Flags"/> set, they are inset by half the extent, giving a footprint turned 45°.
    /// </remarks>
    public RoofPoint Corner1 { get; set; }

    /// <summary>
    /// Gets/sets the second corner of the roof's footprint
    /// </summary>
    /// <remarks>
    /// Present from version 10 on.
    /// </remarks>
    public RoofPoint Corner2 { get; set; }

    /// <summary>
    /// Gets/sets the third corner of the roof's footprint
    /// </summary>
    /// <remarks>
    /// Present from version 10 on.
    /// </remarks>
    public RoofPoint Corner3 { get; set; }

    /// <summary>
    /// Gets/sets the fourth corner of the roof's footprint
    /// </summary>
    /// <remarks>
    /// Present from version 10 on.
    /// </remarks>
    public RoofPoint Corner4 { get; set; }

    /// <summary>
    /// Gets/sets the curvature of the roof
    /// </summary>
    /// <remarks>
    /// In-game tests show that curving a roof changes only this value (from 0 to 0.5604 in one test).
    /// It is signed, −1 through 1, and defaults to 0.
    /// </remarks>
    public float Curvature { get; set; }

    /// <summary>
    /// Gets/sets the direction of the roof, a value of the game's eight-way direction enumeration
    /// </summary>
    /// <remarks>
    /// The game rotates it in steps of the eight-way enumeration (modulo 8); it is 0, 2, 4 or 6 in every lot examined.
    /// Turning a gable roof 90° in game changed it from 0 to 6, two steps of the enumeration (in-game test).
    /// That it is the direction of the roof's ridge is a hypothesis.
    /// </remarks>
    public byte Direction { get; set; }

    /// <summary>
    /// Gets/sets the key of the first trim, a <c>CTProductRoofTrim</c> (0xB0311D0F) or the null key
    /// </summary>
    /// <remarks>
    /// Changing the roof trim in game left it the null key and changed only <see cref="SecondTrimKey"/> (in-game test).
    /// </remarks>
    public ResourceKey FirstTrimKey { get; set; }

    /// <summary>
    /// Gets/sets the flags of the roof
    /// </summary>
    /// <remarks>
    /// Present from version 10 on.
    /// Bit 0 insets the footprint's corners into a diamond turned 45°; the other bits are unknown.
    /// Placing the catalog's diagonal gable roof set bit 0 and inset the corners, as the game's code describes (in-game test).
    /// It is 0, 1 or 2 in every lot examined.
    /// </remarks>
    public uint Flags { get; set; }

    /// <summary>
    /// Gets/sets the id of the roof
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    /// Gets/sets the version of the roof's own layout
    /// </summary>
    /// <remarks>
    /// Present from version 10 of the section on; for older sections it keeps its default, 3, and encoding throws if it is changed.
    /// The game writes 3 and its reader reads any value, laying out the roof as for 3 when the value is 3 or more; this library supports 0 through 3.
    /// In the lots examined the roofs of version 11 sections hold 2 and those of version 13 sections 3; no version 10 or 12 section examined holds roofs.
    /// Inner version 3 holds <see cref="Unknown1"/> through <see cref="Unknown4"/>, which encoding a lower inner version requires to keep their defaults; before inner version 2, a single per-side byte is stored and copied to <see cref="Unknown5"/> through <see cref="Unknown8"/>.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The inner version is not 0 through 3</exception>
    public uint InnerVersion
    {
        get => innerVersion;
        set
        {
            if (value > maximumInnerVersion)
                throw new ArgumentOutOfRangeException(nameof(value), value, $"The inner version must be 0 through {maximumInnerVersion}");
            innerVersion = value;
        }
    }

    /// <summary>
    /// Gets/sets the key of the pattern, a <c>CTProductRoofPattern</c> (0xF1EDBD86)
    /// </summary>
    /// <remarks>
    /// Present from version 6 on.
    /// Changing the shingles in game changed only this key (in-game test).
    /// </remarks>
    public ResourceKey PatternKey { get; set; }

    /// <summary>
    /// Gets/sets the pitch of the roof
    /// </summary>
    /// <remarks>
    /// The game compares it with the <c>MaxRoofPitch</c> build-buy tuning (reporting <c>MaxRoofSlopeExceeded</c>); one pitch step in the game adds 0.05.
    /// It defaults to 0.5 and is 0.05 through 1.75 in the lots examined.
    /// </remarks>
    public float Pitch { get; set; } = 0.5F;

    /// <summary>
    /// Gets/sets the key of the roof, a <c>CTProductRoof</c> (0x91EDBD3E), which decides its shape
    /// </summary>
    /// <remarks>
    /// In-game tests show that the shape is this product alone: replacing one shape with another changed only this key and the per-side bytes <see cref="Unknown5"/> through <see cref="Unknown8"/>.
    /// The instances seen are 0x144D (gable), 0x1B03 (half gable), 0x144F (hip), 0x29DC (half hip), 0x282AD (octagonal), 0x282B8 (cone) and 0x7DD4 (the catalog's diagonal gable).
    /// </remarks>
    public ResourceKey RoofKey { get; set; }

    /// <summary>
    /// Gets/sets the key of the second trim, a <c>CTProductRoofTrim</c> (0xB0311D0F)
    /// </summary>
    /// <remarks>
    /// Changing the roof trim in game changed only this key (in-game test).
    /// </remarks>
    public ResourceKey SecondTrimKey { get; set; }

    /// <summary>
    /// Gets/sets the first unknown float
    /// </summary>
    /// <remarks>
    /// Present from inner version 3 on; the game's default is 0.66.
    /// The four unknown floats look like the parameters of a curve profile (hypothesis).
    /// All four kept their defaults (0.66, 0.33, −1 and 1) through every in-game test.
    /// </remarks>
    public float Unknown1 { get; set; } = defaultUnknown1;

    /// <summary>
    /// Gets/sets the second unknown float
    /// </summary>
    /// <remarks>
    /// Present from inner version 3 on; the game's default is 0.33.
    /// </remarks>
    public float Unknown2 { get; set; } = defaultUnknown2;

    /// <summary>
    /// Gets/sets the third unknown float
    /// </summary>
    /// <remarks>
    /// Present from inner version 3 on; the game's default is −1.
    /// </remarks>
    public float Unknown3 { get; set; } = defaultUnknown3;

    /// <summary>
    /// Gets/sets the fourth unknown float
    /// </summary>
    /// <remarks>
    /// Present from inner version 3 on; the game's default is 1.
    /// </remarks>
    public float Unknown4 { get; set; } = defaultUnknown4;

    /// <summary>
    /// Gets/sets the first per-side byte
    /// </summary>
    /// <remarks>
    /// The four per-side bytes are 0 through 12 in the lots examined, and 0 through 3 in roofs of sections before version 13.
    /// In in-game tests they were 1, 1, 1, 1 for the gable, half gable and diagonal gable roofs and 0, 0, 0, 0 for the hip, half hip, octagonal and cone roofs, and replacing one shape with another changed only these bytes and <see cref="RoofKey"/>; that they choose gable or eave per side is a hypothesis, and their meaning is not proven.
    /// Before version 10 and before inner version 2, a single byte is stored, so the four must be equal to be written.
    /// </remarks>
    public byte Unknown5 { get; set; }

    /// <summary>
    /// Gets/sets the second per-side byte
    /// </summary>
    public byte Unknown6 { get; set; }

    /// <summary>
    /// Gets/sets the third per-side byte
    /// </summary>
    public byte Unknown7 { get; set; }

    /// <summary>
    /// Gets/sets the fourth per-side byte
    /// </summary>
    public byte Unknown8 { get; set; }

    /// <summary>
    /// Gets/sets the wall key, a <c>CTProductWallPattern</c> (0xD5F0F921) or the null key
    /// </summary>
    /// <remarks>
    /// Present from version 11 on.
    /// </remarks>
    public ResourceKey WallKey { get; set; }

    void RequireDefaults(SectionWriter writer, uint version)
    {
        if (version < 6)
            writer.RequireDefault(PatternKey, default, $"the {nameof(PatternKey)} of roof {Id}", version);
        if (version < 11)
            writer.RequireDefault(WallKey, default, $"the {nameof(WallKey)} of roof {Id}", version);
        if (version < 9)
            writer.RequireDefault(BaseElevation, 0F, $"the {nameof(BaseElevation)} of roof {Id}", version);
        if (version < 10)
        {
            writer.RequireDefault(BoundsMinimum.Reserved, (byte)0, $"the high byte of the {nameof(BoundsMinimum)} of roof {Id}", version);
            writer.RequireDefault(BoundsMaximum.Reserved, (byte)0, $"the high byte of the {nameof(BoundsMaximum)} of roof {Id}", version);
            writer.RequireDefault(InnerVersion, maximumInnerVersion, $"the {nameof(InnerVersion)} of roof {Id}", version);
            writer.RequireDefault(Corner1, default, $"the {nameof(Corner1)} of roof {Id}", version);
            writer.RequireDefault(Corner2, default, $"the {nameof(Corner2)} of roof {Id}", version);
            writer.RequireDefault(Corner3, default, $"the {nameof(Corner3)} of roof {Id}", version);
            writer.RequireDefault(Corner4, default, $"the {nameof(Corner4)} of roof {Id}", version);
            writer.RequireDefault(Flags, 0U, $"the {nameof(Flags)} of roof {Id}", version);
        }
        if (version < 10 || InnerVersion < 3)
        {
            var layout = version < 10 ? $"version {version}" : $"inner version {InnerVersion}";
            if (BitConverter.SingleToInt32Bits(Unknown1) != BitConverter.SingleToInt32Bits(defaultUnknown1)
                || BitConverter.SingleToInt32Bits(Unknown2) != BitConverter.SingleToInt32Bits(defaultUnknown2)
                || BitConverter.SingleToInt32Bits(Unknown3) != BitConverter.SingleToInt32Bits(defaultUnknown3)
                || BitConverter.SingleToInt32Bits(Unknown4) != BitConverter.SingleToInt32Bits(defaultUnknown4))
                throw writer.Invalid($"roof {Id} has values other than the defaults in {nameof(Unknown1)} through {nameof(Unknown4)}, which {layout} cannot store");
        }
    }

    void SetSides(byte value)
    {
        Unknown5 = value;
        Unknown6 = value;
        Unknown7 = value;
        Unknown8 = value;
    }

    void WriteSides(SectionWriter writer)
    {
        if (Unknown6 != Unknown5 || Unknown7 != Unknown5 || Unknown8 != Unknown5)
            throw writer.Invalid($"roof {Id} stores a single per-side byte in this layout, but its four per-side bytes differ");
        writer.Write(Unknown5);
    }

    internal void Write(SectionWriter writer, uint version)
    {
        RequireDefaults(writer, version);
        writer.Write(Id);
        if (version < 10)
        {
            WritePoint(writer, BoundsMinimum);
            WritePoint(writer, BoundsMaximum);
        }
        writer.WriteKeyTypeGroupInstance(RoofKey);
        writer.WriteKeyTypeGroupInstance(FirstTrimKey);
        writer.WriteKeyTypeGroupInstance(SecondTrimKey);
        if (version > 5)
            writer.WriteKeyTypeGroupInstance(PatternKey);
        if (version > 10)
            writer.WriteKeyTypeGroupInstance(WallKey);
        if (version < 10)
        {
            writer.Write(Direction);
            WriteSides(writer);
            writer.Write(Curvature);
            writer.Write(Pitch);
            if (version > 8)
                writer.Write(BaseElevation);
            return;
        }
        writer.Write(InnerVersion);
        writer.Write(BoundsMinimum.ToPacked());
        writer.Write(BoundsMaximum.ToPacked());
        writer.Write(Corner1.ToPacked());
        writer.Write(Corner2.ToPacked());
        writer.Write(Corner3.ToPacked());
        writer.Write(Corner4.ToPacked());
        writer.Write(Direction);
        writer.Write(Pitch);
        writer.Write(Curvature);
        if (InnerVersion > 2)
        {
            writer.Write(Unknown1);
            writer.Write(Unknown2);
            writer.Write(Unknown3);
            writer.Write(Unknown4);
        }
        writer.Write(BaseElevation);
        if (InnerVersion < 2)
            WriteSides(writer);
        else
        {
            writer.Write(Unknown5);
            writer.Write(Unknown6);
            writer.Write(Unknown7);
            writer.Write(Unknown8);
        }
        writer.Write(Flags);
    }
}
