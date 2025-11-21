using CommonPlayniteShared.Common;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CommonPluginsShared
{
    /// <summary>
    /// Enum representing image color transformations.
    /// </summary>
    public enum ImageColor
    {
        None = 0,
        Gray = 1,
        Black = 2
    }


    /// <summary>
    /// Stores basic image properties such as width and height.
    /// </summary>
    public class ImageProperty
    {
        /// <summary>
        /// Gets or sets the width of the image.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Gets or sets the height of the image.
        /// </summary>
        public int Height { get; set; }
    }


    /// <summary>
    /// Provides utility methods for image manipulation and conversion.
    /// </summary>
    public class ImageTools
    {
        #region ImageProperty

        /// <summary>
        /// Gets the width and height of an image from a file path.
        /// </summary>
        /// <param name="srcPath">The source image file path.</param>
        /// <returns>An <see cref="ImageProperty"/> object, or null if the file does not exist or an error occurs.</returns>
        public static ImageProperty GetImageProperty(string srcPath)
        {
            if (!File.Exists(srcPath))
            {
                return null;
            }

            try
            {
                using (Image image = Image.FromFile(srcPath))
                {
                    return new ImageProperty
                    {
                        Width = image.Width,
                        Height = image.Height,
                    };
                };
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Error on GetImageProperty({srcPath})");
                return null;
            }
        }

		/// <summary>
		/// Gets the width and height of an image from a stream.
		/// </summary>
		/// <param name="imgStream">The image stream.</param>
		/// <returns>An <see cref="ImageProperty"/> object, or null if an error occurs.</returns>
		public static ImageProperty GetImageProperty(Stream imgStream)
		{
			try
            {
                using (Image image = Image.FromStream(imgStream))
                {
                    return new ImageProperty
                    {
                        Width = image.Width,
                        Height = image.Height,
                    };
                };
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return null;
            }
        }

        /// <summary>
        /// Gets the width and height of an image from an <see cref="Image"/> object.
        /// </summary>
        /// <param name="image">The image object.</param>
        /// <returns>An <see cref="ImageProperty"/> object, or null if an error occurs.</returns>
        public static ImageProperty GetImageProperty(Image image)
        {
            try
            {
                return new ImageProperty
                {
                    Width = image.Width,
                    Height = image.Height,
                };
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return null;
            }
        }

        #endregion

        #region Resize

        /// <summary>
        /// Resizes an image from a file path to the specified width and height, and saves the result as a new file.
        /// </summary>
        /// <param name="srcPath">The source image file path.</param>
        /// <param name="width">The target width.</param>
        /// <param name="height">The target height.</param>
        /// <returns>The path to the resized image, or an empty string if an error occurs.</returns>
        public static string Resize(string srcPath, int width, int height)
        {
            if (!File.Exists(srcPath))
            {
                return string.Empty;
            }

            try
            {
                Image image = Image.FromFile(srcPath);
                Bitmap resultImage = Resize(image, width, height);
                string newPath = srcPath.Replace(".png", "_" + width + "x" + height + ".png");
                resultImage.Save(newPath);

                image.Dispose();
                resultImage.Dispose();

                return newPath;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Error on Resize({srcPath})");
                return string.Empty;
            }
        }

        /// <summary>
        /// Resizes an image to fit within the specified maximum dimension, maintaining aspect ratio.
        /// </summary>
        /// <param name="srcPath">The source image file path.</param>
        /// <param name="max">The maximum width or height.</param>
        /// <param name="path">The destination file path.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public static bool Resize(string srcPath, int max, string path)
        {
            if (!File.Exists(srcPath))
            {
                return false;
            }

            try
            {
                Image image = Image.FromFile(srcPath);

                int width = image.Width;
                int height = image.Height;
                if (width > height)
                {
                    width = max;
                    height = height * max / image.Width;
                }
                else
                {
                    height = max;
                    width = width * max / image.Height;
                }

                Bitmap resultImage = Resize(image, width, height);
                resultImage.Save(path);

                image.Dispose();
                resultImage.Dispose();

                return true;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, true, $"Error on Resize({srcPath})");
                return false;
            }
        }

        /// <summary>
        /// Resizes an image to the specified width and height, or copies the file if resizing is not needed.
        /// </summary>
        /// <param name="srcPath">The source image file path.</param>
        /// <param name="width">The target width.</param>
        /// <param name="height">The target height.</param>
        /// <param name="path">The destination file path.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public static bool Resize(string srcPath, int width, int height, string path)
        {
            if (!File.Exists(srcPath))
            {
                return false;
            }

            try
            {
                Image image = Image.FromFile(srcPath);
                Bitmap resultImage = null;
                if (image.Width > width || image.Height > height)
                {
                    resultImage = Resize(image, width, height);
                    resultImage.Save(path);
                    resultImage.Dispose();
                }
                else
                {
                    FileSystem.CopyFile(srcPath, path);
                }

                image.Dispose();
                return true;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, true, $"Error on Resize({srcPath})");
                return false;
            }
        }

        /// <summary>
        /// Resizes an image from a stream to the specified width and height, and saves the result.
        /// </summary>
        /// <param name="imgStream">The image stream.</param>
        /// <param name="width">The target width.</param>
        /// <param name="height">The target height.</param>
        /// <param name="path">The destination file path.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public static bool Resize(Stream imgStream, int width, int height, string path)
        {
            try
            {
                Image image = Image.FromStream(imgStream);
                Bitmap resultImage = Resize(image, width, height);
                resultImage.Save(path);

                image.Dispose();
                resultImage.Dispose();

                return true;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, true);
                return false;
            }
        }

        /// <summary>
        /// Resizes an <see cref="Image"/> object to the specified width and height.
        /// </summary>
        /// <param name="image">The source image.</param>
        /// <param name="width">The target width.</param>
        /// <param name="height">The target height.</param>
        /// <returns>A resized <see cref="Bitmap"/> object, or null if an error occurs.</returns>
        public static Bitmap Resize(Image image, int width, int height)
        {
            try
            { 
                Rectangle destRect = new Rectangle(0, 0, width, height);
                Bitmap destImage = new Bitmap(width, height);

                destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

                using (Graphics graphics = Graphics.FromImage(destImage))
                {
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    using (ImageAttributes wrapMode = new ImageAttributes())
                    {
                        wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                        graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                    }
                }

                return destImage;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return null;
            }
        }

        #endregion

        #region Convert

        /// <summary>
        /// Converts a <see cref="BitmapImage"/> to a <see cref="FormatConvertedBitmap"/> with optional color transformation.
        /// </summary>
        /// <param name="iconImage">The source bitmap image.</param>
        /// <param name="imageColor">The color transformation to apply.</param>
        /// <returns>A <see cref="FormatConvertedBitmap"/> object, or null if the source is null or an error occurs.</returns>
        public static FormatConvertedBitmap ConvertBitmapImage(BitmapImage iconImage, ImageColor imageColor = ImageColor.None)
        {
            if (iconImage is null)
            {
                return null;
            }


            FormatConvertedBitmap convertBitmapSource = new FormatConvertedBitmap();

            try
            {
                convertBitmapSource.BeginInit();
                convertBitmapSource.Source = iconImage;

                switch (imageColor)
                {
                    case ImageColor.Gray:
                        convertBitmapSource.DestinationFormat = PixelFormats.Gray32Float;
                        break;

                    case ImageColor.Black:
                        convertBitmapSource.Source = iconImage;
                        break;

                    case ImageColor.None:
                        break;

                    default:
                        break;
                }

                convertBitmapSource.EndInit();
                convertBitmapSource.Freeze();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            return convertBitmapSource;
        }

        /// <summary>
        /// Converts a <see cref="Bitmap"/> to a <see cref="BitmapSource"/>.
        /// </summary>
        /// <param name="bitmap">The source bitmap.</param>
        /// <returns>A <see cref="BitmapSource"/> object, or null if an error occurs.</returns>
        public static BitmapSource ConvertBitmapToBitmapSource(Bitmap bitmap)
        {
            try
            {
                BitmapData bitmapData = bitmap.LockBits(
                    new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    ImageLockMode.ReadOnly, bitmap.PixelFormat);

                BitmapSource bitmapSource = BitmapSource.Create(
                    bitmapData.Width, bitmapData.Height,
                    bitmap.HorizontalResolution, bitmap.VerticalResolution,
                    PixelFormats.Bgr24, null,
                    bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

                bitmap.UnlockBits(bitmapData);

                return bitmapSource;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return null;
            }
        }

        /// <summary>
        /// Converts a <see cref="Bitmap"/> to a <see cref="BitmapImage"/>.
        /// </summary>
        /// <param name="bitmap">The source bitmap.</param>
        /// <returns>A <see cref="BitmapImage"/> object, or null if an error occurs.</returns>
        public static BitmapImage ConvertBitmapToBitmapImage(Bitmap bitmap)
        {
            try
            {
                using (MemoryStream memory = new MemoryStream())
                {
                    bitmap.Save(memory, ImageFormat.Png);
                    memory.Position = 0;

                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memory;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();

                    return bitmapImage;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return null;
            }
        }

        /// <summary>
        /// Converts an <see cref="Image"/> to a <see cref="BitmapImage"/>.
        /// </summary>
        /// <param name="image">The source image.</param>
        /// <returns>A <see cref="BitmapImage"/> object, or null if an error occurs.</returns>
        public static BitmapImage ConvertImageToBitmapImage(Image image)
        {
            try
            {
                using (MemoryStream memory = new MemoryStream())
                {
                    image.Save(memory, ImageFormat.Png);
                    memory.Position = 0;

                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memory;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();

                    return bitmapImage;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return null;
            }
        }

        /// <summary>
        /// Converts an image file to JPEG format with the specified quality.
        /// </summary>
        /// <param name="srcPath">The source image file path.</param>
        /// <param name="quality">The JPEG quality (default is 98).</param>
        /// <returns>The path to the converted JPEG file, or null if an error occurs or the file already exists.</returns>
        public static string ConvertToJpg(string srcPath, long quality = 98L)
        {
            try
            {
                if (File.Exists(srcPath) && Path.GetExtension(srcPath).ToLower() != ".jpg" && Path.GetExtension(srcPath).ToLower() != ".jpeg")
                {
                    using (Image image = Image.FromFile(srcPath))
                    {
                        ImageCodecInfo codecInfo = GetEncoderInfo(ImageFormat.Jpeg);

                        //  Set the quality
                        EncoderParameters parameters = new EncoderParameters(1);
                        parameters.Param[0] = new EncoderParameter(Encoder.Quality, quality);

                        string destPath = srcPath.Replace(Path.GetExtension(srcPath), ".jpg");
                        if (!File.Exists(destPath))
                        {
                            image.Save(destPath, codecInfo, parameters);
                            return destPath;
                        }
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            return null;
        }

        /// <summary>
        /// Gets the <see cref="ImageCodecInfo"/> for the specified image format.
        /// </summary>
        /// <param name="format">The image format.</param>
        /// <returns>The <see cref="ImageCodecInfo"/> object for the format.</returns>
        public static ImageCodecInfo GetEncoderInfo(ImageFormat format)
        {
            return ImageCodecInfo.GetImageEncoders().ToList().Find(delegate (ImageCodecInfo codec)
            {
                return codec.FormatID == format.Guid;
            });
        }

        #endregion
    }
}