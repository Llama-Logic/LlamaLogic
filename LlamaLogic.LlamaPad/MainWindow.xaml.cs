namespace LlamaLogic.LlamaPad;

public partial class MainWindow : Window
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

    void NewExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        TypedDataContext.EditorFileName = null;
        UpdateTitle(null);
        TypedDataContext.PythonScript = string.Empty;
    }

    void OpenCanExecute(object sender, CanExecuteRoutedEventArgs e) =>
        e.CanExecute = true;

    void OpenExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Python Files (*.py)|*.py|All Files (*.*)|*.*",
            Title = "Open Python File"
        };
        if (dialog.ShowDialog(this) ?? false)
        {
            TypedDataContext.EditorFileName = dialog.FileName;
            UpdateTitle(dialog.FileName);
            TypedDataContext.PythonScript = File.ReadAllText(dialog.FileName);
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

    void SaveExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (TypedDataContext.EditorFileName is null)
        {
            SaveAsExecuted(sender, e);
            return;
        }
        File.WriteAllText(TypedDataContext.EditorFileName, TypedDataContext.PythonScript);
    }

    void SaveAsCanExecute(object sender, CanExecuteRoutedEventArgs e) =>
        e.CanExecute = true;

    void SaveAsExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Python Files (*.py)|*.py|All Files (*.*)|*.*",
            Title = "Save Python File"
        };
        if (dialog.ShowDialog(this) ?? false)
        {
            TypedDataContext.EditorFileName = dialog.FileName;
            UpdateTitle(dialog.FileName);
            File.WriteAllText(dialog.FileName, TypedDataContext.PythonScript);
        }
    }

    void UpdateTitle(string? fileName) =>
        Title = string.IsNullOrEmpty(fileName) ? "Llama Pad" : $"Llama Pad - {new FileInfo(fileName).Name}";
}