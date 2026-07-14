using CommonPlayniteShared;
using CommonPlayniteShared.Common;
using CommonPluginsShared.Caching;
using CommonPluginsShared.Converters;
using CommonPluginsShared.Images;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace CommonPluginsShared.Controls
{
    /// <summary>
    /// WPF <see cref="Image"/> control that loads sources asynchronously from local paths or remote URLs.
    /// Supports download throttling, stale-request cancellation, and configurable decode height for memory efficiency.
    /// </summary>
    public class ImageAsync : Image
    {
        private const int MaxConcurrentDownloads = 8;
        private static readonly SemaphoreSlim DownloadSemaphore = new SemaphoreSlim(MaxConcurrentDownloads, MaxConcurrentDownloads);

        /// <summary>
        /// Tracks the source currently being loaded or displayed; used to discard superseded async results.
        /// </summary>
        internal object CurrentImage { get; set; }

        #region Properties

        /// <summary>
        /// Identifies the <see cref="Source"/> dependency property (string path or URL).
        /// </summary>
        public static new readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            nameof(Source),
            typeof(string),
            typeof(ImageAsync),
            new FrameworkPropertyMetadata(string.Empty, SourceChanged));

        /// <summary>
        /// Gets or sets the image source as a local file path or remote URL.
        /// Assigning a new value triggers an asynchronous load; the resolved bitmap is applied to the base <see cref="Image.Source"/>.
        /// </summary>
        public new string Source
        {
            get => (string)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Parameter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ParameterProperty = DependencyProperty.Register(
            nameof(Parameter),
            typeof(string),
            typeof(ImageAsync),
            new FrameworkPropertyMetadata(string.Empty, SourceChanged));

        /// <summary>
        /// Gets or sets an optional converter parameter forwarded to <see cref="ImageConverter"/> for local file resolution.
        /// </summary>
        public string Parameter
        {
            get => (string)GetValue(ParameterProperty);
            set => SetValue(ParameterProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="DecodePixelHeight"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DecodePixelHeightProperty = DependencyProperty.Register(
            nameof(DecodePixelHeight),
            typeof(double),
            typeof(ImageAsync),
            new FrameworkPropertyMetadata(200.0, DecodePixelHeightChanged));

        /// <summary>
        /// Gets or sets the maximum pixel height used when decoding the image (default 200).
        /// Changing this value reloads the current <see cref="Source"/> at the new resolution.
        /// </summary>
        public double DecodePixelHeight
        {
            get => (double)GetValue(DecodePixelHeightProperty);
            set => SetValue(DecodePixelHeightProperty, value);
        }

        /// <summary>
        /// Identifies the read-only <see cref="IsLoading"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsLoadingProperty = DependencyProperty.Register(
            nameof(IsLoading),
            typeof(bool),
            typeof(ImageAsync),
            new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Gets a value indicating whether an image download or decode is in progress.
        /// </summary>
        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            private set => SetValue(IsLoadingProperty, value);
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageAsync"/> class.
        /// </summary>
        public ImageAsync()
        {
        }

        #endregion

        #region Methods

        private static void SourceChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            try
            {
                ImageAsync control = (ImageAsync)obj;
                control.LoadNewSource(args.NewValue, args.OldValue);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, "ImageAsync");
            }
        }

        private static void DecodePixelHeightChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            try
            {
                ImageAsync control = (ImageAsync)obj;
                string currentSource = control.Source;
                if (string.IsNullOrEmpty(currentSource))
                {
                    return;
                }

                // Bypass duplicate-source guard so the image is re-decoded at the new height.
                object previousCurrent = control.CurrentImage;
                control.CurrentImage = null;
                control.LoadNewSource(currentSource, previousCurrent);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, "ImageAsync");
            }
        }

        private async void LoadNewSource(object newSource, object oldSource)
        {
            if (newSource?.Equals(CurrentImage) == true)
            {
                return;
            }

            CurrentImage = newSource;
            SetIsLoading(!string.IsNullOrEmpty(newSource as string));

            dynamic image = null;
            string parameter = Parameter;
            object[] values = new object[] { newSource, DecodePixelHeight };

            if (newSource != null)
            {
                object requestedSource = newSource;
                Stopwatch stopwatch = Stopwatch.StartNew();
                bool throttleDownload = RequiresDownloadThrottle(newSource as string);

                if (throttleDownload)
                {
                    await DownloadSemaphore.WaitAsync();
                }

                try
                {
                    if (!requestedSource.Equals(CurrentImage))
                    {
                        return;
                    }

                    image = await Task.Run(() => LoadImageSync(newSource, values, parameter));
                }
                finally
                {
                    if (throttleDownload)
                    {
                        DownloadSemaphore.Release();
                    }
                }

                stopwatch.Stop();

                if (!requestedSource.Equals(CurrentImage))
                {
                    Common.LogDebug(true, string.Format(
                        "[ImageAsync] Discarded stale load for {0} ({1}ms)",
                        FormatSourceForLog(requestedSource),
                        stopwatch.ElapsedMilliseconds));
                    SetIsLoading(false);
                    return;
                }

                if (image == null)
                {
                    Common.LogDebug(false, string.Format(
                        "[ImageAsync] Load failed for {0} after {1}ms",
                        FormatSourceForLog(requestedSource),
                        stopwatch.ElapsedMilliseconds));
                }
                else
                {
                    Common.LogDebug(true, string.Format(
                        "[ImageAsync] Load succeeded for {0} in {1}ms",
                        FormatSourceForLog(requestedSource),
                        stopwatch.ElapsedMilliseconds));
                }
            }

            SetIsLoading(false);
            base.Source = image;
        }

        private static bool RequiresDownloadThrottle(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return false;
            }

            if (File.Exists(source))
            {
                return false;
            }

            if (ImageSourceManagerPlugin.TryGetCachedImage(source, out _))
            {
                return false;
            }

            if (StringExtensions.IsHttpUrl(source))
            {
                return !HttpFileCacheService.IsFileCached(source);
            }

            return false;
        }

        private static object LoadImageSync(object newSource, object[] values, string parameter)
        {
            if (newSource is string str)
            {
                object tmpImage = new ImageConverter().Convert(values, null, parameter, null);
                if (tmpImage is BitmapImage)
                {
                    Common.LogDebug(true, string.Format(
                        "[ImageAsync] ImageConverter (local file) for {0}",
                        FormatSourceForLog(str)));
                    ((BitmapImage)tmpImage).Freeze();
                    return tmpImage;
                }

                Common.LogDebug(true, string.Format(
                    "[ImageAsync] ImageSourceManagerPlugin.GetImage for {0}",
                    FormatSourceForLog(str)));
                tmpImage = ImageSourceManagerPlugin.GetImage(str, true);
                if (tmpImage is BitmapImage)
                {
                    ((BitmapImage)tmpImage).Freeze();
                }

                return tmpImage;
            }

            Common.LogDebug(false, string.Format(
                "[ImageAsync] Unsupported source type: {0}",
                newSource.GetType().Name));
            return null;
        }

        private void SetIsLoading(bool value)
        {
            if (Dispatcher.CheckAccess())
            {
                IsLoading = value;
            }
            else
            {
                Dispatcher.BeginInvoke(new Action(() => IsLoading = value));
            }
        }

        private static string FormatSourceForLog(object source)
        {
            if (source == null)
            {
                return "(null)";
            }

            string text = source.ToString();
            if (text.Length <= 120)
            {
                return text;
            }

            return text.Substring(0, 117) + "...";
        }

        #endregion
    }
}
