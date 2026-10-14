namespace LlamaLogic.TuningDescriptions;

/// <summary>
/// Represents a set of tuning descriptions for a version of The Sims 4, as Maxis distributes it (🔒)
/// </summary>
/// <remarks>
/// <para>
/// A set is indexed by the paths of its documents when it is opened: instances are <c>*/Descriptions/{class}.tdesc</c>, modules are <c>Tuning/{module with . replaced by -}.tdesc</c>, and fragments are <c>TdescFrag/{class}.tdescfrag</c>.
/// A document is read the first time it is requested, and it may be read again once nothing refers to it any longer, so requesting a document twice does not necessarily return the same object.
/// </para>
/// <para>
/// A set's members are safe to use from multiple threads.
/// </para>
/// </remarks>
public sealed class TuningDescriptionSet :
    IDisposable
{
    static readonly Regex gameVersionFileNamePattern = new(@"^(?<version>\d+\.\d+\.\d+\.\d+)-TDESC\.zip$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    /// <summary>
    /// Opens the set of tuning descriptions extracted to the directory at the specified <paramref name="path"/> (🗑️)
    /// </summary>
    /// <exception cref="DirectoryNotFoundException">There is no directory at <paramref name="path"/></exception>
    public static TuningDescriptionSet FromDirectory(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        var root = new DirectoryInfo(path);
        if (!root.Exists)
            throw new DirectoryNotFoundException($"There is no directory at {path}");
        var documentPaths = root.EnumerateFiles("*", SearchOption.AllDirectories)
            .Select(file => System.IO.Path.GetRelativePath(root.FullName, file.FullName).Replace(System.IO.Path.DirectorySeparatorChar, '/'));
        return new(documentPaths, documentPath => File.ReadAllBytes(System.IO.Path.Combine(root.FullName, documentPath.Replace('/', System.IO.Path.DirectorySeparatorChar))), null, null);
    }

    /// <summary>
    /// Opens the ZIP archive of tuning descriptions at the specified <paramref name="path"/> (🗑️)
    /// </summary>
    /// <remarks>
    /// When the name of the file is that of a Maxis distribution (such as <c>1.128.90.1030-TDESC.zip</c>), <see cref="GameVersion"/> is the version it names.
    /// </remarks>
    public static TuningDescriptionSet FromZip(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
#pragma warning disable CA2000 // Dispose objects before losing scope
        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
#pragma warning restore CA2000 // Dispose objects before losing scope
        try
        {
            var gameVersion = gameVersionFileNamePattern.Match(System.IO.Path.GetFileName(path)) is { Success: true } match
                ? Version.Parse(match.Groups["version"].Value)
                : null;
            return FromZip(stream, stream, gameVersion);
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Opens the ZIP archive of tuning descriptions in the specified <paramref name="stream"/>, which must remain open and seekable until the set is disposed (🗑️)
    /// </summary>
    public static TuningDescriptionSet FromZip(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return FromZip(stream, null, null);
    }

    static TuningDescriptionSet FromZip(Stream stream, IDisposable? ownedStream, Version? gameVersion)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        var archive = ZipArchive.OpenArchive(stream, new SharpCompress.Readers.ReaderOptions { LeaveStreamOpen = true });
#pragma warning restore CA2000 // Dispose objects before losing scope
        try
        {
            var entries = archive.Entries.Where(entry => !entry.IsDirectory && entry.Key is not null).ToDictionary(entry => entry.Key!.Replace('\\', '/'), StringComparer.Ordinal);
            var archiveLock = new object();
            byte[] read(string documentPath)
            {
                lock (archiveLock)
                {
                    using var entryStream = entries[documentPath].OpenEntryStream();
                    using var buffer = new MemoryStream();
                    entryStream.CopyTo(buffer);
                    return buffer.ToArray();
                }
            }
            return new(entries.Keys, read, gameVersion, () =>
            {
                archive.Dispose();
                ownedStream?.Dispose();
            });
        }
        catch
        {
            archive.Dispose();
            throw;
        }
    }

    TuningDescriptionSet(IEnumerable<string> documentPaths, Func<string, byte[]> read, Version? gameVersion, Action? dispose)
    {
        this.read = read;
        this.dispose = dispose;
        GameVersion = gameVersion;
        var instancePaths = new Dictionary<string, string>(StringComparer.Ordinal);
        var modulePaths = new Dictionary<string, string>(StringComparer.Ordinal);
        var fragmentPaths = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var documentPath in documentPaths)
        {
            var segments = documentPath.Split('/');
            var fileName = segments[^1];
            if (segments.Length == 3 && segments[1] == "Descriptions" && fileName.EndsWith(".tdesc", StringComparison.OrdinalIgnoreCase))
                instancePaths[fileName[..^".tdesc".Length]] = documentPath;
            else if (segments.Length == 2 && segments[0] == "Tuning" && fileName.EndsWith(".tdesc", StringComparison.OrdinalIgnoreCase))
                modulePaths[fileName[..^".tdesc".Length]] = documentPath;
            else if (segments.Length == 2 && segments[0] == "TdescFrag" && fileName.EndsWith(".tdescfrag", StringComparison.OrdinalIgnoreCase))
                fragmentPaths[fileName[..^".tdescfrag".Length]] = documentPath;
        }
        this.instancePaths = instancePaths;
        this.modulePaths = modulePaths;
        this.fragmentPaths = fragmentPaths;
        ModuleNames = [.. modulePaths.Keys.Select(stem => stem.Replace('-', '.'))];
    }

    readonly Action? dispose;
    readonly ConcurrentDictionary<string, WeakReference<TuningDescriptionDocument>> documents = new(StringComparer.Ordinal);
    readonly Dictionary<string, string> fragmentPaths;
    readonly Dictionary<string, string> instancePaths;
    readonly Dictionary<string, string> modulePaths;
    readonly Func<string, byte[]> read;
    int isDisposed;

    /// <summary>
    /// Gets the names of the classes of the set's fragments
    /// </summary>
    public IReadOnlyCollection<string> FragmentClasses =>
        fragmentPaths.Keys;

    /// <summary>
    /// Gets the version of the game the set describes, if it was opened from a file named as Maxis names them
    /// </summary>
    public Version? GameVersion { get; }

    /// <summary>
    /// Gets the names of the classes of the set's instances
    /// </summary>
    public IReadOnlyCollection<string> InstanceClasses =>
        instancePaths.Keys;

    /// <summary>
    /// Gets the names of the set's modules
    /// </summary>
    public IReadOnlyCollection<string> ModuleNames { get; }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref isDisposed, 1) == 0)
            dispose?.Invoke();
    }

    /// <summary>
    /// Gets the enumeration to which an <see cref="EnumTunableDescription.StaticEntries"/> attribute refers, such as <c>sims-sim_info_types.Age</c>, or <see langword="null"/> if the set does not describe it
    /// </summary>
    /// <exception cref="InvalidDataException">The module that would describe the enumeration is malformed</exception>
    public TuningDescriptionClass? GetEnum(string staticEntries)
    {
        ArgumentNullException.ThrowIfNull(staticEntries);
        ThrowIfDisposed();
        var segments = staticEntries.Split('.');
        if (segments.Length < 2 || !modulePaths.TryGetValue(segments[0], out var path))
            return null;
        var @class = GetDocument(path, (TuningDescriptionModule module) => module.Name.Replace('.', '-') == segments[0] ? module : throw new InvalidDataException($"{path} describes module {module.Name}")).GetClass(segments[1]);
        for (var i = 2; @class is not null && i < segments.Length; ++i)
            @class = @class.GetClass(segments[i]);
        return @class is { IsEnum: true } ? @class : null;
    }

    /// <summary>
    /// Gets the fragment with the specified <paramref name="className"/>, or <see langword="null"/> if the set does not have it
    /// </summary>
    /// <exception cref="InvalidDataException">The fragment's document is malformed or describes a different fragment</exception>
    public TuningDescriptionFragment? GetFragment(string className)
    {
        ArgumentNullException.ThrowIfNull(className);
        ThrowIfDisposed();
        return fragmentPaths.TryGetValue(className, out var path)
            ? GetDocument(path, (TuningDescriptionFragment fragment) => fragment.Class == className ? fragment : throw new InvalidDataException($"{path} describes fragment {fragment.Class}, not {className}"))
            : null;
    }

    /// <summary>
    /// Gets the instance with the specified <paramref name="className"/>, or <see langword="null"/> if the set does not have it
    /// </summary>
    /// <exception cref="InvalidDataException">The instance's document is malformed or describes a different class</exception>
    public TuningDescriptionInstance? GetInstance(string className)
    {
        ArgumentNullException.ThrowIfNull(className);
        ThrowIfDisposed();
        return instancePaths.TryGetValue(className, out var path)
            ? GetDocument(path, (TuningDescriptionInstance instance) => instance.Class == className ? instance : throw new InvalidDataException($"{path} describes class {instance.Class}, not {className}"))
            : null;
    }

    /// <summary>
    /// Gets the module with the specified <paramref name="name"/>, such as <c>sims.sim_info_types</c>, or <see langword="null"/> if the set does not have it
    /// </summary>
    /// <exception cref="InvalidDataException">The module's document is malformed or describes a different module</exception>
    public TuningDescriptionModule? GetModule(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        ThrowIfDisposed();
        return modulePaths.TryGetValue(name.Replace('.', '-'), out var path)
            ? GetDocument(path, (TuningDescriptionModule module) => module.Name == name ? module : throw new InvalidDataException($"{path} describes module {module.Name}, not {name}"))
            : null;
    }

    /// <summary>
    /// Gets the description the specified <paramref name="description"/> stands for: the description of the fragment it refers to if it is a <see cref="FragmentReferenceTunableDescription"/>, and otherwise itself
    /// </summary>
    /// <exception cref="InvalidDataException">The set does not have the fragment to which <paramref name="description"/> refers</exception>
    public TunableDescription Resolve(TunableDescription description)
    {
        ArgumentNullException.ThrowIfNull(description);
        ThrowIfDisposed();
        return description is FragmentReferenceTunableDescription reference
            ? (reference.Class is { } className ? GetFragment(className) : null)?.Description ?? throw new InvalidDataException($"The set does not have fragment {reference.Class}")
            : description;
    }

    TDocument GetDocument<TDocument>(string path, Func<TDocument, TDocument> validate)
        where TDocument : TuningDescriptionDocument
    {
        if (documents.TryGetValue(path, out var reference) && reference.TryGetTarget(out var cached))
            return cached as TDocument ?? throw new InvalidDataException($"{path} is a {cached.GetType().Name}, not a {typeof(TDocument).Name}");
        XDocument xml;
        try
        {
            using var stream = new MemoryStream(read(path));
            xml = XDocument.Load(stream);
        }
        catch (System.Xml.XmlException ex)
        {
            throw new InvalidDataException($"{path} is not well-formed XML: {ex.Message}", ex);
        }
        if (xml.Root is not { Name.LocalName: "TuningRoot" } root)
            throw new InvalidDataException($"The root element of {path} is not TuningRoot");
        var versionHistory = root.Element("VersionHistory");
        var source = root.Elements().FirstOrDefault(element => element.Name.LocalName is "Instance" or "Module" or "TdescFrag")
            ?? throw new InvalidDataException($"{path} does not contain an Instance, Module, or TdescFrag");
        TuningDescriptionDocument document = source.Name.LocalName switch
        {
            "Instance" => new TuningDescriptionInstance(this, path, source, versionHistory),
            "Module" => new TuningDescriptionModule(this, path, source, versionHistory),
            _ => new TuningDescriptionFragment(this, path, source, versionHistory)
        };
        var typed = validate(document as TDocument ?? throw new InvalidDataException($"{path} is a {document.GetType().Name}, not a {typeof(TDocument).Name}"));
        documents[path] = new(typed);
        return typed;
    }

    void ThrowIfDisposed()
    {
        if (Volatile.Read(ref isDisposed) != 0)
            throw new ObjectDisposedException(nameof(TuningDescriptionSet));
    }
}
