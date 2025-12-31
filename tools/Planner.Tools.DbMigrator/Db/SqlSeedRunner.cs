using Microsoft.Data.SqlClient;

namespace Planner.Tools.DbMigrator.Db;

internal sealed class SqlSeedRunner {
    private readonly string _connectionString;
    private readonly IReadOnlyList<SqlScript> _scripts;
    private readonly SeedHistoryRepository _seedHistoryRepository;

    public SqlSeedRunner(
        string connectionString,
        IReadOnlyList<SqlScript> scripts,
        SeedHistoryRepository seedHistoryRepository) {
        _connectionString = connectionString;
        _scripts = scripts;
        _seedHistoryRepository = seedHistoryRepository;
    }

    public async Task RunAsync() {
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        // Ensure seed bookkeeping exists (idempotent)
        await _seedHistoryRepository.EnsureCreatedAsync(conn);

        foreach (var script in _scripts) {
            using var tx = conn.BeginTransaction();

            if (await _seedHistoryRepository.IsAppliedAsync(conn, tx, script)) {
                tx.Commit();
                continue;
            }

            await ExecuteScriptAsync(conn, tx, script);
            await _seedHistoryRepository.RecordAsync(conn, tx, script);

            tx.Commit();
        }
    }

    private static async Task ExecuteScriptAsync(
        SqlConnection conn,
        SqlTransaction tx,
        SqlScript script) {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = script.Sql;
        cmd.CommandType = System.Data.CommandType.Text;

        await cmd.ExecuteNonQueryAsync();
    }
}
