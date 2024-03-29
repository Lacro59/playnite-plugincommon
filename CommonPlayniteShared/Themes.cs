﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Playnite;
using System.Windows;
using System.Windows.Markup;
using System.Text.RegularExpressions;
using CommonPlayniteShared.Extensions.Markup;
using CommonPlayniteShared.Common;
using Playnite.SDK;
using YamlDotNet.Serialization;
//using CommonPlayniteShared.API;
using CommonPlayniteShared.Manifests;
using System.Windows.Input;

namespace CommonPlayniteShared
{
    public class ThemeManager
    {
        private static ILogger logger = LogManager.GetLogger();
        public static System.Version DesktopApiVersion => new System.Version("1.9.0");
        public static System.Version FullscreenApiVersion => new System.Version("1.9.0");
        public static ThemeManifest CurrentTheme { get; private set; }
        public static ThemeManifest DefaultTheme { get; private set; }

        public static System.Version GetApiVersion(ApplicationMode mode)
        {
            return mode == ApplicationMode.Desktop ? DesktopApiVersion : FullscreenApiVersion;
        }

        public static string GetThemeRootDir(ApplicationMode mode)
        {
            return mode == ApplicationMode.Desktop ? "Desktop" : "Fullscreen";
        }

        public static void SetCurrentTheme(ThemeManifest theme)
        {
            CurrentTheme = theme;
        }

        public static void SetDefaultTheme(ThemeManifest theme)
        {
            DefaultTheme = theme;
        }

        //public static void ApplyFullscreenButtonPrompts(Application app, FullscreenButtonPrompts prompts)
        //{
        //    if (prompts == FullscreenSettings.DefaultButtonPrompts)
        //    {
        //        var defaultXaml = $"{FullscreenSettings.DefaultButtonPrompts.ToString()}.xaml";
        //        foreach (var dir in PlayniteApplication.CurrentNative.Resources.MergedDictionaries.ToList())
        //        {
        //            if (dir.Source == null)
        //            {
        //                continue;
        //            }
        //
        //            if (dir.Source.OriginalString.Contains("ButtonPrompts") &&
        //                !dir.Source.OriginalString.EndsWith(defaultXaml))
        //            {
        //                PlayniteApplication.CurrentNative.Resources.MergedDictionaries.Remove(dir);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        var promptsPath = Path.Combine(ThemeManager.DefaultTheme.DirectoryPath, "Images", "ButtonPrompts");
        //        foreach (var dir in Directory.GetDirectories(promptsPath))
        //        {
        //            var dirInfo = new DirectoryInfo(dir);
        //            var promptXaml = Path.Combine(dir, $"{dirInfo.Name}.xaml");
        //            if (File.Exists(promptXaml) && dirInfo.Name == prompts.ToString())
        //            {
        //                var xaml = Xaml.FromFile(promptXaml);
        //                if (xaml is ResourceDictionary xamlDir)
        //                {
        //                    xamlDir.Source = new Uri(promptXaml, UriKind.Absolute);
        //                    PlayniteApplication.CurrentNative.Resources.MergedDictionaries.Add(xamlDir);
        //                }
        //            }
        //        }
        //    }
        //}

