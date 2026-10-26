namespace LlamaLogic.TuningArchive;

/// <summary>
/// Represents an installation of The Sims 4 from which a <see cref="TuningArchiveFile"/> can capture a snapshot (🔒)
/// </summary>
public sealed partial class GameInstallation
{
    const string macBundleIdentifier = "com.ea.mac.thesims4";
    const string steamAppId = "1222670";

    static readonly string[] windowsRelativeInstallations =
    [
        System.IO.Path.Combine("Program Files", "EA Games", "The Sims 4"),
        System.IO.Path.Combine("Program Files (x86)", "EA Games", "The Sims 4"),
        System.IO.Path.Combine("Program Files (x86)", "Origin Games", "The Sims 4"),
        System.IO.Path.Combine("Program Files", "Origin Games", "The Sims 4"),
        System.IO.Path.Combine("Program Files (x86)", "Steam", "steamapps", "common", "The Sims 4"),
        System.IO.Path.Combine("Program Files", "Steam", "steamapps", "common", "The Sims 4"),
        System.IO.Path.Combine("SteamLibrary", "steamapps", "common", "The Sims 4"),
        System.IO.Path.Combine("EA Games", "The Sims 4"),
        System.IO.Path.Combine("Origin Games", "The Sims 4"),
        System.IO.Path.Combine("Games", "The Sims 4")
    ];

#if IS_NET_7_0_OR_GREATER
    [GeneratedRegex(@"^(EP|GP|SP|FP)\d+$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex GetPackFolderPattern();
#else
    static readonly Regex packFolderPattern = new(@"^(EP|GP|SP|FP)\d+$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);

    static Regex GetPackFolderPattern() =>
        packFolderPattern;
#endif

#if IS_NET_7_0_OR_GREATER
    [GeneratedRegex(@"^Strings_(?<locale>[A-Z]{3}_[A-Z]{2})\.package$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex GetStringTablePackagePattern();
#else
    static readonly Regex stringTablePackagePattern = new(@"^Strings_(?<locale>[A-Z]{3}_[A-Z]{2})\.package$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);

    static Regex GetStringTablePackagePattern() =>
        stringTablePackagePattern;
#endif

#if IS_NET_7_0_OR_GREATER
    [GeneratedRegex(@"""installdir""\s+""(?<installdir>[^""]+)""", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex GetSteamInstallDirectoryPattern();
#else
    static readonly Regex steamInstallDirectoryPattern = new(@"""installdir""\s+""(?<installdir>[^""]+)""", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);

    static Regex GetSteamInstallDirectoryPattern() =>
        steamInstallDirectoryPattern;
#endif

#if IS_NET_7_0_OR_GREATER
    [GeneratedRegex(@"""path""\s+""(?<path>[^""]+)""", RegexOptions.CultureInvariant)]
    private static partial Regex GetSteamLibraryPathPattern();
#else
    static readonly Regex steamLibraryPathPattern = new(@"""path""\s+""(?<path>[^""]+)""", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    static Regex GetSteamLibraryPathPattern() =>
        steamLibraryPathPattern;
#endif

    static IEnumerable<string> EnumerateDirectories(string path)
    {
        try
        {
            return [.. Directory.EnumerateDirectories(path).OrderBy(directory => directory, StringComparer.Ordinal)];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return [];
        }
    }

    static string? FindContentRoot(string path)
    {
        foreach (var candidate in new[] { path, System.IO.Path.Combine(path, "Contents"), System.IO.Path.Combine(path, "The Sims 4.app", "Contents") })
            if (Directory.Exists(System.IO.Path.Combine(candidate, "Data", "Simulation")))
                return candidate;
        return null;
    }

    /// <summary>
    /// Finds the installations of The Sims 4 in the places the game's installers usually put it and, on macOS, wherever Spotlight finds its app bundle
    /// </summary>
    public static IReadOnlyList<string> FindInstallations()
    {
        var candidates = new List<string>();
        if (OperatingSystem.IsWindows())
        {
            foreach (var registryKey in new[] { @"SOFTWARE\WOW6432Node\Maxis\The Sims 4", @"SOFTWARE\Maxis\The Sims 4" })
                foreach (var hive in new[] { Microsoft.Win32.Registry.LocalMachine, Microsoft.Win32.Registry.CurrentUser })
                {
                    try
                    {
                        using var key = hive.OpenSubKey(registryKey);
                        if (key?.GetValue("Install Dir") is string installDirectory)
                            candidates.Add(installDirectory);
                    }
                    catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
                    {
                    }
                }
            try
            {
                using var steamKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
                if (steamKey?.GetValue("SteamPath") is string steamPath)
                    candidates.AddRange(GetSteamInstallations(System.IO.Path.GetFullPath(steamPath)));
            }
            catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
            {
            }
            foreach (var drive in DriveInfo.GetDrives().Where(drive => drive.DriveType is DriveType.Fixed))
                candidates.AddRange(windowsRelativeInstallations.Select(relative => System.IO.Path.Combine(drive.RootDirectory.FullName, relative)));
        }
        else if (OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var spotlightBundles = FindMacBundlesWithSpotlight().ToList();
            candidates.AddRange(spotlightBundles);
            foreach (var applications in GetMacApplicationsFolders(home))
            {
                candidates.Add(System.IO.Path.Combine(applications, "The Sims 4.app"));
                candidates.AddRange(EnumerateDirectories(applications).Select(directory => System.IO.Path.Combine(directory, "The Sims 4.app")));
                if (spotlightBundles.Count is 0)
                    foreach (var folder in new[] { applications, System.IO.Path.Combine(applications, "EA Games") }.Where(Directory.Exists))
                        candidates.AddRange(EnumerateDirectories(folder).Where(directory => directory.EndsWith(".app", StringComparison.OrdinalIgnoreCase) && IsMacBundle(directory)));
            }
            candidates.AddRange(GetSteamInstallations(System.IO.Path.Combine(home, "Library", "Application Support", "Steam")));
        }
        var installations = new List<string>();
        var seenContentRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in candidates)
        {
            string fullPath;
            try
            {
                fullPath = System.IO.Path.GetFullPath(candidate);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException or System.Security.SecurityException)
            {
                continue;
            }
            if (Directory.Exists(fullPath) && FindContentRoot(fullPath) is { } contentRoot && seenContentRoots.Add(System.IO.Path.TrimEndingDirectorySeparator(contentRoot)))
                installations.Add(fullPath);
        }
        return installations;
    }

    static IEnumerable<string> FindMacBundlesWithSpotlight()
    {
        string output;
        try
        {
            using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("/usr/bin/mdfind")
            {
                ArgumentList = { $"kMDItemCFBundleIdentifier == '{macBundleIdentifier}'" },
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            });
            if (process is null)
                return [];
            output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode is not 0)
                return [];
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or IOException or PlatformNotSupportedException)
        {
            return [];
        }
        return [.. output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(bundle => !bundle.Contains("/.Trash/", StringComparison.Ordinal) && !bundle.Contains("/.Trashes/", StringComparison.Ordinal) && IsMacBundle(bundle))];
    }

    static IEnumerable<string> GetMacApplicationsFolders(string home)
    {
        var applicationsFolders = new List<string> { "/Applications", System.IO.Path.Combine(home, "Applications") };
        applicationsFolders.AddRange(EnumerateDirectories("/Volumes").Where(volume => new DirectoryInfo(volume).LinkTarget is null).Select(volume => System.IO.Path.Combine(volume, "Applications")));
        return applicationsFolders.Where(Directory.Exists);
    }

    static string GetPacksRoot(string contentRoot)
    {
        if (System.IO.Path.GetDirectoryName(System.IO.Path.TrimEndingDirectorySeparator(contentRoot)) is { } bundle
            && bundle.EndsWith(".app", StringComparison.OrdinalIgnoreCase)
            && System.IO.Path.GetDirectoryName(bundle) is { } bundleParent
            && System.IO.Path.Combine(bundleParent, "The Sims 4 Packs") is { } macPacksRoot
            && Directory.Exists(macPacksRoot))
            return macPacksRoot;
        return contentRoot;
    }

    static IEnumerable<string> GetSteamInstallations(string steamRoot) =>
        GetSteamLibraries(steamRoot).Select(library =>
        {
            var installDirectory = "The Sims 4";
            try
            {
                if (GetSteamInstallDirectoryPattern().Match(File.ReadAllText(System.IO.Path.Combine(library, "steamapps", $"appmanifest_{steamAppId}.acf"))) is { Success: true } match)
                    installDirectory = match.Groups["installdir"].Value;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
            {
            }
            return System.IO.Path.Combine(library, "steamapps", "common", installDirectory);
        });

    static IEnumerable<string> GetSteamLibraries(string steamRoot)
    {
        yield return steamRoot;
        foreach (var libraryFolders in new[] { System.IO.Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf"), System.IO.Path.Combine(steamRoot, "config", "libraryfolders.vdf") })
        {
            string text;
            try
            {
                text = File.ReadAllText(libraryFolders);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                continue;
            }
            foreach (Match match in GetSteamLibraryPathPattern().Matches(text))
                yield return match.Groups["path"].Value.Replace(@"\\", @"\", StringComparison.Ordinal);
        }
    }

    static bool IsMacBundle(string bundle)
    {
        var infoPropertyList = System.IO.Path.Combine(bundle, "Contents", "Info.plist");
        if (!File.Exists(infoPropertyList))
            return false;
        try
        {
            using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("/usr/libexec/PlistBuddy")
            {
                ArgumentList = { "-c", "Print :CFBundleIdentifier", infoPropertyList },
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            });
            if (process is null)
                return false;
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return process.ExitCode is 0 && string.Equals(output.Trim(), macBundleIdentifier, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or IOException or PlatformNotSupportedException)
        {
            return false;
        }
    }

    static string? ReadIniValue(string path, string section, string key)
    {
        if (!File.Exists(path))
            return null;
        var currentSection = string.Empty;
        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                currentSection = line[1..^1];
                continue;
            }
            if (!string.Equals(currentSection, section, StringComparison.OrdinalIgnoreCase))
                continue;
            var separator = line.IndexOf('=', StringComparison.Ordinal);
            if (separator > 0 && string.Equals(line[..separator].Trim(), key, StringComparison.OrdinalIgnoreCase))
                return line[(separator + 1)..].Trim();
        }
        return null;
    }

    /// <summary>
    /// Opens the installation of The Sims 4 at the specified <paramref name="path"/>, which may be the installation folder, the app bundle on macOS, or the folder containing it
    /// </summary>
    /// <remarks>
    /// On macOS, packs are read from the <c>The Sims 4 Packs</c> folder beside the app bundle, where the game installs them
    /// </remarks>
    /// <exception cref="DirectoryNotFoundException">No installation of The Sims 4 was found at <paramref name="path"/></exception>
    public static GameInstallation Open(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (FindContentRoot(path) is not { } contentRoot)
            throw new DirectoryNotFoundException($"No installation of The Sims 4 was found at {path}");
        var gameVersion = new[] { System.IO.Path.Combine("Game", "Bin", "Default.ini"), System.IO.Path.Combine("Resources", "Default.ini"), System.IO.Path.Combine("Bin", "Default.ini") }
            .Select(relative => ReadIniValue(System.IO.Path.Combine(contentRoot, relative), "Version", "gameversion"))
            .FirstOrDefault(version => version is not null);
        var packs = new List<GameInstallationPack>
        {
            OpenPack("BG", null, System.IO.Path.Combine(contentRoot, "Data", "Simulation"), System.IO.Path.Combine(contentRoot, "Data", "Client"))
        };
        var deltaRoot = System.IO.Path.Combine(contentRoot, "Delta");
        var packsRoot = GetPacksRoot(contentRoot);
        var packCodes = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var directory in new[] { packsRoot, deltaRoot }.Where(Directory.Exists).SelectMany(EnumerateDirectories))
            if (System.IO.Path.GetFileName(directory) is { } name && GetPackFolderPattern().IsMatch(name) && Directory.Exists(System.IO.Path.Combine(packsRoot, name)))
                packCodes.Add(name.ToUpperInvariant());
        foreach (var code in packCodes)
        {
            var packDirectory = EnumerateDirectories(packsRoot).First(directory => string.Equals(System.IO.Path.GetFileName(directory), code, StringComparison.OrdinalIgnoreCase));
            var deltaDirectory = Directory.Exists(deltaRoot)
                ? Directory.EnumerateDirectories(deltaRoot).FirstOrDefault(directory => string.Equals(System.IO.Path.GetFileName(directory), code, StringComparison.OrdinalIgnoreCase))
                : null;
            var pack = OpenPack(code, deltaDirectory, packDirectory, null);
            if (pack.SimulationPackages.Count > 0)
                packs.Add(pack);
        }
        return new(path, contentRoot, gameVersion, packs);
    }

    static GameInstallationPack OpenPack(string code, string? deltaDirectory, string directory, string? clientDirectory)
    {
        var simulationPackages = new List<(GameInstallationPackage package, int directoryRank)>();
        var clientPackages = new List<(GameInstallationPackage package, int directoryRank)>();
        var stringTablePackages = new Dictionary<string, List<(GameInstallationPackage package, int directoryRank)>>(StringComparer.OrdinalIgnoreCase);
        var directories = new List<(string directory, int rank, string simulationConfiguration, string clientConfiguration)>();
        if (deltaDirectory is not null)
            directories.Add((deltaDirectory, 0, "ResourceSimulation.cfg", "ResourceClient.cfg"));
        if (clientDirectory is null)
            directories.Add((directory, 1, "ResourceSimulation.cfg", "ResourceClient.cfg"));
        else
        {
            directories.Add((directory, 1, "Resource.cfg", string.Empty));
            directories.Add((clientDirectory, 2, string.Empty, "Resource.cfg"));
        }
        foreach (var (packageDirectory, rank, simulationConfiguration, clientConfiguration) in directories)
        {
            if (!Directory.Exists(packageDirectory))
                continue;
            var simulationPriorities = simulationConfiguration.Length > 0 ? ResourceConfiguration.Read(System.IO.Path.Combine(packageDirectory, simulationConfiguration)) : null;
            var clientPriorities = clientConfiguration.Length > 0 ? ResourceConfiguration.Read(System.IO.Path.Combine(packageDirectory, clientConfiguration)) : null;
            foreach (var file in Directory.EnumerateFiles(packageDirectory, "*.package"))
            {
                var fileName = System.IO.Path.GetFileName(file);
                if (fileName.StartsWith("Simulation", StringComparison.OrdinalIgnoreCase) && (simulationConfiguration.Length > 0 || clientConfiguration.Length > 0 && clientDirectory is null))
                {
                    var priority = simulationPriorities?.GetPriority(fileName) ?? ResourceConfiguration.GetDefaultPriority(fileName, rank == 0);
                    if (priority is { } p)
                        simulationPackages.Add((new(file, p, rank == 0), rank));
                }
                else if (GetStringTablePackagePattern().Match(fileName) is { Success: true } match && clientConfiguration.Length > 0)
                {
                    var priority = clientPriorities?.GetPriority(fileName) ?? ResourceConfiguration.GetDefaultPriority(fileName, rank == 0);
                    if (priority is { } p)
                    {
                        var locale = match.Groups["locale"].Value.ToUpperInvariant();
                        if (!stringTablePackages.TryGetValue(locale, out var localePackages))
                        {
                            localePackages = [];
                            stringTablePackages.Add(locale, localePackages);
                        }
                        localePackages.Add((new(file, p, rank == 0), rank));
                    }
                }
                else if (clientConfiguration.Length > 0 && !fileName.StartsWith("Simulation", StringComparison.OrdinalIgnoreCase))
                {
                    var priority = clientPriorities?.GetPriority(fileName) ?? ResourceConfiguration.GetDefaultPriority(fileName, rank == 0);
                    if (priority is { } p)
                        clientPackages.Add((new(file, p, rank == 0), rank));
                }
            }
        }
        static IReadOnlyList<GameInstallationPackage> order(IEnumerable<(GameInstallationPackage package, int directoryRank)> packages) =>
            [.. packages.OrderByDescending(entry => entry.package.Priority).ThenBy(entry => entry.directoryRank).ThenByDescending(entry => entry.package.Path, StringComparer.OrdinalIgnoreCase).Select(entry => entry.package)];
        var version = new[] { deltaDirectory, directory }
            .Where(versionDirectory => versionDirectory is not null)
            .Select(versionDirectory => ReadIniValue(System.IO.Path.Combine(versionDirectory!, "Version.ini"), "Version", "packversion"))
            .FirstOrDefault(packVersion => packVersion is not null);
        return new(code, version, order(simulationPackages), order(clientPackages), stringTablePackages.ToDictionary(entry => entry.Key, entry => order(entry.Value), StringComparer.OrdinalIgnoreCase));
    }

    GameInstallation(string path, string contentRoot, string? gameVersion, IReadOnlyList<GameInstallationPack> packs)
    {
        Path = path;
        ContentRoot = contentRoot;
        GameVersion = gameVersion;
        Packs = packs;
    }

    /// <summary>
    /// Gets the folder containing the installation's <c>Data</c> folder
    /// </summary>
    public string ContentRoot { get; }

    /// <summary>
    /// Gets the version of the game, as the installation's configuration states it, if it could be found
    /// </summary>
    public string? GameVersion { get; }

    /// <summary>
    /// Gets the locales for which the base game has string tables (such as <c>ENG_US</c>)
    /// </summary>
    public IReadOnlyCollection<string> Locales =>
        Packs[0].Locales;

    /// <summary>
    /// Gets the base game (always first) and the packs that are installed
    /// </summary>
    public IReadOnlyList<GameInstallationPack> Packs { get; }

    /// <summary>
    /// Gets the path with which the installation was opened
    /// </summary>
    public string Path { get; }
}