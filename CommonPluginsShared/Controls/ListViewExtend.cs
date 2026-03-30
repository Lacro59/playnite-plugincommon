using CommonPluginsShared.Extensions;
using CommonPluginsShared.UI;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace CommonPluginsShared.Controls
{
    /// <summary>
    /// Extended ListView control with additional features including sorting, column management, and size stretching.
    /// </summary>
    public class ListViewExtend : ListView
    {
		#region HeightStretch

		/// <summary>
		/// Dependency property for HeightStretch.
		/// </summary>
		public static readonly DependencyProperty HeightStretchProperty;

		/// <summary>
		/// Gets or sets whether the ListView should stretch to fill the available height.
		/// </summary>
		public bool HeightStretch
		{
			get => HeightStretchProperty == null || (bool)GetValue(HeightStretchProperty);
			set => SetValue(HeightStretchProperty, value);
		}

		#endregion

		#region WidthStretch

		/// <summary>
		/// Dependency property for WidthStretch.
		/// </summary>
		public static readonly DependencyProperty WidthStretchProperty;

		/// <summary>
		/// Gets or sets whether the ListView should stretch to fill the available width.
		/// </summary>
		public bool WidthStretch
		{
			get => WidthStretchProperty == null || (bool)GetValue(WidthStretchProperty);
			set => SetValue(WidthStretchProperty, value);
		}

		#endregion

		#region BubblingScrollEvents

		/// <summary>
		/// Gets or sets whether scroll events should bubble up to parent controls.
		/// </summary>
		public bool BubblingScrollEvents
        {
            get => (bool)GetValue(BubblingScrollEventsProperty);
            set => SetValue(BubblingScrollEventsProperty, value);
        }

        /// <summary>
        /// Dependency property for BubblingScrollEvents.
        /// </summary>
        public static readonly DependencyProperty BubblingScrollEventsProperty = DependencyProperty.Register(
            nameof(BubblingScrollEvents),
            typeof(bool),
            typeof(ListViewExtend),
            new FrameworkPropertyMetadata(false, BubblingScrollEventsChangedCallback));

        /// <summary>
        /// Callback when BubblingScrollEvents property changes.
        /// </summary>
        /// <param name="sender">The dependency object.</param>
        /// <param name="e">Event arguments containing old and new values.</param>
        private static void BubblingScrollEventsChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is ListViewExtend obj && e.NewValue != e.OldValue)
            {
                if ((bool)e.NewValue)
                {
                    obj.PreviewMouseWheel += UIHelper.HandlePreviewMouseWheel;
                }
                else
                {
                    obj.PreviewMouseWheel -= UIHelper.HandlePreviewMouseWheel;
                }
            }
        }

        #endregion

        #region Save Column Order

        /// <summary>
        /// Dependency property for SaveColumn.
        /// </summary>
        public static readonly DependencyProperty SaveColumnProperty;

        /// <summary>
        /// Gets or sets whether column order should be saved.
        /// </summary>
        public bool SaveColumn
        {
            get => (bool)GetValue(SaveColumnProperty);
            set => SetValue(SaveColumnProperty, value);
        }

        /// <summary>
        /// Dependency property for SaveColumnFilePath.
        /// </summary>
        public static readonly DependencyProperty SaveColumnFilePathProperty;

        /// <summary>
        /// Gets or sets the file path where column order is saved.
        /// </summary>
        public string SaveColumnFilePath
        {
            get => (string)GetValue(SaveColumnFilePathProperty);
            set => SetValue(SaveColumnFilePathProperty, value);
        }

        /// <summary>
        /// Gets or sets whether column persistence is enabled.
        /// This property mirrors <see cref="SaveColumn"/> for easier usage.
        /// </summary>
        public bool EnableColumnPersistence
        {
            get => SaveColumn;
            set => SaveColumn = value;
        }

        /// <summary>
        /// Gets or sets the file path where column configuration is saved.
        /// This property mirrors <see cref="SaveColumnFilePath"/> for easier usage.
        /// </summary>
        public string ColumnConfigurationFilePath
        {
            get => SaveColumnFilePath;
            set => SaveColumnFilePath = value;
        }

        /// <summary>
        /// Dependency property for SaveColumnConfigurationName.
        /// </summary>
        public static readonly DependencyProperty SaveColumnConfigurationNameProperty;

        /// <summary>
        /// Gets or sets the configuration name used to scope persisted column state per view/component.
        /// </summary>
        public string SaveColumnConfigurationName
        {
            get => (string)GetValue(SaveColumnConfigurationNameProperty);
            set => SetValue(SaveColumnConfigurationNameProperty, value);
        }

        /// <summary>
        /// Dependency property for ColumnConfigurationScope.
        /// </summary>
        public static readonly DependencyProperty ColumnConfigurationScopeProperty;

        /// <summary>
        /// Gets or sets how column configuration key is computed.
        /// </summary>
        public ColumnConfigurationScope ColumnConfigurationScope
        {
            get => (ColumnConfigurationScope)GetValue(ColumnConfigurationScopeProperty);
            set => SetValue(ColumnConfigurationScopeProperty, value);
        }

        /// <summary>
        /// Dependency property for ColumnConfigurationKey.
        /// </summary>
        public static readonly DependencyProperty ColumnConfigurationKeyProperty;

        /// <summary>
        /// Gets or sets the custom key used when <see cref="ColumnConfigurationScope"/> is Custom.
        /// </summary>
        public string ColumnConfigurationKey
        {
            get => (string)GetValue(ColumnConfigurationKeyProperty);
            set => SetValue(ColumnConfigurationKeyProperty, value);
        }

        /// <summary>
        /// Dependency property for ColumnManagementMenuEnable.
        /// </summary>
        public static readonly DependencyProperty ColumnManagementMenuEnableProperty;

        /// <summary>
        /// Gets or sets whether the column management context menu is enabled.
        /// </summary>
        public bool ColumnManagementMenuEnable
        {
            get => (bool)GetValue(ColumnManagementMenuEnableProperty);
            set => SetValue(ColumnManagementMenuEnableProperty, value);
        }

        /// <summary>
        /// Dependency property for EnableColumnVisibilityToggle.
        /// </summary>
        public static readonly DependencyProperty EnableColumnVisibilityToggleProperty;

        /// <summary>
        /// Gets or sets whether the context menu allows showing or hiding columns.
        /// </summary>
        public bool EnableColumnVisibilityToggle
        {
            get => (bool)GetValue(EnableColumnVisibilityToggleProperty);
            set => SetValue(EnableColumnVisibilityToggleProperty, value);
        }

        /// <summary>
        /// Dependency property for EnableColumnResetAction.
        /// </summary>
        public static readonly DependencyProperty EnableColumnResetActionProperty;

        /// <summary>
        /// Gets or sets whether the context menu exposes reset action.
        /// </summary>
        public bool EnableColumnResetAction
        {
            get => (bool)GetValue(EnableColumnResetActionProperty);
            set => SetValue(EnableColumnResetActionProperty, value);
        }

        #endregion

        #region Sorting Properties

        /// <summary>
        /// Gets the down caret character for descending sort indicator.
        /// </summary>
        private string CaretDown => "\uea67";

        /// <summary>
        /// Gets the up caret character for ascending sort indicator.
        /// </summary>
        private string CaretUp => "\uea6a";

        /// <summary>
        /// Gets or sets whether sorting is enabled.
        /// </summary>
        public bool SortingEnable
        {
            get => (bool)GetValue(SortingEnableProperty);
            set => SetValue(SortingEnableProperty, value);
        }

        /// <summary>
        /// Dependency property for SortingEnable.
        /// </summary>
        public static readonly DependencyProperty SortingEnableProperty = DependencyProperty.Register(
            nameof(SortingEnable),
            typeof(bool),
            typeof(ListViewExtend),
            new FrameworkPropertyMetadata(false, SortingPropertyChangedCallback));

        /// <summary>
        /// Gets or sets the default column name to sort by.
        /// </summary>
        public string SortingDefaultDataName
        {
            get => (string)GetValue(SortingDefaultDataNameProperty);
            set => SetValue(SortingDefaultDataNameProperty, value);
        }

        /// <summary>
        /// Dependency property for SortingDefaultDataName.
        /// </summary>
        public static readonly DependencyProperty SortingDefaultDataNameProperty = DependencyProperty.Register(
            nameof(SortingDefaultDataName),
            typeof(string),
            typeof(ListViewExtend),
            new FrameworkPropertyMetadata(string.Empty, SortingPropertyChangedCallback));

        /// <summary>
        /// Gets or sets the default sort direction.
        /// </summary>
        public ListSortDirection SortingSortDirection
        {
            get => (ListSortDirection)GetValue(SortingSortDirectionProperty);
            set => SetValue(SortingSortDirectionProperty, value);
        }

        /// <summary>
        /// Dependency property for SortingSortDirection.
        /// </summary>
        public static readonly DependencyProperty SortingSortDirectionProperty = DependencyProperty.Register(
            nameof(SortingSortDirection),
            typeof(ListSortDirection),
            typeof(ListViewExtend),
            new FrameworkPropertyMetadata(ListSortDirection.Ascending, SortingPropertyChangedCallback));

        /// <summary>
        /// Callback when sorting-related properties change.
        /// </summary>
        /// <param name="sender">The dependency object.</param>
        /// <param name="e">Event arguments containing old and new values.</param>
        private static void SortingPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ListViewExtend obj = sender as ListViewExtend;
            if (obj != null && e.NewValue != e.OldValue)
            {
                if (e.NewValue is ListSortDirection)
                {
                    // Reset to force initial sort
                    obj._lastDirection = null;
                }
                else
                {
                    obj.TryApplyInitialSort();
                }
            }
        }

        /// <summary>
        /// The last column header that was clicked for sorting.
        /// </summary>
        private GridViewColumnHeader _lastHeaderClicked = null;

        /// <summary>
        /// The last sort direction applied.
        /// </summary>
        private ListSortDirection? _lastDirection;

        /// <summary>
        /// Flag to track if initial sort has been applied.
        /// </summary>
        private bool _isInitialSortApplied = false;

        /// <summary>
        /// Initial columns order snapshot captured once on load.
        /// </summary>
        private readonly List<GridViewColumn> _initialColumns = new List<GridViewColumn>();

        /// <summary>
        /// Cached initial index by column to restore default order.
        /// </summary>
        private readonly Dictionary<GridViewColumn, int> _initialColumnIndexes = new Dictionary<GridViewColumn, int>();

        #endregion

        #region Static Constructor

        /// <summary>
        /// Static constructor to initialize dependency properties.
        /// </summary>
        static ListViewExtend()
        {
            HeightStretchProperty = DependencyProperty.Register(
                nameof(HeightStretch),
                typeof(bool),
                typeof(ListViewExtend),
                new PropertyMetadata(false));

            WidthStretchProperty = DependencyProperty.Register(
                nameof(WidthStretch),
                typeof(bool),
                typeof(ListViewExtend),
                new PropertyMetadata(false));

            SaveColumnProperty = DependencyProperty.Register(
                nameof(SaveColumn),
                typeof(bool),
                typeof(ListViewExtend),
                new PropertyMetadata(false));

            SaveColumnFilePathProperty = DependencyProperty.Register(
                nameof(SaveColumnFilePath),
                typeof(string),
                typeof(ListViewExtend),
                new PropertyMetadata(string.Empty));

            SaveColumnConfigurationNameProperty = DependencyProperty.Register(
                nameof(SaveColumnConfigurationName),
                typeof(string),
                typeof(ListViewExtend),
                new PropertyMetadata(string.Empty));

            ColumnConfigurationScopeProperty = DependencyProperty.Register(
                nameof(ColumnConfigurationScope),
                typeof(ColumnConfigurationScope),
                typeof(ListViewExtend),
                new PropertyMetadata(ColumnConfigurationScope.Name));

            ColumnConfigurationKeyProperty = DependencyProperty.Register(
                nameof(ColumnConfigurationKey),
                typeof(string),
                typeof(ListViewExtend),
                new PropertyMetadata(string.Empty));

            ColumnManagementMenuEnableProperty = DependencyProperty.Register(
                nameof(ColumnManagementMenuEnable),
                typeof(bool),
                typeof(ListViewExtend),
                new PropertyMetadata(true));

            EnableColumnVisibilityToggleProperty = DependencyProperty.Register(
                nameof(EnableColumnVisibilityToggle),
                typeof(bool),
                typeof(ListViewExtend),
                new PropertyMetadata(true));

            EnableColumnResetActionProperty = DependencyProperty.Register(
                nameof(EnableColumnResetAction),
                typeof(bool),
                typeof(ListViewExtend),
                new PropertyMetadata(true));
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the ListViewExtend class.
        /// </summary>
        public ListViewExtend()
        {
            this.Loaded += ListViewExtend_Loaded;
            this.AddHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(ListViewExtend_onHeaderClick));
            this.PreviewMouseRightButtonUp += ListViewExtend_PreviewMouseRightButtonUp;

            // Monitor ItemsSource changes
            DependencyPropertyDescriptor.FromProperty(ItemsSourceProperty, typeof(ListViewExtend))
                .AddValueChanged(this, OnItemsSourceChanged);
        }

        #endregion

        #region Loaded Event Handler

        /// <summary>
        /// Handles the Loaded event of the ListView.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void ListViewExtend_Loaded(object sender, RoutedEventArgs e)
        {
            if (HeightStretch)
            {
                this.Height = ((FrameworkElement)sender).ActualHeight;
            }
            if (WidthStretch)
            {
                this.Width = ((FrameworkElement)sender).ActualWidth;
            }

            ((FrameworkElement)this.Parent).SizeChanged += Parent_SizeChanged;

            GridView gridView = (GridView)this.View;
            gridView.Columns.CollectionChanged += Columns_CollectionChanged;
            CaptureInitialColumns(gridView);
            LoadColumnState();
            ApplyForcedHiddenColumns(gridView);

            // Try to apply initial sort if data is already available
            TryApplyInitialSort();
        }

        /// <summary>
        /// Handles the SizeChanged event of the parent control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void Parent_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (HeightStretch)
            {
                this.Height = ((FrameworkElement)sender).ActualHeight;
            }
            if (WidthStretch)
            {
                this.Width = ((FrameworkElement)sender).ActualWidth;
            }
        }

        #endregion

        #region ItemsSource Changed Handler

        /// <summary>
        /// Handles changes to the ItemsSource property.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void OnItemsSourceChanged(object sender, EventArgs e)
        {
            // Reset to allow new sort
            _isInitialSortApplied = false;

            if (this.ItemsSource != null)
            {
                // Check if containers are generated
                if (this.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
                {
                    TryApplyInitialSort();
                }
                else
                {
                    // Wait for container generation
                    void handler(object s, EventArgs args)
                    {
                        if (this.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
                        {
                            this.ItemContainerGenerator.StatusChanged -= handler;
                            TryApplyInitialSort();
                        }
                    }

                    this.ItemContainerGenerator.StatusChanged += handler;
                }
            }
        }

        #endregion

        #region Initial Sort

        /// <summary>
        /// Attempts to apply the initial sort when the control is ready with data.
        /// </summary>
        private void TryApplyInitialSort()
        {
            if (_isInitialSortApplied || !SortingEnable || SortingDefaultDataName.IsNullOrEmpty())
            {
                return;
            }

            if (this.ItemsSource == null || this.View == null || !(this.View is GridView))
            {
                return;
            }

            // Ensure everything is ready with a slight delay
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
					var result = FindGridViewColumnWithIndex(SortingDefaultDataName);
					var header = result.Item1;
					var index = result.Item2;

					if (header != null && index >= 0)
                    {
                        ListSortDirection direction = SortingSortDirection;
                        Sort(SortingDefaultDataName, direction);

                        _lastHeaderClicked = header;
                        _lastDirection = direction;

                        UpdateHeaderCaret(header, direction);
                        _isInitialSortApplied = true;
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false);
                }
            }), System.Windows.Threading.DispatcherPriority.DataBind);
        }

        #endregion

        #region Column Management Menu

        /// <summary>
        /// Handles right-click on headers and opens a column management menu.
        /// </summary>
        private void ListViewExtend_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!ColumnManagementMenuEnable)
            {
                return;
            }

            GridViewColumnHeader header = FindVisualParent<GridViewColumnHeader>(e.OriginalSource as DependencyObject);
            if (header == null || header.Role == GridViewColumnHeaderRole.Padding || !(this.View is GridView))
            {
                return;
            }

            ContextMenu menu = BuildColumnManagementContextMenu();
            if (menu == null)
            {
                return;
            }

            header.ContextMenu = menu;
            menu.PlacementTarget = header;
            menu.IsOpen = true;
            e.Handled = true;
        }

        /// <summary>
        /// Builds the context menu used to manage columns visibility and layout.
        /// </summary>
        private ContextMenu BuildColumnManagementContextMenu()
        {
            if (!(this.View is GridView gridView))
            {
                return null;
            }

            ContextMenu contextMenu = new ContextMenu();

            if (EnableColumnResetAction)
            {
                MenuItem resetItem = new MenuItem { Header = "Reset columns" };
                resetItem.Click += (sender, args) => ResetColumnConfiguration();
                contextMenu.Items.Add(resetItem);
            }

            if (EnableColumnVisibilityToggle)
            {
                MenuItem showAllItem = new MenuItem { Header = "Show all columns" };
                showAllItem.Click += (sender, args) => ShowAllColumns(gridView);
                contextMenu.Items.Add(showAllItem);

                if (EnableColumnResetAction)
                {
                    contextMenu.Items.Add(new Separator());
                }

                foreach (GridViewColumn column in _initialColumns)
                {
                    if (ListViewColumnOptions.GetForceHidden(column) || !ListViewColumnOptions.GetShowInColumnManagementMenu(column))
                    {
                        continue;
                    }

                    string columnName = GetColumnDisplayName(column);
                    if (columnName.IsNullOrEmpty())
                    {
                        continue;
                    }

                    MenuItem columnItem = new MenuItem
                    {
                        Header = columnName,
                        IsCheckable = true,
                        IsChecked = gridView.Columns.Contains(column)
                    };

                    columnItem.Click += (sender, args) =>
                    {
                        ToggleColumnVisibility(gridView, column, columnItem.IsChecked);
                    };

                    contextMenu.Items.Add(columnItem);
                }
            }

            if (contextMenu.Items.Count == 0)
            {
                return null;
            }

            return contextMenu;
        }

        /// <summary>
        /// Captures the initial columns and their default order once.
        /// </summary>
        /// <param name="gridView">The grid view instance.</param>
        private void CaptureInitialColumns(GridView gridView)
        {
            if (_initialColumns.Count > 0 || gridView == null)
            {
                return;
            }

            for (int i = 0; i < gridView.Columns.Count; i++)
            {
                GridViewColumn column = gridView.Columns[i];
                _initialColumns.Add(column);
                _initialColumnIndexes[column] = i;
            }
        }

        /// <summary>
        /// Toggles a column visibility by adding/removing it from the view.
        /// </summary>
        private void ToggleColumnVisibility(GridView gridView, GridViewColumn column, bool isVisible)
        {
            if (gridView == null || column == null)
            {
                return;
            }

            if (ListViewColumnOptions.GetForceHidden(column))
            {
                if (gridView.Columns.Contains(column))
                {
                    gridView.Columns.Remove(column);
                    SaveColumnState();
                }

                return;
            }

            if (isVisible)
            {
                if (gridView.Columns.Contains(column))
                {
                    return;
                }

                int targetIndex = GetVisibleInsertIndex(gridView, column);
                gridView.Columns.Insert(targetIndex, column);
            }
            else
            {
                if (!gridView.Columns.Contains(column))
                {
                    return;
                }

                if (gridView.Columns.Count <= 1)
                {
                    return;
                }

                if (_lastHeaderClicked == column.Header as GridViewColumnHeader)
                {
                    _lastHeaderClicked = null;
                    _lastDirection = null;
                }

                gridView.Columns.Remove(column);
            }

            SaveColumnState();
        }

        /// <summary>
        /// Shows all initial columns and restores default order.
        /// </summary>
        /// <param name="gridView">The grid view instance.</param>
        private void ShowAllColumns(GridView gridView)
        {
            if (gridView == null)
            {
                return;
            }

            for (int i = 0; i < _initialColumns.Count; i++)
            {
                GridViewColumn column = _initialColumns[i];
                if (ListViewColumnOptions.GetForceHidden(column))
                {
                    continue;
                }

                if (!gridView.Columns.Contains(column))
                {
                    int targetIndex = i <= gridView.Columns.Count ? i : gridView.Columns.Count;
                    gridView.Columns.Insert(targetIndex, column);
                }
            }

            ReorderColumnsToInitialOrder(gridView);
            ApplyForcedHiddenColumns(gridView);
            SaveColumnState();
        }

        /// <summary>
        /// Resets columns to their default configuration and clears persisted state.
        /// </summary>
        public void ResetColumnConfiguration()
        {
            if (!(this.View is GridView gridView))
            {
                return;
            }

            ShowAllColumns(gridView);

            if (EnableColumnPersistence && !ColumnConfigurationFilePath.IsNullOrEmpty())
            {
                RemovePersistedState();
            }
        }

        /// <summary>
        /// Reorders visible columns to their captured initial order.
        /// </summary>
        private void ReorderColumnsToInitialOrder(GridView gridView)
        {
            List<GridViewColumn> visibleInDefaultOrder = _initialColumns.Where(gridView.Columns.Contains).ToList();
            for (int i = 0; i < visibleInDefaultOrder.Count; i++)
            {
                GridViewColumn column = visibleInDefaultOrder[i];
                int currentIndex = gridView.Columns.IndexOf(column);
                if (currentIndex >= 0 && currentIndex != i)
                {
                    gridView.Columns.Move(currentIndex, i);
                }
            }
        }

        /// <summary>
        /// Applies force-hidden columns by removing them from visible collection.
        /// </summary>
        /// <param name="gridView">The grid view instance.</param>
        private void ApplyForcedHiddenColumns(GridView gridView)
        {
            if (gridView == null)
            {
                return;
            }

            foreach (GridViewColumn column in _initialColumns)
            {
                if (ListViewColumnOptions.GetForceHidden(column) && gridView.Columns.Contains(column))
                {
                    gridView.Columns.Remove(column);
                }
            }
        }

        /// <summary>
        /// Gets insert index for a column based on initial order among visible columns.
        /// </summary>
        private int GetVisibleInsertIndex(GridView gridView, GridViewColumn column)
        {
            int desiredOrder = GetInitialColumnOrder(column);
            int insertIndex = 0;

            foreach (GridViewColumn visibleColumn in gridView.Columns)
            {
                if (GetInitialColumnOrder(visibleColumn) < desiredOrder)
                {
                    insertIndex++;
                }
            }

            return insertIndex;
        }

        /// <summary>
        /// Gets initial order index for a column.
        /// </summary>
        private int GetInitialColumnOrder(GridViewColumn column)
        {
            if (column != null && _initialColumnIndexes.ContainsKey(column))
            {
                return _initialColumnIndexes[column];
            }

            return int.MaxValue;
        }

        /// <summary>
        /// Finds a visual parent of a given type.
        /// </summary>
        private static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent = child;
            while (parent != null)
            {
                if (parent is T target)
                {
                    return target;
                }

                parent = VisualTreeHelper.GetParent(parent);
            }

            return null;
        }

        #endregion

        #region Sorting Methods

        /// <summary>
        /// Finds a GridViewColumnHeader by data binding name.
        /// </summary>
        /// <param name="dataName">The data binding property name.</param>
        /// <returns>The GridViewColumnHeader if found, otherwise null.</returns>
        private GridViewColumnHeader FindGridViewColumn(string dataName)
        {
            if (this.View != null && this.View is GridView)
            {
                try
                {
                    GridView gridView = this.View as GridView;
                    foreach (GridViewColumn gridViewColumn in gridView.Columns)
                    {
                        if (gridViewColumn.DisplayMemberBinding is Binding binding)
                        {
                            string property = binding.Path.Path;
                            if (property == dataName)
                            {
                                return gridViewColumn.Header as GridViewColumnHeader;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false);
                }
            }
            return null;
        }

        /// <summary>
        /// Finds a GridViewColumnHeader and its index by data binding name.
        /// </summary>
        /// <param name="dataName">The data binding property name.</param>
        /// <returns>A tuple containing the header and its index, or (null, -1) if not found.</returns>
        private Tuple<GridViewColumnHeader, int> FindGridViewColumnWithIndex(string dataName)
        {
            if (this.View != null && this.View is GridView)
            {
                try
                {
                    GridView gridView = this.View as GridView;
                    for (int i = 0; i < gridView.Columns.Count; i++)
                    {
                        var gridViewColumn = gridView.Columns[i];
                        if (gridViewColumn.DisplayMemberBinding is Binding binding)
                        {
                            string property = binding.Path.Path;
                            if (property == dataName)
                            {
								return Tuple.Create(gridViewColumn.Header as GridViewColumnHeader, i);
							}
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false);
                }
            }
			return Tuple.Create<GridViewColumnHeader, int>(null, -1);
		}

        /// <summary>
        /// Handles column header click events for sorting.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void ListViewExtend_onHeaderClick(object sender, RoutedEventArgs e)
        {
            if (SortingEnable)
            {
                try
                {
                    ListSortDirection direction;

                    if (!(e.OriginalSource is GridViewColumnHeader headerClicked))
                    {
                        return;
                    }

                    // No sort
                    if (((string)headerClicked.Tag)?.IsEqual("nosort") ?? false)
                    {
                        headerClicked = null;
                    }

                    if (headerClicked != null)
                    {
                        if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                        {
                            if (_lastDirection == null)
                            {
                                direction = ListSortDirection.Ascending;
                            }
                            else if (_lastDirection == ListSortDirection.Ascending)
                            {
                                direction = ListSortDirection.Descending;
                            }
                            else
                            {
                                direction = ListSortDirection.Ascending;
                            }

                            if (_lastHeaderClicked != null && headerClicked != _lastHeaderClicked)
                            {
                                direction = ListSortDirection.Ascending;
                            }

                            if (headerClicked.Column != null)
                            {
                                string sortBy = ResolveSortBy(headerClicked);
                                if (sortBy.IsNullOrEmpty())
                                {
                                    return;
                                }

                                Sort(sortBy, direction);

                                if (_lastHeaderClicked != null)
                                {
                                    RestoreHeaderContent(_lastHeaderClicked);
                                }

                                // Show caret
                                UpdateHeaderCaret(headerClicked, direction);

                                // Remove arrow from previously sorted header
                                if (_lastHeaderClicked != null && _lastHeaderClicked != headerClicked)
                                {
                                    _lastHeaderClicked.Column.HeaderTemplate = null;
                                }

                                _lastHeaderClicked = headerClicked;
                                _lastDirection = direction;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false);
                }
            }
        }

        /// <summary>
        /// Updates the header content to display a sort direction caret.
        /// </summary>
        /// <param name="headerClicked">The header to update.</param>
        /// <param name="direction">The sort direction.</param>
        private void UpdateHeaderCaret(GridViewColumnHeader headerClicked, ListSortDirection direction)
        {
            if (headerClicked == null)
            {
                return;
            }

            if (headerClicked is GridViewColumnHeaderExtend headerExtend)
            {
                int refIndex = headerExtend.RefIndex;
				if (refIndex >= 0 && this.View is GridView gridView && refIndex < gridView.Columns.Count)
				{
                    GridViewColumn gridViewColumn = gridView.Columns[refIndex];
                    if (!(gridViewColumn.Header is GridViewColumnHeader mappedHeader))
                    {
                        return;
                    }
                    headerClicked = mappedHeader;
				}
            }

            // Handle case where content is already a StackPanel
            object originalContent = headerClicked.Content;
            if (originalContent is StackPanel existingPanel && existingPanel.Children.Count > 0)
            {
                originalContent = (existingPanel.Children[0] as Label)?.Content ?? originalContent;
            }

            Label labelHeader = new Label { Content = originalContent };

            Label labelCaret = new Label
            {
                FontFamily = Application.Current?.TryFindResource("FontIcoFont") as FontFamily,
                Content = direction == ListSortDirection.Ascending ? $" {CaretUp}" : $" {CaretDown}"
            };

            StackPanel stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
            stackPanel.Children.Add(labelHeader);
            stackPanel.Children.Add(labelCaret);

            headerClicked.Content = stackPanel;
        }

        /// <summary>
        /// Restores the original content of a header (removes the sort caret).
        /// </summary>
        /// <param name="header">The header to restore.</param>
        private void RestoreHeaderContent(GridViewColumnHeader header)
        {
            if (header?.Content is StackPanel stackPanel && stackPanel.Children.Count > 0)
            {
                if (stackPanel.Children[0] is Label label)
                {
                    header.Content = label.Content;
                }
            }
        }

        /// <summary>
        /// Re-applies the current sort.
        /// </summary>
        public void Sorting()
        {
            if (_lastHeaderClicked != null)
            {
                GridViewColumnHeader headerClicked = _lastHeaderClicked;
                if (headerClicked.Column != null)
                {
                    string sortBy = ResolveSortBy(headerClicked);
                    if (sortBy.IsNullOrEmpty())
                    {
                        return;
                    }

                    Sort(sortBy, (ListSortDirection)_lastDirection);
                }
            }
        }

        /// <summary>
        /// Resolves the property name used for sorting from a column header.
        /// </summary>
        private string ResolveSortBy(GridViewColumnHeader headerClicked)
        {
            if (headerClicked?.Column == null)
            {
                return string.Empty;
            }

            // Preferred explicit setting on the visible column.
            string attachedSortMemberPath = ListViewColumnOptions.GetSortMemberPath(headerClicked.Column);
            if (!attachedSortMemberPath.IsNullOrEmpty())
            {
                return attachedSortMemberPath;
            }

            // Backward compatibility: header tag can either target another bound column,
            // or directly provide a property path.
            string headerTag = headerClicked.Tag as string;
            if (!headerTag.IsNullOrEmpty())
            {
                GridViewColumnHeader gridViewColumnHeader = FindGridViewColumn(headerTag);
                if (gridViewColumnHeader?.Column?.DisplayMemberBinding is Binding tagBinding)
                {
                    return tagBinding.Path.Path;
                }

                return headerTag;
            }

            Binding columnBinding = headerClicked.Column.DisplayMemberBinding as Binding;
            return columnBinding?.Path.Path ?? headerClicked.Column.Header as string;
        }

        /// <summary>
        /// Sorts the ListView by a specified property and direction.
        /// </summary>
        /// <param name="sortBy">The property name to sort by.</param>
        /// <param name="direction">The sort direction.</param>
        private void Sort(string sortBy, ListSortDirection direction)
        {
            if (this.ItemsSource != null)
            {
                ICollectionView dataView = CollectionViewSource.GetDefaultView(this.ItemsSource);
                dataView.SortDescriptions.Clear();
                SortDescription sd = new SortDescription(sortBy, direction);
                dataView.SortDescriptions.Add(sd);
                dataView.Refresh();
            }
        }

        #endregion

        #region Save Column Order

        /// <summary>
        /// Handles changes to the columns collection for saving column order.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void Columns_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            try
            {
                if (e.Action == NotifyCollectionChangedAction.Move)
                {
                    SaveColumnState();
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }
        }

        /// <summary>
        /// Loads the saved column order from file.
        /// </summary>
        private void LoadColumnState()
        {
            if (!EnableColumnPersistence || ColumnConfigurationFilePath.IsNullOrEmpty() || !File.Exists(ColumnConfigurationFilePath))
            {
                return;
            }

            try
            {
                if (!(this.View is GridView gridView))
                {
                    return;
                }

                Dictionary<string, ListViewColumnState> statesByKey = LoadAllPersistedStates();
                if (statesByKey == null)
                {
                    return;
                }

                ListViewColumnState state;
                if (!statesByKey.TryGetValue(GetColumnConfigurationKey(), out state) || state == null)
                {
                    return;
                }

                ApplyColumnState(gridView, state);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }
        }

        /// <summary>
        /// Saves the current columns state using the active configuration key.
        /// </summary>
        private void SaveColumnState()
        {
            if (!EnableColumnPersistence || ColumnConfigurationFilePath.IsNullOrEmpty())
            {
                return;
            }

            if (!(this.View is GridView gridView))
            {
                return;
            }

            try
            {
                Dictionary<string, ListViewColumnState> statesByKey = LoadAllPersistedStates() ?? new Dictionary<string, ListViewColumnState>();
                statesByKey[GetColumnConfigurationKey()] = BuildCurrentColumnState(gridView);

                string serializedData = Serialization.ToJson(statesByKey);
                File.WriteAllText(ColumnConfigurationFilePath, serializedData);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }
        }

        /// <summary>
        /// Removes persisted state for the active configuration key.
        /// </summary>
        private void RemovePersistedState()
        {
            if (ColumnConfigurationFilePath.IsNullOrEmpty() || !File.Exists(ColumnConfigurationFilePath))
            {
                return;
            }

            try
            {
                Dictionary<string, ListViewColumnState> statesByKey = LoadAllPersistedStates();
                if (statesByKey == null)
                {
                    return;
                }

                if (statesByKey.Remove(GetColumnConfigurationKey()))
                {
                    if (statesByKey.Count == 0)
                    {
                        File.Delete(ColumnConfigurationFilePath);
                    }
                    else
                    {
                        string serializedData = Serialization.ToJson(statesByKey);
                        File.WriteAllText(ColumnConfigurationFilePath, serializedData);
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }
        }

        /// <summary>
        /// Builds current column state from visible and initial columns.
        /// </summary>
        private ListViewColumnState BuildCurrentColumnState(GridView gridView)
        {
            List<string> visibleColumns = new List<string>();
            foreach (GridViewColumn column in gridView.Columns)
            {
                if (ListViewColumnOptions.GetForceHidden(column))
                {
                    continue;
                }

                string key = GetColumnKey(column);
                if (!key.IsNullOrEmpty())
                {
                    visibleColumns.Add(key);
                }
            }

            List<string> orderColumns = new List<string>();
            foreach (GridViewColumn column in gridView.Columns)
            {
                if (ListViewColumnOptions.GetForceHidden(column))
                {
                    continue;
                }

                string key = GetColumnKey(column);
                if (!key.IsNullOrEmpty())
                {
                    orderColumns.Add(key);
                }
            }

            foreach (GridViewColumn column in _initialColumns)
            {
                if (ListViewColumnOptions.GetForceHidden(column))
                {
                    continue;
                }

                string key = GetColumnKey(column);
                if (!key.IsNullOrEmpty() && !orderColumns.Contains(key))
                {
                    orderColumns.Add(key);
                }
            }

            return new ListViewColumnState
            {
                VisibleColumnKeys = visibleColumns,
                OrderedColumnKeys = orderColumns
            };
        }

        /// <summary>
        /// Applies saved state to current grid view columns.
        /// </summary>
        private void ApplyColumnState(GridView gridView, ListViewColumnState state)
        {
            List<string> orderedKeys = state.OrderedColumnKeys ?? new List<string>();
            List<string> visibleKeys = state.VisibleColumnKeys ?? new List<string>();

            if (visibleKeys.Count == 0)
            {
                visibleKeys = orderedKeys;
            }

            List<GridViewColumn> restoredColumns = new List<GridViewColumn>();
            foreach (string key in orderedKeys)
            {
                GridViewColumn column = FindInitialColumnByPersistedKey(key);
                if (column != null && visibleKeys.Contains(key) && !ListViewColumnOptions.GetForceHidden(column))
                {
                    restoredColumns.Add(column);
                }
            }

            if (restoredColumns.Count == 0)
            {
                restoredColumns = _initialColumns.ToList();
            }

            if (restoredColumns.Count > 0)
            {
                gridView.Columns.Clear();
                foreach (GridViewColumn column in restoredColumns)
                {
                    gridView.Columns.Add(column);
                }
            }

            ApplyForcedHiddenColumns(gridView);
        }

        /// <summary>
        /// Resolves a persisted key to an initial column while supporting legacy header-based keys.
        /// </summary>
        private GridViewColumn FindInitialColumnByPersistedKey(string persistedKey)
        {
            if (persistedKey.IsNullOrEmpty())
            {
                return null;
            }

            GridViewColumn byKey = _initialColumns.FirstOrDefault(c => GetColumnKey(c).IsEqual(persistedKey));
            if (byKey != null)
            {
                return byKey;
            }

            return _initialColumns.FirstOrDefault(c => GetColumnDisplayName(c).IsEqual(persistedKey));
        }

        /// <summary>
        /// Loads persisted states map from file while supporting legacy format.
        /// </summary>
        private Dictionary<string, ListViewColumnState> LoadAllPersistedStates()
        {
            if (ColumnConfigurationFilePath.IsNullOrEmpty() || !File.Exists(ColumnConfigurationFilePath))
            {
                return new Dictionary<string, ListViewColumnState>();
            }

            try
            {
                Dictionary<string, ListViewColumnState> statesByKey = Serialization.FromJsonFile<Dictionary<string, ListViewColumnState>>(ColumnConfigurationFilePath);
                if (statesByKey != null)
                {
                    return statesByKey;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            try
            {
                List<string> legacyOrder = Serialization.FromJsonFile<List<string>>(ColumnConfigurationFilePath);
                if (legacyOrder != null && legacyOrder.Count > 0)
                {
                    return new Dictionary<string, ListViewColumnState>
                    {
                        {
                            GetColumnConfigurationKey(),
                            new ListViewColumnState
                            {
                                OrderedColumnKeys = legacyOrder,
                                VisibleColumnKeys = legacyOrder
                            }
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            return new Dictionary<string, ListViewColumnState>();
        }

        /// <summary>
        /// Gets the active persistence key for this component instance.
        /// </summary>
        private string GetColumnConfigurationKey()
        {
            if (!SaveColumnConfigurationName.IsNullOrEmpty())
            {
                return SaveColumnConfigurationName;
            }

            switch (ColumnConfigurationScope)
            {
                case ColumnConfigurationScope.Custom:
                    if (!ColumnConfigurationKey.IsNullOrEmpty())
                    {
                        return ColumnConfigurationKey;
                    }
                    break;

                case ColumnConfigurationScope.ViewType:
                    {
                        string ownerType = GetOwnerTypeName();
                        if (!ownerType.IsNullOrEmpty())
                        {
                            return ownerType;
                        }
                    }
                    break;

                case ColumnConfigurationScope.Name:
                default:
                    if (!this.Name.IsNullOrEmpty())
                    {
                        return this.Name;
                    }
                    break;
            }

            if (!this.Name.IsNullOrEmpty())
            {
                return this.Name;
            }

            return "Default";
        }

        /// <summary>
        /// Gets owner view type name for configuration scoping.
        /// </summary>
        private string GetOwnerTypeName()
        {
            FrameworkElement currentElement = this;
            while (currentElement != null)
            {
                FrameworkElement parentElement = VisualTreeHelper.GetParent(currentElement) as FrameworkElement;
                if (parentElement == null)
                {
                    break;
                }

                if (parentElement is UserControl || parentElement is Window)
                {
                    return parentElement.GetType().FullName;
                }

                currentElement = parentElement;
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets a stable key for a column.
        /// </summary>
        private string GetColumnKey(GridViewColumn column)
        {
            if (column == null)
            {
                return string.Empty;
            }

            Binding binding = column.DisplayMemberBinding as Binding;
            if (binding?.Path != null && !binding.Path.Path.IsNullOrEmpty())
            {
                return binding.Path.Path;
            }

            return GetColumnDisplayName(column);
        }

        /// <summary>
        /// Gets display name for a column.
        /// </summary>
        private string GetColumnDisplayName(GridViewColumn column)
        {
            if (column?.Header == null)
            {
                return string.Empty;
            }

            if (column.Header is GridViewColumnHeader header)
            {
                if (header.Content is StackPanel stackPanel && stackPanel.Children.Count > 0)
                {
                    Label firstLabel = stackPanel.Children[0] as Label;
                    if (firstLabel?.Content != null && !firstLabel.Content.ToString().IsNullOrEmpty())
                    {
                        return firstLabel.Content.ToString();
                    }
                }

                if (header.Content != null && !header.Content.ToString().IsNullOrEmpty())
                {
                    return header.Content.ToString();
                }

                return string.Empty;
            }

            string value = column.Header.ToString();
            return value.IsNullOrEmpty() ? string.Empty : value;
        }

        #endregion
    }


    /// <summary>
    /// Defines how column configuration key is resolved.
    /// </summary>
    public enum ColumnConfigurationScope
    {
        /// <summary>
        /// Uses ListView control Name when available.
        /// </summary>
        Name = 0,

        /// <summary>
        /// Uses parent view type name (UserControl or Window).
        /// </summary>
        ViewType = 1,

        /// <summary>
        /// Uses explicit key provided by ColumnConfigurationKey.
        /// </summary>
        Custom = 2
    }


    /// <summary>
    /// Persisted column state for a ListView configuration key.
    /// </summary>
    public class ListViewColumnState
    {
        /// <summary>
        /// Gets or sets ordered column keys, including hidden columns.
        /// </summary>
        public List<string> OrderedColumnKeys { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets visible column keys.
        /// </summary>
        public List<string> VisibleColumnKeys { get; set; } = new List<string>();
    }


    /// <summary>
    /// Provides attached options for GridViewColumn behavior in ListViewExtend.
    /// </summary>
    public static class ListViewColumnOptions
    {
        /// <summary>
        /// Attached property to define whether a column is listed in the column management menu.
        /// </summary>
        public static readonly DependencyProperty ShowInColumnManagementMenuProperty = DependencyProperty.RegisterAttached(
            "ShowInColumnManagementMenu",
            typeof(bool),
            typeof(ListViewColumnOptions),
            new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Attached property to define the property path used for sorting this column.
        /// </summary>
        public static readonly DependencyProperty SortMemberPathProperty = DependencyProperty.RegisterAttached(
            "SortMemberPath",
            typeof(string),
            typeof(ListViewColumnOptions),
            new FrameworkPropertyMetadata(string.Empty));

        /// <summary>
        /// Attached property to force a column to stay hidden and excluded from management menu.
        /// </summary>
        public static readonly DependencyProperty ForceHiddenProperty = DependencyProperty.RegisterAttached(
            "ForceHidden",
            typeof(bool),
            typeof(ListViewColumnOptions),
            new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Sets whether the target column should be listed in the column management menu.
        /// </summary>
        public static void SetShowInColumnManagementMenu(DependencyObject element, bool value)
        {
            element.SetValue(ShowInColumnManagementMenuProperty, value);
        }

        /// <summary>
        /// Gets whether the target column should be listed in the column management menu.
        /// </summary>
        public static bool GetShowInColumnManagementMenu(DependencyObject element)
        {
            return (bool)element.GetValue(ShowInColumnManagementMenuProperty);
        }

        /// <summary>
        /// Sets property path used when sorting by this column.
        /// </summary>
        public static void SetSortMemberPath(DependencyObject element, string value)
        {
            element.SetValue(SortMemberPathProperty, value);
        }

        /// <summary>
        /// Gets property path used when sorting by this column.
        /// </summary>
        public static string GetSortMemberPath(DependencyObject element)
        {
            return (string)element.GetValue(SortMemberPathProperty);
        }

        /// <summary>
        /// Sets whether the target column is force hidden.
        /// </summary>
        public static void SetForceHidden(DependencyObject element, bool value)
        {
            element.SetValue(ForceHiddenProperty, value);
        }

        /// <summary>
        /// Gets whether the target column is force hidden.
        /// </summary>
        public static bool GetForceHidden(DependencyObject element)
        {
            return (bool)element.GetValue(ForceHiddenProperty);
        }
    }


    /// <summary>
    /// Extended GridViewColumnHeader with additional reference index property.
    /// </summary>
    public class GridViewColumnHeaderExtend : GridViewColumnHeader
    {
        /// <summary>
        /// Gets or sets the reference index for this column header.
        /// </summary>
        public int RefIndex
        {
            get => (int)GetValue(RefIndexProperty);
            set => SetValue(RefIndexProperty, value);
        }

        /// <summary>
        /// Dependency property for RefIndex.
        /// </summary>
        public static readonly DependencyProperty RefIndexProperty = DependencyProperty.Register(
            nameof(RefIndex),
            typeof(int),
            typeof(GridViewColumnHeaderExtend),
            new FrameworkPropertyMetadata(-1));
    }
}