namespace LlamaLogic.EpicTuningTime;

partial class MauiProgram
{
    static partial void ConfigurePlatform(MauiAppBuilder builder) =>
        builder.ConfigureLifecycleEvents(events => events.AddWindows(windows => windows.OnWindowCreated(WindowPlacementMemory.Attach)));
}