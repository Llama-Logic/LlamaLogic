namespace LlamaLogic.EpicTuningTime.Components.Dialogs;

sealed record ImportFolderRequest(string Folder, string GameVersion, TuningArchiveImportOptions Options);