using CommonPlayniteShared.Commands;
using Microsoft.Win32;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace CommonPluginsControls.ViewModels
{
    public class ExportField : ObservableObject
    {
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetValue(ref _isSelected, value);
        }

        public string TechnicalName { get; set; }
        public string LocalizedName { get; set; }
    }

    public class ExportCsvViewModel : ObservableObject
    {
        // ── Fields collection ────────────────────────────────────────────────
        public ObservableCollection<ExportField> Fields { get; set; }

        // ── Separator options ────────────────────────────────────────────────
        public List<string> Separators { get; } = new List<string> { ";", ",", "Tab" };

        private string _selectedSeparator = ";";
        public string SelectedSeparator
        {
            get => _selectedSeparator;
            set => SetValue(ref _selectedSeparator, value);
        }

        // ── Output file path (new) ───────────────────────────────────────────
        private string _outputPath;
        public string OutputPath
        {
            get => _outputPath;
            set => SetValue(ref _outputPath, value);
        }

        // ── Base string for the default suggested filename (Browse dialog) ───
        private readonly string _suggestedFileNameBase;

        // ── Dialog result ────────────────────────────────────────────────────
        public bool Result { get; private set; } = false;

        // ── Constructor ──────────────────────────────────────────────────────
        /// <param name="availableFields">Key = technical name, Value = localized name.</param>
        /// <param name="pluginName">Plugin display name.</param>
        /// <param name="suggestedFileNameBase">
        /// When set, used as the first segment of the default CSV filename instead of <paramref name="pluginName"/>.
        /// Invalid file-name characters are replaced with underscores and the value is trimmed to a safe length.
        /// </param>
        public ExportCsvViewModel(Dictionary<string, string> availableFields, string pluginName = "Export", string suggestedFileNameBase = null)
        {
            _suggestedFileNameBase = SanitizeSuggestedFileNameBase(suggestedFileNameBase, pluginName);

            Fields = new ObservableCollection<ExportField>(
                availableFields.Select(f => new ExportField
                {
                    IsSelected = true,
                    TechnicalName = f.Key,
                    LocalizedName = f.Value
                })
            );
        }

        // ── Commands ─────────────────────────────────────────────────────────

        /// <summary>Selects or deselects all fields at once.</summary>
        public ICommand SelectAllCommand => new RelayCommand<bool>(isFilled =>
        {
            foreach (ExportField field in Fields)
            {
                field.IsSelected = isFilled;
            }
        });

        /// <summary>Opens a SaveFileDialog so the user picks the destination file.</summary>
        public ICommand BrowseOutputCommand => new RelayCommand(() =>
        {
            string defaultFileName = string.Format(
                "{0}_Export_{1}.csv",
                _suggestedFileNameBase,
                DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));

            var dialog = new SaveFileDialog
            {
                Filter = "CSV|*.csv",
                FileName = defaultFileName,
                Title = ResourceProvider.GetString("LOCCommonExportOutputFile")
            };

            if (dialog.ShowDialog() == true)
            {
                OutputPath = dialog.FileName;
            }
        });

        /// <summary>
        /// Validates the form and closes the window.
        /// CanExecute blocks the button when no output path is set.
        /// </summary>
        public ICommand ConfirmCommand => new RelayCommand<System.Windows.Window>(
            win =>
            {
                Result = true;
                win?.Close();
            },
            win => !string.IsNullOrWhiteSpace(OutputPath) // CanExecute
        );

        // ── Helpers ──────────────────────────────────────────────────────────

        /// <summary>Returns only the fields the user has checked.</summary>
        public List<string> GetSelectedFields() =>
            Fields.Where(x => x.IsSelected).Select(x => x.TechnicalName).ToList();

        /// <summary>Resolves "Tab" to the actual tab character.</summary>
        public string GetRealSeparator() =>
            SelectedSeparator == "Tab" ? "\t" : SelectedSeparator;

        private static string SanitizeSuggestedFileNameBase(string suggested, string pluginNameFallback)
        {
            string raw = string.IsNullOrWhiteSpace(suggested) ? pluginNameFallback : suggested.Trim();
            if (string.IsNullOrEmpty(raw))
            {
                raw = "Export";
            }

            char[] invalid = Path.GetInvalidFileNameChars();
            var chars = new char[Math.Min(raw.Length, 180)];
            int len = 0;
            for (int i = 0; i < raw.Length && len < chars.Length; i++)
            {
                char c = raw[i];
                if (Array.IndexOf(invalid, c) >= 0)
                {
                    c = '_';
                }
                chars[len++] = c;
            }

            string trimmed = new string(chars, 0, len).Trim();
            if (trimmed.Length == 0)
            {
                trimmed = "Export";
            }

            return trimmed;
        }
    }
}