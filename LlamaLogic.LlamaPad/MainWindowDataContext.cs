namespace LlamaLogic.LlamaPad;

class MainWindowDataContext :
    INotifyPropertyChanged
{
    public MainWindowDataContext(MainWindow mainWindow)
    {
        pythonProgressIsIndeterminate = true;
        pythonSynchronizationContext = new();
        this.mainWindow = mainWindow;
        this.mainWindow.ContentRendered += ContentRendered;
        this.mainWindow.Closing += Closing;
        this.mainWindow.Closed += Closed;
    }

    ulong? activePythonThread;
    string? editorFileName;
    readonly MainWindow mainWindow;
    string? pythonExceptionMessage;
    bool pythonProgressIsIndeterminate;
    double pythonProgressValue;
    string? pythonScript;
    string? pythonStatus;
    readonly PythonSynchronizationContext pythonSynchronizationContext;
    string? pythonVersion;
    string? selectedPythonScript;

    public string? EditorFileName
    {
        get => editorFileName;
        set
        {
            editorFileName = value;
            OnPropertyChanged();
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

    public bool CanSaveBeExecuted()
    {
        try
        {
            return editorFileName is null && !string.IsNullOrWhiteSpace(pythonScript) || editorFileName is not null && (!File.Exists(editorFileName) || pythonScript != File.ReadAllText(editorFileName));
        }
        catch
        {
            return false;
        }
    }

    void Closed(object? sender, EventArgs e)
    {
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
                e.Cancel = CanSaveBeExecuted();
                return;
            }
            e.Cancel = result is MessageBoxResult.Cancel;
        }
    }

    async void ContentRendered(object? sender, EventArgs e)
    {
        mainWindow.ContentRendered -= ContentRendered;
        PythonStatus = "Summoning Serpents";
        await Installer.SetupPython();
        await pythonSynchronizationContext.PostAsync(() =>
        {
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
        var interop = new
        {
            open_read = new Func<string, FileStream>(path => File.OpenRead(path)),
            open_write = new Func<string, FileStream>(path => File.OpenWrite(path)),
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
                var statusStr = (status?.ToString() ?? string.Empty).Trim();
                PythonStatus = string.IsNullOrWhiteSpace(statusStr) ? "Executing Python" : statusStr;
            }),
            user_select_directory = new Func<string?, string?>(title => Application.Current.Dispatcher.Invoke(() =>
            {
                var dialog = new OpenFolderDialog();
                if (!string.IsNullOrEmpty(title))
                    dialog.Title = title;
                if (dialog.ShowDialog(mainWindow) ?? false)
                    return dialog.FolderName;
                return (string?)null;
            })),
        };
        PythonStatus = "Executing Python";
        string? pythonFormattedException = null;
        await pythonSynchronizationContext.PostAsync(() =>
        {
            using (Py.GIL())
            {
                try
                {
                    using var scope = Py.CreateScope();
                    using dynamic clr = scope.Import("clr");
                    clr.AddReference("LlamaLogic.Packages");
                    using dynamic llPackages = scope.Import("LlamaLogic.Packages");
                    scope.Set("LlamaLogic", llPackages);
                    using var pythonInterop = interop.ToPython();
                    scope.Set("llama_pad", pythonInterop);
                    activePythonThread = PythonEngine.GetPythonThreadID();
                    OnPropertyChanged(nameof(PythonCanBeInterrupted));
                    scope.Exec(python);
                }
                catch (PythonException ex)
                {
                    pythonFormattedException = ex.Format();
                }
            }
        });
        PythonStatus = null;
        if (activePythonThread is not null)
        {
            activePythonThread = null;
            OnPropertyChanged(nameof(PythonCanBeInterrupted));
            PythonExceptionMessage = pythonFormattedException;
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
