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

    public class ImageAsync : Image

    {

        private const int MaxConcurrentDownloads = 8;

        private static readonly SemaphoreSlim DownloadSemaphore = new SemaphoreSlim(MaxConcurrentDownloads, MaxConcurrentDownloads);



        internal object CurrentImage { get; set; }





        #region Properties

        public static new readonly DependencyProperty SourceProperty = DependencyProperty.Register(

            nameof(Source),

            typeof(string),

            typeof(ImageAsync),

            new FrameworkPropertyMetadata(string.Empty, SourceChanged)

        );

        public new string Source

        {

            get => (string)GetValue(SourceProperty);

            set => SetValue(SourceProperty, value);

        }

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



        public static readonly DependencyProperty ParameterProperty = DependencyProperty.Register(

            nameof(Parameter),

            typeof(string),

            typeof(ImageAsync),

            new FrameworkPropertyMetadata(string.Empty, SourceChanged)

        );

        public string Parameter

        {

            get => (string)GetValue(ParameterProperty);

            set => SetValue(ParameterProperty, value);

        }



        public static readonly DependencyProperty DecodePixelHeightProperty = DependencyProperty.Register(

            nameof(DecodePixelHeight),

            typeof(double),

            typeof(ImageAsync),

            new FrameworkPropertyMetadata(200.0, SourceChanged)

        );

        public double DecodePixelHeight

        {

            get => (double)GetValue(DecodePixelHeightProperty);

            set => SetValue(DecodePixelHeightProperty, value);

        }



        public static readonly DependencyProperty IsLoadingProperty = DependencyProperty.Register(

            nameof(IsLoading),

            typeof(bool),

            typeof(ImageAsync),

            new FrameworkPropertyMetadata(false));



        /// <summary>

        /// Gets a value indicating whether an image download/decode is in progress.

        /// </summary>

        public bool IsLoading

        {

            get { return (bool)GetValue(IsLoadingProperty); }

            private set { SetValue(IsLoadingProperty, value); }

        }

        #endregion





        public ImageAsync()

        {

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

    }

}


