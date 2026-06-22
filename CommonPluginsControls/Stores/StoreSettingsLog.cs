using CommonPluginsStores.Models.Enumerations;
using CommonPluginsStores.Models.Interfaces;
using CommonPluginsShared;
using Playnite.SDK;
using System;

namespace CommonPluginsControls.Stores
{
    /// <summary>
    /// Centralized logging for the stores settings master-detail UI and auth actions.
    /// </summary>
    internal static class StoreSettingsLog
    {
        private const string Prefix = "[StoreSettings]";
        private static readonly ILogger Logger = LogManager.GetLogger();

        public static void Info(string message)
        {
            Logger.Info($"{Prefix} {message}");
        }

        public static void Warn(string message)
        {
            Logger.Warn($"{Prefix} {message}");
        }

        public static void Error(Exception ex, string context)
        {
            Logger.Error(ex, $"{Prefix} {context}");
        }

        public static void Debug(string message)
        {
            Common.LogDebug(true, $"{Prefix} {message}");
        }

        public static void StoreRegistered(string storeId, bool isVisible, int sortOrder)
        {
            Info($"Store registered: {storeId} (visible={isVisible}, sortOrder={sortOrder})");
        }

        public static void StoreRegistrationSkipped(string reason)
        {
            Warn($"Store registration skipped: {reason}");
        }

        public static void AuthStatusChanged(string storeId, AuthStatus status, bool showIndicator)
        {
            Info($"Auth status changed for {storeId}: {status} (sidebar indicator={showIndicator})");
        }

        public static void AuthProviderBound(string storeId, bool hasProvider)
        {
            Debug($"Auth provider bound for {storeId}: hasProvider={hasProvider}");
        }

        public static void SelectedStoreChanged(string storeId)
        {
            Debug($"Selected store changed: {storeId ?? "(none)"}");
        }

        public static void PanelVisibilityUpdated(string storeId, bool isVisible)
        {
            Debug($"Panel visibility updated for {storeId}: visible={isVisible}");
        }

        public static void RefreshStores(int storeCount)
        {
            Debug($"RefreshStores invoked for {storeCount} registered store(s)");
        }

        public static void LoginRequested(IStoreApi storeApi)
        {
            Info($"Login requested for {ResolveStoreLabel(storeApi)}");
        }

        public static void LoginCompleted(IStoreApi storeApi, AuthStatus status)
        {
            Info($"Login completed for {ResolveStoreLabel(storeApi)}: {status}");
        }

        public static void LoginAlternativeRequested(IStoreApi storeApi)
        {
            Info($"Alternative login requested for {ResolveStoreLabel(storeApi)}");
        }

        public static void LogoutRequested(IStoreApi storeApi)
        {
            Info($"Logout requested for {ResolveStoreLabel(storeApi)}");
        }

        public static void LogoutCompleted(IStoreApi storeApi, AuthStatus status)
        {
            Info($"Logout completed for {ResolveStoreLabel(storeApi)}: {status}");
        }

        public static void ClearSessionStarted(string clientName)
        {
            Info($"ClearSession started for {clientName}");
        }

        public static void ClearSessionCompleted(string clientName)
        {
            Info($"ClearSession completed for {clientName}");
        }

        private static string ResolveStoreLabel(IStoreApi storeApi)
        {
            if (storeApi == null)
            {
                return "unknown store";
            }

            return storeApi.GetType().Name;
        }
    }
}
