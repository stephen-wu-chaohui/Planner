namespace Planner.Messaging.Firestore;

/// <summary>
/// Shared Firestore collection names used across the application.
/// </summary>
public static class FirestoreCollections
{
    /// <summary>
    /// Collection for optimization results pending AI analysis.
    /// Also used by BlazorApp to receive real-time optimization results.
    /// </summary>
    public const string PendingAnalysis = "pending_analysis";
    
    /// <summary>
    /// Collection for AI-generated route insights.
    /// </summary>
    public const string RouteInsights = "route_insights";
}
