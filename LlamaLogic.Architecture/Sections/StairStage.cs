namespace LlamaLogic.Architecture;

/// <summary>
/// Represents one stage of a <see cref="Stair"/>: a straight run of steps
/// </summary>
/// <remarks>
/// <para>A stage is stored as a 16-bit value holding <see cref="StepCount"/> in its low 10 bits and a byte (<see cref="Unknown1"/>).
/// From version 13 of <see cref="StairsSection"/> on, each stage also has <see cref="Unknown2"/> and <see cref="Unknown3"/>, and from version 15 on <see cref="Unknown4"/>; these follow the stair's other fields, one set per stage in stage order.</para>
/// <para>The game keeps only the low 10 bits of <see cref="StoredStepCount"/>, <see cref="Unknown2"/> and <see cref="Unknown3"/>, the low 6 bits of <see cref="Unknown1"/> and the low 4 bits of <see cref="Unknown4"/>, and writes only those; the stored values are kept whole here so that every record round-trips.</para>
/// <para>In-game tests: a straight flight of 12 steps had one stage with <see cref="Unknown2"/> 5 and <see cref="Unknown3"/> 7; turning its upper part 90° split it into a stage of 8 steps (<see cref="Unknown2"/> 8, <see cref="Unknown3"/> 0) and one of 4 (<see cref="Unknown2"/> 0, <see cref="Unknown3"/> 4), both with <see cref="Unknown1"/> 2; after taller walls and a landing, the stages had 12 steps (<see cref="Unknown1"/> 2) and 8 (<see cref="Unknown1"/> 4).
/// These are observations; the meanings of the values are not proven.</para>
/// </remarks>
public sealed class StairStage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StairStage"/> class
    /// </summary>
    public StairStage()
    {
    }

    /// <summary>
    /// Gets/sets the number of steps of the stage, the low 10 bits of <see cref="StoredStepCount"/>
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is greater than 1,023</exception>
    public ushort StepCount
    {
        get => (ushort)(StoredStepCount & 0x3FF);
        set
        {
            if (value > 0x3FF)
                throw new ArgumentOutOfRangeException(nameof(value), value, "The step count must be 1,023 or less");
            StoredStepCount = (ushort)(StoredStepCount & 0xFC00 | value);
        }
    }

    /// <summary>
    /// Gets/sets the stored 16-bit value whose low 10 bits are <see cref="StepCount"/>
    /// </summary>
    /// <remarks>
    /// The high 6 bits are 0 in every lot examined.
    /// </remarks>
    public ushort StoredStepCount { get; set; }

    /// <summary>
    /// Gets/sets the unknown byte of the stage
    /// </summary>
    /// <remarks>
    /// The game reads its low 6 bits; it is 0, 2, 4 or 6 in the lots examined, except 12 in 90 stair stages.
    /// That it is the stage's direction in the game's eight-way direction enumeration is a hypothesis; it did not change when a flight was turned 90° in game (in-game test).
    /// </remarks>
    public byte Unknown1 { get; set; }

    /// <summary>
    /// Gets/sets the first unknown 16-bit value of the stage
    /// </summary>
    /// <remarks>
    /// Present from version 13 on. The game reads its low 10 bits; it looks like a step index (hypothesis).
    /// </remarks>
    public ushort Unknown2 { get; set; }

    /// <summary>
    /// Gets/sets the second unknown 16-bit value of the stage
    /// </summary>
    /// <remarks>
    /// Present from version 13 on. The game reads its low 10 bits; it looks like a step index (hypothesis).
    /// </remarks>
    public ushort Unknown3 { get; set; }

    /// <summary>
    /// Gets/sets the unknown byte that follows <see cref="Unknown3"/>
    /// </summary>
    /// <remarks>
    /// Present from version 15 on. The game reads its low 4 bits.
    /// </remarks>
    public byte Unknown4 { get; set; }
}
