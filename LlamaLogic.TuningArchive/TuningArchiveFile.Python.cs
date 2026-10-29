namespace LlamaLogic.TuningArchive;

partial class TuningArchiveFile
{
    internal const string MotherlodeReportPath = "motherlode-report.json";

    /// <summary>
    /// Gets the resource instance under which the archive keeps the Python module at the specified <paramref name="path"/> (relative to the decompiled scripts, with forward slashes, such as <c>buffs/buff.py</c>)
    /// </summary>
    public static ulong GetPythonModuleInstance(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return Fnv.Hash64(path.Replace('\\', '/'));
    }

    /// <summary>
    /// Gets the path of the decompiled source file of the specified Python <paramref name="module"/> (such as <c>buffs.buff</c>), relative to the decompiled scripts
    /// </summary>
    public static IReadOnlyList<string> GetPythonModulePaths(string module)
    {
        ArgumentNullException.ThrowIfNull(module);
        var stem = module.Replace('.', '/');
        return [$"{stem}.py", $"{stem}/__init__.py"];
    }

    /// <summary>
    /// Imports the game's decompiled Python scripts, as Motherlode writes them, into the specified <paramref name="snapshot"/>, replacing any it already has (💤)
    /// </summary>
    /// <remarks>
    /// <paramref name="folder"/> is Motherlode's output folder (containing <c>scripts</c> and <c>motherlode-report.json</c>) or its <c>scripts</c> folder.
    /// Every <c>.py</c> file under <c>scripts</c> is imported, keyed by its path relative to it; Motherlode's report, if present, is imported with them, so that the functions it could not verify can be shown.
    /// </remarks>
    /// <exception cref="NotSupportedException">The archive was opened for reading only</exception>
    public async Task<TuningArchiveCaptureResult> ImportPythonAsync(TuningArchiveSnapshot snapshot, string folder, IProgress<TuningArchiveProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(folder);
        ThrowIfReadOnly();
        var scripts = Directory.Exists(System.IO.Path.Combine(folder, "scripts")) ? System.IO.Path.Combine(folder, "scripts") : folder;
        if (!Directory.Exists(scripts))
            throw new DirectoryNotFoundException($"There is no folder at {folder}");
        var report = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(scripts))!, MotherlodeReportPath);
        await writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await Task.Run(() =>
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var problems = new List<string>();
                var files = Directory.EnumerateFiles(scripts, "*.py", SearchOption.AllDirectories).Select(file => (file, path: System.IO.Path.GetRelativePath(scripts, file).Replace('\\', '/'))).ToList();
                if (File.Exists(report))
                    files.Add((report, MotherlodeReportPath));
                progress?.Report(new("Reading Python modules", 0, files.Count));
                var read = 0;
                var documents = files.AsParallel().AsOrdered().WithCancellation(cancellationToken).Select(entry =>
                {
                    var content = File.ReadAllBytes(entry.file);
                    var done = Interlocked.Increment(ref read);
                    if (done % 100 == 0)
                        progress?.Report(new("Reading Python modules", done, files.Count));
                    var module = entry.path == MotherlodeReportPath ? null : entry.path[..^".py".Length].Replace('/', '.').Replace(".__init__", string.Empty, StringComparison.Ordinal);
                    return new IngestDocument(0, 0, GetPythonModuleInstance(entry.path), entry.path, null, module, "BG", null, content, BlobHash.Compute(content));
                }).ToList();
                using var connection = OpenConnection();
                var ingestor = new Ingestor(this, connection);
                var manifest = new Dictionary<long, ResourceContent>();
                ingestor.Ingest(TuningArchiveResourceKind.PythonModule, documents, manifest, problems, progress, "Storing Python modules");
                using (var transaction = connection.BeginTransaction())
                {
                    ApplyResourceScope(connection, transaction, snapshot.Id, snapshot.Ordinal, TuningArchiveResourceKind.PythonModule, manifest);
                    transaction.Commit();
                }
                return new TuningArchiveCaptureResult(snapshot, 0, 0, 0, 0, ingestor.NewContent, ingestor.NewContentBytes, problems, stopwatch.Elapsed);
            }, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            writeLock.Release();
        }
    }

    /// <summary>
    /// Gets the decompiled source of the specified Python <paramref name="module"/> (such as <c>buffs.buff</c>) in the specified <paramref name="snapshot"/>, if the snapshot has the game's scripts
    /// </summary>
    public TuningArchiveEntry? GetPythonModule(TuningArchiveSnapshot snapshot, string module)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(module);
        foreach (var path in GetPythonModulePaths(module))
            if (GetEntry(snapshot, TuningArchiveResourceKind.PythonModule, 0, GetPythonModuleInstance(path)) is { } entry)
                return entry;
        return null;
    }
}