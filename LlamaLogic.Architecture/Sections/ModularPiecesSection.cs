namespace LlamaLogic.Architecture;

/// <summary>
/// Represents the <see cref="LotArchitectureSectionType.ModularPieces"/> section: the modular pieces placed on the lot (🔓)
/// </summary>
/// <remarks>
/// The layout follows the game's reader.
/// Versions 4 through 6 are supported, those in the lots examined, and the game writes version 6.
/// <see cref="ModularPiece.FirstWallPatternKey"/> and <see cref="ModularPiece.SecondWallPatternKey"/> exist from version 5, <see cref="ModularPiece.Unknown3"/> from version 6.
/// </remarks>
public sealed class ModularPiecesSection :
    LotArchitectureSection
{
    const uint minimumVersion = 4;
    const uint maximumVersion = 6;

    internal static ModularPiecesSection Read(ref SectionReader reader, SectionContext context)
    {
        var section = new ModularPiecesSection { Version = reader.ReadVersion(minimumVersion, maximumVersion) };
        var count = reader.ReadCount(reader.ReadUInt32() * 85L) / 85;
        for (var i = 0; i < count; ++i)
        {
            var piece = new ModularPiece
            {
                Position = reader.ReadUInt32(),
                ModularPieceKey = reader.ReadKeyTypeGroupInstance(),
                FloorPatternKey = reader.ReadKeyTypeGroupInstance(),
                FenceKey = reader.ReadKeyTypeGroupInstance(),
                CeilingRailKey = reader.ReadKeyTypeGroupInstance(),
                Unknown1 = reader.ReadKeyTypeGroupInstance()
            };
            if (section.Version > 4)
            {
                piece.FirstWallPatternKey = reader.ReadKeyTypeGroupInstance();
                piece.SecondWallPatternKey = reader.ReadKeyTypeGroupInstance();
            }
            piece.Unknown2 = reader.ReadByte();
            if (section.Version > 5)
                piece.Unknown3 = reader.ReadUInt16();
            section.Pieces.Add(piece);
        }
        return section;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModularPiecesSection"/> class
    /// </summary>
    public ModularPiecesSection() :
        base(LotArchitectureSectionType.ModularPieces)
    {
    }

    uint version = maximumVersion;

    /// <summary>
    /// Gets the modular pieces
    /// </summary>
    public Collection<ModularPiece> Pieces { get; } = [];

    /// <summary>
    /// Gets/sets the version of the section, which decides its layout
    /// </summary>
    /// <remarks>
    /// Encoding throws an <see cref="InvalidOperationException"/> when a field the version lacks holds a value other than its default, or a collection the version lacks is not empty.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The version is not 4 through 6</exception>
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

    internal override void Write(SectionWriter writer, SectionContext context)
    {
        writer.Write(version);
        writer.WriteCount(Pieces.Count);
        foreach (var piece in Pieces)
        {
            if (version < 5)
            {
                writer.RequireDefault(piece.FirstWallPatternKey, default, $"the {nameof(ModularPiece.FirstWallPatternKey)} of the piece at 0x{piece.Position:X8}", version);
                writer.RequireDefault(piece.SecondWallPatternKey, default, $"the {nameof(ModularPiece.SecondWallPatternKey)} of the piece at 0x{piece.Position:X8}", version);
            }
            if (version < 6)
                writer.RequireDefault(piece.Unknown3, (ushort)0, $"the {nameof(ModularPiece.Unknown3)} of the piece at 0x{piece.Position:X8}", version);
            writer.Write(piece.Position);
            writer.WriteKeyTypeGroupInstance(piece.ModularPieceKey);
            writer.WriteKeyTypeGroupInstance(piece.FloorPatternKey);
            writer.WriteKeyTypeGroupInstance(piece.FenceKey);
            writer.WriteKeyTypeGroupInstance(piece.CeilingRailKey);
            writer.WriteKeyTypeGroupInstance(piece.Unknown1);
            if (version > 4)
            {
                writer.WriteKeyTypeGroupInstance(piece.FirstWallPatternKey);
                writer.WriteKeyTypeGroupInstance(piece.SecondWallPatternKey);
            }
            writer.Write(piece.Unknown2);
            if (version > 5)
                writer.Write(piece.Unknown3);
        }
    }
}
