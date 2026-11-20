using Playnite.SDK;
using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shell;

namespace CommonPluginsShared.Controls
{
    public partial class SidebarItemControl : UserControl
    {
        private TextBlock PART_TextBlockTitle { get; set; }
        private Grid PART_GridContener { get; set; }
        private ScrollViewer PART_ContentScrollViewer { get; set; }


        public SidebarItemControl()
        {
            // Link
            TextBlock textBlockLink = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center
            };
            WindowChrome.SetIsHitTestVisibleInChrome(textBlockLink, true);

            Hyperlink hyperlink = new Hyperlink();
            hyperlink.Click += (s, e) => { API.Instance.MainView.SwitchToLibraryView(); };
            hyperlink.Inlines.Add(new TextBlock
            {
                Text = "\uea5c",
                FontFamily = ResourceProvider.GetResource("FontIcoFont") as FontFamily,
                FontSize = 26
            });
            textBlockLink.Inlines.Add(hyperlink);

            // Title link
            PART_TextBlockTitle = new TextBlock
            {
                Style = ResourceProvider.GetResource("BaseTextBlockStyle") as Style,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10,0,0,0),
                FontSize = 18
            };

            // Link contener
            StackPanel stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(10, 10, 0, 0)
            };
            DockPanel.SetDock(stackPanel, Dock.Top);

            stackPanel.Children.Add(textBlockLink);
            stackPanel.Children.Add(PART_TextBlockTitle);


            // Content Grid
            PART_GridContener = new Grid();

            // Content ScrollViewer — Auto by default; views with their own list scroll
            // should call SetContentVerticalScrollBarVisibility(Disabled) so the host
            // passes a finite height and the list shrinks instead of growing the page.
            PART_ContentScrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            };

            PART_ContentScrollViewer.Content = PART_GridContener;


            // Control
            DockPanel dockPanel = new DockPanel();
            _ = dockPanel.Children.Add(stackPanel);
            _ = dockPanel.Children.Add(PART_ContentScrollViewer);

            this.Content = dockPanel;
        }


        public void SetTitle(string Title)
        {
            PART_TextBlockTitle.Text = Title;
        }

        public void AddContent(FrameworkElement content)
        {
            _ = PART_GridContener.Children.Add(content);
        }

        /// <summary>
        /// Controls the host content ScrollViewer. Use <see cref="ScrollBarVisibility.Disabled"/>
        /// when the embedded view already scrolls (ListBox / DataGrid) so expanders shrink the list instead of the page.
        /// </summary>
        public void SetContentVerticalScrollBarVisibility(ScrollBarVisibility visibility)
        {
            if (PART_ContentScrollViewer == null)
            {
                return;
            }

            PART_ContentScrollViewer.VerticalScrollBarVisibility = visibility;
        }
    }
}
