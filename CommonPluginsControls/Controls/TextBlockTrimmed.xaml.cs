using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CommonPluginsControls.Controls
{
    /// <summary>
    /// A <see cref="TextBlock"/> that trims overflowing text with an ellipsis and
    /// dynamically shows a tooltip only when the text is actually truncated.
    /// The tooltip is intentionally invisible until needed to avoid flicker on short text.
    /// </summary>
    public partial class TextBlockTrimmed : TextBlock
    {
        public TextBlockTrimmed()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Measures the rendered text width and reveals the tooltip only when the
        /// content is wider than the available layout slot.
        /// Uses <see cref="FrameworkElement.ActualWidth"/> instead of
        /// <see cref="UIElement.DesiredSize"/> for a post-layout measurement that
        /// accounts for alignment and stretching.
        /// </summary>
        private void TextBlock_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!(sender is TextBlock textBlock))
            {
                return;
            }

            // Build a FormattedText to measure the full (untrimmed) string width.
            Typeface typeface = new Typeface(
                textBlock.FontFamily,
                textBlock.FontStyle,
                textBlock.FontWeight,
                textBlock.FontStretch);

            // PixelsPerDip must be retrieved from the visual for DPI-awareness.
            double pixelsPerDip = VisualTreeHelper.GetDpi(textBlock).PixelsPerDip;

            FormattedText formattedText = new FormattedText(
                textBlock.Text ?? string.Empty,
                CultureInfo.CurrentUICulture,
                textBlock.FlowDirection,
                typeface,
                textBlock.FontSize,
                textBlock.Foreground,
                pixelsPerDip);

            // ActualWidth reflects the control's rendered size after layout;
            // it is more reliable than DesiredSize for clipping detection.
            bool isTruncated = formattedText.Width > textBlock.ActualWidth;

            if (textBlock.ToolTip is ToolTip tooltip)
            {
                tooltip.Content = isTruncated ? textBlock.Text : string.Empty;
                tooltip.Visibility = isTruncated ? Visibility.Visible : Visibility.Hidden;
            }
        }
    }
}