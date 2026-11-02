using CommonPluginsStores.Models;

namespace CommonPluginsStores.Models.Interfaces
{
    /// <summary>
    /// Probe-first session contract for store authentication (shared across plugins).
    /// Inspired by PlayniteAchievements <c>ISessionManager</c>.
    /// </summary>
    public interface IStoreSessionManager
    {
        /// <summary>
        /// Gets the unique key identifying this store (e.g. Steam, GOG, Epic, EA).
        /// </summary>
        string ProviderKey { get; }

        /// <summary>
        /// Probes the current authentication state from the source of truth (full verification).
        /// </summary>
        /// <returns>Probe result.</returns>
        AuthProbeResult ProbeAuthState();

        /// <summary>
        /// Performs interactive authentication. When <paramref name="forceInteractive"/> is false,
        /// probes first and returns early if already authenticated.
        /// </summary>
        /// <param name="forceInteractive">If true, clears session then runs login.</param>
        /// <returns>Authentication result.</returns>
        AuthProbeResult AuthenticateInteractive(bool forceInteractive);

        /// <summary>
        /// Clears the current session (cookies, tokens, profile fields as implemented by the store).
        /// </summary>
        void ClearSession();
    }
}
