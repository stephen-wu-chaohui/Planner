using System.Reflection;
using System.Security.Cryptography;
using System.Text;

internal sealed class SqlScriptLoader {
    public static IReadOnlyList<SqlScript> LoadScripts() {
        var asm = Assembly.GetExecutingAssembly();

        return asm.GetManifestResourceNames()
            .Where(name =>
                name.Contains(".SeedScripts.") &&
                name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name)
            .Select(name => LoadScript(asm, name))
            .ToList();
    }

    private static SqlScript LoadScript(Assembly asm, string resourceName) {
        using var stream = asm.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);

        var sql = reader.ReadToEnd();
        var checksum = SHA256.HashData(
            Encoding.UTF8.GetBytes(sql));

        return new SqlScript(
            Name: Path.GetFileName(resourceName),
            Sql: sql,
            Checksum: checksum
        );
    }
}
