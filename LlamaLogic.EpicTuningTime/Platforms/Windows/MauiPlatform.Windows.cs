namespace LlamaLogic.EpicTuningTime;

partial class MauiPlatform
{
    private static partial Task RevealOnMainThreadAsync(string path)
    {
        if (File.Exists(path))
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = false });
        else if (Directory.Exists(path))
            Process.Start(new ProcessStartInfo("explorer.exe") { ArgumentList = { path }, UseShellExecute = false });
        return Task.CompletedTask;
    }
}