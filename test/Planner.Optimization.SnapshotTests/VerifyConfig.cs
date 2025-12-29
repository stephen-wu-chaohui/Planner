using VerifyTests;

public static class VerifyConfig {
    static VerifyConfig() {
        // Stable ordering of JSON properties
        VerifierSettings.SortPropertiesAlphabetically();

        // Strict comparison (no silent coercions)
        VerifierSettings.UseStrictJson();

        // Optional but recommended:
        // prevents path-related noise in snapshots
        VerifierSettings.ScrubLinesContaining(
            "CompletedAt",
            "Timestamp"
        );
    }
}
