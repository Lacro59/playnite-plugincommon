using CommonPluginsShared.Extensions;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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
                    obj.PreviewMouseWheel += UI.HandlePreviewMouseWheel;
                }
                else
                {
                    obj.PreviewMouseWheel -= UI.HandlePreviewMouseWheel;
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
            LoadColumnState();

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
                    var (header, index) = FindGridViewColumnWithIndex(SortingDefaultDataName);
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
        private (GridViewColumnHeader header, int index) FindGridViewColumnWithIndex(string dataName)
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
                                return (gridViewColumn.Header as GridViewColumnHeader, i);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false);
                }
            }
            return (null, -1);
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
                                Binding columnBinding;
                                string sortBy;

                                // Specific sort with another column
                                if (headerClicked.Tag != null && !((string)headerClicked.Tag).IsNullOrEmpty())
                                {
                                    GridViewColumnHeader gridViewColumnHeader = FindGridViewColumn((string)headerClicked.Tag);
                                    if (gridViewColumnHeader == null)
                                    {
                                        return;
                                    }

                                    columnBinding = gridViewColumnHeader.Column.DisplayMemberBinding as Binding;
                                    sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;
                                }
                                else
                                {
                                    columnBinding = headerClicked.Column.DisplayMemberBinding as Binding;
                                    sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;
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
                    Binding columnBinding;
                    string sortBy;

                    // Specific sort with another column
                    if (headerClicked.Tag != null && !((string)headerClicked.Tag).IsNullOrEmpty())
                    {
                        GridViewColumnHeader gridViewColumnHeader = FindGridViewColumn((string)headerClicked.Tag);
                        if (gridViewColumnHeader == null)
                        {
                            return;
                        }

                        columnBinding = gridViewColumnHeader.Column.DisplayMemberBinding as Binding;
                        sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;
                    }
                    else
                    {
                        columnBinding = headerClicked.Column.DisplayMemberBinding as Binding;
                        sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;
                    }

                    Sort(sortBy, (ListSortDirection)_lastDirection);
                }
            }
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
                if (e.Action == NotifyCollectionChangedAction.Move && SaveColumn && !SaveColumnFilePath.IsNullOrEmpty())
                {
                    List<string> columnOrder = new List<string>();
                    foreach (var col in ((GridView)this.View).Columns)
                    {
                        columnOrder.Add(col.Header.ToString());
                    }
                    string dataOrder = Serialization.ToJson(columnOrder);
                    File.WriteAllText(SaveColumnFilePath, dataOrder);
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
            if (File.Exists(SaveColumnFilePath))
            {
                try
                {
                    List<string> columnOrder = Serialization.FromJsonFile<List<string>>(SaveColumnFilePath);
                    if (columnOrder != null && columnOrder.Count > 0)
                    {
                        int newIndex = 0;
                        foreach (var colName in columnOrder)
                        {
                            int oldIndex = 0;
                            for (int i = 0; i < ((GridView)this.View).Columns.Count; i++)
                            {
                                if (((GridView)this.View).Columns[i].Header.ToString().Equals(colName))
                                {
                                    oldIndex = i;
                                    break;
                                }
                            }
                            ((GridView)this.View).Columns.Move(oldIndex, newIndex++);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false);
                }
            }
        }

        #endregion
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