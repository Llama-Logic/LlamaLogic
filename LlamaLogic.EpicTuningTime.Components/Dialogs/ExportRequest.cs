namespace LlamaLogic.EpicTuningTime.Components.Dialogs;

sealed record ExportRequest(string Folder, TuningArchiveExportOptions Options);