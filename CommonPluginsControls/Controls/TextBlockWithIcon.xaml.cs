using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CommonPluginsControls.Controls
{
    /// <summary>
    /// Text block that can show a file/image icon, a font glyph, and/or a text label.
    /// Does not overwrite <see cref="FrameworkElement.DataContext"/> so parent bindings stay valid.
    /// </summary>
    public partial class TextBlockWithIcon : TextBlock
    {
        #region Properties
        public TextBlockWithIconMode Mode
        {
            get { return (TextBlockWithIconMode)GetValue(ModeProperty); }
            set { SetValue(ModeProperty, value); }
        }

        public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(
            nameof(Mode),
            typeof(TextBlockWithIconMode),
            typeof(TextBlockWithIcon),
            new FrameworkPropertyMetadata(TextBlockWithIconMode.IconTextFirstWithText, ControlsPropertyChangedCallback));

        public object Icon
        {
            get { return GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
            nameof(Icon),
            typeof(object),
            typeof(TextBlockWithIcon),
            new FrameworkPropertyMetadata(null, ControlsPropertyChangedCallback));

        /// <summary>
        /// Label shown beside the icon (not <see cref="TextBlock.Text"/> — that DP conflicts with the control's Grid content).
        /// </summary>
        public string LabelText
        {
            get { return (string)GetValue(LabelTextProperty); }
            set { SetValue(LabelTextProperty, value); }
        }

        public static readonly DependencyProperty LabelTextProperty = DependencyProperty.Register(
            nameof(LabelText),
            typeof(string),
            typeof(TextBlockWithIcon),
            new FrameworkPropertyMetadata(string.Empty, ControlsPropertyChangedCallback));

        /// <summary>
        /// Alias of <see cref="LabelText"/> for existing C# callers. Prefer <see cref="LabelText"/> in XAML.
        /// </summary>
        public new string Text
        {
            get { return LabelText; }
            set { LabelText = value; }
        }

        public string IconText
        {
            get { return (string)GetValue(IconTextProperty); }
            set { SetValue(IconTextProperty, value); }
        }

        public static readonly DependencyProperty IconTextProperty = DependencyProperty.Register(
            nameof(IconText),
            typeof(string),
            typeof(TextBlockWithIcon),
            new FrameworkPropertyMetadata(string.Empty, ControlsPropertyChangedCallback));
        #endregion

        private static void ControlsPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is TextBlockWithIcon obj && e.NewValue != e.OldValue)
            {
                obj.SetData();
            }
        }

        public TextBlockWithIcon()
        {
            InitializeComponent();
        }

        private void SetData()
        {
            if (PART_Icon == null || PART_IconText == null || PART_Text == null || PART_Spacer == null)
            {
                return;
            }

            bool useIcon = false;
            bool useIconText = false;
            bool useText = false;
            bool useMargin = false;

            switch (Mode)
            {
                case TextBlockWithIconMode.TextOnly:
                    useText = true;
                    break;

                case TextBlockWithIconMode.IconOnly:
                    useIcon = true;
                    break;

                case TextBlockWithIconMode.IconWithText:
                    useIcon = true;
                    useText = true;
                    useMargin = !string.IsNullOrEmpty(LabelText);
                    break;

                case TextBlockWithIconMode.IconTextOnly:
                    useIconText = true;
                    break;

                case TextBlockWithIconMode.IconTextWithText:
                    useIconText = true;
                    useText = true;
                    useMargin = !string.IsNullOrEmpty(LabelText);
                    break;

                case TextBlockWithIconMode.IconFirstOnly:
                    if (HasRenderableIcon())
                    {
                        useIcon = true;
                    }
                    else
                    {
                        useIconText = true;
                    }
                    break;

                case TextBlockWithIconMode.IconFirstWithText:
                    if (HasRenderableIcon())
                    {
                        useIcon = true;
                    }
                    else
                    {
                        useIconText = true;
                    }

                    useText = true;
                    useMargin = !string.IsNullOrEmpty(LabelText);
                    break;

                case TextBlockWithIconMode.IconTextFirstOnly:
                    if (!string.IsNullOrEmpty(IconText))
                    {
                        useIconText = true;
                    }
                    else if (HasRenderableIcon())
                    {
                        useIcon = true;
                    }
                    break;

                case TextBlockWithIconMode.IconTextFirstWithText:
                    if (!string.IsNullOrEmpty(IconText))
                    {
                        useIconText = true;
                    }
                    else if (HasRenderableIcon())
                    {
                        useIcon = true;
                    }

                    useText = true;
                    useMargin = !string.IsNullOrEmpty(LabelText);
                    break;
            }

            if (useIcon && !HasRenderableIcon())
            {
                useIcon = false;
            }

            if (useIconText && string.IsNullOrEmpty(IconText))
            {
                useIconText = false;
            }

            if (useMargin && !useIcon && !useIconText)
            {
                useMargin = false;
            }

            ApplyIconSource(useIcon);
            PART_Icon.Visibility = useIcon ? Visibility.Visible : Visibility.Collapsed;

            PART_IconText.Text = IconText ?? string.Empty;
            PART_IconText.Visibility = useIconText ? Visibility.Visible : Visibility.Collapsed;

            PART_Text.Text = LabelText ?? string.Empty;
            PART_Text.Visibility = useText ? Visibility.Visible : Visibility.Collapsed;

            PART_Spacer.Visibility = useMargin ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ApplyIconSource(bool useIcon)
        {
            if (!useIcon)
            {
                PART_Icon.Source = null;
                return;
            }

            ImageSource imageSource = Icon as ImageSource;
            if (imageSource != null)
            {
                PART_Icon.Source = imageSource;
                return;
            }

            string path = Icon as string;
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                try
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new System.Uri(path, System.UriKind.Absolute);
                    bitmap.EndInit();
                    bitmap.Freeze();
                    PART_Icon.Source = bitmap;
                    return;
                }
                catch
                {
                    // Fall through to clear source.
                }
            }

            PART_Icon.Source = null;
        }

        /// <summary>
        /// True when <see cref="Icon"/> is an existing file path or a non-string image source (e.g. DefaultGameIcon).
        /// </summary>
        private bool HasRenderableIcon()
        {
            if (Icon == null)
            {
                return false;
            }

            string path = Icon as string;
            if (path != null)
            {
                return !string.IsNullOrEmpty(path) && File.Exists(path);
            }

            return Icon is ImageSource;
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            SetData();
            double fontSize = PART_Text.FontSize;
            PART_IconText.FontSize = fontSize + 8;
            PART_Icon.Height = fontSize + 12;
        }
    }

    public enum TextBlockWithIconMode
    {
        TextOnly,
        IconOnly, IconWithText,
        IconTextOnly, IconTextWithText,
        IconFirstOnly, IconFirstWithText,
        IconTextFirstOnly, IconTextFirstWithText
    }
}
