using CommonPluginsStores.Models.Enumerations;
using System;

namespace CommonPluginsStores.Models
{
    /// <summary>
    /// Result of probing authentication state from the source of truth (cookies, tokens, network).
    /// Shared contract inspired by PlayniteAchievements <c>AuthProbeResult</c>.
    /// </summary>
    public sealed class AuthProbeResult
    {
        /// <summary>
        /// Gets the outcome of the authentication probe.
        /// </summary>
        public AuthOutcome Outcome { get; private set; }

        /// <summary>
        /// Gets an optional localization key for UI messaging (may be null).
        /// </summary>
        public string MessageKey { get; private set; }

        /// <summary>
        /// Gets the user id when authenticated; otherwise null.
        /// </summary>
        public string UserId { get; private set; }

        /// <summary>
        /// Gets token expiry when available.
        /// </summary>
        public DateTime? ExpiresUtc { get; private set; }

        /// <summary>
        /// Gets whether a login window was opened during this attempt.
        /// </summary>
        public bool WindowOpened { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the user is authenticated.
        /// </summary>
        public bool IsSuccess
        {
            get
            {
                return Outcome == AuthOutcome.AlreadyAuthenticated
                    || Outcome == AuthOutcome.Authenticated;
            }
        }

        private AuthProbeResult()
        {
        }

        /// <summary>
        /// Creates a result indicating the user is already authenticated.
        /// </summary>
        /// <param name="userId">Optional user identifier.</param>
        /// <param name="expiresUtc">Optional token expiry.</param>
        /// <returns>Probe result.</returns>
        public static AuthProbeResult AlreadyAuthenticated(string userId = null, DateTime? expiresUtc = null)
        {
            return new AuthProbeResult
            {
                Outcome = AuthOutcome.AlreadyAuthenticated,
                UserId = userId,
                ExpiresUtc = expiresUtc,
                WindowOpened = false
            };
        }

        /// <summary>
        /// Creates a result indicating interactive authentication succeeded.
        /// </summary>
        /// <param name="userId">Optional user identifier.</param>
        /// <param name="expiresUtc">Optional token expiry.</param>
        /// <param name="windowOpened">Whether a login window was shown.</param>
        /// <returns>Probe result.</returns>
        public static AuthProbeResult Authenticated(string userId = null, DateTime? expiresUtc = null, bool windowOpened = false)
        {
            return new AuthProbeResult
            {
                Outcome = AuthOutcome.Authenticated,
                UserId = userId,
                ExpiresUtc = expiresUtc,
                WindowOpened = windowOpened
            };
        }

        /// <summary>
        /// Creates a result indicating the user is not authenticated.
        /// </summary>
        /// <returns>Probe result.</returns>
        public static AuthProbeResult NotAuthenticated()
        {
            return new AuthProbeResult
            {
                Outcome = AuthOutcome.NotAuthenticated,
                WindowOpened = false
            };
        }

        /// <summary>
        /// Creates a result indicating authentication timed out.
        /// </summary>
        /// <param name="windowOpened">Whether a login window was shown.</param>
        /// <returns>Probe result.</returns>
        public static AuthProbeResult TimedOut(bool windowOpened = false)
        {
            return new AuthProbeResult
            {
                Outcome = AuthOutcome.TimedOut,
                WindowOpened = windowOpened
            };
        }

        /// <summary>
        /// Creates a result indicating authentication was cancelled.
        /// </summary>
        /// <param name="windowOpened">Whether a login window was shown.</param>
        /// <returns>Probe result.</returns>
        public static AuthProbeResult Cancelled(bool windowOpened = false)
        {
            return new AuthProbeResult
            {
                Outcome = AuthOutcome.Cancelled,
                WindowOpened = windowOpened
            };
        }

        /// <summary>
        /// Creates a result indicating authentication failed.
        /// </summary>
        /// <param name="windowOpened">Whether a login window was shown.</param>
        /// <returns>Probe result.</returns>
        public static AuthProbeResult Failed(bool windowOpened = false)
        {
            return new AuthProbeResult
            {
                Outcome = AuthOutcome.Failed,
                WindowOpened = windowOpened
            };
        }

        /// <summary>
        /// Creates a result indicating the probe itself failed.
        /// </summary>
        /// <returns>Probe result.</returns>
        public static AuthProbeResult ProbeFailed()
        {
            return new AuthProbeResult
            {
                Outcome = AuthOutcome.ProbeFailed,
                WindowOpened = false
            };
        }
    }
}
