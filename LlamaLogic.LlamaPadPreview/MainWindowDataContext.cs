namespace LlamaLogic.LlamaPadPreview;

class MainWindowDataContext :
    INotifyPropertyChanged
{
    public MainWindowDataContext(MainWindow mainWindow)
    {
        if (Debugger.IsAttached)
            typeof(DataBasePackedFile).ToString(); // force the LlamaLogic.Packages assembly to load so the debugger has symbols
        EditorFileOperationManualResetEvent = new(true);
        editorFileSystemWatcherLock = new();
        editorFileSystemWatcher = new()
        {
            Filter = "*.*",
            IncludeSubdirectories = false,
            NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
        };
        editorFileSystemWatcher.Changed += FileChanged;
        editorFileSystemWatcher.Created += FileCreated;
        editorFileSystemWatcher.Deleted += FileDeleted;
        editorFileSystemWatcher.Renamed += FileRenamed;
        pythonProgressIsIndeterminate = true;
        pythonSynchronizationContext = new();
        this.mainWindow = mainWindow;
        this.mainWindow.ContentRendered += ContentRendered;
        this.mainWindow.Closing += Closing;
        this.mainWindow.Closed += Closed;
    }

    ulong? activePythonThread;
    string? editorFileName;
    readonly FileSystemWatcher editorFileSystemWatcher;
    readonly AsyncLock editorFileSystemWatcherLock;
    readonly MainWindow mainWindow;
    string? pythonExceptionMessage;
    bool pythonProgressIsIndeterminate;
    double pythonProgressValue;
    string? pythonScript;
    string? pythonScriptInFile;
    string? pythonStatus;
    readonly PythonSynchronizationContext pythonSynchronizationContext;
    string? pythonVersion;
    string? selectedPythonScript;

    public readonly AsyncManualResetEvent EditorFileOperationManualResetEvent;

    public string? EditorFileName
    {
        get => editorFileName;
        set
        {
            editorFileName = value;
            OnPropertyChanged();
            if (value is null)
                editorFileSystemWatcher.EnableRaisingEvents = false;
            else if (Path.GetDirectoryName(value) is { } nonNullDirectory)
            {
                editorFileSystemWatcher.EnableRaisingEvents = false;
                editorFileSystemWatcher.Path = nonNullDirectory;
                PythonScriptInFile = pythonScript;
                editorFileSystemWatcher.EnableRaisingEvents = true;
            }
        }
    }

    public bool PythonCanBeInterrupted =>
        activePythonThread is not null;

    public bool PythonExceptionEncountered =>
        PythonExceptionMessage is not null;

    public string? PythonExceptionMessage
    {
        get => pythonExceptionMessage;
        private set
        {
            pythonExceptionMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PythonExceptionEncountered));
        }
    }

    public bool PythonProgressIsIndeterminate
    {
        get => pythonProgressIsIndeterminate;
        set
        {
            pythonProgressIsIndeterminate = value;
            OnPropertyChanged();
        }
    }

    public double PythonProgressValue
    {
        get => pythonProgressValue;
        set
        {
            pythonProgressValue = value;
            OnPropertyChanged();
        }
    }

    public string? PythonScript
    {
        get => pythonScript;
        set
        {
            pythonScript = value;
            OnPropertyChanged();
        }
    }

    public string? PythonScriptInFile
    {
        get => pythonScriptInFile;
        private set
        {
            pythonScriptInFile = value;
            OnPropertyChanged();
        }
    }

    public string? PythonStatus
    {
        get => pythonStatus;
        set
        {
            pythonStatus = value;
            OnPropertyChanged();
        }
    }

    public string? PythonVersion
    {
        get => pythonVersion;
        private set
        {
            pythonVersion = value;
            OnPropertyChanged();
        }
    }

    public string? SelectedPythonScript
    {
        get => selectedPythonScript;
        set
        {
            selectedPythonScript = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool CanSaveBeExecuted() =>
        editorFileName is null && !string.IsNullOrWhiteSpace(pythonScript) || editorFileName is not null && pythonScript != pythonScriptInFile;

    void Closed(object? sender, EventArgs e)
    {
        editorFileSystemWatcher.Changed -= FileChanged;
        editorFileSystemWatcher.Created -= FileCreated;
        editorFileSystemWatcher.Deleted -= FileDeleted;
        editorFileSystemWatcher.Renamed -= FileRenamed;
        editorFileSystemWatcher.EnableRaisingEvents = false;
        editorFileSystemWatcher.Dispose();
        mainWindow.Closing -= Closing;
        mainWindow.Closed -= Closed;
        pythonSynchronizationContext.Send(() =>
        {
            if (PythonEngine.IsInitialized)
                PythonEngine.Shutdown();
        });
        pythonSynchronizationContext.Dispose();
        Application.Current.Shutdown();
    }

    void Closing(object? sender, CancelEventArgs e)
    {
        if (PythonStatus is not null)
        {
            e.Cancel = true;
            return;
        }
        if (CanSaveBeExecuted())
        {
            var result = MessageBox.Show(mainWindow, "Do you want to save your current Python script before you quit?", "Woah there, tiger...", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (result is MessageBoxResult.Yes)
            {
                ApplicationCommands.Save.Execute(null, mainWindow);
                EditorFileOperationManualResetEvent.Wait();
                e.Cancel = CanSaveBeExecuted();
                return;
            }
            e.Cancel = result is MessageBoxResult.Cancel;
        }
    }

    async void ContentRendered(object? sender, EventArgs e)
    {
        mainWindow.ContentRendered -= ContentRendered;
        if (Debugger.IsAttached)
            Installer.LogMessage += msg => Debug.WriteLine(msg);
        await Task.Run(async () =>
        {
            PythonStatus = "Summoning Serpents";
            Installer.Source = new Installer.DownloadInstallationSource
            {
                DownloadUrl = "https://www.python.org/ftp/python/3.12.4/python-3.12.4-embed-amd64.zip"
            };
            if (!Installer.IsPythonInstalled())
                await Installer.SetupPython();
            if (!Installer.IsPipInstalled())
            {
                PythonStatus = "Summoning Package Installer";
                await Installer.TryInstallPip();
            }
            if (!Installer.IsModuleInstalled("lxml"))
            {
                PythonStatus = "Summoning lxml";
                await Installer.PipInstallModule("lxml");
            }
        });
        await pythonSynchronizationContext.PostAsync(() =>
        {
            PyObjectConversions.RegisterDecoder(EnumerableDecoder.Instance);
            PyObjectConversions.RegisterDecoder(IndexDecoder.Instance);
            PyObjectConversions.RegisterDecoder(RangeDecoder.Instance);
            PythonStatus = "Getting Binary Version";
            var pythonCliVersionInfo = FileVersionInfo.GetVersionInfo(Path.Combine(Installer.EmbeddedPythonHome, "python.exe"));
            PythonStatus = "Configuring Runtime";
            Runtime.PythonDLL = Path.Combine(Installer.EmbeddedPythonHome, $"python{pythonCliVersionInfo.FileMajorPart}{pythonCliVersionInfo.FileMinorPart}.dll");
            PythonStatus = "Configuring Engine";
            PythonEngine.PythonHome = Installer.EmbeddedPythonHome;
            PythonStatus = "Initializing Engine";
            PythonEngine.Initialize();
            using (Py.GIL())
            {
                PythonStatus = "Getting Engine Version";
                using dynamic sys = Py.Import("sys");
                PythonVersion = sys.version.ToString();
            }
        });
        PythonStatus = null;
    }

    public void DismissPythonException() =>
        PythonExceptionMessage = null;

    public async Task ExecutePythonAsync(string python)
    {
        static string ensureString(object? obj) =>
            obj?.ToString() ?? string.Empty;

        var interop = new
        {
            stop = new Action(() => InterruptPython()),
            update_progress = new Action<double?>(progress =>
            {
                if (progress is not { } nonNullProgress)
                {
                    PythonProgressIsIndeterminate = true;
                    PythonProgressValue = 0;
                    return;
                }
                PythonProgressIsIndeterminate = false;
                PythonProgressValue = nonNullProgress;
            }),
            update_status = new Action<object?>(status =>
            {
                var statusStr = ensureString(status);
                PythonStatus = string.IsNullOrWhiteSpace(statusStr) ? "Executing Python" : statusStr;
            }),
            user_select_directory = new Func<object?, string?>(title => Application.Current.Dispatcher.Invoke(() =>
            {
                var titleStr = ensureString(title);
                var dialog = new OpenFolderDialog();
                if (!string.IsNullOrEmpty(titleStr))
                    dialog.Title = titleStr;
                if (dialog.ShowDialog(mainWindow) ?? false)
                    return dialog.FolderName;
                return (string?)null;
            }))
        };

        // update the status to indicate that Python is being executed
        PythonStatus = "Executing Python";
        string? pythonFormattedException = null;

        // do this all on Python's dedicated synchronization context
        await pythonSynchronizationContext.PostAsync(() =>
        {
            // enter the Python.NET global interpreter lock mutex
            using (Py.GIL())
            {
                try
                {
                    // create a new Python scope
                    using var scope = Py.CreateScope();

                    // import standard Python modules
                    using dynamic os = scope.Import("os");
                    using dynamic sys = scope.Import("sys");

                    // if the Python script is being executed from a file, change the working directory to the file's directory
                    if (editorFileName is not null && File.Exists(editorFileName))
                    {
                        var editorFileDirectory = Path.GetDirectoryName(Path.GetFullPath(editorFileName));
                        os.chdir(editorFileDirectory);
                        sys.path.insert(0, editorFileDirectory);
                    }

                    // import the Python.NET interop module
                    using dynamic clr = scope.Import("clr");

                    // import the System namespace as a Python module
                    using dynamic system = scope.Import("System");

                    // add the System.Range type to the Python global scope
                    scope.Set("Range", system.Range);

                    // reference the LlamaLogic.Packages assembly
                    clr.AddReference("LlamaLogic.Packages");

                    // import the LlamaLogic.Packages namespace as a Python module
                    using dynamic packages = scope.Import("LlamaLogic.Packages");

                    // add the LlamaLogic Python module to the scope
                    scope.Set("Packages", packages);

                    // convert the Llama Pad interop object to a Python object
                    using var pythonInterop = interop.ToPython();

                    // add the Llama Pad interop object to the scope
                    scope.Set("llama_pad", pythonInterop);

                    // arm the emergency escape system
                    activePythonThread = PythonEngine.GetPythonThreadID();
                    OnPropertyChanged(nameof(PythonCanBeInterrupted));

                    // execute the Python script
                    scope.Exec(python);
                }
                catch (PythonException ex)
                {
                    // something went wrong, collect evidence from the scene of the crime
                    pythonFormattedException = $"{ex.Message}{Environment.NewLine}{ex.Format()}";
                }
            }
        });

        // update the status to indicate that Python has finished executing
        PythonStatus = null;
        PythonProgressValue = 0;
        PythonProgressIsIndeterminate = true;

        // only do this if an emergency escape was NOT triggered
        if (activePythonThread is not null)
        {
            // disarm the emergency escape system
            activePythonThread = null;
            OnPropertyChanged(nameof(PythonCanBeInterrupted));

            // if a Python exception was encountered, inform the UI
            PythonExceptionMessage = pythonFormattedException;
        }
    }

    async void FileChanged(object sender, FileSystemEventArgs e)
    {
        using (await editorFileSystemWatcherLock.LockAsync())
            if (!string.IsNullOrEmpty(editorFileName)
                && File.Exists(editorFileName)
                && Path.GetFullPath(editorFileName).Equals(e.FullPath, StringComparison.OrdinalIgnoreCase))
            {
                var wasInSync = pythonScript == pythonScriptInFile;
                var remainingReadAttempts = 5;
                while (true)
                {
                    try
                    {
                        await Task.Delay(200);
                        PythonScriptInFile = await File.ReadAllTextAsync(editorFileName);
                        break;
                    }
                    catch (IOException) when (--remainingReadAttempts >= 0)
                    {
                        continue;
                    }
                }
                if (remainingReadAttempts >= 0 && wasInSync)
                    PythonScript = pythonScriptInFile;
            }
    }

    async void FileCreated(object sender, FileSystemEventArgs e)
    {
        using (await editorFileSystemWatcherLock.LockAsync())
            if (!string.IsNullOrEmpty(editorFileName)
                && File.Exists(editorFileName)
                && Path.GetFullPath(editorFileName).Equals(e.FullPath, StringComparison.OrdinalIgnoreCase))
            {
                var wasInSync = pythonScript == pythonScriptInFile;
                var remainingReadAttempts = 5;
                while (true)
                {
                    try
                    {
                        await Task.Delay(200);
                        PythonScriptInFile = await File.ReadAllTextAsync(editorFileName);
                        break;
                    }
                    catch (IOException) when (--remainingReadAttempts >= 0)
                    {
                        continue;
                    }
                }
                if (remainingReadAttempts >= 0 && wasInSync)
                    PythonScript = pythonScriptInFile;
            }
    }

    async void FileDeleted(object sender, FileSystemEventArgs e)
    {
        using (await editorFileSystemWatcherLock.LockAsync())
            if (!string.IsNullOrEmpty(editorFileName)
                && Path.GetFullPath(editorFileName).Equals(e.FullPath, StringComparison.OrdinalIgnoreCase))
                PythonScriptInFile = null;
    }

    async void FileRenamed(object sender, RenamedEventArgs e)
    {
        using (await editorFileSystemWatcherLock.LockAsync())
            if (!string.IsNullOrEmpty(editorFileName))
            {
                var editorFileFullPath = Path.GetFullPath(editorFileName);
                if (editorFileFullPath.Equals(e.OldFullPath, StringComparison.OrdinalIgnoreCase))
                    PythonScriptInFile = null;
                if (File.Exists(editorFileName)
                    && editorFileFullPath.Equals(e.FullPath, StringComparison.OrdinalIgnoreCase))
                {
                    var wasInSync = pythonScript == pythonScriptInFile;
                    var remainingReadAttempts = 5;
                    while (true)
                    {
                        try
                        {
                            await Task.Delay(200);
                            PythonScriptInFile = await File.ReadAllTextAsync(editorFileName);
                            break;
                        }
                        catch (IOException) when (--remainingReadAttempts >= 0)
                        {
                            continue;
                        }
                    }
                    if (remainingReadAttempts >= 0 && wasInSync)
                        PythonScript = pythonScriptInFile;
                }
            }
    }

    public void InterruptPython()
    {
        if (activePythonThread is { } nonNullActivePythonThread)
        {
            activePythonThread = null;
            OnPropertyChanged(nameof(PythonCanBeInterrupted));
            using (Py.GIL())
                PythonEngine.Interrupt(nonNullActivePythonThread);
        }
    }

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
