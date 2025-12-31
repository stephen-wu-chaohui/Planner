using Microsoft.Data.SqlClient;

namespace Planner.Tools.DbMigrator.Db;

internal sealed class SeedHistoryRepository {
    public async Task EnsureCreatedAsync(SqlConnection conn) {
        using var cmd = conn.CreateCommand();
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
                    [Checksum] VARBINARY(32) NOT NULL,
                    [AppliedAtUtc] DATETIME2 NOT NULL
                );

                CREATE UNIQUE INDEX UX_SeedHistory_ScriptName
                    ON [dbo].[__SeedHistory]([ScriptName]);
            END
            """;

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<bool> IsAppliedAsync(
        SqlConnection conn,
        SqlTransaction tx,
        SqlScript script) {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            SELECT [Checksum]
            FROM [dbo].[__SeedHistory]
            WHERE [ScriptName] = @name
            """;

        cmd.Parameters.AddWithValue("@name", script.Name);

        var result = await cmd.ExecuteScalarAsync();
        if (result is null)
            return false;

        var existingChecksum = (byte[])result;

        if (!existingChecksum.SequenceEqual(script.Checksum)) {
            throw new InvalidOperationException(
                $"Seed script '{script.Name}' was modified after being applied.");
        }

        return true;
    }

    public async Task RecordAsync(
        SqlConnection conn,
        SqlTransaction tx,
        SqlScript script) {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            INSERT INTO [dbo].[__SeedHistory] (
                [ScriptName],
                [Checksum],
                [AppliedAtUtc]
            )
            VALUES (
                @name,
                @checksum,
                SYSUTCDATETIME()
            )
            """;

        cmd.Parameters.AddWithValue("@name", script.Name);
        cmd.Parameters.Add("@checksum", System.Data.SqlDbType.VarBinary, 32)
           .Value = script.Checksum;

        await cmd.ExecuteNonQueryAsync();
    }
}
