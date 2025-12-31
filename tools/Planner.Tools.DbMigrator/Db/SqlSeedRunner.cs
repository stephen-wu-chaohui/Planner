using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Planner.Infrastructure.Persistence;

internal sealed class SqlSeedRunner(PlannerDbContext db) {

    public async Task RunAsync() {
        var conn = (SqlConnection)db.Database.GetDbConnection();
        await conn.OpenAsync();

        var scripts = SqlScriptLoader.LoadScripts();
        if (!scripts.Any())
            throw new InvalidOperationException(
                "No SQL seed scripts found. Check embedded resources.");

        foreach (var script in scripts) {
            if (await SeedHistoryRepository.IsAppliedAsync(conn, script))
                continue;

            using var tx = conn.BeginTransaction();
            try {
                var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = script.Sql;
                await cmd.ExecuteNonQueryAsync();

                await SeedHistoryRepository.RecordAsync(conn, script);
                tx.Commit();

                Console.WriteLine($"Applied {script.Name}");
            } catch {
                tx.Rollback();
                throw;
            }
        }
    }
}
