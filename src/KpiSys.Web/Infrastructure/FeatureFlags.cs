namespace KpiSys.Web;

/// <summary>
/// Feature flags that allow work-in-progress functionality to be toggled
/// without removing the underlying implementation.
/// </summary>
public static class FeatureFlags
{
    /// <summary>
    /// Controls whether session-based login enforcement is active.
    /// Set to <c>false</c> during early development to keep login checks hidden
    /// until all related features are ready, then switch to <c>true</c> to enable
    /// the existing authorization flow.
    /// </summary>
    public const bool EnableLoginGuard = false;
}
