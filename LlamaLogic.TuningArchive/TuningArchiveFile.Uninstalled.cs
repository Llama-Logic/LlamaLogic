namespace LlamaLogic.TuningArchive;

partial class TuningArchiveFile
{
    static void InsertUninstalledTunings(SqliteConnection connection, SqliteTransaction transaction, long snapshotId, IEnumerable<TuningArchiveUninstalledTuning> tunings)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO snapshot_uninstalled_tuning (snapshot_id, type, instance, name, class, pack) VALUES ($snapshot, $type, $instance, $name, $class, $pack)";
        var snapshotParameter = command.Parameters.Add("$snapshot", SqliteType.Integer);
        var typeParameter = command.Parameters.Add("$type", SqliteType.Integer);
        var instanceParameter = command.Parameters.Add("$instance", SqliteType.Integer);
        var nameParameter = command.Parameters.Add("$name", SqliteType.Text);
        var classParameter = command.Parameters.Add("$class", SqliteType.Text);
        var packParameter = command.Parameters.Add("$pack", SqliteType.Text);
        snapshotParameter.Value = snapshotId;
        foreach (var tuning in tunings)
        {
            typeParameter.Value = (long)(uint)tuning.Type;
            instanceParameter.Value = unchecked((long)tuning.Instance);
            nameParameter.Value = tuning.Name;
            classParameter.Value = (object?)tuning.Class ?? DBNull.Value;
            packParameter.Value = tuning.Pack;
            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Gets the tunings of packs that were not installed when the specified <paramref name="snapshot"/> was captured, known only by name from the packs' patches in the game's <c>Delta</c> folder
    /// </summary>
    /// <remarks>
    /// None of them is in the snapshot. Only snapshots captured from an installation have them.
    /// </remarks>
    public IReadOnlyList<TuningArchiveUninstalledTuning> GetUninstalledTunings(TuningArchiveSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT type, instance, name, class, pack FROM snapshot_uninstalled_tuning WHERE snapshot_id = $snapshot";
        command.Parameters.AddWithValue("$snapshot", snapshot.Id);
        var tunings = new List<TuningArchiveUninstalledTuning>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
            tunings.Add(new((ResourceType)(uint)reader.GetInt64(0), unchecked((ulong)reader.GetInt64(1)), reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetString(3), reader.GetString(4)));
        return tunings;
    }
}