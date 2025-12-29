using VerifyTests;

public static class VerifyConfig {
    static VerifyConfig() {
        VerifierSettings.SortPropertiesAlphabetically();
        VerifierSettings.UseStrictJson();
    }
}
