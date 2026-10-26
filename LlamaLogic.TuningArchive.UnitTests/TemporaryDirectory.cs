namespace LlamaLogic.TuningArchive.UnitTests;

sealed class TemporaryDirectory :
    IDisposable
{
    public TemporaryDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "LlamaLogic.TuningArchive.UnitTests", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void Dispose() =>
        Directory.Delete(Path, true);

    public string GetPath(string relativePath) =>
        System.IO.Path.Combine(Path, relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar));

    public string WriteFile(string relativePath, string content)
    {
        var path = GetPath(relativePath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    public string WriteFiles(string relativeFolder, IReadOnlyDictionary<string, string> files)
    {
        foreach (var (relativePath, content) in files)
            WriteFile($"{relativeFolder}/{relativePath}", content);
        return GetPath(relativeFolder);
    }
}