        public static bool ApplyTheme(Application app, ThemeManifest theme, ApplicationMode mode)
        {
            if (theme.Id.IsNullOrEmpty())
            {
                logger.Error($"Theme {theme.Name}, doesn't have ID.");
                return false;
            }

            var apiVesion = mode == ApplicationMode.Desktop ? DesktopApiVersion : FullscreenApiVersion;
            if (!theme.ThemeApiVersion.IsNullOrEmpty())
            {
                var themeVersion = new Version(theme.ThemeApiVersion);
                if (themeVersion.Major != apiVesion.Major || themeVersion > apiVesion)
                {
                    logger.Error($"Failed to apply {theme.Name} theme, unsupported API version {theme.ThemeApiVersion}.");
                    return false;
                }
            }

            var allLoaded = true;
            var loadedXamls = new List<ResourceDictionary>();
            var acceptableXamls = new List<string>();
            var defaultRoot = $"Themes/{mode.GetDescription()}/{DefaultTheme.DirectoryName}/";
            foreach (var dict in app.Resources.MergedDictionaries)
            {
                if (dict.Source.OriginalString.StartsWith("Themes") && dict.Source.OriginalString.EndsWith("xaml"))
                {
                    acceptableXamls.Add(dict.Source.OriginalString.Replace(defaultRoot, "").Replace('/', '\\'));
                }
            }

            foreach (var accXaml in acceptableXamls)
            {
                var xamlPath = Path.Combine(theme.DirectoryPath, accXaml);
                if (!File.Exists(xamlPath))
                {
                    continue;
                }

                try
                {
                    var xaml = Xaml.FromFile(xamlPath);
                    if (xaml is ResourceDictionary xamlDir)
                    {
                        xamlDir.Source = new Uri(xamlPath, UriKind.Absolute);
                        loadedXamls.Add(xamlDir as ResourceDictionary);
                    }
                    else
                    {
                        logger.Error($"Skipping theme file {xamlPath}, it's not resource dictionary.");
                    }
                }
                catch (Exception e) //when (!PlayniteEnvironment.ThrowAllErrors)
                {
                    logger.Error(e, $"Failed to load xaml {xamlPath}");
                    allLoaded = false;
                    break;
                }
            }

            try
            {
                var cursorFile = ThemeFile.GetFilePath("cursor.cur");
                if (cursorFile.IsNullOrEmpty())
                {
                    cursorFile = ThemeFile.GetFilePath("cursor.ani");
                }

                if (!cursorFile.IsNullOrEmpty())
                {
                    Mouse.OverrideCursor = new Cursor(cursorFile, true);
                }
            }
            catch (Exception e) //when (!PlayniteEnvironment.ThrowAllErrors)
            {
                logger.Error(e, "Failed to set custom mouse cursor.");
            }

            if (allLoaded)
            {
                loadedXamls.ForEach(a => app.Resources.MergedDictionaries.Add(a));
                return true;
            }

            return false;
        }

        public static IEnumerable<ThemeManifest> GetAvailableThemes()
        {
            foreach (var theme in GetAvailableThemes(ApplicationMode.Desktop))
            {
                yield return theme;
            }

            foreach (var theme in GetAvailableThemes(ApplicationMode.Fullscreen))
            {
                yield return theme;
            }
        }

        public static List<ThemeManifest> GetAvailableThemes(ApplicationMode mode)
        {
            var modeDir = GetThemeRootDir(mode);
            var added = new List<string>();
            var themes = new List<ThemeManifest>();

            var userPath = Path.Combine(PlaynitePaths.ThemesUserDataPath, modeDir);
            if (!PlayniteSettings.IsPortable && Directory.Exists(userPath))
            {
                foreach (var dir in Directory.GetDirectories(userPath))
                {
                    try
                    {
                        var descriptorPath = Path.Combine(dir, PlaynitePaths.ThemeManifestFileName);
                        if (File.Exists(descriptorPath))
                        {
                            var info = new FileInfo(descriptorPath);
                            added.Add(info.Directory.Name);
                            var man = new ThemeManifest(descriptorPath);
                            if (!man.Id.IsNullOrEmpty())
                            {
                                themes.Add(man);
                            }
                        }
                    }
                    catch (Exception e) //when (!PlayniteEnvironment.ThrowAllErrors)
                    {
                        logger.Error(e, $"Failed to load theme info {dir}");
                    }
                }
            }

            var programPath = Path.Combine(PlaynitePaths.ThemesProgramPath, modeDir);
            if (Directory.Exists(programPath))
            {
                foreach (var dir in Directory.GetDirectories(programPath))
                {
                    try
                    {
                        var descriptorPath = Path.Combine(dir, PlaynitePaths.ThemeManifestFileName);
                        if (File.Exists(descriptorPath))
                        {
                            var info = new FileInfo(descriptorPath);
                            if (!added.Contains(info.Directory.Name))
                            {
                                var man = new ThemeManifest(descriptorPath);
                                if (!man.Id.IsNullOrEmpty())
                                {
                                    themes.Add(man);
                                }
                            }
                        }
                    }
                    catch (Exception e) //when (!PlayniteEnvironment.ThrowAllErrors)
                    {
                        logger.Error(e, $"Failed to load theme info {dir}");
                    }
                }
            }

            return themes;
        }
    }
}