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
        IF NOT EXISTS (
            SELECT 1
            FROM sys.objects
            WHERE object_id = OBJECT_ID(N'[dbo].[__SeedHistory]')
              AND type = 'U'
        )
        BEGIN
            CREATE TABLE [dbo].[__SeedHistory] (
                [Id] INT IDENTITY(1,1) PRIMARY KEY,
                [ScriptName] NVARCHAR(255) NOT NULL,
                [AppliedAtUtc] DATETIME2 NOT NULL
            );
        END
        """;

        await cmd.ExecuteNonQueryAsync();
    }
}
