namespace LlamaLogic.EpicTuningTime.Components.Services;

/// <summary>
/// Writes a folder update's report as an Excel workbook: a summary, what needs the creator, every override and SimData of the mods' own tuning, every conflict, and every package
/// </summary>
static class UpdateReportWorkbook
{
    const int maximumCellLength = 32767;

    static readonly XLColor headerFill = XLColor.FromHtml("#0F3D5C");
    static readonly XLColor needsCreatorFill = XLColor.FromHtml("#FDECD2");

    static string Fit(string? text) =>
        text is null ? string.Empty : text.Length <= maximumCellLength ? text : $"{text[..(maximumCellLength - 1)]}…";

    static XLColor GetOutcomeFill(string outcome) =>
        outcome switch
        {
            "Merges" => XLColor.FromHtml("#DCF3E3"),
            "Conflicts" or "Unreadable" or "Unreadable package" => XLColor.FromHtml("#F9D9D6"),
            "Old copy" or "Gone from game" => XLColor.FromHtml("#FCEBC7"),
            "Does nothing" => XLColor.FromHtml("#DCEBF7"),
            "Mod's own tuning" => XLColor.FromHtml("#EAE4F5"),
            _ => XLColor.NoColor
        };

    static IXLTable AddTable(IXLWorksheet sheet, int row, string name, IReadOnlyList<(string header, double width, bool wrap)> columns, IReadOnlyList<object?[]> values, int firstColumn = 1)
    {
        for (var column = 0; column < columns.Count; ++column)
        {
            sheet.Cell(row, firstColumn + column).Value = columns[column].header;
            sheet.Column(firstColumn + column).Width = columns[column].width;
            if (columns[column].wrap)
                sheet.Column(firstColumn + column).Style.Alignment.WrapText = true;
        }
        for (var index = 0; index < values.Count; ++index)
            for (var column = 0; column < columns.Count; ++column)
            {
                var cell = sheet.Cell(row + 1 + index, firstColumn + column);
                switch (values[index][column])
                {
                    case int number:
                        cell.Value = number;
                        break;
                    case string text:
                        cell.Value = Fit(text);
                        break;
                }
            }
        var range = sheet.Range(row, firstColumn, row + Math.Max(values.Count, 1), firstColumn + columns.Count - 1);
        var table = range.CreateTable(name);
        table.Theme = XLTableTheme.TableStyleLight9;
        table.ShowAutoFilter = true;
        var header = sheet.Range(row, firstColumn, row, firstColumn + columns.Count - 1);
        header.Style.Font.Bold = true;
        header.Style.Font.FontColor = XLColor.White;
        header.Style.Fill.BackgroundColor = headerFill;
        header.Style.Alignment.WrapText = true;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        sheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        sheet.PageSetup.FitToPages(1, 0);
        sheet.PageSetup.SetRowsToRepeatAtTop(row, row);
        return table;
    }

    static void ColorOutcomes(IXLWorksheet sheet, int firstRow, int count, int outcomeColumn, int? needsCreatorColumn, IReadOnlyList<TuningFolderUpdateReportRow> rows)
    {
        for (var index = 0; index < count; ++index)
        {
            var fill = GetOutcomeFill(rows[index].Outcome);
            if (fill != XLColor.NoColor)
                sheet.Cell(firstRow + index, outcomeColumn).Style.Fill.BackgroundColor = fill;
            if (needsCreatorColumn is { } column && rows[index].NeedsCreator)
            {
                var cell = sheet.Cell(firstRow + index, column);
                cell.Style.Fill.BackgroundColor = needsCreatorFill;
                cell.Style.Font.Bold = true;
            }
        }
    }

    static string GetWhy(TuningFolderUpdateReportRow row)
    {
        var parts = new List<string> { row.Message };
        if (row.BaseNote.Length > 0)
            parts.Add($"Base: {row.BaseNote}.");
        if (row.SimDataMessage.Length > 0)
            parts.Add($"SimData: {row.SimDataMessage}");
        return string.Join(" ", parts.Where(part => part.Length > 0));
    }

    static string DescribeFixes(TuningPackageAddedSimDataFixes fixes)
    {
        var parts = new List<string>();
        if (fixes.HasFlag(TuningPackageAddedSimDataFixes.AddMissingColumns))
            parts.Add("missing columns added");
        if (fixes.HasFlag(TuningPackageAddedSimDataFixes.CorrectSchemaHashes))
            parts.Add("schema hashes corrected");
        if (fixes.HasFlag(TuningPackageAddedSimDataFixes.CorrectColumnTypes))
            parts.Add("column types corrected");
        if (fixes.HasFlag(TuningPackageAddedSimDataFixes.RemoveExtraColumns))
            parts.Add("extra columns removed");
        if (fixes.HasFlag(TuningPackageAddedSimDataFixes.MatchTuning))
            parts.Add("rebuilt from tuning where they disagree");
        if (fixes.HasFlag(TuningPackageAddedSimDataFixes.AmendTuning))
            parts.Add("values the tuning leaves out added to it");
        return parts.Count == 0 ? "not fixed" : string.Join("; ", parts);
    }

