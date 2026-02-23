using CommonPlayniteShared;
using CommonPlayniteShared.Common;
using CommonPluginsShared.Caching;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Resources;

namespace CommonPluginsShared.Images
{
	#region Enums and Models

	/// <summary>
	/// Defines color transformation modes for image processing.
	/// </summary>
	public enum ImageColorMode
	{
		/// <summary>
		/// No color transformation.
		/// </summary>
		None = 0,

		/// <summary>
		/// Convert to grayscale.
		/// </summary>
		Gray = 1,

		/// <summary>
		/// Convert to black and white.
		/// </summary>
		Black = 2
	}

	/// <summary>
	/// Represents basic image dimensions.
	/// </summary>
	public class ImageProperty
	{
		/// <summary>
		/// Gets or sets the image width in pixels.
		/// </summary>
		public int Width { get; set; }

		/// <summary>
		/// Gets or sets the image height in pixels.
		/// </summary>
		public int Height { get; set; }
	}

	#endregion

	#region Image Manipulation

	/// <summary>
	/// Provides image manipulation utilities (resize, convert, properties).
	/// Supports GDI+ (System.Drawing) and WPF (System.Windows.Media) formats.
	/// </summary>
	public static class ImageTools
	{
		#region Property Retrieval

		/// <summary>
		/// Gets image dimensions from a file path.
		/// </summary>
		/// <param name="srcPath">Image file path</param>
		/// <returns>Image dimensions or null if file not found/invalid</returns>
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
						Height = image.Height
					};
				}
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, $"Error getting image properties: {srcPath}");
				return null;
			}
		}

		/// <summary>
		/// Gets image dimensions from a stream.
		/// </summary>
		/// <param name="imgStream">Image stream</param>
		/// <returns>Image dimensions or null on error</returns>
		public static ImageProperty GetImageProperty(Stream imgStream)
		{
			try
			{
				using (Image image = Image.FromStream(imgStream))
				{
					return new ImageProperty
					{
						Width = image.Width,
						Height = image.Height
					};
				}
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, "Error getting image properties from stream");
				return null;
			}
		}

		/// <summary>
		/// Gets image dimensions from an Image object.
		/// </summary>
		/// <param name="image">Image object</param>
		/// <returns>Image dimensions or null on error</returns>
		public static ImageProperty GetImageProperty(Image image)
		{
			try
			{
				return new ImageProperty
				{
					Width = image.Width,
					Height = image.Height
				};
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, "Error getting image properties");
				return null;
			}
		}

		#endregion

		#region Resize Operations

		/// <summary>
		/// Resizes an image file and saves to a new file with dimensions in filename.
		/// </summary>
		/// <param name="srcPath">Source image path</param>
		/// <param name="width">Target width</param>
		/// <param name="height">Target height</param>
		/// <returns>New file path or empty string on error</returns>
		/// <example>
		/// Resize("icon.png", 64, 64) => "icon_64x64.png"
		/// </example>
		public static string Resize(string srcPath, int width, int height)
		{
			if (!File.Exists(srcPath))
			{
				return string.Empty;
			}

			try
			{
				using (Image image = Image.FromFile(srcPath))
				{
					using (Bitmap resultImage = Resize(image, width, height))
					{
						string extension = Path.GetExtension(srcPath);
						string newPath = srcPath.Replace(extension, $"_{width}x{height}{extension}");
						resultImage.Save(newPath);
						return newPath;
					}
				}
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, $"Error resizing image: {srcPath}");
				return string.Empty;
			}
		}

		/// <summary>
		/// Resizes an image to fit within a maximum dimension while maintaining aspect ratio.
		/// </summary>
		/// <param name="srcPath">Source image path</param>
		/// <param name="max">Maximum dimension (width or height)</param>
		/// <param name="destPath">Destination file path</param>
		/// <returns>True if successful, false otherwise</returns>
		public static bool Resize(string srcPath, int max, string destPath)
		{
			if (!File.Exists(srcPath))
			{
				return false;
			}

			try
			{
				using (Image image = Image.FromFile(srcPath))
				{
					int width = image.Width;
					int height = image.Height;

					if (width > height)
					{
						width = max;
						height = (int)((double)image.Height * max / image.Width);
					}
					else
					{
						height = max;
						width = (int)((double)image.Width * max / image.Height);
					}

					using (Bitmap resultImage = Resize(image, width, height))
					{
						resultImage.Save(destPath);
						return true;
					}
				}
			}
			catch (Exception ex)
			{
				Common.LogError(ex, true, $"Error resizing image: {srcPath}");
				return false;
			}
		}

		/// <summary>
		/// Resizes an image to specific dimensions or copies if resize not needed.
		/// </summary>
		/// <param name="srcPath">Source image path</param>
		/// <param name="width">Target width</param>
		/// <param name="height">Target height</param>
		/// <param name="destPath">Destination file path</param>
		/// <returns>True if successful, false otherwise</returns>
		public static bool Resize(string srcPath, int width, int height, string destPath)
		{
			if (!File.Exists(srcPath))
			{
				return false;
			}

			try
			{
				using (Image image = Image.FromFile(srcPath))
				{
					if (image.Width > width || image.Height > height)
					{
						using (Bitmap resultImage = Resize(image, width, height))
						{
							resultImage.Save(destPath);
						}
					}
					else
					{
						FileSystem.CopyFile(srcPath, destPath);
					}

					return true;
				}
			}
			catch (Exception ex)
			{
				Common.LogError(ex, true, $"Error resizing image: {srcPath}");
				return false;
			}
		}

		/// <summary>
		/// Resizes an image from a stream to specific dimensions.
		/// </summary>
		/// <param name="imgStream">Image stream</param>
		/// <param name="width">Target width</param>
		/// <param name="height">Target height</param>
		/// <param name="destPath">Destination file path</param>
		/// <returns>True if successful, false otherwise</returns>
		public static bool Resize(Stream imgStream, int width, int height, string destPath)
		{
			try
			{
				using (Image image = Image.FromStream(imgStream))
				{
					using (Bitmap resultImage = Resize(image, width, height))
					{
						resultImage.Save(destPath);
						return true;
					}
				}
			}
			catch (Exception ex)
			{
				Common.LogError(ex, true, "Error resizing image from stream");
				return false;
			}
		}

		/// <summary>
		/// Resizes an Image object to specific dimensions with high quality interpolation.
		/// </summary>
		/// <param name="image">Source image</param>
		/// <param name="width">Target width</param>
		/// <param name="height">Target height</param>
		/// <returns>Resized Bitmap or null on error</returns>
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
				Common.LogError(ex, false, "Error resizing image");
				return null;
			}
		}

		#endregion

		#region Conversion Operations

		/// <summary>
		/// Converts a BitmapImage to FormatConvertedBitmap with optional color transformation.
		/// </summary>
		/// <param name="iconImage">Source BitmapImage</param>
		/// <param name="colorMode">Color transformation to apply</param>
		/// <returns>Converted bitmap or null if source is null</returns>
		public static FormatConvertedBitmap ConvertBitmapImage(BitmapImage iconImage, ImageColorMode colorMode = ImageColorMode.None)
		{
			if (iconImage == null)
			{
				return null;
			}

			FormatConvertedBitmap convertedBitmap = new FormatConvertedBitmap();

			try
			{
				convertedBitmap.BeginInit();
				convertedBitmap.Source = iconImage;

				switch (colorMode)
				{
					case ImageColorMode.Gray:
						convertedBitmap.DestinationFormat = PixelFormats.Gray32Float;
						break;

					case ImageColorMode.Black:
						// Keep source format for black mode
						break;

					case ImageColorMode.None:
					default:
						// No transformation
						break;
				}

				convertedBitmap.EndInit();
				convertedBitmap.Freeze();
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, "Error converting BitmapImage");
			}

			return convertedBitmap;
		}

		/// <summary>
		/// Converts a GDI+ Bitmap to WPF BitmapSource.
		/// </summary>
		/// <param name="bitmap">Source Bitmap</param>
		/// <returns>BitmapSource or null on error</returns>
		public static BitmapSource ConvertBitmapToBitmapSource(Bitmap bitmap)
		{
			try
			{
				BitmapData bitmapData = bitmap.LockBits(
					new Rectangle(0, 0, bitmap.Width, bitmap.Height),
					ImageLockMode.ReadOnly,
					bitmap.PixelFormat);

				BitmapSource bitmapSource = BitmapSource.Create(
					bitmapData.Width,
					bitmapData.Height,
					bitmap.HorizontalResolution,
					bitmap.VerticalResolution,
					PixelFormats.Bgr24,
					null,
					bitmapData.Scan0,
					bitmapData.Stride * bitmapData.Height,
					bitmapData.Stride);

				bitmap.UnlockBits(bitmapData);
				return bitmapSource;
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, "Error converting Bitmap to BitmapSource");
				return null;
			}
		}

		/// <summary>
		/// Converts a GDI+ Bitmap to WPF BitmapImage.
		/// </summary>
		/// <param name="bitmap">Source Bitmap</param>
		/// <returns>BitmapImage or null on error</returns>
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
				Common.LogError(ex, false, "Error converting Bitmap to BitmapImage");
				return null;
			}
		}

		/// <summary>
		/// Converts a GDI+ Image to WPF BitmapImage.
		/// </summary>
		/// <param name="image">Source Image</param>
		/// <returns>BitmapImage or null on error</returns>
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
					bitmapImage.Freeze();

					return bitmapImage;
				}
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, "Error converting Image to BitmapImage");
				return null;
			}
		}

		/// <summary>
		/// Converts an image file to JPEG format with specified quality.
		/// </summary>
		/// <param name="srcPath">Source image path</param>
		/// <param name="quality">JPEG quality (0-100, default 98)</param>
		/// <returns>Path to converted JPEG file or null on error</returns>
		public static string ConvertToJpg(string srcPath, long quality = 98L)
		{
			try
			{
				if (!File.Exists(srcPath))
				{
					return null;
				}

				string extension = Path.GetExtension(srcPath).ToLower();
				if (extension == ".jpg" || extension == ".jpeg")
				{
					return srcPath;
				}

				using (Image image = Image.FromFile(srcPath))
				{
					ImageCodecInfo codecInfo = GetEncoderInfo(ImageFormat.Jpeg);
					EncoderParameters encoderParams = new EncoderParameters(1);
					encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);

					string jpgPath = Path.ChangeExtension(srcPath, ".jpg");
					image.Save(jpgPath, codecInfo, encoderParams);

					return jpgPath;
				}
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, $"Error converting to JPG: {srcPath}");
				return null;
			}
		}

		/// <summary>
		/// Gets the JPEG encoder codec info.
		/// </summary>
		/// <param name="format">Image format</param>
		/// <returns>ImageCodecInfo or null if not found</returns>
		private static ImageCodecInfo GetEncoderInfo(ImageFormat format)
		{
			ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
			return codecs.FirstOrDefault(codec => codec.FormatID == format.Guid);
		}

		#endregion
	}

	#endregion

	#region Image Source Manager

	/// <summary>
	/// Manages image loading from multiple sources (files, URLs, resources) with integrated caching.
	/// Supports automatic fallback between HTTP cache implementations.
	/// </summary>
	public static class ImageSourceManagerPlugin
	{
		private static readonly ILogger Logger = LogManager.GetLogger();
		private const string BitmapPropsField = "bitmapprops";

		/// <summary>
		/// Gets the shared memory cache for loaded images.
		/// Default size: 100 MB
		/// </summary>
		public static MemoryCache Cache { get; } = new MemoryCache(Units.MegaBytesToBytes(100));

		#region Path Resolution

		/// <summary>
		/// Resolves an image source to a local file path.
		/// Supports: files, HTTP URLs (with caching), pack:// resources, resources: URIs.
		/// </summary>
		/// <param name="source">Image source (path, URL, or resource URI)</param>
		/// <param name="resize">Optional resize dimension (for HTTP sources)</param>
		/// <returns>Local file path or resource URI, null if unavailable</returns>
		public static string GetImagePath(string source, int resize = 0)
		{
			if (source.IsNullOrEmpty())
			{
				return null;
			}

			// Handle pack:// and resources: URIs
			if (source.StartsWith("resources:") || source.StartsWith("pack://"))
			{
				try
				{
					string imagePath = source;
					if (source.StartsWith("resources:"))
					{
						imagePath = source.Replace("resources:", "pack://application:,,,");
					}

					return imagePath;
				}
				catch (Exception e)
				{
					Logger.Error(e, $"Failed to process resource URI: {source}");
					return null;
				}
			}

			// Handle HTTP URLs with caching
			if (StringExtensions.IsHttpUrl(source))
			{
				try
				{
					string cachedFile = HttpFileCacheService.GetWebFile(source, resize);

					if (string.IsNullOrEmpty(cachedFile))
					{
						// Fallback to CommonPlayniteShared cache
						cachedFile = HttpFileCache.GetWebFile(source);
					}

					return cachedFile;
				}
				catch (Exception exc)
				{
					Common.LogError(exc, true, $"Failed to load HTTP image: {source}");
					return null;
				}
			}

			// Handle local files
			if (File.Exists(source))
			{
				return source;
			}

			return null;
		}

		#endregion

		#region Resource Loading

		/// <summary>
		/// Loads a BitmapImage from application resources with optional caching.
		/// </summary>
		/// <param name="resourceKey">Resource key in application resources</param>
		/// <param name="cached">Enable memory caching</param>
		/// <param name="loadProperties">Optional load properties (decode size, etc.)</param>
		/// <returns>BitmapImage or null if not found</returns>
		public static BitmapImage GetResourceImage(string resourceKey, bool cached, BitmapLoadProperties loadProperties = null)
		{
			if (cached && Cache.TryGet(resourceKey, out var cachedItem))
			{
				BitmapLoadProperties existingMetadata = null;
				if (cachedItem.Metadata.TryGetValue(BitmapPropsField, out object metaValue))
				{
					existingMetadata = (BitmapLoadProperties)metaValue;
				}

				if (existingMetadata?.MaxDecodePixelWidth == loadProperties?.MaxDecodePixelWidth)
				{
					return cachedItem.CacheObject as BitmapImage;
				}

				Cache.TryRemove(resourceKey);
			}

			BitmapImage resource = ResourceProvider.GetResource(resourceKey) as BitmapImage;

			if (loadProperties?.MaxDecodePixelWidth > 0 && resource?.PixelWidth > loadProperties.MaxDecodePixelWidth)
			{
				resource = resource.GetClone(loadProperties);
			}

			if (cached && resource != null)
			{
				long imageSize = 0;
				try
				{
					imageSize = resource.GetSizeInMemory();
				}
				catch (Exception e)
				{
					Logger.Error(e, $"Failed to get image size: {resourceKey}");
				}

				if (imageSize > 0)
				{
					Cache.TryAdd(resourceKey, resource, imageSize, new Dictionary<string, object>
					{
						{ BitmapPropsField, loadProperties }
					});
				}
			}

			return resource;
		}

		#endregion

		#region Image Loading

		/// <summary>
		/// Loads a BitmapImage from any supported source with optional caching.
		/// Supports: local files, HTTP URLs, pack:// resources, resources: URIs.
		/// </summary>
		/// <param name="source">Image source</param>
		/// <param name="cached">Enable memory caching</param>
		/// <param name="loadProperties">Optional load properties</param>
		/// <returns>BitmapImage or null if loading failed</returns>
		public static BitmapImage GetImage(string source, bool cached, BitmapLoadProperties loadProperties = null)
		{
			if (DesignerTools.IsInDesignMode)
			{
				cached = false;
			}

			if (source.IsNullOrEmpty())
			{
				return null;
			}

			// Check cache
			if (cached && Cache.TryGet(source, out var cachedItem))
			{
				BitmapLoadProperties existingMetadata = null;
				if (cachedItem.Metadata.TryGetValue(BitmapPropsField, out object metaValue))
				{
					existingMetadata = (BitmapLoadProperties)metaValue;
				}

				if (existingMetadata == loadProperties)
				{
					return cachedItem.CacheObject as BitmapImage;
				}

				Cache.TryRemove(source);
			}

			// Load from pack:// or resources:
			if (source.StartsWith("resources:") || source.StartsWith("pack://"))
			{
				try
				{
					string imagePath = source;
					if (source.StartsWith("resources:"))
					{
						imagePath = source.Replace("resources:", "pack://application:,,,");
					}

					StreamResourceInfo streamInfo = Application.GetResourceStream(new Uri(imagePath));
					using (Stream stream = streamInfo.Stream)
					{
						BitmapImage imageData = BitmapExtensions.BitmapFromStream(stream, loadProperties);
						if (imageData != null && cached)
						{
							Cache.TryAdd(source, imageData, imageData.GetSizeInMemory(),
								new Dictionary<string, object>
								{
									{ BitmapPropsField, loadProperties }
								});
						}

						return imageData;
					}
				}
				catch (Exception e)
				{
					Logger.Error(e, $"Failed to load resource image: {source}");
					return null;
				}
			}

			// Load from HTTP URL
			if (StringExtensions.IsHttpUrl(source))
			{
				try
				{
					string cachedFile = HttpFileCacheService.GetWebFile(source);

					if (string.IsNullOrEmpty(cachedFile))
					{
						cachedFile = HttpFileCache.GetWebFile(source);
						if (string.IsNullOrEmpty(cachedFile))
						{
							return null;
						}
					}

					return BitmapExtensions.BitmapFromFile(cachedFile, loadProperties);
				}
				catch (Exception exc)
				{
					Logger.Error(exc, $"Failed to load HTTP image: {source}");
					return null;
				}
			}

			// Load from local file
			if (File.Exists(source))
			{
				try
				{
					BitmapImage imageData = BitmapExtensions.BitmapFromFile(source, loadProperties);
					if (imageData != null && cached)
					{
						Cache.TryAdd(source, imageData, imageData.GetSizeInMemory(),
							new Dictionary<string, object>
							{
								{ BitmapPropsField, loadProperties }
							});
					}

					return imageData;
				}
				catch (Exception e)
				{
					Logger.Error(e, $"Failed to load file image: {source}");
					return null;
				}
			}

			return null;
		}

		#endregion
	}

	#endregion
}