namespace LlamaLogic.EpicTuningTime;

/// <summary>
/// Builds the application
/// </summary>
public static partial class MauiProgram
{
    /// <summary>
    /// Creates the application
    /// </summary>
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<EpicTuningTimeApplication>();
        builder.UseMauiCommunityToolkit();
        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddEpicTuningTimeComponents<MauiPlatform>();
        ConfigurePlatform(builder);
#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }

    static partial void ConfigurePlatform(MauiAppBuilder builder);
}