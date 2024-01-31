using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;

namespace documentprocessor
{
    public static class ImageHelper
    {
        private const int OrientationKey = 0x0112;
        private const int LowDpi = 0x0112;

        enum ImageOrientation
        {
            NotSpecified = 0,
            NormalOrientation = 1,
            MirrorHorizontal = 2,
            UpsideDown = 3,
            MirrorVertical = 4,
            MirrorHorizontalAndRotateRight = 5,
            RotateLeft = 6,
            MirorHorizontalAndRotateLeft = 7,
            RotateRight = 8,
        }

        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        static Size GetScaledSize(Size curSize, float xDpi, float yDpi, float targetXDpi, float targetYDpi, Size maxSize)
        {
            double width = curSize.Width / xDpi * targetXDpi;
            double height = curSize.Height / yDpi * targetYDpi;
            if (height > maxSize.Height || width > maxSize.Width)
            {
                double scaleFactor = height / maxSize.Height;
                if (width / maxSize.Width > scaleFactor)
                {
                    scaleFactor = width / maxSize.Width;
                }
                height /= scaleFactor;
                width /= scaleFactor;
            }
            return new Size((int)width, (int)height);
        }

        public static void ResizeImage(string sourcePath, string outputPath, ImageFormat format, Size maxSize)
        {
            using MemoryStream ms = new MemoryStream();
            try
            {
                using Image source = Image.FromFile(sourcePath);
                ResizeImage(source, ms, format, 0, 0, maxSize);
                
            }
            catch (OutOfMemoryException)
            {
                log.Warn($"{sourcePath} failed to resize. Likely invalid or empty image. Writing blank image instead.");
                GetBlankImage(ms, format, LowDpi, LowDpi, maxSize);
            }
            using FileStream fs = new FileStream(outputPath, FileMode.Create);
            ms.WriteTo(fs);
        }

        public static void FixOrientation(Image image)
        {
            if (image.PropertyIdList.Contains(OrientationKey))
            {
                var orientation = (ImageOrientation)image.GetPropertyItem(OrientationKey).Value[0];
                switch (orientation)
                {
                    case ImageOrientation.NotSpecified: // Assume it is good.
                    case ImageOrientation.NormalOrientation:
                        // No rotation required.
                        break;
                    case ImageOrientation.MirrorHorizontal:
                        image.RotateFlip(RotateFlipType.RotateNoneFlipX);
                        break;
                    case ImageOrientation.UpsideDown:
                        image.RotateFlip(RotateFlipType.Rotate180FlipNone);
                        break;
                    case ImageOrientation.MirrorVertical:
                        image.RotateFlip(RotateFlipType.Rotate180FlipX);
                        break;
                    case ImageOrientation.MirrorHorizontalAndRotateRight:
                        image.RotateFlip(RotateFlipType.Rotate90FlipX);
                        break;
                    case ImageOrientation.RotateLeft:
                        image.RotateFlip(RotateFlipType.Rotate90FlipNone);
                        break;
                    case ImageOrientation.MirorHorizontalAndRotateLeft:
                        image.RotateFlip(RotateFlipType.Rotate270FlipX);
                        break;
                    case ImageOrientation.RotateRight:
                        image.RotateFlip(RotateFlipType.Rotate270FlipNone);
                        break;
                    default:
                        throw new NotImplementedException("An orientation of " + orientation + " isn't implemented.");
                }
            }
        }

        public static void GetBlankImage(MemoryStream outputStream, ImageFormat format, float xDpi, float yDpi, Size maxSize)
        {
            using Bitmap blankImage = new Bitmap(maxSize.Width, maxSize.Height);
            using Graphics g = Graphics.FromImage(blankImage);
            g.FillRectangle(Brushes.White, 0, 0, maxSize.Width, maxSize.Height);
            blankImage.SetResolution(xDpi, yDpi);
            blankImage.Save(outputStream, format);
        }

        public static void ResizeImage(Image source, MemoryStream outputStream, ImageFormat format, float xDpi, float yDpi, Size maxSize)
        {
            xDpi = xDpi == 0 ? source.HorizontalResolution : xDpi;
            yDpi = yDpi == 0 ? source.VerticalResolution : yDpi;

            Size scaledSize = GetScaledSize(
                source.Size,
                source.HorizontalResolution,
                source.VerticalResolution,
                xDpi,
                yDpi,
                maxSize);

            using Bitmap scaledImage = new Bitmap(scaledSize.Width, scaledSize.Height);
            scaledImage.SetResolution(xDpi, yDpi);

            using Graphics g = Graphics.FromImage(scaledImage);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.DrawImage(
                source,
                new Rectangle(0, 0, scaledSize.Width, scaledSize.Height),
                0, 0, source.Width, source.Height, GraphicsUnit.Pixel);

            scaledImage.Save(outputStream, format);
        }
    }
}