    /// <summary>
    /// Saves the report of the specified <paramref name="update"/> with the specified <paramref name="options"/> and, if the updated copies were written, what writing them did, to the specified <paramref name="path"/>
    /// </summary>
    public static void Save(string path, TuningFolderUpdate update, TuningFolderUpdateOptions options, TuningFolderUpdateResult? result, string? outputFolder)
    {
        var rows = update.GetReportRows(options, result?.Packages);
        using var workbook = new XLWorkbook();
        var summary = workbook.Worksheets.Add("Summary");
        summary.Cell(1, 1).Value = "Update report";
        summary.Cell(1, 1).Style.Font.Bold = true;
        summary.Cell(1, 1).Style.Font.FontSize = 16;
        summary.Cell(1, 1).Style.Font.FontColor = headerFill;
        var facts = new List<(string label, string value)>
        {
            ("Folder", update.Folder),
            ("Updated for", update.Target.DisplayName),
            ("Last updated for", update.DeclaredBase?.DisplayName ?? "not said; worked out for each override"),
            ("Overrides that do nothing", options.RemoveOverridesThatDoNothing ? "removed" : "kept"),
            ("Missing SimData", options.SimDataChanges.HasFlag(TuningPackageSimDataChanges.Add) ? "added" : "not added"),
            ("Own tuning's SimData", DescribeFixes(options.AddedSimDataFixes)),
            ("Updated copies", result is null ? "not written" : $"{result.Packages.Count(write => write.WrittenPath is not null):N0} written to {outputFolder}"),
            ("Made", DateTime.Now.ToString("f", CultureInfo.CurrentCulture))
        };
        for (var index = 0; index < facts.Count; ++index)
        {
            summary.Cell(3 + index, 1).Value = facts[index].label;
            summary.Cell(3 + index, 1).Style.Font.Bold = true;
            summary.Cell(3 + index, 2).Value = facts[index].value;
        }
        var overrides = rows.Where(row => row.Override is not null).ToList();
        var counts = rows.GroupBy(row => row.Outcome).Select(group => new object?[] { group.Key, group.Count(), group.Count(row => row.NeedsCreator) }).OrderByDescending(values => (int)values[2]!).ThenByDescending(values => (int)values[1]!).ToList();
        counts.Add(["Total", rows.Count, rows.Count(row => row.NeedsCreator)]);
        const int countsRow = 3;
        const int countsColumn = 4;
        AddTable(summary, countsRow, "Outcomes", [("Outcome", 22, false), ("Count", 12, false), ("Need you", 12, false)], counts, countsColumn);
        for (var index = 0; index < counts.Count - 1; ++index)
        {
            var fill = GetOutcomeFill((string)counts[index][0]!);
            if (fill != XLColor.NoColor)
                summary.Cell(countsRow + 1 + index, countsColumn).Style.Fill.BackgroundColor = fill;
        }
        summary.Range(countsRow + counts.Count, countsColumn, countsRow + counts.Count, countsColumn + 2).Style.Font.Bold = true;
        summary.Column(2).Width = 70;
        summary.Column(3).Width = 4;
        var legendRow = Math.Max(3 + facts.Count, countsRow + counts.Count + 1) + 1;
        var legend = new (string outcome, string meaning)[]
        {
            ("Merges", "The game changed the tuning, and the mod's changes merge onto its new version. The update merges it only when the version the override was made from is certain."),
            ("Conflicts", "Some of the mod's changes conflict with the game's. Review the package in Epic Tuning Time to choose."),
            ("Old copy", "The override is a copy of an older version of the game's tuning, so all it does is undo the game's changes since then."),
            ("Does nothing", "The override is the same as the game's new version."),
            ("Gone from game", "The game no longer has the tuning."),
            ("Up to date", "The game hasn't changed the tuning since the version the override was made from."),
            ("Mod's own tuning", "Tuning the mod adds whose SimData isn't what the new version's tuning descriptions give. The update makes only the fixes chosen, and only with values it can generate reliably.")
        };
        summary.Cell(legendRow, 1).Value = "What the outcomes mean";
        summary.Cell(legendRow, 1).Style.Font.Bold = true;
        for (var index = 0; index < legend.Length; ++index)
        {
            var label = summary.Cell(legendRow + 1 + index, 1);
            label.Value = legend[index].outcome;
            var fill = GetOutcomeFill(legend[index].outcome);
            if (fill != XLColor.NoColor)
                label.Style.Fill.BackgroundColor = fill;
            summary.Cell(legendRow + 1 + index, 2).Value = legend[index].meaning;
            summary.Cell(legendRow + 1 + index, 2).Style.Alignment.WrapText = true;
            summary.Row(legendRow + 1 + index).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        }
        summary.Column(1).Width = 26;
        summary.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        summary.PageSetup.FitToPages(1, 0);

        var needing = rows.Where(row => row.NeedsCreator).ToList();
        var needsSheet = workbook.Worksheets.Add("Needs you");
        AddTable(needsSheet, 1, "NeedsYou", [("Package", 55, false), ("Tuning", 45, false), ("Type", 16, false), ("Outcome", 14, false), ("Conflicts", 10, false), ("Base", 16, false), ("Base source", 11, false), ("Why", 90, true)],
            [.. needing.Select(row => new object?[] { row.Package, row.Tuning, row.Type, row.Outcome, row.Conflicts > 0 ? row.Conflicts : null, row.Base, row.BaseSource, GetWhy(row) })]);
        ColorOutcomes(needsSheet, 2, needing.Count, 4, null, needing);
        needsSheet.SheetView.FreezeRows(1);

        var allSheet = workbook.Worksheets.Add("Everything");
        AddTable(allSheet, 1, "Everything", [("Package", 55, false), ("Tuning", 45, false), ("Type", 16, false), ("Instance", 22, false), ("Outcome", 14, false), ("Needs you", 10, false), ("Done", 24, true), ("Base", 16, false), ("Base source", 11, false), ("Base note", 40, true), ("Conflicts", 10, false), ("SimData", 13, false), ("Message", 70, true), ("SimData message", 60, true)],
            [.. rows.Select(row => new object?[] { row.Package, row.Tuning, row.Type, row.Instance, row.Outcome, row.NeedsCreator ? "yes" : string.Empty, row.Done, row.Base, row.BaseSource, row.BaseNote, row.Conflicts > 0 ? row.Conflicts : null, row.SimData, row.Message, row.SimDataMessage })]);
        ColorOutcomes(allSheet, 2, rows.Count, 5, 6, rows);
        allSheet.SheetView.FreezeRows(1);

        var conflictRows = overrides.SelectMany(row => (row.Override!.Merge?.Conflicts ?? []).Select(conflict => new object?[] { row.Package, row.Tuning, conflict.Path, conflict.Kind switch
        {
            TuningMergeConflictKind.ModRemovedGameChanged => "mod removed, game changed",
            TuningMergeConflictKind.GameRemovedModChanged => "game removed, mod changed",
            TuningMergeConflictKind.TooLarge => "list too large to align",
            _ => "both changed"
        }, conflict.Base ?? "(not there)", conflict.Mod ?? "(removed)", conflict.Game ?? "(removed)" })).ToList();
        var conflictsSheet = workbook.Worksheets.Add("Conflicts");
        AddTable(conflictsSheet, 1, "Conflicts", [("Package", 45, false), ("Tuning", 40, false), ("Path", 36, true), ("Kind", 22, false), ("Was", 50, true), ("Mod has", 50, true), ("Game has", 50, true)], conflictRows);
        conflictsSheet.SheetView.FreezeRows(1);

        var writes = (result?.Packages ?? []).ToDictionary(write => write.RelativePath, StringComparer.OrdinalIgnoreCase);
        var packageRows = rows.GroupBy(row => row.Package, StringComparer.OrdinalIgnoreCase).Select(group =>
        {
            writes.TryGetValue(group.Key, out var write);
            return new object?[]
            {
                group.Key,
                group.Count(row => row.Override is not null),
                group.Count(row => row.AddedSimData is not null),
                group.Count(row => row.Done.Length > 0 && !row.Done.StartsWith("not written", StringComparison.Ordinal)),
                group.Count(row => row.NeedsCreator),
                write?.WrittenPath ?? (write?.Skipped is { } skipped ? $"not written: {skipped}" : string.Empty)
            };
        }).ToList();
        var packagesSheet = workbook.Worksheets.Add("Packages");
        AddTable(packagesSheet, 1, "Packages", [("Package", 70, false), ("Overrides", 11, false), ("Own SimData", 12, false), ("Changed", 10, false), ("Need you", 10, false), ("Updated copy", 70, true)], packageRows);
        packagesSheet.SheetView.FreezeRows(1);

        workbook.SaveAs(path);
    }
}