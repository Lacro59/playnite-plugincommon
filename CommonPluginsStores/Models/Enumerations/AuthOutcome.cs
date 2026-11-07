namespace CommonPluginsStores.Models.Enumerations
{
    /// <summary>
    /// Outcome of an authentication probe or interactive authentication attempt.
    /// Aligned with PlayniteAchievements <c>AuthOutcome</c> for shared store auth.
    /// </summary>
    public enum AuthOutcome
    {
        /// <summary>
        /// Authentication was already valid; no interactive login needed.
        /// </summary>
        AlreadyAuthenticated,

        /// <summary>
        /// Authentication completed successfully via login flow.
        /// </summary>
        Authenticated,

        /// <summary>
        /// User is not authenticated.
        /// </summary>
        NotAuthenticated,

        /// <summary>
        /// Authentication timed out waiting for user action.
        /// </summary>
        TimedOut,

        /// <summary>
        /// Authentication was cancelled by the user.
        /// </summary>
        Cancelled,

        /// <summary>
        /// Authentication failed due to an error.
        /// </summary>
        Failed,

        /// <summary>
        /// Probe to check authentication status failed.
        /// </summary>
        ProbeFailed
    }
}
