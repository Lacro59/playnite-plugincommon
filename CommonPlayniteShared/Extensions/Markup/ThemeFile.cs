﻿using CommonPlayniteShared.Common;
//using Playnite.Extensions;
using Playnite.SDK;
//using Playnite.Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xaml;
using System.Diagnostics;
using System.Text.RegularExpressions;
using CommonPlayniteShared.Manifests;

namespace CommonPlayniteShared.Extensions.Markup
{
    public class ThemeFile : MarkupExtension
    {
        private static FileInfo lastUserTheme = null;
        private static bool? lastUserThemeFound = null;

        internal ThemeManifest CurrentTheme { get; set; }
        internal ThemeManifest DefaultTheme { get; set; }

        public string RelativePath { get; set; }

        public ThemeFile(ApplicationMode mode)
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                DefaultTheme = GetDesignTimeDefaultTheme(mode);
            }
            else
            {
                DefaultTheme = ThemeManager.DefaultTheme;
                CurrentTheme = ThemeManager.CurrentTheme;
            }
        }

        public ThemeFile(string path, ApplicationMode mode) : this(mode)
        {
            RelativePath = path;
        }

        public static ThemeManifest GetDesignTimeDefaultTheme(ApplicationMode mode)
        {
            if (lastUserThemeFound == null)
            {
                if (Process.GetCurrentProcess().TryGetParentId(out var parentId))
                {
                    var proc = Process.GetProcessById(parentId);
                    var cmdline = proc.GetCommandLine();
                    var regEx = Regex.Match(cmdline, @"([^""]+\.sln?)""");
                    if (regEx.Success)
                    {
                        var spath = regEx.Groups[1].Value;
                        if (spath.Contains($"Themes\\{mode.GetDescription()}"))
                        {
                            lastUserTheme = new FileInfo(spath);
                            lastUserThemeFound = true;
                        }
                    }
                }
            }

            if (lastUserThemeFound == true)
            {
                return new ThemeManifest()
                {
                    DirectoryName = lastUserTheme.DirectoryName,
                    DirectoryPath = lastUserTheme.Directory.FullName,
                    Name = "Default"
                };
            }

            lastUserThemeFound = false;
            var defaultTheme = "Default";
            var projectName = mode == ApplicationMode.Fullscreen ? "Playnite.FullscreenApp" : "Playnite.DesktopApp";
            var slnPath = Path.Combine(Environment.GetEnvironmentVariable("PLAYNITE_SLN", EnvironmentVariableTarget.User), projectName);
            var themePath = Path.Combine(slnPath, "Themes", ThemeManager.GetThemeRootDir(mode), defaultTheme);
            return new ThemeManifest()
            {
                DirectoryName = defaultTheme,
                DirectoryPath = themePath,
                Name = defaultTheme
            };
        }

        public static string GetFilePath(string relPath, bool checkExistance = true)
        {
            return GetFilePath(relPath, ThemeManager.DefaultTheme, ThemeManager.CurrentTheme, checkExistance);
        }

        public static string GetFilePath(string relPath, ThemeManifest defaultTheme, bool checkExistance = true)
        {
            return GetFilePath(relPath, defaultTheme, ThemeManager.CurrentTheme, checkExistance);
        }

        public static string GetFilePath(string relPath, ThemeManifest defaultTheme, ThemeManifest currentTheme, bool checkExistance = true)
        {
            var relativePath = Paths.FixSeparators(relPath).TrimStart(new char[] { Path.DirectorySeparatorChar });

            if (currentTheme != null)
            {
                var themePath = Path.Combine(currentTheme.DirectoryPath, relativePath);
                if (File.Exists(themePath) && checkExistance)
                {
                    return themePath;
                }
                else if (!checkExistance)
                {
                    return themePath;
                }
            }

            if (defaultTheme != null)
            {
                var defaultPath = Path.Combine(defaultTheme.DirectoryPath, relativePath);
                if (File.Exists(defaultPath) && checkExistance)
                {
                    return defaultPath;
                }
                else if (!checkExistance)
                {
                    return defaultPath;
                }
            }

            return null;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            string path = GetFilePath(RelativePath, DefaultTheme, CurrentTheme);
            if (path.IsNullOrEmpty())
            {
                return null;
            }

            var provider = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
            var type = IProvideValueTargetExtensions.GetTargetType(provider);
            var converter = TypeDescriptor.GetConverter(type);
            return converter.ConvertFrom(path);
        }
    }
}
