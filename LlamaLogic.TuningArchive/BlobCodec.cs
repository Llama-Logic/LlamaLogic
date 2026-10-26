namespace LlamaLogic.TuningArchive;

sealed class BlobCodec :
    IDisposable
{
    const int compressionLevel = 9;
    const int mediaCompressionLevel = 3;
    const int dictionarySize = 112 * 1024;
    const int maximumTrainingSamples = 4000;
    const int minimumTrainingSamples = 64;

    public static byte[]? TrainDictionary(IReadOnlyList<byte[]> samples)
    {
        if (samples.Count < minimumTrainingSamples)
            return null;
        var chosen = samples.Count <= maximumTrainingSamples
            ? samples
            : [.. Enumerable.Range(0, maximumTrainingSamples).Select(index => samples[(int)((long)index * samples.Count / maximumTrainingSamples)])];
        try
        {
            return DictBuilder.TrainFromBuffer(chosen, dictionarySize).ToArray();
        }
        catch (ZstdException)
        {
            return null;
        }
    }

    public static BlobCodec CreateForMedia() =>
        new(null, null, mediaCompressionLevel);

    public BlobCodec(long? dictionaryId, byte[]? dictionary, int level = compressionLevel)
    {
        DictionaryId = dictionaryId;
        compressors = new(() =>
        {
            var compressor = new Compressor(level);
            if (dictionary is not null)
                compressor.LoadDictionary(dictionary);
            return compressor;
        }, true);
        decompressors = new(() =>
        {
            var decompressor = new Decompressor();
            if (dictionary is not null)
                decompressor.LoadDictionary(dictionary);
            return decompressor;
        }, true);
    }

    readonly ThreadLocal<Compressor> compressors;
    readonly ThreadLocal<Decompressor> decompressors;

    public long? DictionaryId { get; }

    public byte[] Compress(ReadOnlySpan<byte> content) =>
        compressors.Value!.Wrap(content).ToArray();

    public byte[] Decompress(ReadOnlySpan<byte> compressed) =>
        decompressors.Value!.Unwrap(compressed).ToArray();

    public void Dispose()
    {
        foreach (var compressor in compressors.Values)
            compressor.Dispose();
        foreach (var decompressor in decompressors.Values)
            decompressor.Dispose();
        compressors.Dispose();
        decompressors.Dispose();
    }
}