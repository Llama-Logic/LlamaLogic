namespace LlamaLogic.EpicTuningTime;

sealed partial class MauiPlatform :
    IEpicTuningTimePlatform
{
    public bool CanPick =>
        true;

    public string DefaultArchiveFolder =>
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

    public string? GetSetting(string key) =>
        Preferences.Default.Get<string?>(key, null);

    public Task<string?> PickFileAsync(string title, IReadOnlyList<string> extensions) =>
        MainThread.InvokeOnMainThreadAsync(async () =>
            (await FilePicker.Default.PickAsync(new()
            {
                PickerTitle = title,
                FileTypes = extensions.Count == 0 ? null : new(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, extensions },
                    { DevicePlatform.macOS, ["public.data"] },
                    { DevicePlatform.MacCatalyst, ["public.data"] }
                })
            }))?.FullPath);

    public Task<string?> PickFolderAsync(string title) =>
        MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var result = await FolderPicker.Default.PickAsync(CancellationToken.None);
            return result.IsSuccessful ? result.Folder?.Path : null;
        });

    public Task<string?> PickNewFileAsync(string title, string suggestedFileName) =>
        MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var result = await FileSaver.Default.SaveAsync(DefaultArchiveFolder, suggestedFileName, Stream.Null, CancellationToken.None);
            if (result.IsSuccessful)
                return result.FilePath;
            if (result.IsCancelled)
                return null;
            throw new IOException(result.Exception?.Message ?? "The file couldn't be created", result.Exception);
        });

    public Task RevealAsync(string path) =>
        MainThread.InvokeOnMainThreadAsync(() => RevealOnMainThreadAsync(path));

    public void SetSetting(string key, string? value)
    {
        if (value is null)
            Preferences.Default.Remove(key);
        else
            Preferences.Default.Set(key, value);
    }

    private static partial Task RevealOnMainThreadAsync(string path);
}