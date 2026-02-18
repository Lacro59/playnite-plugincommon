using CommonPlayniteShared.Common;
using CommonPluginsControls.PlayniteControls;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml.Linq;

namespace CommonPlayniteShared.DesktopApp
{
    /// <summary>
    /// A dropdown selection box bound to a <see cref="SelectableDbItemList"/>.
    /// Supports search filtering, three-state checkboxes, and "show selected only" toggle.
    /// </summary>
    public partial class DdItemListSelectionBox : UserControl
    {
        // Prevents recursive change propagation between BoundIds and ItemsList.
        private bool _ignoreChanges;

        // ── Dependency Property: IsThreeState ────────────────────────────────

        public static readonly DependencyProperty IsThreeStateProperty =
            DependencyProperty.Register(
                nameof(IsThreeState),
                typeof(bool),
                typeof(DdItemListSelectionBox));

        /// <summary>
        /// Gets or sets whether checkboxes in the list support a three-state mode.
        /// </summary>
        public bool IsThreeState
        {
            get { return (bool)GetValue(IsThreeStateProperty); }
            set { SetValue(IsThreeStateProperty, value); }
        }

        // ── Dependency Property: ShowSearchBox ───────────────────────────────

        public static readonly DependencyProperty ShowSearchBoxProperty =
            DependencyProperty.Register(
                nameof(ShowSearchBox),
                typeof(bool),
                typeof(DdItemListSelectionBox),
                new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether the search box and filter toggle are visible in the popup.
        /// </summary>
        public bool ShowSearchBox
        {
            get { return (bool)GetValue(ShowSearchBoxProperty); }
            set { SetValue(ShowSearchBoxProperty, value); }
        }

        // ── Dependency Property: ItemsList ───────────────────────────────────

        public static readonly DependencyProperty ItemsListProperty =
            DependencyProperty.Register(
                nameof(ItemsList),
                typeof(SelectableDbItemList),
                typeof(DdItemListSelectionBox),
                new PropertyMetadata(null, OnItemsListChanged));

        /// <summary>
        /// Gets or sets the source list of selectable items.
        /// Subscribes to <see cref="SelectableDbItemList.SelectionChanged"/> for two-way sync.
        /// </summary>
        public SelectableDbItemList ItemsList
        {
            get { return (SelectableDbItemList)GetValue(ItemsListProperty); }
            set { SetValue(ItemsListProperty, value); }
        }

        private static void OnItemsListChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (DdItemListSelectionBox)d;

            // Unsubscribe from previous list to avoid memory leaks.
            var oldList = e.OldValue as SelectableDbItemList;
            if (oldList != null)
            {
                oldList.SelectionChanged -= control.OnListSelectionChanged;
            }

            var newList = e.NewValue as SelectableDbItemList;
            if (newList != null)
            {
                newList.SelectionChanged += control.OnListSelectionChanged;
            }

            control.UpdateTextStatus();
        }

        /// <summary>
        /// Called when the underlying list selection changes.
        /// Syncs the selection back to <see cref="BoundIds"/>.
        /// </summary>
        private void OnListSelectionChanged(object sender, EventArgs e)
        {
            if (_ignoreChanges)
            {
                return;
            }

            _ignoreChanges = true;
            BoundIds = ItemsList?.GetSelectedIds();
            _ignoreChanges = false;

            UpdateTextStatus();
        }

        // ── Dependency Property: BoundIds ────────────────────────────────────

        public static readonly DependencyProperty BoundIdsProperty =
            DependencyProperty.Register(
                nameof(BoundIds),
                typeof(object),
                typeof(DdItemListSelectionBox),
                new PropertyMetadata(null, OnBoundIdsChanged));

        /// <summary>
        /// Gets or sets the externally-bound collection of selected item IDs.
        /// Changing this property updates the checkboxes in the list.
        /// </summary>
        public object BoundIds
        {
            get { return GetValue(BoundIdsProperty); }
            set { SetValue(BoundIdsProperty, value); }
        }

        private static void OnBoundIdsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (DdItemListSelectionBox)d;

            if (control._ignoreChanges)
            {
                return;
            }

            control._ignoreChanges = true;
            control.ItemsList?.SetSelection(control.BoundIds as IEnumerable<Guid>);
            control._ignoreChanges = false;

            control.UpdateTextStatus();
        }

        // ── Constructor ──────────────────────────────────────────────────────

