namespace LlamaLogic.TuningArchive;

/// <summary>
/// Represents a tuning a creator has changed in a draft: the tuning as the archive has it and as the creator wants it
/// </summary>
/// <param name="Entry">The tuning</param>
/// <param name="Original">The analysis of the tuning as the archive has it</param>
/// <param name="Draft">The analysis of the tuning as the creator wants it</param>
/// <param name="HasSimData">Whether the game has SimData for the tuning; when it doesn't, nothing reads the tunables the tuning descriptions export to SimData from SimData, so drafts that can't change SimData can still change them</param>
public sealed record TuningDraftTarget(TuningArchiveEntry Entry, TuningAnalysis Original, TuningAnalysis Draft, bool HasSimData = true);