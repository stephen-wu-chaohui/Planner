using System.Security.Cryptography;
using System.Text;

internal static class SqlScriptFactory {
    public static SqlScript FromText(string name, string sql) {
        using var sha256 = SHA256.Create();
        var checksum = sha256.ComputeHash(Encoding.UTF8.GetBytes(sql));

        return new SqlScript(name, sql, checksum);
    }
}
