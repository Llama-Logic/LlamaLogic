namespace LlamaLogic.Architecture;

/// <summary>
/// Represents one of the two user records of a <see cref="LotInfoSection"/>
/// </summary>
/// <remarks>
/// <para>The names are account names in the lots examined (or empty); that the first record is the lot's creator and the second its last modifier is a hypothesis.</para>
/// <para>The section stores both <see cref="Id"/> values and both <see cref="Name"/> values first; from version 19 on, each record's <see cref="Unknown1"/>, <see cref="Unknown2"/> and <see cref="RepeatedId"/> follow.</para>
/// </remarks>
public sealed class LotInfoUser
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LotInfoUser"/> class
    /// </summary>
    public LotInfoUser()
    {
    }

    /// <summary>
    /// Gets/sets the id of the user
    /// </summary>
    /// <remarks>
    /// That it is an account id is a hypothesis.
    /// </remarks>
    public ulong Id { get; set; }

    /// <summary>
    /// Gets/sets the name of the user
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets/sets the id that repeats <see cref="Id"/>
    /// </summary>
    /// <remarks>
    /// Present from version 19 on.
    /// The game reads it after <see cref="Id"/> and keeps it in place of <see cref="Id"/>; the two are equal in every lot examined.
    /// </remarks>
    public ulong RepeatedId { get; set; }

    /// <summary>
    /// Gets/sets the first unknown value
    /// </summary>
    /// <remarks>
    /// Present from version 19 on. It is 0 or 100 in the lots examined.
    /// </remarks>
    public uint Unknown1 { get; set; }

    /// <summary>
    /// Gets/sets the second unknown value
    /// </summary>
    /// <remarks>
    /// Present from version 19 on. It is empty or an account name in the lots examined.
    /// </remarks>
    public string Unknown2 { get; set; } = string.Empty;
}
