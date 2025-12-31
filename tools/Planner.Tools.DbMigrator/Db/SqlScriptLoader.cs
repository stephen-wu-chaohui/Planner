using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Planner.Tools.DbMigrator.Db;

internal static class SqlScriptLoader {
    // Change this if you move the Sql folder
    private const string ResourceFolder = "SeedScripts";

    public static IReadOnlyList<SqlScript> Load() {
        var assembly = Assembly.GetExecutingAssembly();

        var resourceNames = assembly
            .GetManifestResourceNames()
            .Where(n => IsSqlSeedResource(n))
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (resourceNames.Length == 0) {
            throw new InvalidOperationException(
                $"No embedded SQL seed resources found under '{ResourceFolder}'. " +
                "Ensure .sql files are marked as EmbeddedResource.");
        }

        var scripts = new List<SqlScript>(resourceNames.Length);

        foreach (var resourceName in resourceNames) {
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException(
                    $"Unable to open embedded resource '{resourceName}'.");

            using var reader = new StreamReader(stream, Encoding.UTF8);
            var sql = reader.ReadToEnd();

            if (string.IsNullOrWhiteSpace(sql)) {
                throw new InvalidOperationException(
                    $"Embedded SQL seed '{resourceName}' is empty.");
            }

            scripts.Add(new SqlScript(
                Name: ExtractScriptName(resourceName),
                Sql: sql,
                Checksum: ComputeChecksum(sql)
            ));
        }

        return scripts;
    }

    private static bool IsSqlSeedResource(string resourceName) {
        // Example:
        // Planner.Tools.DbMigrator.Sql.001_reference_data.sql
        return resourceName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)
            && resourceName.Contains($".{ResourceFolder}.");
    }

    private static string ExtractScriptName(string resourceName) {
        // Keep only the filename portion for readability and stability
        var lastDot = resourceName.LastIndexOf('.');
        var secondLastDot = resourceName.LastIndexOf('.', lastDot - 1);

        return resourceName[(secondLastDot + 1)..];
    }

    private static byte[] ComputeChecksum(string sql) {
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(sql));
    }
}
