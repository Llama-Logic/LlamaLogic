namespace LlamaLogic.Packages.Models.AudioConfiguration;

/// <summary>
/// Represents a <see cref="ResourceType.AudioConfiguration"/> resource (a PROPX): the audio a sound plays and how the game plays it (🔓)
/// </summary>
/// <remarks>
/// Tuning refers to a configuration with a key of type <see cref="ResourceType.AudioConfigurationReference"/> and the configuration's instance.
/// A configuration's <see cref="Samples"/> are the instances of <see cref="ResourceType.AudioVocals"/> resources (with their <see cref="ResourceType.AudioEffects"/> bodies, if streamed), and nearly all of the game's configurations have a <see cref="Parent"/>.
/// Versions 1 through 7 are supported; version 3 is that of nearly all of the game's configurations.
/// Adapted from S4S by Andrew, extended to read the list version 3 added, which S4S does not.
/// </remarks>
public sealed class AudioConfigurationModel :
    Model,
    IModel<AudioConfigurationModel>
{
    const ushort minimumVersion = 1;
    const ushort maximumVersion = 7;
    static readonly ImmutableHashSet<ResourceType> supportedTypes =
    [
        ResourceType.AudioConfiguration
    ];

    /// <inheritdoc/>
    public static new ISet<ResourceType> SupportedTypes =>
        supportedTypes;

    /// <inheritdoc/>
    /// <exception cref="InvalidDataException">The data is not a well-formed audio configuration of version 1 through 7</exception>
    public static AudioConfigurationModel Decode(ReadOnlyMemory<byte> data)
    {
        var span = data.Span;
        try
        {
            var position = 0;
            var version = span.ReadAndAdvancePosition<ushort>(ref position);
            if (version is < minimumVersion or > maximumVersion)
                throw new InvalidDataException($"Version {version} is not supported, expected {minimumVersion} through {maximumVersion}");
            var model = new AudioConfigurationModel
            {
                Version = version,
                Parent = span.ReadAndAdvancePosition<ulong>(ref position)
            };
            ReadList(span, ref position, model.Samples, 8, (ReadOnlySpan<byte> s, ref int p) => s.ReadAndAdvancePosition<ulong>(ref p));
            ReadList(span, ref position, model.GlobalConditions, 4, (ReadOnlySpan<byte> s, ref int p) =>
            {
                var properties = new Collection<AudioConfigurationProperty>();
                ReadProperties(s, ref p, properties);
                return properties;
            });
            ReadList(span, ref position, model.InteractiveMusic, 8, ReadInteractiveMusicEntry);
            ReadList(span, ref position, model.MoodPitchChanges, 12, ReadUInt64Float);
            ReadList(span, ref position, model.MoodFrequencyPeriods, 12, ReadUInt64Float);
            ReadList(span, ref position, model.MoodFrequencyOffsets, 12, ReadUInt64Float);
            ReadList(span, ref position, model.MoodAttenuations, 12, ReadUInt64Float);
            ReadList(span, ref position, model.MoodPlays, 9, ReadUInt64Boolean);
            ReadList(span, ref position, model.VuMeterFacialOverlayModifierOffsets, 8, ReadUInt32Float);
            ReadList(span, ref position, model.VuMeterFacialOverlayModifierScales, 8, ReadUInt32Float);
            model.Gain = span.ReadAndAdvancePosition<float>(ref position);
            model.Attenuation = span.ReadAndAdvancePosition<float>(ref position);
            model.AggregateGain = span.ReadAndAdvancePosition<byte>(ref position);
            model.PolyphonyDiffersFromBase1 = span.ReadAndAdvancePosition<byte>(ref position);
            model.PolyphonyDiffersFromBase2 = span.ReadAndAdvancePosition<byte>(ref position);
            model.PolyphonyDiffersFromBase3 = span.ReadAndAdvancePosition<byte>(ref position);
            ReadProperties(span, ref position, model.Properties);
            if (version >= 2)
                ReadList(span, ref position, model.SampleWeights, 12, ReadUInt64UInt32);
            if (version >= 3)
                ReadList(span, ref position, model.UnidentifiedMap, 12, ReadUInt64UInt32);
            for (var map = 0; map < GetUnidentifiedFlagMapCount(version); ++map)
            {
                var flags = new Collection<KeyValuePair<ulong, bool>>();
                ReadList(span, ref position, flags, 9, ReadUInt64Boolean);
                model.UnidentifiedFlagMaps.Add(flags);
            }
            if (position != span.Length)
                throw new InvalidDataException($"{span.Length - position} bytes follow the last list");
            return model;
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new InvalidDataException("The data ends before the configuration does", ex);
        }
    }

    /// <inheritdoc/>
    public static new string? GetName(Stream stream) =>
        null;

    /// <inheritdoc/>
    public static new Task<string?> GetNameAsync(Stream stream, CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);

    static int GetUnidentifiedFlagMapCount(ushort version) =>
        version >= 4 ? version : 0;

    delegate T ItemReader<T>(ReadOnlySpan<byte> span, ref int position);

    static bool ReadBoolean(ReadOnlySpan<byte> span, ref int position)
    {
        var value = span.ReadAndAdvancePosition<byte>(ref position);
        return value switch
        {
            0 => false,
            1 => true,
            _ => throw new InvalidDataException($"A flag has the value {value}, where 0 or 1 was expected")
        };
    }

    static AudioConfigurationInteractiveMusicEntry ReadInteractiveMusicEntry(ReadOnlySpan<byte> span, ref int position)
    {
        var entry = new AudioConfigurationInteractiveMusicEntry { Key = span.ReadAndAdvancePosition<uint>(ref position) };
        ReadList(span, ref position, entry.Values, 4, (ReadOnlySpan<byte> s, ref int p) => s.ReadAndAdvancePosition<float>(ref p));
        return entry;
    }

    static void ReadList<T>(ReadOnlySpan<byte> span, ref int position, Collection<T> list, int minimumItemSize, ItemReader<T> read)
    {
        var count = span.ReadAndAdvancePosition<uint>(ref position);
        if (count > (uint)(span.Length - position) / (uint)minimumItemSize)
            throw new InvalidDataException($"A list of {count} items cannot fit in the {span.Length - position} remaining bytes");
        for (var i = 0U; i < count; ++i)
            list.Add(read(span, ref position));
    }

    static void ReadProperties(ReadOnlySpan<byte> span, ref int position, Collection<AudioConfigurationProperty> properties) =>
        ReadList(span, ref position, properties, 7, ReadProperty);

    static AudioConfigurationProperty ReadProperty(ReadOnlySpan<byte> span, ref int position)
    {
        var key = span.ReadAndAdvancePosition<uint>(ref position);
        var type = (AudioConfigurationPropertyType)span.ReadAndAdvancePosition<ushort>(ref position);
        object value;
        switch (type)
        {
            case AudioConfigurationPropertyType.Float:
                value = span.ReadAndAdvancePosition<float>(ref position);
                break;
            case AudioConfigurationPropertyType.UInt32:
                value = span.ReadAndAdvancePosition<uint>(ref position);
                break;
            case AudioConfigurationPropertyType.UInt64:
                value = span.ReadAndAdvancePosition<ulong>(ref position);
                break;
            case AudioConfigurationPropertyType.Boolean:
                value = ReadBoolean(span, ref position);
                break;
            case AudioConfigurationPropertyType.String:
                var length = span.ReadAndAdvancePosition<uint>(ref position);
                if (length > (uint)(span.Length - position))
                    throw new InvalidDataException($"A string of {length} bytes cannot fit in the {span.Length - position} remaining bytes");
                value = Encoding.Latin1.GetString(span.Slice(position, (int)length));
                position += (int)length;
                break;
            case AudioConfigurationPropertyType.Curve:
                var curve = new Collection<AudioConfigurationCurvePoint>();
                ReadList(span, ref position, curve, 20, (ReadOnlySpan<byte> s, ref int p) => new AudioConfigurationCurvePoint
                {
                    InputValue = s.ReadAndAdvancePosition<float>(ref p),
                    OutputValue = s.ReadAndAdvancePosition<float>(ref p),
                    PreviousInputValue = s.ReadAndAdvancePosition<float>(ref p),
                    PreviousOutputValue = s.ReadAndAdvancePosition<float>(ref p),
                    DeltaRatio = s.ReadAndAdvancePosition<float>(ref p)
                });
                value = curve;
                break;
            case AudioConfigurationPropertyType.LongSet:
                var set = new Collection<ulong>();
                ReadList(span, ref position, set, 8, (ReadOnlySpan<byte> s, ref int p) => s.ReadAndAdvancePosition<ulong>(ref p));
                value = set;
                break;
            default:
                throw new InvalidDataException($"Property 0x{key:x8} has the unknown type {(ushort)type}");
        }
        return new(key, value);
    }

    static KeyValuePair<uint, float> ReadUInt32Float(ReadOnlySpan<byte> span, ref int position) =>
        new(span.ReadAndAdvancePosition<uint>(ref position), span.ReadAndAdvancePosition<float>(ref position));

    static KeyValuePair<ulong, bool> ReadUInt64Boolean(ReadOnlySpan<byte> span, ref int position) =>
        new(span.ReadAndAdvancePosition<ulong>(ref position), ReadBoolean(span, ref position));

    static KeyValuePair<ulong, float> ReadUInt64Float(ReadOnlySpan<byte> span, ref int position) =>
        new(span.ReadAndAdvancePosition<ulong>(ref position), span.ReadAndAdvancePosition<float>(ref position));

    static KeyValuePair<ulong, uint> ReadUInt64UInt32(ReadOnlySpan<byte> span, ref int position) =>
        new(span.ReadAndAdvancePosition<ulong>(ref position), span.ReadAndAdvancePosition<uint>(ref position));

    static void Write<T>(ArrayBufferWriter<byte> writer, T value)
        where T : struct =>
        writer.Write(ref value);

    static void WriteCount<T>(ArrayBufferWriter<byte> writer, Collection<T> list) =>
        Write(writer, (uint)list.Count);

    static void WriteProperties(ArrayBufferWriter<byte> writer, Collection<AudioConfigurationProperty> properties)
    {
        WriteCount(writer, properties);
        foreach (var property in properties)
        {
            Write(writer, property.Key);
            Write(writer, (ushort)property.Type);
            switch (property.Value)
            {
                case float value:
                    Write(writer, value);
                    break;
                case uint value:
                    Write(writer, value);
                    break;
                case ulong value:
                    Write(writer, value);
                    break;
                case bool value:
                    Write(writer, value ? (byte)1 : (byte)0);
                    break;
                case string value:
                    var bytes = Encoding.Latin1.GetBytes(value);
                    Write(writer, (uint)bytes.Length);
                    writer.Write(bytes);
                    break;
                case Collection<AudioConfigurationCurvePoint> curve:
                    WriteCount(writer, curve);
                    foreach (var point in curve)
                    {
                        Write(writer, point.InputValue);
                        Write(writer, point.OutputValue);
                        Write(writer, point.PreviousInputValue);
                        Write(writer, point.PreviousOutputValue);
                        Write(writer, point.DeltaRatio);
                    }
                    break;
                case Collection<ulong> set:
                    WriteCount(writer, set);
                    foreach (var value in set)
                        Write(writer, value);
                    break;
            }
        }
    }

    static void WriteUInt32Floats(ArrayBufferWriter<byte> writer, Collection<KeyValuePair<uint, float>> map)
    {
        WriteCount(writer, map);
        foreach (var (key, value) in map)
        {
            Write(writer, key);
            Write(writer, value);
        }
    }

    static void WriteUInt64Booleans(ArrayBufferWriter<byte> writer, Collection<KeyValuePair<ulong, bool>> map)
    {
        WriteCount(writer, map);
        foreach (var (key, value) in map)
        {
            Write(writer, key);
            Write(writer, value ? (byte)1 : (byte)0);
        }
    }

    static void WriteUInt64Floats(ArrayBufferWriter<byte> writer, Collection<KeyValuePair<ulong, float>> map)
    {
        WriteCount(writer, map);
        foreach (var (key, value) in map)
        {
            Write(writer, key);
            Write(writer, value);
        }
    }

    static void WriteUInt64UInt32s(ArrayBufferWriter<byte> writer, Collection<KeyValuePair<ulong, uint>> map)
    {
        WriteCount(writer, map);
        foreach (var (key, value) in map)
        {
            Write(writer, key);
            Write(writer, value);
        }
    }

    ushort version = 3;

    /// <summary>
    /// Gets/sets the aggregate gain
    /// </summary>
    public byte AggregateGain { get; set; }

    /// <summary>
    /// Gets/sets the attenuation
    /// </summary>
    public float Attenuation { get; set; }

    /// <summary>
    /// Gets/sets the gain
    /// </summary>
    public float Gain { get; set; }

    /// <summary>
    /// Gets the global conditions, each a set of properties
    /// </summary>
    public Collection<Collection<AudioConfigurationProperty>> GlobalConditions { get; } = [];

    /// <summary>
    /// Gets the entries of the interactive music
    /// </summary>
    public Collection<AudioConfigurationInteractiveMusicEntry> InteractiveMusic { get; } = [];

    /// <summary>
    /// Gets the attenuations by mood
    /// </summary>
    public Collection<KeyValuePair<ulong, float>> MoodAttenuations { get; } = [];

    /// <summary>
    /// Gets the frequency offsets by mood
    /// </summary>
    public Collection<KeyValuePair<ulong, float>> MoodFrequencyOffsets { get; } = [];

    /// <summary>
    /// Gets the frequency periods by mood
    /// </summary>
    public Collection<KeyValuePair<ulong, float>> MoodFrequencyPeriods { get; } = [];

    /// <summary>
    /// Gets the pitch changes by mood
    /// </summary>
    public Collection<KeyValuePair<ulong, float>> MoodPitchChanges { get; } = [];

    /// <summary>
    /// Gets whether the sound plays, by mood
    /// </summary>
    public Collection<KeyValuePair<ulong, bool>> MoodPlays { get; } = [];

    /// <summary>
    /// Gets/sets the instance of the configuration this one inherits from, or zero if it has none
    /// </summary>
    public ulong Parent { get; set; }

    /// <summary>
    /// Gets/sets the first of three bytes S4S calls "polyphony differs from base"
    /// </summary>
    public byte PolyphonyDiffersFromBase1 { get; set; }

    /// <summary>
    /// Gets/sets the second of three bytes S4S calls "polyphony differs from base"
    /// </summary>
    public byte PolyphonyDiffersFromBase2 { get; set; }

    /// <summary>
    /// Gets/sets the third of three bytes S4S calls "polyphony differs from base"
    /// </summary>
    public byte PolyphonyDiffersFromBase3 { get; set; }

    /// <summary>
    /// Gets the properties
    /// </summary>
    public Collection<AudioConfigurationProperty> Properties { get; } = [];

    /// <inheritdoc/>
    public override string? ResourceName =>
        null;

    /// <summary>
    /// Gets the instances of the <see cref="ResourceType.AudioVocals"/> resources the configuration plays
    /// </summary>
    public Collection<ulong> Samples { get; } = [];

    /// <summary>
    /// Gets the weights of samples, by sample instance (written in version 2 and later)
    /// </summary>
    public Collection<KeyValuePair<ulong, uint>> SampleWeights { get; } = [];

    /// <summary>
    /// Gets the maps from 64-bit keys to flags that version 4 and later add (four in version 4 and one more in each later version), whose meaning is not known
    /// </summary>
    public Collection<Collection<KeyValuePair<ulong, bool>>> UnidentifiedFlagMaps { get; } = [];

    /// <summary>
    /// Gets the map from 64-bit keys to 32-bit values that version 3 added, whose meaning is not known (written in version 3 and later)
    /// </summary>
    public Collection<KeyValuePair<ulong, uint>> UnidentifiedMap { get; } = [];

    /// <summary>
    /// Gets/sets the version of the resource, which determines which values <see cref="Encode"/> writes
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not 1 through 7</exception>
    public ushort Version
    {
        get => version;
        set
        {
            if (value is < minimumVersion or > maximumVersion)
                throw new ArgumentOutOfRangeException(nameof(value), value, $"The version must be {minimumVersion} through {maximumVersion}");
            version = value;
        }
    }

    /// <summary>
    /// Gets the offsets of facial overlay modifiers driven by the VU meter
    /// </summary>
    public Collection<KeyValuePair<uint, float>> VuMeterFacialOverlayModifierOffsets { get; } = [];

    /// <summary>
    /// Gets the scales of facial overlay modifiers driven by the VU meter
    /// </summary>
    public Collection<KeyValuePair<uint, float>> VuMeterFacialOverlayModifierScales { get; } = [];

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException"><see cref="UnidentifiedFlagMaps"/> does not have as many maps as <see cref="Version"/> has (none before version 4, then as many as the version)</exception>
    public override ReadOnlyMemory<byte> Encode()
    {
        var flagMapCount = GetUnidentifiedFlagMapCount(version);
        if (UnidentifiedFlagMaps.Count != flagMapCount)
            throw new InvalidOperationException($"Version {version} has {flagMapCount} unidentified flag maps, but {UnidentifiedFlagMaps.Count} are set");
        var writer = new ArrayBufferWriter<byte>();
        Write(writer, version);
        Write(writer, Parent);
        WriteCount(writer, Samples);
        foreach (var sample in Samples)
            Write(writer, sample);
        WriteCount(writer, GlobalConditions);
        foreach (var condition in GlobalConditions)
            WriteProperties(writer, condition);
        WriteCount(writer, InteractiveMusic);
        foreach (var entry in InteractiveMusic)
        {
            Write(writer, entry.Key);
            WriteCount(writer, entry.Values);
            foreach (var value in entry.Values)
                Write(writer, value);
        }
        WriteUInt64Floats(writer, MoodPitchChanges);
        WriteUInt64Floats(writer, MoodFrequencyPeriods);
        WriteUInt64Floats(writer, MoodFrequencyOffsets);
        WriteUInt64Floats(writer, MoodAttenuations);
        WriteUInt64Booleans(writer, MoodPlays);
        WriteUInt32Floats(writer, VuMeterFacialOverlayModifierOffsets);
        WriteUInt32Floats(writer, VuMeterFacialOverlayModifierScales);
        Write(writer, Gain);
        Write(writer, Attenuation);
        Write(writer, AggregateGain);
        Write(writer, PolyphonyDiffersFromBase1);
        Write(writer, PolyphonyDiffersFromBase2);
        Write(writer, PolyphonyDiffersFromBase3);
        WriteProperties(writer, Properties);
        if (version >= 2)
            WriteUInt64UInt32s(writer, SampleWeights);
        if (version >= 3)
            WriteUInt64UInt32s(writer, UnidentifiedMap);
        foreach (var map in UnidentifiedFlagMaps)
            WriteUInt64Booleans(writer, map);
        return writer.WrittenMemory;
    }
}