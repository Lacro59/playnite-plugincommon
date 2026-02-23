using CommonPlayniteShared.Commands;
using Microsoft.Win32;
using Playnite.SDK;
using System;
using System.Collections.Generic;
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

        // ── Plugin name, used to build the default suggested filename ────────
        private readonly string _pluginName;

        // ── Dialog result ────────────────────────────────────────────────────
        public bool Result { get; private set; } = false;

        // ── Constructor ──────────────────────────────────────────────────────
        /// <param name="availableFields">Key = technical name, Value = localized name.</param>
        /// <param name="pluginName">Used to pre-fill the suggested filename.</param>
        public ExportCsvViewModel(Dictionary<string, string> availableFields, string pluginName = "Export")
        {
            _pluginName = pluginName;

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
                _pluginName,
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
    }
}