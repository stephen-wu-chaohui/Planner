internal sealed record SqlScript(
    string Name,
    string Sql,
    byte[] Checksum
);
