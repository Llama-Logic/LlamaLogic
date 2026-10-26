namespace LlamaLogic.EpicTuningTime.Components.Shared;

/// <summary>
/// Describes, decodes and names the media an archive captured
/// </summary>
static class MediaContent
{
    static readonly string[] audioCodecs = ["none", "reserved", "PCM", "EA-XMA", "EA-XAS", "EALayer3 v1", "EALayer3 v2 (PCM)", "EALayer3 v2 (spike)", "GameCube ADPCM", "EA Speex", "EA ATRAC3+", "EA MP3", "EA Opus", "EA ATRAC9", "EA Opus (multistream)", "EA Opus (multistream, uncoupled)"];

    /// <summary>
    /// Gets the extension of the file a medium is exported to as the game stores it
    /// </summary>
    public static string GetExtension(TuningArchiveEntry entry, byte[] content) =>
        entry.Type switch
        {
            _ when IsPng(content) => ".png",
            ResourceType.DSTImage => ".dst",
            ResourceType.AudioConfiguration => ".propx",
            ResourceType.AudioVocals => ".snr",
            ResourceType.AudioEffects => ".sns",
            ResourceType.AVI => ".vp6",
            _ => ".bin"
        };

    /// <summary>
    /// Gets the name a medium is shown and exported with: the file name tuning gives it, if it gives one, otherwise its instance
    /// </summary>
    public static string GetName(TuningArchiveEntry entry) =>
        entry.Name is { Length: > 0 } name ? Path.GetFileName(name.Replace('\\', '/')) : entry.Instance.ToString("X16", CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets the name of the specified <paramref name="kind"/> of media, in the plural
    /// </summary>
    public static string GetPluralName(TuningArchiveResourceKind kind) =>
        kind switch
        {
            TuningArchiveResourceKind.Image => "images",
            TuningArchiveResourceKind.Sound => "sounds",
            _ => "videos"
        };

    /// <summary>
    /// Gets the name of the specified <paramref name="kind"/> of media
    /// </summary>
    public static string GetSingularName(TuningArchiveResourceKind kind) =>
        kind switch
        {
            TuningArchiveResourceKind.Image => "image",
            TuningArchiveResourceKind.Sound => "sound",
            _ => "video"
        };

    static bool IsDds(ReadOnlySpan<byte> content) =>
        content.Length >= 128 && content[..4].SequenceEqual("DDS "u8);

    static bool IsPng(ReadOnlySpan<byte> content) =>
        content.Length >= 24 && content[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a });

    /// <summary>
    /// Decodes an image as the game stores it into a PNG, describing it
    /// </summary>
    /// <exception cref="InvalidDataException">The image could not be decoded</exception>
    public static (byte[] png, string description) DecodeImage(byte[] content)
    {
        if (IsPng(content))
            return (content, $"{BinaryPrimitives.ReadUInt32BigEndian(content.AsSpan(16))} × {BinaryPrimitives.ReadUInt32BigEndian(content.AsSpan(20))} · PNG · {FormatBytes(content.Length)}");
        if (!IsDds(content))
            throw new InvalidDataException("The image is neither a DirectDraw Surface nor a PNG");
        var span = content.AsSpan();
        var width = BinaryPrimitives.ReadUInt32LittleEndian(span[16..]);
        var height = BinaryPrimitives.ReadUInt32LittleEndian(span[12..]);
        var mipMaps = Math.Max(1u, BinaryPrimitives.ReadUInt32LittleEndian(span[28..]));
        var format = span[0x54] is 0
            ? $"{BinaryPrimitives.ReadUInt32LittleEndian(span[0x58..])}-bit"
            : Encoding.ASCII.GetString(span[0x54..0x58]);
        var png = DirectDrawSurface.GetPngDataFromDdsData(content).ToArray();
        return (png, $"{width} × {height} · {format} · {mipMaps} mipmap level{(mipMaps == 1 ? string.Empty : "s")} · {FormatBytes(content.Length)}");
    }

    /// <summary>
    /// Gets an image as the game stores it as a DirectDraw Surface (unshuffled, so other tools can open it), or <see langword="null"/> if it is not a DirectDraw Surface
    /// </summary>
    public static byte[]? GetDds(byte[] content) =>
        IsDds(content) ? DirectDrawSurface.GetUnshuffledDdsData(content).ToArray() : null;

    /// <summary>
    /// Describes audio as the game stores it (an AudioCore header, with or without its audio, or the body of a stream)
    /// </summary>
    public static string DescribeAudio(TuningArchiveEntry entry, byte[] content)
    {
        if (entry.Type is ResourceType.AudioEffects)
            return $"Audio stream body · {FormatBytes(content.Length)}";
        if (entry.Type is not ResourceType.AudioVocals)
            return FormatBytes(content.Length);
        ElectronicArtsAudioCoreHeader header;
        try
        {
            header = ElectronicArtsAudioCoreHeader.Read(content);
        }
        catch (InvalidDataException)
        {
            return $"Unreadable audio header · {FormatBytes(content.Length)}";
        }
        var codec = (int)header.Codec < audioCodecs.Length ? audioCodecs[(int)header.Codec] : $"codec {(int)header.Codec}";
        var loop = header.LoopStartSample is { } loopStart
            ? loopStart is 0 ? " · loops" : $" · loops from {FormatDuration(header, loopStart)}"
            : string.Empty;
        return $"{codec} · {(header.ChannelCount == 1 ? "mono" : header.ChannelCount == 2 ? "stereo" : $"{header.ChannelCount} channels")} · {header.SampleRate.ToString("N0", CultureInfo.CurrentCulture)} Hz · {FormatDuration(header, header.SampleCount)}{loop} · {(header.Storage is ElectronicArtsAudioCoreStorage.Stream ? $"streamed (this {FormatBytes(content.Length)} header is all; the audio is in the body, a separate resource)" : $"in memory · {FormatBytes(content.Length)}")}";
    }

