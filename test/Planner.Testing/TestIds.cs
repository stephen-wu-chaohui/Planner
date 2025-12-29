namespace Planner.Testing;

public static class TestIds {
    public static readonly Guid TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid RunId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    // LocationIds (long) – stable and non-overlapping
    public const long Depot1Loc = 1001;
    public const long Depot2Loc = 1002;

    public const long Job1Loc = 2001;
    public const long Job2Loc = 2002;
    public const long Job3Loc = 2003;

    // Vehicle IDs (int)
    public const int Vehicle1 = 1;
    public const int Vehicle2 = 2;

    // Job IDs (int)
    public const int Job1 = 11;
    public const int Job2 = 12;
    public const int Job3 = 13;
}
