namespace LlamaLogic.EpicTuningTime.WinUI;

/// <summary>
/// The WinUI application
/// </summary>
public partial class App :
    MauiWinUIApplication
{
    /// <summary>
    /// Initializes the application
    /// </summary>
    public App() =>
        InitializeComponent();

    /// <inheritdoc/>
    protected override MauiApp CreateMauiApp() =>
        MauiProgram.CreateMauiApp();
}