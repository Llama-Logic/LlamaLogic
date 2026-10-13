namespace LlamaLogic.Packages.CorpusTests;

[TestClass]
public class PackageCorpusTests
{
    public TestContext TestContext { get; set; } = null!;

    static void CheckCopy(DataBasePackedFile original, DataBasePackedFile copy, ProblemLog problems)
    {
        if (!original.Keys.SequenceEqual(copy.Keys))
        {
            problems.Add("copy keys", $"original has {original.Keys.Count} keys and copy has {copy.Keys.Count}, or their order differs");
            return;
        }
        foreach (var key in original.Keys)
        {
            try
            {
                if (original.GetExplicitCompressionMode(key) != copy.GetExplicitCompressionMode(key))
                    problems.Add("copy compression", $"{key}: {original.GetExplicitCompressionMode(key)} became {copy.GetExplicitCompressionMode(key)}");
                if (original.GetSize(key) != copy.GetSize(key))
                    problems.Add("copy size", $"{key}: {original.GetSize(key)} became {copy.GetSize(key)}");
                if (!original.GetRaw(key, true).Span.SequenceEqual(copy.GetRaw(key, true).Span))
                    problems.Add("copy content", $"{key}");
            }
            catch (Exception ex)
            {
                problems.Add("copy read", $"{key}: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }

    static void CheckResource(DataBasePackedFile package, ResourceKey key, ProblemLog problems)
    {
        CompressionMode compressionMode;
        try
        {
            compressionMode = package.GetExplicitCompressionMode(key);
        }
        catch (Exception ex)
        {
            problems.Add("compression type", $"{key}: {ex.GetType().Name}: {ex.Message}");
            return;
        }
        if (compressionMode is CompressionMode.SetDeletedFlag)
            return;
        try
        {
            var raw = package.GetRaw(key);
            var size = package.GetSize(key);
            var decompressed = compressionMode switch
            {
                CompressionMode.ForceOff => raw,
                CompressionMode.ForceZLib => Inflate(raw),
                _ when package.FileVersion.Major is 1 => DataBasePackedFile.InternalDecompress(raw[4..]), // DBPF 1.x prefixes compressed content with its stored size
                _ => DataBasePackedFile.InternalDecompress(raw)
            };
            if (decompressed.Length != size)
                problems.Add("size", $"{key}: index says {size}, content decompresses to {decompressed.Length}");
            if (!package.Get(key).Span.SequenceEqual(decompressed.Span))
                problems.Add("content", $"{key}: Get differs from independent decompression");
        }
        catch (Exception ex)
        {
            problems.Add("read", $"{key}: {ex.GetType().Name}: {ex.Message}");
        }
    }

    public static string GetDisplayName(MethodInfo methodInfo, object[] data) =>
        Path.Combine(Path.GetFileName(((string)data[0]).TrimEnd(Path.DirectorySeparatorChar)), Path.GetRelativePath((string)data[0], (string)data[1]));

    static ReadOnlyMemory<byte> Inflate(ReadOnlyMemory<byte> compressed)
    {
        using var compressedStream = new MemoryStream(compressed.ToArray());
        using var inflater = new System.IO.Compression.ZLibStream(compressedStream, System.IO.Compression.CompressionMode.Decompress);
        using var decompressedStream = new MemoryStream();
        inflater.CopyTo(decompressedStream);
        return decompressedStream.ToArray();
    }

    [TestMethod]
    [DynamicData(nameof(PackageCorpus.Packages), typeof(PackageCorpus), DynamicDataDisplayName = nameof(GetDisplayName))]
    public void PackageIsConsistent(string root, string path)
    {
        var problems = new ProblemLog();
        var temporaryDirectory = Path.Combine(Path.GetTempPath(), "LlamaLogic.Packages.CorpusTests");
        var temporaryPath = Path.Combine(temporaryDirectory, $"{Guid.NewGuid():n}.package");
        Directory.CreateDirectory(temporaryDirectory);
        try
        {
            using var package = new DataBasePackedFile(path);
            var keys = package.Keys;
            TestContext.WriteLine($"{keys.Count} resources, {new FileInfo(path).Length} bytes");
            foreach (var undefinedType in keys.Where(key => !Enum.IsDefined(key.Type)).GroupBy(key => key.Type).OrderBy(group => group.Key))
                TestContext.WriteLine($"undefined type 0x{(uint)undefinedType.Key:x8}: {undefinedType.Count()} resources");
            foreach (var key in keys)
                CheckResource(package, key, problems);
            try
            {
                package.GetNames();
            }
            catch (Exception ex)
            {
                problems.Add("names", $"{ex.GetType().Name}: {ex.Message}");
            }
            package.SaveAs(temporaryPath);
            using var copy = new DataBasePackedFile(temporaryPath);
            CheckCopy(package, copy, problems);
        }
        catch (Exception ex)
        {
            problems.Add("package", $"{ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            File.Delete(temporaryPath);
        }
        if (problems.Count > 0)
            Assert.Fail(problems.ToString());
    }
}