    /// <summary>
    /// Formats a number of samples of a sound as a duration
    /// </summary>
    public static string FormatDuration(ElectronicArtsAudioCoreHeader header, int samples)
    {
        var duration = TimeSpan.FromSeconds((double)samples / header.SampleRate);
        return duration.ToString(duration.TotalHours >= 1 ? @"h\:mm\:ss\.f" : @"m\:ss\.f", CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Finds the header and, for a streamed sound, the body of the sound an <see cref="ResourceType.AudioVocals"/> or <see cref="ResourceType.AudioEffects"/> entry belongs to, in the specified <paramref name="snapshot"/>, or explains why they could not be found
    /// </summary>
    public static (TuningArchiveEntry? header, TuningArchiveEntry? body, string? problem) FindSound(TuningArchiveFile archive, TuningArchiveSnapshot snapshot, TuningArchiveEntry entry)
    {
        var header = entry.Type is ResourceType.AudioVocals ? entry : archive.GetEntry(snapshot, TuningArchiveResourceKind.Sound, ResourceType.AudioVocals, entry.Instance);
        if (header is null)
            return (null, null, "This snapshot doesn't have the header of this stream, so it can't be played.");
        var body = archive.GetEntry(snapshot, TuningArchiveResourceKind.Sound, ResourceType.AudioEffects, entry.Instance);
        return (header, body, null);
    }

    /// <summary>
    /// Rebuilds the sound an <see cref="ResourceType.AudioVocals"/> or <see cref="ResourceType.AudioEffects"/> entry belongs to as MP3
    /// </summary>
    /// <exception cref="InvalidDataException">The sound could not be found or rebuilt</exception>
    /// <exception cref="NotSupportedException">The sound uses a codec that cannot be rebuilt as MP3</exception>
    public static ElectronicArtsAudioCoreMp3 GetMp3(TuningArchiveFile archive, TuningArchiveSnapshot snapshot, TuningArchiveEntry entry)
    {
        var (header, body, problem) = FindSound(archive, snapshot, entry);
        if (problem is not null)
            throw new InvalidDataException(problem);
        var headerData = archive.GetContent(header!);
        if (ElectronicArtsAudioCoreHeader.Read(headerData).Storage is ElectronicArtsAudioCoreStorage.Stream && body is null)
            throw new InvalidDataException("This snapshot doesn't have the body of this streamed sound, so it can't be played.");
        try
        {
            return ElectronicArtsAudioCore.GetMp3(headerData, body is null ? default : archive.GetContent(body));
        }
        catch (ArgumentException ex)
        {
            throw new InvalidDataException(ex.Message, ex);
        }
    }

    /// <summary>
    /// Describes a video as the game stores it (EA VP6)
    /// </summary>
    public static string DescribeVideo(byte[] content)
    {
        var span = content.AsSpan();
        for (var offset = 0; offset + 32 <= span.Length && offset < 4096;)
        {
            var size = (int)BinaryPrimitives.ReadUInt32LittleEndian(span[(offset + 4)..]);
            if (span.Slice(offset, 4).SequenceEqual("MVhd"u8))
            {
                var width = BinaryPrimitives.ReadUInt16LittleEndian(span[(offset + 12)..]);
                var height = BinaryPrimitives.ReadUInt16LittleEndian(span[(offset + 14)..]);
                var frames = BinaryPrimitives.ReadUInt32LittleEndian(span[(offset + 16)..]);
                var rateDenominator = BinaryPrimitives.ReadUInt32LittleEndian(span[(offset + 24)..]);
                var rateNumerator = BinaryPrimitives.ReadUInt32LittleEndian(span[(offset + 28)..]);
                var framesPerSecond = rateNumerator > 0 ? (double)rateDenominator / rateNumerator : 0;
                var duration = framesPerSecond > 0 ? TimeSpan.FromSeconds(frames / framesPerSecond) : TimeSpan.Zero;
                return $"EA VP6 · {width} × {height} · {frames.ToString("N0", CultureInfo.CurrentCulture)} frames at {framesPerSecond.ToString("0.##", CultureInfo.CurrentCulture)} fps · {duration.ToString(@"m\:ss", CultureInfo.CurrentCulture)} · {FormatBytes(content.Length)}";
            }
            if (size < 8)
                break;
            offset += size;
        }
        return $"Video · {FormatBytes(content.Length)}";
    }

    public static string FormatBytes(long bytes) =>
        bytes >= 1024 * 1024
            ? $"{(bytes / (1024.0 * 1024.0)).ToString("N1", CultureInfo.CurrentCulture)} MB"
            : bytes >= 1024
            ? $"{(bytes / 1024.0).ToString("N1", CultureInfo.CurrentCulture)} KB"
            : $"{bytes.ToString("N0", CultureInfo.CurrentCulture)} bytes";
}