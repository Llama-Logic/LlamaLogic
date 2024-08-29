namespace LlamaLogic.LlamaPad;

class PythonInterop
{
    static PythonSynchronizationContext? pythonSynchronizationContext;

    public static string? Build { get; private set; }

    public static string? Compiler { get; private set; }

    public static bool IsInitialized { get; private set; }

    public static Version? Version { get; private set; }

    public static event EventHandler? Initialized;

    public static async Task InitializeAsync(InitializationDialog dialog)
    {
        if (pythonSynchronizationContext is not null)
            throw new InvalidOperationException("Python has already been initialized");
#if WINDOWS
        var downloadUrl = "https://www.python.org/ftp/python/3.12.4/python-3.12.4-embed-amd64.zip";
#elif MACCATALYST
        var downloadUrl = "https://www.python.org/ftp/python/3.12.4/python-3.12.4-macos11.pkg";
#else
        throw new NotSupportedException("The host operating system is not supported");
#endif
        Installer.Source = new Installer.DownloadInstallationSource
        {
            DownloadUrl = downloadUrl
        };
        dialog.SetState("Detecting host environment", 0, 0, 4);
        dialog.SetState("Looking for Python", 0, 0, 4);
        var isPythonInstalled = Installer.IsPythonInstalled();
        if (!isPythonInstalled)
        {
            dialog.SetState("Setting up Python", 0, 1, 4);
            await Task.Run(async () => await Installer.SetupPython());
        }
        dialog.SetState("Looking for pip", 1, 1, 4);
        var isPipInstalled = Installer.IsPipInstalled();
        if (!isPipInstalled)
        {
            dialog.SetState("Setting up pip", 1, 2, 4);
            await Task.Run(async () => await Installer.TryInstallPip());
        }
        dialog.SetState("Looking for lxml", 2, 2, 4);
        var isLxmlInstalled = Installer.IsModuleInstalled("lxml");
        if (!isLxmlInstalled)
        {
            dialog.SetState("Installing lxml", 2, 3, 4);
            await Task.Run(async () => await Installer.PipInstallModule("lxml"));
        }
        pythonSynchronizationContext = new();
        dialog.SetState("Initializing CPython", 3, 4, 4);
        await pythonSynchronizationContext.PostAsync(() =>
        {
            PyObjectConversions.RegisterDecoder(EnumerableDecoder.Instance);
            PyObjectConversions.RegisterDecoder(IndexDecoder.Instance);
            PyObjectConversions.RegisterDecoder(RangeDecoder.Instance);
#if WINDOWS
            var pythonCliVersionInfo = FileVersionInfo.GetVersionInfo(Path.Combine(Installer.EmbeddedPythonHome, "python.exe"));
            Runtime.PythonDLL = Path.Combine(Installer.EmbeddedPythonHome, $"python{pythonCliVersionInfo.FileMajorPart}{pythonCliVersionInfo.FileMinorPart}.dll");
#elif MACCATALYST
            throw new NotImplementedException("The macOS implementation of determining the location of the Python DLL is not complete");
#else
            throw new NotSupportedException("The host operating system is not supported");
#endif
            PythonEngine.PythonHome = Installer.EmbeddedPythonHome;
            PythonEngine.Initialize();
            using (Py.GIL())
            {
                using dynamic sys = Py.Import("sys");
                string versionMoniker = sys.version.ToString();
                if (Regex.Match(versionMoniker, @"^(?<version>(\d+\.){2}\d+)\s+\((?<build>.*?)\)\s+\[(?<compiler>.*?)\]$") is { } match)
                {
                    Version = new Version(match.Groups["version"].Value);
                    Build = match.Groups["build"].Value;
                    Compiler = match.Groups["compiler"].Value;
                    IsInitialized = true;
                }
            }
        });
        Initialized?.Invoke(null, EventArgs.Empty);
    }
}
