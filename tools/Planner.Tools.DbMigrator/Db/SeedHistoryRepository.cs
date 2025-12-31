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

    public static async Task EnsureCreatedAsync(SqlConnection conn) {
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            IF OBJECT_ID(N'dbo.__SeedHistory', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.__SeedHistory (
                    [Id] INT IDENTITY(1,1) PRIMARY KEY,
                    [ScriptName] NVARCHAR(255) NOT NULL,
                    [Checksum] NVARCHAR(255) NOT NULL,
                    [ExecutedAt] DATETIME2 NOT NULL
                );
            END
        """;

        await cmd.ExecuteNonQueryAsync();
    }
}
