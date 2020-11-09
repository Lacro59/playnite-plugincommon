using Playnite.SDK;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PluginCommon
{
    public enum ImageColor
    {
        None = 0,
        Gray = 1,
        Black = 2
    }

    public class ImageProperty
    {
        public int Width { get; set; } 
        public int Height { get; set; } 
    }

    public class ImageTools
    {
        private static ILogger logger = LogManager.GetLogger();


        public static ImageProperty GetImapeProperty(string srcPath)
        {
            try
            {
                Image image = Image.FromFile(srcPath);

                return new ImageProperty
                {
                    Width = image.Width,
                    Height = image.Height,
                };
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "PluginCommon", $"Error on GetImapeProperty()");
                return null;
            }
        }

        public static ImageProperty GetImapeProperty(Stream imgStream)
        {
            try
            {
                Image image = Image.FromStream(imgStream);

                return new ImageProperty
                {
                    Width = image.Width,
                    Height = image.Height,
                };
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "PluginCommon", $"Error on GetImapeProperty()");
                return null;
            }
        }


        #region Resize
        public static string Resize(string srcPath, int width, int height)
        {
            try
            {
                Image image = Image.FromFile(srcPath);
                Bitmap resultImage = Resize(image, width, height);
                string newPath = srcPath.Replace(".png", "_" + width + "x" + height + ".png");
                resultImage.Save(newPath);
                return newPath;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "PluginCommon", $"Error on create image");
                return string.Empty;
            }
        }

        public static void Resize(Stream imgStream, int width, int height, string path)
        {
            try
            {
#if DEBUG
                logger.Debug("PluginCommon - Resize: " + path + ".png");
#endif
                Image image = Image.FromStream(imgStream);
                Bitmap resultImage = Resize(image, width, height);
                resultImage.Save(path + ".png");
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "PluginCommon", $"Error on create image");
            }
        }

        public static Bitmap Resize(Image image, int width, int height)
        {

            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }
        #endregion


        public static FormatConvertedBitmap ConvertBitmapImage(BitmapImage IconImage, ImageColor imageColor = ImageColor.None)
        {
            FormatConvertedBitmap ConvertBitmapSource = new FormatConvertedBitmap();

            ConvertBitmapSource.BeginInit();
            ConvertBitmapSource.Source = IconImage;

            switch (imageColor)
            {
                case ImageColor.Gray:
                    ConvertBitmapSource.DestinationFormat = PixelFormats.Gray32Float;
                    break;

                case ImageColor.Black:
                    ConvertBitmapSource.Source = IconImage;
                    break;
            }

            ConvertBitmapSource.EndInit();

            return ConvertBitmapSource;
        }
    }
}
