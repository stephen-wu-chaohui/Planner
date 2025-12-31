using Microsoft.Data.SqlClient;

internal sealed class SeedHistoryRepository {
    public static async Task<bool> IsAppliedAsync(
        SqlConnection conn, SqlScript script) {

        var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            SELECT 1
            FROM __SeedHistory
            WHERE ScriptName = @name AND Checksum = @checksum
            """;

        cmd.Parameters.AddWithValue("@name", script.Name);
        cmd.Parameters.AddWithValue("@checksum", script.Checksum);

        return await cmd.ExecuteScalarAsync() is not null;
    }

    public static async Task RecordAsync(
        SqlConnection conn, SqlScript script) {

        var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            INSERT INTO __SeedHistory
            (ScriptName, Checksum, ExecutedAt)
            VALUES (@name, @checksum, SYSUTCDATETIME())
            """;

        cmd.Parameters.AddWithValue("@name", script.Name);
        cmd.Parameters.AddWithValue("@checksum", script.Checksum);

        await cmd.ExecuteNonQueryAsync();
    }
}
