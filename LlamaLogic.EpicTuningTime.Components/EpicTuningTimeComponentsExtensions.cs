namespace LlamaLogic.EpicTuningTime.Components;

/// <summary>
/// Registers the services the Epic Tuning Time components need
/// </summary>
public static class EpicTuningTimeComponentsExtensions
{
    /// <summary>
    /// Adds the Epic Tuning Time components' services, with the specified platform implementation
    /// </summary>
    public static IServiceCollection AddEpicTuningTimeComponents<TPlatform>(this IServiceCollection services)
        where TPlatform : class, IEpicTuningTimePlatform
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddMudServices();
        services.AddSingleton<IEpicTuningTimePlatform, TPlatform>();
        services.AddSingleton<TuningArchiveSession>();
        services.AddSingleton<BrowseState>();
        services.AddSingleton<CompareState>();
        services.AddSingleton<DraftsState>();
        services.AddSingleton<ProblemsState>();
        services.AddSingleton<UpdateFolderState>();
        services.AddSingleton<UpdateState>();
        return services;
    }
}