namespace LlamaLogic.LlamaPadPreview;

class FileZoneIdentifier
{
    public static async Task<bool> IsFileDownloadedFromInternetAsync(string filePath)
    {
        var zoneIdentifierPath = filePath + ":Zone.Identifier";
        if (!File.Exists(zoneIdentifierPath))
            return false; // Zone.Identifier stream not found
        try
        {
            var zoneData = await File.ReadAllLinesAsync(zoneIdentifierPath);
            foreach (var line in zoneData)
                if (line.StartsWith("ZoneId="))
                {
                    var zoneId = int.Parse(line.Substring(7));
                    return zoneId == 3; // ZoneId=3 indicates the Internet Zone
                }
        }
        catch (Exception)
        {
            return false; // whoops
        }
        return false;
    }
}