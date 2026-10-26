namespace LlamaLogic.EpicTuningTime;

/// <summary>
/// Remembers where a window was for each arrangement of displays and puts it back there when the window is created again
/// </summary>
static class WindowPlacementMemory
{
    const string preferencesKey = "WindowPlacements";

    static unsafe string CurrentDisplayConfiguration
    {
        get
        {
            var displays = new List<(int left, int top, int right, int bottom, uint dpiX, uint dpiY)>();
            BOOL enumerator(HMONITOR monitor, HDC hdc, RECT* rect, LPARAM lParam)
            {
                if (PInvoke.GetDpiForMonitor(monitor, MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out var dpiX, out var dpiY).Succeeded)
                    displays.Add((rect->left, rect->top, rect->right, rect->bottom, dpiX, dpiY));
                return true;
            }
            PInvoke.EnumDisplayMonitors(default, default, enumerator, default);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            Span<byte> buffer = stackalloc byte[24];
            foreach (var (left, top, right, bottom, dpiX, dpiY) in displays.OrderBy(display => display.left).ThenBy(display => display.top))
            {
                BinaryPrimitives.WriteInt32LittleEndian(buffer[0..4], left);
                BinaryPrimitives.WriteInt32LittleEndian(buffer[4..8], top);
                BinaryPrimitives.WriteInt32LittleEndian(buffer[8..12], right);
                BinaryPrimitives.WriteInt32LittleEndian(buffer[12..16], bottom);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer[16..20], dpiX);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer[20..24], dpiY);
                hash.AppendData(buffer);
            }
            return Convert.ToHexString(hash.GetCurrentHash());
        }
    }

    /// <summary>
    /// Restores the window's placement for the current arrangement of displays, if one was saved, and saves its placement when it closes
    /// </summary>
    public static void Attach(Microsoft.UI.Xaml.Window window)
    {
        var handle = new HWND(WindowNative.GetWindowHandle(window));
        var displayConfiguration = CurrentDisplayConfiguration;
        if (Load().FirstOrDefault(saved => saved.DisplayConfiguration == displayConfiguration) is { } saved)
        {
            var placement = new WINDOWPLACEMENT
            {
                length = (uint)Marshal.SizeOf<WINDOWPLACEMENT>(),
                rcNormalPosition = new(saved.Left, saved.Top, saved.Right, saved.Bottom),
                showCmd = saved.Maximized ? SHOW_WINDOW_CMD.SW_SHOWMAXIMIZED : SHOW_WINDOW_CMD.SW_SHOWNORMAL
            };
            PInvoke.SetWindowPlacement(handle, in placement);
        }
        window.AppWindow.Closing += (_, _) => Save(handle);
    }

    static List<SavedWindowPlacement> Load()
    {
        if (Preferences.Default.Get<string?>(preferencesKey, null) is not { } json)
            return [];
        try
        {
            return JsonSerializer.Deserialize<List<SavedWindowPlacement>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    static void Save(HWND handle)
    {
        var placement = new WINDOWPLACEMENT { length = (uint)Marshal.SizeOf<WINDOWPLACEMENT>() };
        if (PInvoke.GetWindowPlacement(handle, ref placement).Value is 0)
            return;
        var displayConfiguration = CurrentDisplayConfiguration;
        var placements = Load();
        placements.RemoveAll(saved => saved.DisplayConfiguration == displayConfiguration);
        placements.Add(new
        (
            displayConfiguration,
            placement.rcNormalPosition.left,
            placement.rcNormalPosition.top,
            placement.rcNormalPosition.right,
            placement.rcNormalPosition.bottom,
            placement.showCmd is SHOW_WINDOW_CMD.SW_SHOWMAXIMIZED
            || (placement.showCmd is SHOW_WINDOW_CMD.SW_SHOWMINIMIZED && placement.flags.HasFlag(WINDOWPLACEMENT_FLAGS.WPF_RESTORETOMAXIMIZED))
        ));
        Preferences.Default.Set(preferencesKey, JsonSerializer.Serialize(placements));
    }
}