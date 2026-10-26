namespace LlamaLogic.Architecture;

/// <summary>
/// Represents one business entry of a <see cref="VenueSection"/>
/// </summary>
/// <remarks>
/// <see cref="Key"/> equals <see cref="VenueTuning"/> in all 50 entries examined, and differs from the section's own venue tuning in 13 of them.
/// </remarks>
public sealed class VenueBusinessInfo
{
    /// <summary>
    /// Gets/sets the key under which the game stores the entry
    /// </summary>
    public ulong Key { get; set; }

    /// <summary>
    /// Gets/sets the message of the entry, serialized
    /// </summary>
    /// <remarks>
    /// The message type depends on the venue, so the bytes are kept as they are.
    /// A venue of 0x1FE99 uses the restaurant configuration message.
    /// In the entries examined the payloads of venue 0x53533 are empty, those of 0x1FE99 and 0x26C7F are not, and those of 0x6F417 are either.
    /// </remarks>
    public ReadOnlyMemory<byte> Payload { get; set; }

    /// <summary>
    /// Gets/sets the tuning id of the venue the entry belongs to
    /// </summary>
    public ulong VenueTuning { get; set; }
}
