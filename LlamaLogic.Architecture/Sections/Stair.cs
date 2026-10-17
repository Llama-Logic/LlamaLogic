namespace LlamaLogic.Architecture;

/// <summary>
/// Represents one stair or ladder of a <see cref="StairsSection"/>
/// </summary>
/// <remarks>
/// <para>Each stair is also a lot object: <see cref="ObjectId"/> is the id of its object in <see cref="LotObjectsSection"/>.</para>
/// <para>Before version 7, a stair holds a second position and its bottom level (<see cref="LegacyX"/>, <see cref="LegacyZ"/>, <see cref="BottomLevel"/>) ahead of <see cref="AnchorX"/>, <see cref="AnchorZ"/> and <see cref="TopLevel"/>, and no stages: the game derives one stage from the distance between the two positions and computes <see cref="TopElevation"/> while loading.
/// Versions 7 through 9 hold exactly one stage and versions 10 and later a 16-bit stage count.
/// Before version 8, <see cref="Flags"/> is stored as separate Booleans (four, or six from version 6 on).</para>
/// </remarks>
public sealed class Stair
{
    internal static Stair Read(ref SectionReader reader, uint version)
    {
        var stair = new Stair();
        if (version < 7)
        {
            stair.LegacyX = reader.ReadSingle();
            stair.LegacyZ = reader.ReadSingle();
            var bottomLevel = reader.ReadInt32();
            if (bottomLevel is < sbyte.MinValue or > sbyte.MaxValue)
                throw reader.Invalid($"the bottom level {bottomLevel} is not a level");
            stair.BottomLevel = (sbyte)bottomLevel;
        }
        stair.AnchorX = reader.ReadSingle();
        stair.AnchorZ = reader.ReadSingle();
        stair.TopLevel = reader.ReadInt32();
        if (version > 6)
        {
            stair.BottomLevel = reader.ReadSByte();
            stair.TopElevation = reader.ReadSingle();
        }
        stair.LaneCount = reader.ReadByte();
        if (version > 6)
        {
            var count = version > 9 ? reader.ReadCount(reader.ReadUInt16() * 3L) / 3 : 1;
            for (var i = 0; i < count; ++i)
                stair.Stages.Add(new StairStage { StoredStepCount = reader.ReadUInt16(), Unknown1 = reader.ReadByte() });
        }
        stair.Id = reader.ReadUInt16();
        stair.ObjectId = reader.ReadUInt64();
        stair.StairsKey = reader.ReadKeyTypeGroupInstance();
        stair.RailingKey = reader.ReadKeyTypeGroupInstance();
        stair.WallKey = reader.ReadKeyTypeGroupInstance();
        if (version > 11)
            stair.SecondRailingKey = reader.ReadKeyTypeGroupInstance();
        if (version < 8)
        {
            var flags = 0;
            flags |= reader.ReadBoolean() ? 0x1 : 0;
            flags |= reader.ReadBoolean() ? 0x2 : 0;
            flags |= reader.ReadBoolean() ? 0x4 : 0;
            stair.Unknown1 = reader.ReadBoolean();
            flags |= reader.ReadBoolean() ? 0x8 : 0;
            if (version is 5 or 6)
                stair.Unknown2 = reader.ReadSingle();
            if (version > 5)
            {
                flags |= reader.ReadBoolean() ? 0x10 : 0;
                flags |= reader.ReadBoolean() ? 0x20 : 0;
            }
            stair.Flags = (ushort)flags;
            return stair;
        }
        stair.Flags = reader.ReadUInt16();
        stair.Unknown1 = reader.ReadBoolean();
        if (version > 12)
        {
            if (version < 15)
                stair.Unknown3 = reader.ReadByte();
            stair.Unknown4 = reader.ReadSingle();
            stair.Unknown5 = reader.ReadSingle();
            stair.Unknown6 = reader.ReadSingle();
            stair.Unknown7 = reader.ReadSingle();
            if (version > 14)
                stair.Unknown8 = reader.ReadByte();
            foreach (var stage in stair.Stages)
            {
                stage.Unknown2 = reader.ReadUInt16();
                stage.Unknown3 = reader.ReadUInt16();
                if (version > 14)
                    stage.Unknown4 = reader.ReadByte();
            }
        }
        if (version > 13)
        {
            stair.FirstStep = reader.ReadUInt16();
            stair.LastStep = reader.ReadUInt16();
        }
        return stair;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Stair"/> class
    /// </summary>
    public Stair()
    {
    }

    /// <summary>
    /// Gets/sets the position of the stair's centerline anchor along the lot's local X axis, in tiles
    /// </summary>
    /// <remarks>
    /// Its fraction is 0 or 0.5 in the lots examined. Which end of the stair it marks has not been established.
    /// </remarks>
    public float AnchorX { get; set; }

    /// <summary>
    /// Gets/sets the position of the stair's centerline anchor along the lot's local Z axis, in tiles
    /// </summary>
    /// <remarks>
    /// Widening a stair in the game by one lane moved it by 0.5.
    /// </remarks>
    public float AnchorZ { get; set; }

    /// <summary>
    /// Gets/sets the signed level at the bottom of the stair
    /// </summary>
    /// <remarks>
    /// Before version 7 it is stored as a 32-bit value next to <see cref="LegacyX"/> and <see cref="LegacyZ"/>.
    /// </remarks>
    public sbyte BottomLevel { get; set; }

    /// <summary>
    /// Gets/sets the first step of the stair's step range
    /// </summary>
    /// <remarks>
    /// Present from version 14 on. The game clamps the stages to the range <see cref="FirstStep"/> through <see cref="LastStep"/>.
    /// It is 0 through 5 in the lots examined.
    /// </remarks>
    public ushort FirstStep { get; set; }

    /// <summary>
    /// Gets/sets the flags of the stair
    /// </summary>
    /// <remarks>
    /// Before version 8, bits 0 through 3 (and bits 4 and 5 from version 6 on) are stored as Booleans, and the other bits cannot be stored.
    /// The game recomputes bits 4 and 5 while loading when <see cref="StairsSection.Unknown1"/> is set, and sets bits 11 and 12 when bit 0 is set and <see cref="LastStep"/> is 65,535; the meaning of the bits is otherwise unknown.
    /// Changing the railing in game set bits 0x0001 and 0x1800 together with <see cref="RailingKey"/> (in-game test).
    /// </remarks>
    public ushort Flags { get; set; }

    /// <summary>
    /// Gets/sets the id of the stair, unique among the stairs and ladders of the lot
    /// </summary>
    public ushort Id { get; set; }

    /// <summary>
    /// Gets/sets the number of lanes of the stair: its width in tiles
    /// </summary>
    public byte LaneCount { get; set; }

    /// <summary>
    /// Gets/sets the last step of the stair's step range
    /// </summary>
    /// <remarks>
    /// Present from version 14 on. When it is 65,535, the game computes it while loading as the total step count minus 1 (or the total with bit 3 of <see cref="Flags"/> set).
    /// It was the total step count minus 1 in every in-game test (11 for 12 steps, 19 for 20).
    /// </remarks>
    public ushort LastStep { get; set; } = ushort.MaxValue;

    /// <summary>
    /// Gets/sets a second position of the stair along the lot's local X axis, in tiles
    /// </summary>
    /// <remarks>
    /// Present before version 7. The game derives the step count of the stair's single stage from its distance to <see cref="AnchorX"/> and <see cref="AnchorZ"/>; that it marks the bottom end is a hypothesis.
    /// </remarks>
    public float LegacyX { get; set; }

    /// <summary>
    /// Gets/sets a second position of the stair along the lot's local Z axis, in tiles
    /// </summary>
    /// <remarks>
    /// Present before version 7.
    /// </remarks>
    public float LegacyZ { get; set; }

    /// <summary>
    /// Gets/sets the id of the stair's lot object
    /// </summary>
    public ulong ObjectId { get; set; }

    /// <summary>
    /// Gets/sets the key of the railing, a <c>CTProductRailing</c> (0x1C1CF1F7) or the null key
    /// </summary>
    /// <remarks>
    /// It is the null key for every ladder examined.
    /// Changing the railing in game set this key and bits 0x0001 and 0x1800 of <see cref="Flags"/> (in-game test).
    /// </remarks>
    public ResourceKey RailingKey { get; set; }

    /// <summary>
    /// Gets/sets the key of the second railing, a <c>CTProductRailing</c> (0x1C1CF1F7) or the null key
    /// </summary>
    /// <remarks>
    /// Present from version 12 on.
    /// </remarks>
    public ResourceKey SecondRailingKey { get; set; }

    /// <summary>
    /// Gets the stages, ordered from the top
    /// </summary>
    /// <remarks>
    /// Versions 7 through 9 hold exactly one stage; versions before 7 hold none.
    /// </remarks>
    public Collection<StairStage> Stages { get; } = [];

    /// <summary>
    /// Gets/sets the key of the stair, a <c>CTProductStair</c> (0x9A20CD1C) for a stair or a <c>CTProductLadder</c> (0xCC09004D) for a ladder
    /// </summary>
    public ResourceKey StairsKey { get; set; }

    /// <summary>
    /// Gets/sets the elevation at the top of the stair
    /// </summary>
    /// <remarks>
    /// Present from version 7 on. The game places each stage's start at this elevation minus the steps above it times the step height; this value minus 0.25 per step is the bottom elevation in most lots examined (hypothesis: the step height is 0.25).
    /// Raising the walls of the stair's level from 3 m to 5 m changed it from 3 to 5 and the total step count from 12 to 20 (in-game test).
    /// </remarks>
    public float TopElevation { get; set; }

    /// <summary>
    /// Gets/sets the signed level at the top of the stair
    /// </summary>
    public int TopLevel { get; set; }

    /// <summary>
    /// Gets/sets the first unknown value
    /// </summary>
    /// <remarks>
    /// It is <see langword="true"/> in every stair and ladder examined except four stairs of version 5 and four of version 15.
    /// </remarks>
    public bool Unknown1 { get; set; } = true;

    /// <summary>
    /// Gets/sets the second unknown value
    /// </summary>
    /// <remarks>
    /// Present in versions 5 and 6. The game converts it into the stair's bottom elevation while loading (hypothesis: a legacy bottom elevation).
    /// </remarks>
    public float Unknown2 { get; set; }

    /// <summary>
    /// Gets/sets the third unknown value
    /// </summary>
    /// <remarks>
    /// Present in versions 13 and 14. The game's reader discards it.
    /// </remarks>
    public byte Unknown3 { get; set; }

    /// <summary>
    /// Gets/sets the fourth unknown value
    /// </summary>
    /// <remarks>
    /// Present from version 13 on. It is 0, 0.5 or 1 in the lots examined.
    /// </remarks>
    public float Unknown4 { get; set; }

    /// <summary>
    /// Gets/sets the fifth unknown value
    /// </summary>
    /// <remarks>
    /// Present from version 13 on. It is 0, 0.5 or 1 in the lots examined.
    /// </remarks>
    public float Unknown5 { get; set; }

    /// <summary>
    /// Gets/sets the sixth unknown value
    /// </summary>
    /// <remarks>
    /// Present from version 13 on. It is 0 in every lot examined.
    /// </remarks>
    public float Unknown6 { get; set; }

    /// <summary>
    /// Gets/sets the seventh unknown value
    /// </summary>
    /// <remarks>
    /// Present from version 13 on. It is 0 in every lot examined.
    /// </remarks>
    public float Unknown7 { get; set; }

    /// <summary>
    /// Gets/sets the eighth unknown value
    /// </summary>
    /// <remarks>
    /// Present from version 15 on. It is 0 or 4 in the lots examined.
    /// </remarks>
    public byte Unknown8 { get; set; }

    /// <summary>
    /// Gets/sets the wall key, a <c>CTProductWallPattern</c> (0xD5F0F921) or the null key
    /// </summary>
    /// <remarks>
    /// It is the null key for every ladder examined.
    /// </remarks>
    public ResourceKey WallKey { get; set; }

    void RequireDefaults(SectionWriter writer, uint version)
    {
        if (version > 6)
        {
            writer.RequireDefault(LegacyX, 0F, $"the {nameof(LegacyX)} of stair {Id}", version);
            writer.RequireDefault(LegacyZ, 0F, $"the {nameof(LegacyZ)} of stair {Id}", version);
        }
        else
        {
            writer.RequireDefault(TopElevation, 0F, $"the {nameof(TopElevation)} of stair {Id}", version);
            writer.RequireEmpty(Stages.Count, $"the {nameof(Stages)} of stair {Id}", version);
        }
        if (version is not (5 or 6))
            writer.RequireDefault(Unknown2, 0F, $"the {nameof(Unknown2)} of stair {Id}", version);
        if (version < 12)
            writer.RequireDefault(SecondRailingKey, default, $"the {nameof(SecondRailingKey)} of stair {Id}", version);
        if (version is not (13 or 14))
            writer.RequireDefault(Unknown3, (byte)0, $"the {nameof(Unknown3)} of stair {Id}", version);
        if (version < 13)
        {
            writer.RequireDefault(Unknown4, 0F, $"the {nameof(Unknown4)} of stair {Id}", version);
            writer.RequireDefault(Unknown5, 0F, $"the {nameof(Unknown5)} of stair {Id}", version);
            writer.RequireDefault(Unknown6, 0F, $"the {nameof(Unknown6)} of stair {Id}", version);
            writer.RequireDefault(Unknown7, 0F, $"the {nameof(Unknown7)} of stair {Id}", version);
            foreach (var stage in Stages)
            {
                writer.RequireDefault(stage.Unknown2, (ushort)0, $"the {nameof(StairStage.Unknown2)} of a stage of stair {Id}", version);
                writer.RequireDefault(stage.Unknown3, (ushort)0, $"the {nameof(StairStage.Unknown3)} of a stage of stair {Id}", version);
            }
        }
        if (version < 14)
        {
            writer.RequireDefault(FirstStep, (ushort)0, $"the {nameof(FirstStep)} of stair {Id}", version);
            writer.RequireDefault(LastStep, ushort.MaxValue, $"the {nameof(LastStep)} of stair {Id}", version);
        }
        if (version < 15)
        {
            writer.RequireDefault(Unknown8, (byte)0, $"the {nameof(Unknown8)} of stair {Id}", version);
            foreach (var stage in Stages)
                writer.RequireDefault(stage.Unknown4, (byte)0, $"the {nameof(StairStage.Unknown4)} of a stage of stair {Id}", version);
        }
    }

    internal void Write(SectionWriter writer, uint version)
    {
        RequireDefaults(writer, version);
        if (version < 7)
        {
            writer.Write(LegacyX);
            writer.Write(LegacyZ);
            writer.Write((int)BottomLevel);
        }
        writer.Write(AnchorX);
        writer.Write(AnchorZ);
        writer.Write(TopLevel);
        if (version > 6)
        {
            writer.Write(BottomLevel);
            writer.Write(TopElevation);
        }
        writer.Write(LaneCount);
        if (version > 6)
        {
            if (version > 9)
            {
                if (Stages.Count > ushort.MaxValue)
                    throw writer.Invalid($"stair {Id} has {Stages.Count} stages, more than the 65,535 its count can hold");
                writer.Write((ushort)Stages.Count);
            }
            else if (Stages.Count != 1)
                throw writer.Invalid($"stair {Id} has {Stages.Count} stages, but version {version} holds exactly one");
            foreach (var stage in Stages)
            {
                writer.Write(stage.StoredStepCount);
                writer.Write(stage.Unknown1);
            }
        }
        writer.Write(Id);
        writer.Write(ObjectId);
        writer.WriteKeyTypeGroupInstance(StairsKey);
        writer.WriteKeyTypeGroupInstance(RailingKey);
        writer.WriteKeyTypeGroupInstance(WallKey);
        if (version > 11)
            writer.WriteKeyTypeGroupInstance(SecondRailingKey);
        if (version < 8)
        {
            var storable = version > 5 ? 0x3F : 0xF;
            if ((Flags & ~storable) != 0)
                throw writer.Invalid($"stair {Id} has flags 0x{Flags:X4}, but version {version} can store only 0x{storable:X4}");
            writer.Write((Flags & 0x1) != 0);
            writer.Write((Flags & 0x2) != 0);
            writer.Write((Flags & 0x4) != 0);
            writer.Write(Unknown1);
            writer.Write((Flags & 0x8) != 0);
            if (version is 5 or 6)
                writer.Write(Unknown2);
            if (version > 5)
            {
                writer.Write((Flags & 0x10) != 0);
                writer.Write((Flags & 0x20) != 0);
            }
            return;
        }
        writer.Write(Flags);
        writer.Write(Unknown1);
        if (version > 12)
        {
            if (version < 15)
                writer.Write(Unknown3);
            writer.Write(Unknown4);
            writer.Write(Unknown5);
            writer.Write(Unknown6);
            writer.Write(Unknown7);
            if (version > 14)
                writer.Write(Unknown8);
            foreach (var stage in Stages)
            {
                writer.Write(stage.Unknown2);
                writer.Write(stage.Unknown3);
                if (version > 14)
                    writer.Write(stage.Unknown4);
            }
        }
        if (version > 13)
        {
            writer.Write(FirstStep);
            writer.Write(LastStep);
        }
    }
}
