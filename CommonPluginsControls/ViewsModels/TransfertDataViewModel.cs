using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Interfaces;
using CommonPluginsShared.Models;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace CommonPluginsControls.ViewModels
{
    /// <summary>
    /// ViewModel for the TransfertData dialog.
    /// Handles the logic for transferring or merging plugin data between two games.
    /// Both lists support live filtering via an editable ComboBox, insensitive to
    /// case and diacritics (accents).
    /// </summary>
    public class TransfertDataViewModel : ObservableObject
    {
        private static readonly ILogger Logger = LogManager.GetLogger();
        private readonly IPluginDatabase _pluginDatabase;

        // ── Close event ───────────────────────────────────────────────────────

        /// <summary>
        /// Raised when the ViewModel requests the dialog to be closed
        /// (after a successful transfer or on an explicit close request).
        /// </summary>
        public event Action RequestClose;

        // ── Raw collections (never modified after construction) ───────────────

        private readonly ObservableCollection<DataGame> _sourceGames;
        private readonly ObservableCollection<DataGame> _targetGames;

        // ── Filtered views exposed to the UI ─────────────────────────────────

        /// <summary>
        /// Filtered view of source games, bound to the source ComboBox.
        /// Refreshed whenever <see cref="SearchTextSource"/> changes.
        /// </summary>
        public ICollectionView SourceGamesView { get; }

        /// <summary>
        /// Filtered view of target games, bound to the target ComboBox.
        /// Refreshed whenever <see cref="SearchTextTarget"/> changes.
        /// </summary>
        public ICollectionView TargetGamesView { get; }

        // ── Search / selection guard flags ────────────────────────────────────

        // When the user clicks an item, WPF writes ToString() back into the
        // ComboBox Text binding. That write must NOT trigger a filter refresh,
        // otherwise the view collapses and SelectedItem becomes null.
        // These flags are set by the code-behind SelectionChanged handler
        // before WPF pushes the new Text value, and cleared immediately after.
        internal bool IsSelectingSource = false;
        internal bool IsSelectingTarget = false;

        // ── Search text properties ────────────────────────────────────────────

        private string _searchTextSource = string.Empty;
        /// <summary>
        /// Text typed by the user in the source ComboBox.
        /// Triggers a filter refresh only when the change originates from a keystroke,
        /// not from a programmatic selection (guarded by <see cref="IsSelectingSource"/>).
        /// </summary>
        public string SearchTextSource
        {
            get => _searchTextSource;
            set
            {
                if (_searchTextSource == value) return;
                SetValue(ref _searchTextSource, value ?? string.Empty);

                // Do NOT refresh the filter when WPF is writing back the selected
                // item's ToString() — that write comes from the selection, not the user.
                if (!IsSelectingSource)
                {
                    SourceGamesView.Refresh();
                }
            }
        }

        private string _searchTextTarget = string.Empty;
        /// <summary>
        /// Text typed by the user in the target ComboBox.
        /// Triggers a filter refresh only when the change originates from a keystroke,
        /// not from a programmatic selection (guarded by <see cref="IsSelectingTarget"/>).
        /// </summary>
        public string SearchTextTarget
        {
            get => _searchTextTarget;
            set
            {
                if (_searchTextTarget == value) return;
                SetValue(ref _searchTextTarget, value ?? string.Empty);

                if (!IsSelectingTarget)
                {
                    TargetGamesView.Refresh();
                }
            }
        }

        // ── Selected items ────────────────────────────────────────────────────

        private DataGame _selectedSource;
        /// <summary>Currently selected source game (with plugin data).</summary>
        public DataGame SelectedSource
        {
            get => _selectedSource;
            set
            {
                SetValue(ref _selectedSource, value);
                OnPropertyChanged(nameof(IsTransferEnabled));
            }
        }

        private DataGame _selectedTarget;
        /// <summary>Currently selected target game (destination).</summary>
        public DataGame SelectedTarget
        {
            get => _selectedTarget;
            set
            {
                SetValue(ref _selectedTarget, value);
                OnPropertyChanged(nameof(IsTransferEnabled));
            }
        }

        // ── Options ───────────────────────────────────────────────────────────

        private bool _isMergeMode = true;
        /// <summary>When true, data is merged into the target instead of overwriting it.</summary>
        public bool IsMergeMode
        {
            get => _isMergeMode;
            set => SetValue(ref _isMergeMode, value);
        }

        private bool _isSourceSelectable = true;
        /// <summary>
        /// Controls whether the source ComboBox is interactive.
        /// Set to false when the dialog is opened for a single specific game.
        /// </summary>
        public bool IsSourceSelectable
        {
            get => _isSourceSelectable;
            set => SetValue(ref _isSourceSelectable, value);
        }

        // ── Computed state ────────────────────────────────────────────────────

        /// <summary>
        /// True when both source and target are selected and refer to different games.
        /// Bound to the Transfer button's IsEnabled property via the command CanExecute.
        /// </summary>
        public bool IsTransferEnabled =>
            SelectedSource != null &&
            SelectedTarget != null &&
            SelectedSource.Id != SelectedTarget.Id;

        // ── Commands ──────────────────────────────────────────────────────────

        /// <summary>Executes the data transfer then requests the dialog to close.</summary>
        public RelayCommand TransferCommand { get; }

        /// <summary>Closes the dialog without performing any action.</summary>
        public RelayCommand CloseCommand { get; }

        // ── Constructor ───────────────────────────────────────────────────────

        /// <summary>
        /// Initialises the ViewModel with the list of source games and the plugin database.
        /// </summary>
        /// <param name="sourceGames">Games that have plugin data and can be used as a source.</param>
        /// <param name="pluginDatabase">Plugin database used to perform the transfer.</param>
        public TransfertDataViewModel(List<DataGame> sourceGames, IPluginDatabase pluginDatabase)
        {
            _pluginDatabase = pluginDatabase;

            _sourceGames = new ObservableCollection<DataGame>(
                sourceGames.OrderBy(x => x.Name));

            // Build the target list from the full Playnite library.
            // GroupBy replaces Distinct() because DataGame does not implement IEquatable —
            // Distinct() would fall back to reference equality and have no deduplication effect.
            _targetGames = new ObservableCollection<DataGame>(
                API.Instance.Database.Games
                    .Select(x => new DataGame
                    {
                        Id = x.Id,
                        Icon = x.Icon.IsNullOrEmpty() ? x.Icon : API.Instance.Database.GetFullFilePath(x.Icon),
                        Name = x.Name,
                        CountData = pluginDatabase.Get(x.Id, true)?.Count ?? 0
                    })
                    .GroupBy(x => x.Id)
                    .Select(g => g.First())
                    .OrderBy(x => x.Name));

            // Wrap raw collections in ICollectionView so filtering never mutates
            // the underlying ObservableCollection.
            SourceGamesView = CollectionViewSource.GetDefaultView(_sourceGames);
            SourceGamesView.Filter = FilterSourceGame;

            TargetGamesView = CollectionViewSource.GetDefaultView(_targetGames);
            TargetGamesView.Filter = FilterTargetGame;

            TransferCommand = new RelayCommand(
                execute: () => { ExecuteTransfer(); RequestClose?.Invoke(); },
                canExecute: () => IsTransferEnabled);

            CloseCommand = new RelayCommand(() => RequestClose?.Invoke());
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Pre-selects a specific source game and locks the source ComboBox.
        /// Used when the dialog is opened from a single-game context menu entry.
        /// </summary>
        public void LockSourceGame(DataGame game)
        {
            SelectedSource = _sourceGames.FirstOrDefault(x => x.Id == game.Id);
            IsSourceSelectable = false;
        }

        /// <summary>
        /// Clears the source search text and restores the full unfiltered list.
        /// Called from the code-behind SelectionChanged handler after the user picks an item,
        /// so the drop-down shows all games again the next time it is opened.
        /// The caller must set <see cref="IsSelectingSource"/> = true before calling this,
        /// and reset it to false after, to prevent the Refresh from re-filtering.
        /// </summary>
        public void ResetSearchSource()
        {
            _searchTextSource = string.Empty;
            OnPropertyChanged(nameof(SearchTextSource));
            SourceGamesView.Refresh();
        }

        /// <summary>
        /// Clears the target search text and restores the full unfiltered list.
        /// Same guard contract as <see cref="ResetSearchSource"/>.
        /// </summary>
        public void ResetSearchTarget()
        {
            _searchTextTarget = string.Empty;
            OnPropertyChanged(nameof(SearchTextTarget));
            TargetGamesView.Refresh();
        }

        // ── Filter predicates ─────────────────────────────────────────────────

        /// <summary>Filter predicate applied to the source collection view.</summary>
        private bool FilterSourceGame(object item)
            => MatchesSearch(item as DataGame, _searchTextSource);

        /// <summary>Filter predicate applied to the target collection view.</summary>
        private bool FilterTargetGame(object item)
            => MatchesSearch(item as DataGame, _searchTextTarget);

        /// <summary>
        /// Returns true when <paramref name="game"/> matches the search term.
        /// The match is insensitive to case AND diacritics (accents):
        ///   "elda" will match "Élda", "ëlDà", etc.
        /// An empty or null search term always matches (shows the full list).
        /// </summary>
        private static bool MatchesSearch(DataGame game, string searchTerm)
        {
            if (game == null) return false;
            if (string.IsNullOrEmpty(searchTerm)) return true;

            string normalizedName = RemoveDiacritics(game.Name ?? string.Empty);
            string normalizedSearch = RemoveDiacritics(searchTerm);

            return normalizedName.IndexOf(normalizedSearch, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Strips diacritic marks (accents) from a string by decomposing Unicode characters
        /// into base characters + combining marks, then discarding all combining marks.
        /// Fully compatible with .NET 4.6.2 — no external dependencies required.
        /// </summary>
        private static string RemoveDiacritics(string text)
        {
            // FormD splits "é" into "e" + combining acute accent.
            string normalized = text.Normalize(NormalizationForm.FormD);

            StringBuilder sb = new StringBuilder(normalized.Length);
            foreach (char c in normalized)
            {
                // Discard combining (diacritic) characters; keep everything else.
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            // Recompose into the standard NFC form.
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Performs the actual data transfer or merge between the selected games.
        /// Errors are logged and surfaced via Playnite notifications.
        /// </summary>
        private void ExecuteTransfer()
        {
            try
            {
                PluginDataBaseGameBase pluginData;

                if (IsMergeMode)
                {
                    // Merge: combine source data into the existing target entry.
                    pluginData = _pluginDatabase.MergeData(SelectedSource.Id, SelectedTarget.Id);
                }
                else
                {
                    // Overwrite: clone source data and re-assign it to the target game.
                    pluginData = _pluginDatabase.GetClone(SelectedSource.Id);
                    pluginData.Id = SelectedTarget.Id;
                    pluginData.Name = SelectedTarget.Name;
                }

                if (pluginData != null)
                {
                    _pluginDatabase.AddOrUpdate(pluginData);
                }
                else
                {
                    Logger.Warn(
                        $"{_pluginDatabase.PluginName} - No data transferred " +
                        $"from '{SelectedSource.Name}' to '{SelectedTarget.Name}'.");
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, _pluginDatabase.PluginName);
            }
        }
    }
}