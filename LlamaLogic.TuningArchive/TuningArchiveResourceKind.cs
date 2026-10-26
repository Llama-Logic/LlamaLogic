namespace LlamaLogic.TuningArchive;

/// <summary>
/// Specifies the kind of a resource in a <see cref="TuningArchiveFile"/>
/// </summary>
public enum TuningArchiveResourceKind
{
    /// <summary>
    /// A tuning document (instance or module tuning)
    /// </summary>
    Tuning = 0,

    /// <summary>
    /// A SimData resource, stored as XML in the format Sims 4 Studio uses
    /// </summary>
    SimData = 1,

    /// <summary>
    /// A Python module of the game's scripts, as decompiled source
    /// </summary>
    PythonModule = 2,

    /// <summary>
    /// An animation state machine (also known as an ASM or JAZZ)
    /// </summary>
    AnimationStateMachine = 3,

    /// <summary>
    /// An image to which tuning or SimData refers (such as an icon), stored as the game stores it (usually a <see cref="ResourceType.DSTImage"/>)
    /// </summary>
    Image = 4,

    /// <summary>
    /// A sound to which tuning or SimData refers: an audio configuration (<see cref="ResourceType.AudioConfiguration"/>), or audio one uses (<see cref="ResourceType.AudioVocals"/> or <see cref="ResourceType.AudioEffects"/>), stored as the game stores it
    /// </summary>
    Sound = 5,

    /// <summary>
    /// A video to which tuning or SimData refers (<see cref="ResourceType.AVI"/>), stored as the game stores it
    /// </summary>
    Video = 6
}