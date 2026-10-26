namespace LlamaLogic.EpicTuningTime;

partial class MauiPlatform
{
    private static async partial Task RevealOnMainThreadAsync(string path)
    {
        var folder = Directory.Exists(path) ? path : System.IO.Path.GetDirectoryName(path);
        if (folder is not null && Directory.Exists(folder))
            await UIApplication.SharedApplication.OpenUrlAsync(NSUrl.FromFilename(folder), new UIApplicationOpenUrlOptions()).ConfigureAwait(false);
    }
}