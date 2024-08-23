namespace LlamaLogic.LlamaPad;

public partial class MainWindow :
    Window
{
    public static readonly RoutedCommand CancelRunCommand = new();
    public static readonly RoutedCommand DismissExceptionCommand = new();
    public static readonly RoutedCommand ExitCommand = new();
    public static readonly RoutedCommand RunCommand = new();

    public MainWindow()
    {
        DataContext = new MainWindowDataContext(this);
        InitializeComponent();
    }

    MainWindowDataContext TypedDataContext =>
        (MainWindowDataContext)DataContext;

    void CancelRunCanExecute(object sender, CanExecuteRoutedEventArgs e) =>
        e.CanExecute = TypedDataContext.PythonCanBeInterrupted;

    void CancelRunExecuted(object sender, ExecutedRoutedEventArgs e) =>
        TypedDataContext.InterruptPython();

    void DismissExceptionCanExecute(object sender, CanExecuteRoutedEventArgs e) =>
        e.CanExecute = TypedDataContext.PythonExceptionEncountered;

    void DismissExceptionExecuted(object sender, ExecutedRoutedEventArgs e) =>
        TypedDataContext.DismissPythonException();

    void ExitCanExecute(object sender, CanExecuteRoutedEventArgs e) =>
        e.CanExecute = true;

    void ExitExecuted(object sender, ExecutedRoutedEventArgs e) =>
        Close();

    void NewCanExecute(object sender, CanExecuteRoutedEventArgs e) =>
        e.CanExecute = true;

    async void NewExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (TypedDataContext.CanSaveBeExecuted())
        {
            var result = MessageBox.Show(this, "Do you want to save your current Python script before you start a new one?", "Woah there, tiger...", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (result is MessageBoxResult.Yes)
            {
                ApplicationCommands.Save.Execute(null, this);
                await TypedDataContext.EditorFileOperationManualResetEvent.WaitAsync();
                if (TypedDataContext.CanSaveBeExecuted())
                    return;
            }
            else if (result is MessageBoxResult.Cancel)
                return;
        }
        TypedDataContext.EditorFileName = null;
        TypedDataContext.PythonScript = string.Empty;
        UpdateTitle(null);
    }

    void OpenCanExecute(object sender, CanExecuteRoutedEventArgs e) =>
        e.CanExecute = true;

    async void OpenExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Python Files (*.py)|*.py|All Files (*.*)|*.*",
            Title = "Open Python File"
        };
        if (dialog.ShowDialog(this) ?? false)
        {
            if (await FileZoneIdentifier.IsFileDownloadedFromInternetAsync(dialog.FileName) && MessageBox.Show(
                $"""
                According to Windows, this file was downloaded:

                {dialog.FileName}

                The Python engine in this application is NOT sandboxed. Only use this script if you REALLY TRUST the source.
                """, "Woah there, tiger...", MessageBoxButton.OKCancel, MessageBoxImage.Warning) is not MessageBoxResult.OK)
                return;
            TypedDataContext.PythonScript = await File.ReadAllTextAsync(dialog.FileName);
            TypedDataContext.EditorFileName = dialog.FileName;
            UpdateTitle(dialog.FileName);
        }
    }

    void PythonCodeSelectionChanged(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
            TypedDataContext.SelectedPythonScript = textBox.SelectedText;
    }

    void RunCanExecute(object sender, CanExecuteRoutedEventArgs e) =>
        e.CanExecute = !string.IsNullOrEmpty(TypedDataContext.PythonScript);

    async void RunExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        var selectedPython = TypedDataContext.SelectedPythonScript;
        if (!string.IsNullOrEmpty(selectedPython))
        {
            await TypedDataContext.ExecutePythonAsync(selectedPython);
            return;
        }
        var fullPython = TypedDataContext.PythonScript;
        if (!string.IsNullOrEmpty(fullPython))
            await TypedDataContext.ExecutePythonAsync(fullPython);
    }

    void SaveCanExecute(object sender, CanExecuteRoutedEventArgs e) =>
        e.CanExecute = TypedDataContext.CanSaveBeExecuted();

    async void SaveExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (TypedDataContext.EditorFileName is null)
        {
            SaveAsExecuted(sender, e);
            return;
        }
        var editorFileName = TypedDataContext.EditorFileName;
        var pythonScript = TypedDataContext.PythonScript;
        TypedDataContext.EditorFileOperationManualResetEvent.Reset();
        await File.WriteAllTextAsync(editorFileName, pythonScript);
        TypedDataContext.EditorFileOperationManualResetEvent.Set();
    }

    void SaveAsCanExecute(object sender, CanExecuteRoutedEventArgs e) =>
        e.CanExecute = true;

    async void SaveAsExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Python Files (*.py)|*.py|All Files (*.*)|*.*",
            Title = "Save Python File"
        };
        if (dialog.ShowDialog(this) ?? false)
        {
            var pythonScript = TypedDataContext.PythonScript;
            TypedDataContext.EditorFileOperationManualResetEvent.Reset();
            await File.WriteAllTextAsync(dialog.FileName, pythonScript);
            TypedDataContext.EditorFileName = dialog.FileName;
            TypedDataContext.EditorFileOperationManualResetEvent.Set();
            UpdateTitle(dialog.FileName);
        }
    }

    void UpdateTitle(string? fileName) =>
        Title = string.IsNullOrEmpty(fileName) ? "Llama Pad" : $"Llama Pad - {new FileInfo(fileName).Name}";
}