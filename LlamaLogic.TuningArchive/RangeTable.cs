namespace LlamaLogic.TuningArchive;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA2100: Review SQL queries for security vulnerabilities", Justification = "Table and column names are internal constants.")]
sealed class RangeTable<TContent>
    where TContent : IEquatable<TContent>
{
    public RangeTable(string table, string identityColumn, string scopeFilter, IReadOnlyList<string> contentColumns, Func<SqliteDataReader, int, TContent> readContent, Action<SqliteParameterCollection, TContent> bindContent)
    {
        this.table = table;
        this.identityColumn = identityColumn;
        this.scopeFilter = scopeFilter;
        this.contentColumns = contentColumns;
        this.readContent = readContent;
        this.bindContent = bindContent;
    }

    readonly Action<SqliteParameterCollection, TContent> bindContent;
    readonly IReadOnlyList<string> contentColumns;
    readonly string identityColumn;
    readonly Func<SqliteDataReader, int, TContent> readContent;
    readonly string scopeFilter;
    readonly string table;

    readonly record struct Row(long RowId, long Identity, long First, long? Last, TContent Content);

    Dictionary<long, Row> LoadRows(SqliteConnection connection, SqliteTransaction transaction, string condition, Action<SqliteParameterCollection> bind)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT id, {identityColumn}, first_ord, last_ord, {string.Join(", ", contentColumns)} FROM {table} WHERE ({condition}) AND {scopeFilter}";
        bind(command.Parameters);
        var rows = new Dictionary<long, Row>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var row = new Row(reader.GetInt64(0), reader.GetInt64(1), reader.GetInt64(2), reader.IsDBNull(3) ? null : reader.GetInt64(3), readContent(reader, 4));
            rows[row.Identity] = row;
        }
        return rows;
    }

    public void Apply(SqliteConnection connection, SqliteTransaction transaction, Action<SqliteParameterCollection> bindScope, long? previous, long ordinal, long? next, IReadOnlyDictionary<long, TContent> manifest)
    {
        var coveringPrevious = previous is { } a
            ? LoadRows(connection, transaction, "first_ord <= $a AND (last_ord IS NULL OR last_ord >= $a)", parameters =>
            {
                parameters.AddWithValue("$a", a);
                bindScope(parameters);
            })
            : [];
        var startingNext = next is { } b
            ? LoadRows(connection, transaction, "first_ord = $b", parameters =>
            {
                parameters.AddWithValue("$b", b);
                bindScope(parameters);
            })
            : [];
        using var setLast = connection.CreateCommand();
        setLast.Transaction = transaction;
        setLast.CommandText = $"UPDATE {table} SET last_ord = $last WHERE id = $id";
        var setLastId = setLast.Parameters.Add("$id", SqliteType.Integer);
        var setLastValue = setLast.Parameters.Add("$last", SqliteType.Integer);
        using var setFirst = connection.CreateCommand();
        setFirst.Transaction = transaction;
        setFirst.CommandText = $"UPDATE {table} SET first_ord = $first WHERE id = $id";
        var setFirstId = setFirst.Parameters.Add("$id", SqliteType.Integer);
        var setFirstValue = setFirst.Parameters.Add("$first", SqliteType.Integer);
        using var delete = connection.CreateCommand();
        delete.Transaction = transaction;
        delete.CommandText = $"DELETE FROM {table} WHERE id = $id";
        var deleteId = delete.Parameters.Add("$id", SqliteType.Integer);
        using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = $"INSERT INTO {table} ({identityColumn}, first_ord, last_ord, {string.Join(", ", contentColumns)}) VALUES ($identity, $first, $last, {string.Join(", ", contentColumns.Select((_, index) => $"$c{index}"))})";
        void updateLast(long id, long? last)
        {
            setLastId.Value = id;
            setLastValue.Value = (object?)last ?? DBNull.Value;
            setLast.ExecuteNonQuery();
        }
        void updateFirst(long id, long first)
        {
            setFirstId.Value = id;
            setFirstValue.Value = first;
            setFirst.ExecuteNonQuery();
        }
        void deleteRow(long id)
        {
            deleteId.Value = id;
            delete.ExecuteNonQuery();
        }
        void insertRow(long identity, long first, long? last, TContent content)
        {
            insert.Parameters.Clear();
            insert.Parameters.AddWithValue("$identity", identity);
            insert.Parameters.AddWithValue("$first", first);
            insert.Parameters.AddWithValue("$last", (object?)last ?? DBNull.Value);
            bindContent(insert.Parameters, content);
            insert.ExecuteNonQuery();
        }
        void place(long identity, TContent content)
        {
            if (next is not null && startingNext.TryGetValue(identity, out var following) && following.Content.Equals(content))
                updateFirst(following.RowId, ordinal);
            else
                insertRow(identity, ordinal, next is null ? null : ordinal, content);
        }
        foreach (var row in coveringPrevious.Values)
        {
            var hasContent = manifest.TryGetValue(row.Identity, out var content);
            var spansNext = next is { } nextOrdinal && (row.Last is null || row.Last >= nextOrdinal);
            if (spansNext)
            {
                if (hasContent && content!.Equals(row.Content))
                    continue;
                updateLast(row.RowId, previous);
                insertRow(row.Identity, next!.Value, row.Last, row.Content);
                if (hasContent)
                    insertRow(row.Identity, ordinal, ordinal, content!);
                continue;
            }
            if (hasContent && content!.Equals(row.Content))
            {
                if (next is not null && startingNext.TryGetValue(row.Identity, out var following) && following.Content.Equals(content))
                {
                    updateLast(row.RowId, following.Last);
                    deleteRow(following.RowId);
                }
                else if (next is not null)
                    updateLast(row.RowId, ordinal);
                continue;
            }
            if (next is null && row.Last is null)
                updateLast(row.RowId, previous);
            if (hasContent)
                place(row.Identity, content!);
        }
        foreach (var (identity, content) in manifest)
            if (!coveringPrevious.ContainsKey(identity))
                place(identity, content);
    }

    public void Remove(SqliteConnection connection, SqliteTransaction transaction, Action<SqliteParameterCollection> bindScope, long? previous, long ordinal, long? next)
    {
        void execute(string commandText)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = commandText;
            command.Parameters.AddWithValue("$o", ordinal);
            command.Parameters.AddWithValue("$a", (object?)previous ?? DBNull.Value);
            command.Parameters.AddWithValue("$b", (object?)next ?? DBNull.Value);
            bindScope(command.Parameters);
            command.ExecuteNonQuery();
        }
        execute($"DELETE FROM {table} WHERE first_ord = $o AND last_ord = $o AND {scopeFilter}");
        if (next is null)
        {
            execute($"DELETE FROM {table} WHERE first_ord = $o AND last_ord IS NULL AND {scopeFilter}");
            if (previous is not null)
                execute($"UPDATE {table} SET last_ord = NULL WHERE last_ord = $a AND {scopeFilter}");
            return;
        }
        execute($"UPDATE {table} SET first_ord = $b WHERE first_ord = $o AND {scopeFilter}");
        if (previous is null)
            return;
        execute($"UPDATE {table} SET last_ord = $a WHERE last_ord = $o AND {scopeFilter}");
        var ending = LoadRows(connection, transaction, "last_ord = $a", parameters =>
        {
            parameters.AddWithValue("$a", previous.Value);
            bindScope(parameters);
        });
        var starting = LoadRows(connection, transaction, "first_ord = $b", parameters =>
        {
            parameters.AddWithValue("$b", next.Value);
            bindScope(parameters);
        });
        using var update = connection.CreateCommand();
        update.Transaction = transaction;
        update.CommandText = $"UPDATE {table} SET last_ord = $last WHERE id = $id";
        var updateId = update.Parameters.Add("$id", SqliteType.Integer);
        var updateLast = update.Parameters.Add("$last", SqliteType.Integer);
        using var delete = connection.CreateCommand();
        delete.Transaction = transaction;
        delete.CommandText = $"DELETE FROM {table} WHERE id = $id";
        var deleteId = delete.Parameters.Add("$id", SqliteType.Integer);
        foreach (var row in ending.Values)
            if (starting.TryGetValue(row.Identity, out var following) && following.Content.Equals(row.Content))
            {
                updateId.Value = row.RowId;
                updateLast.Value = following.Last is { } last ? last : DBNull.Value;
                update.ExecuteNonQuery();
                deleteId.Value = following.RowId;
                delete.ExecuteNonQuery();
            }
    }
}