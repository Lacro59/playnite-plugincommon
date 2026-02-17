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
    public partial class DdItemListSelectionBox : UserControl
    {
        private bool _ignoreChanges;

        // ── IsThreeState ─────────────────────────────────────────────────────

        public static readonly DependencyProperty IsThreeStateProperty =
            DependencyProperty.Register(
                nameof(IsThreeState), typeof(bool), typeof(DdItemListSelectionBox));

        public bool IsThreeState
        {
            get { return (bool)GetValue(IsThreeStateProperty); }
            set { SetValue(IsThreeStateProperty, value); }
        }

        // ── ShowSearchBox ────────────────────────────────────────────────────

        public static readonly DependencyProperty ShowSearchBoxProperty =
            DependencyProperty.Register(
                nameof(ShowSearchBox), typeof(bool), typeof(DdItemListSelectionBox),
                new PropertyMetadata(false));

        public bool ShowSearchBox
        {
            get { return (bool)GetValue(ShowSearchBoxProperty); }
            set { SetValue(ShowSearchBoxProperty, value); }
        }

        // ── ItemsList ────────────────────────────────────────────────────────

        public static readonly DependencyProperty ItemsListProperty =
            DependencyProperty.Register(
                nameof(ItemsList), typeof(SelectableDbItemList), typeof(DdItemListSelectionBox),
                new PropertyMetadata(null, OnItemsListChanged));

        public SelectableDbItemList ItemsList
        {
            get { return (SelectableDbItemList)GetValue(ItemsListProperty); }
            set { SetValue(ItemsListProperty, value); }
        }

        private static void OnItemsListChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var obj = (DdItemListSelectionBox)d;

            // Unsubscribe from old list
            var oldList = e.OldValue as SelectableDbItemList;
            if (oldList != null)
            {
                oldList.SelectionChanged -= obj.OnListSelectionChanged;
            }

            var newList = e.NewValue as SelectableDbItemList;
            if (newList != null)
            {
                newList.SelectionChanged += obj.OnListSelectionChanged;
            }

            obj.UpdateTextStatus();
        }

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

        // ── BoundIds ─────────────────────────────────────────────────────────

        public static readonly DependencyProperty BoundIdsProperty =
            DependencyProperty.Register(
                nameof(BoundIds), typeof(object), typeof(DdItemListSelectionBox),
                new PropertyMetadata(null, OnBoundIdsChanged));

        public object BoundIds
        {
            get { return GetValue(BoundIdsProperty); }
            set { SetValue(BoundIdsProperty, value); }
        }

        private static void OnBoundIdsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var obj = (DdItemListSelectionBox)d;
            if (obj._ignoreChanges)
            {
                return;
            }

            obj._ignoreChanges = true;
            obj.ItemsList?.SetSelection(obj.BoundIds as IEnumerable<Guid>);
            obj._ignoreChanges = false;

            obj.UpdateTextStatus();
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

        public void ClearButtonAction()
        {
            ItemsList?.SetSelection(null);
            BoundIds = null;
        }

        // ── Setup ────────────────────────────────────────────────────────────

        private void SetupItemsPanel()
        {
            if (PART_ItemsPanel == null)
            {
                return;
            }

            XNamespace pns = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

            PART_ItemsPanel.ItemsPanel = Xaml.FromString<ItemsPanelTemplate>(new XDocument(
                new XElement(pns + nameof(ItemsPanelTemplate),
                    new XElement(pns + nameof(VirtualizingStackPanel)))
            ).ToString());

            PART_ItemsPanel.Template = Xaml.FromString<ControlTemplate>(new XDocument(
                new XElement(pns + nameof(ControlTemplate),
                    new XElement(pns + nameof(ScrollViewer),
                        new XAttribute(nameof(ScrollViewer.Focusable), false),
                        new XElement(pns + nameof(ItemsPresenter))))
            ).ToString());

            ScrollViewer.SetCanContentScroll(PART_ItemsPanel, true);
            KeyboardNavigation.SetDirectionalNavigation(PART_ItemsPanel, KeyboardNavigationMode.Contained);
            VirtualizingPanel.SetIsVirtualizing(PART_ItemsPanel, true);
            VirtualizingPanel.SetVirtualizationMode(PART_ItemsPanel, VirtualizationMode.Recycling);

            // Bind to CollectionView for search/filter support
            BindingTools.SetBinding(
                PART_ItemsPanel,
                ItemsControl.ItemsSourceProperty,
                this,
                nameof(ItemsList) + ".CollectionView");

            RefreshItemTemplate();
        }

        /// <summary>
        /// Rebuilds the ItemTemplate to pick up the current IsThreeState binding.
        /// </summary>
        private void RefreshItemTemplate()
        {
            if (PART_ItemsPanel == null)
            {
                return;
            }

            XNamespace pns = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

            PART_ItemsPanel.ItemTemplate = Xaml.FromString<DataTemplate>(new XDocument(
                new XElement(pns + nameof(DataTemplate),
                    new XElement(pns + nameof(CheckBox),
                        new XAttribute(nameof(CheckBox.IsChecked), "{Binding Selected}"),
                        new XAttribute(nameof(CheckBox.Content), "{Binding Item.Name}"),
                        new XAttribute(nameof(CheckBox.IsThreeState),
                            "{Binding IsThreeState, Mode=OneWay, RelativeSource={RelativeSource AncestorType=DdItemListSelectionBox}}"),
                        new XAttribute(nameof(CheckBox.Style), "{DynamicResource ComboBoxListItemStyle}")))
            ).ToString());
        }

        private void SetupClearButton()
        {
            if (PART_ButtonClearFilter == null)
            {
                return;
            }

            PART_ButtonClearFilter.Click += (_, __) => ClearButtonAction();
        }

        private void SetupToggleSelectedOnly()
        {
            if (PART_ToggleSelectedOnly == null)
            {
                return;
            }

            BindingTools.SetBinding(
                PART_ToggleSelectedOnly,
                ToggleButton.IsCheckedProperty,
                this,
                nameof(ItemsList) + "." + nameof(SelectableDbItemList.ShowSelectedOnly),
                BindingMode.TwoWay);
        }

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
                converter: new System.Windows.Controls.BooleanToVisibilityConverter());
        }

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

        private void SetupPopup()
        {
            if (Popup == null)
            {
                return;
            }

            Popup.Opened += (_, __) =>
            {
                if (ShowSearchBox && PART_SearchBox != null)
                {
                    PART_SearchBox.IsFocused = true;
                }
            };

            Popup.Closed += (_, __) =>
            {
                if (ShowSearchBox && PART_SearchBox != null)
                {
                    PART_SearchBox.IsFocused = false;
                    PART_SearchBox.Text = string.Empty;
                }
            };

            Popup.PreviewKeyUp += (_, keyArgs) =>
            {
                if (keyArgs.Key == Key.Escape)
                {
                    Popup.IsOpen = false;
                }
            };
        }

        // ── Private ──────────────────────────────────────────────────────────

        private void UpdateTextStatus()
        {
            if (PART_TextFilterString != null)
            {
                PART_TextFilterString.Text = ItemsList?.AsString;
            }
        }
    }
}