        public DdItemListSelectionBox()
        {
            InitializeComponent();

            SetupItemsPanel();
            SetupClearButton();
            SetupToggleSelectedOnly();
            SetupSearchHost();
            SetupSearchBox();
            SetupPopup();

            UpdateTextStatus();
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Clears all selected items and resets <see cref="BoundIds"/> to null.
        /// </summary>
        public void ClearButtonAction()
        {
            ItemsList?.SetSelection(null);
            BoundIds = null;
        }

        // ── Setup Methods ────────────────────────────────────────────────────

        /// <summary>
        /// Configures the ItemsControl for virtualization, scrolling, and keyboard navigation.
        /// Builds ItemsPanel, ControlTemplate, and ItemTemplate programmatically via XAML strings
        /// because the template must react to the dynamic <see cref="IsThreeState"/> binding.
        /// </summary>
        private void SetupItemsPanel()
        {
            if (PART_ItemsPanel == null)
            {
                return;
            }

            XNamespace ns = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

            // Use a VirtualizingStackPanel for performance with large lists.
            PART_ItemsPanel.ItemsPanel = Xaml.FromString<ItemsPanelTemplate>(new XDocument(
                new XElement(ns + nameof(ItemsPanelTemplate),
                    new XElement(ns + nameof(VirtualizingStackPanel)))
            ).ToString());

            // Wrap items in a ScrollViewer with an ItemsPresenter.
            PART_ItemsPanel.Template = Xaml.FromString<ControlTemplate>(new XDocument(
                new XElement(ns + nameof(ControlTemplate),
                    new XElement(ns + nameof(ScrollViewer),
                        new XAttribute(nameof(ScrollViewer.Focusable), false),
                        new XElement(ns + nameof(ItemsPresenter))))
            ).ToString());

            // Enable UI virtualization for smooth scrolling.
            ScrollViewer.SetCanContentScroll(PART_ItemsPanel, true);
            KeyboardNavigation.SetDirectionalNavigation(PART_ItemsPanel, KeyboardNavigationMode.Contained);
            VirtualizingPanel.SetIsVirtualizing(PART_ItemsPanel, true);
            VirtualizingPanel.SetVirtualizationMode(PART_ItemsPanel, VirtualizationMode.Recycling);

            // Bind ItemsSource to the filtered CollectionView of the list.
            BindingTools.SetBinding(
                PART_ItemsPanel,
                ItemsControl.ItemsSourceProperty,
                this,
                nameof(ItemsList) + ".CollectionView");

            RefreshItemTemplate();
        }

        /// <summary>
        /// Rebuilds the ItemTemplate to pick up the current <see cref="IsThreeState"/> binding.
        /// Uses <see cref="ModernCheckBox"/> style from Common.xaml for visual consistency.
        /// </summary>
        private void RefreshItemTemplate()
        {
            if (PART_ItemsPanel == null)
            {
                return;
            }

            XNamespace ns = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

            // ModernCheckBox style is defined in Common.xaml and provides hover + focus visuals.
            PART_ItemsPanel.ItemTemplate = Xaml.FromString<DataTemplate>(new XDocument(
                new XElement(ns + nameof(DataTemplate),
                    new XElement(ns + nameof(CheckBox),
                        new XAttribute(nameof(CheckBox.IsChecked),
                            "{Binding Selected}"),
                        new XAttribute(nameof(CheckBox.Content),
                            "{Binding Item.Name}"),
                        new XAttribute(nameof(CheckBox.IsThreeState),
                            "{Binding IsThreeState, Mode=OneWay, RelativeSource={RelativeSource AncestorType=DdItemListSelectionBox}}"),
                        new XAttribute(nameof(CheckBox.Style),
                            "{DynamicResource ModernCheckBox}")))
            ).ToString());
        }

        /// <summary>
        /// Wires up the clear button click to <see cref="ClearButtonAction"/>.
        /// </summary>
        private void SetupClearButton()
        {
            if (PART_ButtonClearFilter == null)
            {
                return;
            }

            PART_ButtonClearFilter.Click += (_, __) => ClearButtonAction();
        }

        /// <summary>
        /// Binds the "show selected only" checkbox to the list's ShowSelectedOnly property.
        /// </summary>
        private void SetupToggleSelectedOnly()
        {
            if (PART_ToggleSelectedOnly == null)
            {
                return;
            }

            BindingTools.SetBinding(
                PART_ToggleSelectedOnly,
                CheckBox.IsCheckedProperty,
                this,
                nameof(ItemsList) + "." + nameof(SelectableDbItemList.ShowSelectedOnly),
                BindingMode.TwoWay);
        }

        /// <summary>
        /// Controls the visibility of the search host row based on <see cref="ShowSearchBox"/>.
        /// </summary>
        private void SetupSearchHost()
        {
            if (PART_ElemSearchHost == null)
            {
                return;
            }

            BindingTools.SetBinding(
                PART_ElemSearchHost,
                VisibilityProperty,
                this,
                nameof(ShowSearchBox),
                converter: new BooleanToVisibilityConverter());
        }

        /// <summary>
        /// Binds the search box text to the list's SearchText property (two-way).
        /// </summary>
        private void SetupSearchBox()
        {
            if (PART_SearchBox == null)
            {
                return;
            }

            BindingTools.SetBinding(
                PART_SearchBox,
                SearchBox.TextProperty,
                this,
                nameof(ItemsList) + "." + nameof(SelectableDbItemList.SearchText),
                BindingMode.TwoWay);
        }

        /// <summary>
        /// Configures popup open/close behavior:
        /// - Focuses the search box on open (if visible).
        /// - Clears search text on close.
        /// - Closes on Escape key press.
        /// </summary>
        private void SetupPopup()
        {
            if (PART_DropdownPopup == null)
            {
                return;
            }

            PART_DropdownPopup.Opened += (_, __) =>
            {
                // Auto-focus the search box when the popup opens for immediate keyboard input.
                if (ShowSearchBox && PART_SearchBox != null)
                {
                    PART_SearchBox.IsFocused = true;
                }
            };

            PART_DropdownPopup.Closed += (_, __) =>
            {
                // Reset search state when the popup closes.
                if (ShowSearchBox && PART_SearchBox != null)
                {
                    PART_SearchBox.IsFocused = false;
                    PART_SearchBox.Text = string.Empty;
                }
            };

            // Allow closing the popup with the Escape key.
            PART_DropdownPopup.PreviewKeyUp += (_, keyArgs) =>
            {
                if (keyArgs.Key == Key.Escape)
                {
                    PART_DropdownPopup.IsOpen = false;
                }
            };
        }

        // ── Private Helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Refreshes the summary text label with the current selection string from the list.
        /// </summary>
        private void UpdateTextStatus()
        {
            if (PART_TextFilterString != null)
            {
                PART_TextFilterString.Text = ItemsList?.AsString;
            }
        }
    }
}