using CommonPluginsControls.ViewModels;
using CommonPluginsShared.Interfaces;
using CommonPluginsShared.Models;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CommonPluginsControls.Views
{
    /// <summary>
    /// Code-behind for TransfertData.xaml.
    /// Kept minimal: business logic lives in <see cref="TransfertDataViewModel"/>.
    ///
    /// SelectionChanged handlers solve two WPF editable ComboBox issues:
    ///   1. WPF writes ToString() back into the Text binding after selection,
    ///      which would retrigger the filter and collapse the selection.
    ///      Guard flags on the ViewModel suppress the refresh during that write-back.
    ///   2. Focus remains trapped inside the editable TextBox after selection.
    ///      MoveFocus releases it to the next logical element.
    /// </summary>
    public partial class TransfertData : UserControl
    {
        // ── Constructors ──────────────────────────────────────────────────────

        /// <summary>Opens the transfer dialog with a selectable list of source games.</summary>
        public TransfertData(List<DataGame> dataPluginGames, IPluginDatabase pluginDatabase)
        {
            InitializeComponent();
            BindViewModel(new TransfertDataViewModel(dataPluginGames, pluginDatabase));
        }

        /// <summary>Opens the transfer dialog with a single pre-selected and locked source game.</summary>
        public TransfertData(DataGame dataPluginGame, IPluginDatabase pluginDatabase)
        {
            InitializeComponent();

            TransfertDataViewModel vm = new TransfertDataViewModel(
                new List<DataGame> { dataPluginGame },
                pluginDatabase);

            vm.LockSourceGame(dataPluginGame);
            BindViewModel(vm);
        }

        // ── Event handlers ────────────────────────────────────────────────────

        /// <summary>
        /// Raised when the user selects an item in the source ComboBox.
        ///
        /// Sequence when the user clicks an item in an editable ComboBox:
        ///   1. SelectionChanged fires → guard flag raised.
        ///   2. WPF writes item.ToString() into Text (SearchTextSource).
        ///      The guard prevents the setter from refreshing the view.
        ///   3. ResetSearchSource() clears the filter so the full list is visible
        ///      the next time the drop-down opens.
        ///   4. Guard flag lowered.
        ///   5. MoveFocus transfers keyboard focus away from the internal TextBox
        ///      so the user is not stuck inside the editable field.
        /// </summary>
        private void PART_CbPluginGame_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(DataContext is TransfertDataViewModel vm)) return;

            vm.IsSelectingSource = true;
            vm.ResetSearchSource();
            vm.IsSelectingSource = false;

            // Release focus from the ComboBox internal TextBox to the next element.
            ReleaseFocus(sender as UIElement);
        }

        /// <summary>
        /// Raised when the user selects an item in the target ComboBox.
        /// Follows the same guard + focus pattern as <see cref="PART_CbPluginGame_SelectionChanged"/>.
        /// </summary>
        private void PART_CbGame_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(DataContext is TransfertDataViewModel vm)) return;

            vm.IsSelectingTarget = true;
            vm.ResetSearchTarget();
            vm.IsSelectingTarget = false;

            ReleaseFocus(sender as UIElement);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Moves keyboard focus forward to the next focusable element in tab order.
        /// If no next element exists (e.g. single-item dialog), focus is simply cleared.
        /// </summary>
        private static void ReleaseFocus(UIElement element)
        {
            if (element == null) return;

            // Request the element to take logical focus first, then move forward.
            // This ensures focus leaves the internal editable TextBox of the ComboBox.
            element.Focus();
            element.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }

        /// <summary>
        /// Sets the DataContext and wires the ViewModel's RequestClose event
        /// so the ViewModel can close the host window without any View reference.
        /// </summary>
        private void BindViewModel(TransfertDataViewModel vm)
        {
            vm.RequestClose += () => Window.GetWindow(this)?.Close();
            DataContext = vm;
        }
    }
}