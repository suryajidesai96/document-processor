using log4net;
using System.Drawing.Imaging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Reflection;
using GemBox.Pdf;
using GemBox.Pdf.Content;
using System.Drawing.Drawing2D;

namespace documentprocessor
{
    public class GemImageToPdfProcessor : GenericProcessor
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        const float renderDpi = 96;
        const double A4_WIDTH_INCH = 210 / 25.4;
        const double A4_HEIGHT_INCH = 297 / 25.4;
        const int USER_SPACE = 72;

        public GemImageToPdfProcessor(Factory factory) : base(factory)
        {
            name = MethodBase.GetCurrentMethod().DeclaringType.ToString().Replace(string.Concat( GetType().Namespace, "."),"");
        }

        public override void LocalProcess(string workId, Dictionary<string, string> requestInstructions, List<GenericProcessor> chain, List<DocumentInfo> documentInfoList, List<ProcessorResults> results)
        {
            log.Debug("Image to PDF process, local process");

            List<DocumentInfo> documentsToProcess = new List<DocumentInfo>();
            List<String> documentPaths = new List<string>();

            foreach (ProcessorResults result in results)
            {
                if (FileFormats.GetFileFormatFamily(result.PathOutputExtension) == FileFormatFamily.Image)
                {
                    documentsToProcess.Add(result.DocumentInfo);
                    documentPaths.Add(result.PathOutput);
                }
            }

            for (int i = 0; i < documentPaths.Count; i++)
            {
                string inputFile = documentPaths[i];
                string extension = FileFormats.GetPreferredExtension(FileFormat.PDF);
                string outputFile = string.Concat(factory.Utility.TempFileName(name), ".", extension);

                if (stopRequested)
                {
                    log.Info("Aborting processing");
                    return;
                }

                GetPageOfImage(inputFile, outputFile);

                results.Add(new ProcessorResults(documentsToProcess[i], inputFile, name, outputFile.ToString(), true, "image print"));
            }
        }

        public void GetPageOfImage(string imagePath, string outputPath)
        {
            using MemoryStream ms = new MemoryStream();
            using Image source = Image.FromFile(imagePath);
            ImageHelper.FixOrientation(source);

            double pageWidth = (source.Width > source.Height) ? A4_HEIGHT_INCH : A4_WIDTH_INCH;
            double pageHeight = (source.Width > source.Height) ? A4_WIDTH_INCH : A4_HEIGHT_INCH;
            int widthPixel = Convert.ToInt32(renderDpi * pageWidth);
            int heightPixel = Convert.ToInt32(renderDpi * pageHeight);
            Size maxSize = new Size(widthPixel, heightPixel);

            ImageHelper.ResizeImage(source, ms, ImageFormat.Bmp, renderDpi, renderDpi, maxSize);

            using PdfDocument doc = new PdfDocument();
            PdfPage page = doc.Pages.Add();
            PdfImage image = PdfImage.Load(ms);
            PdfSize pageSize = new PdfSize(pageWidth * USER_SPACE, pageHeight * USER_SPACE);

            // Make sure the page is always A4 (landscape or portrait)
            page.SetMediaBox(pageSize.Width, pageSize.Height);

            // Align the picture centre
            double xOffset = (pageSize.Width - image.Size.Width) / 2;
            double yOffset = (pageSize.Height - image.Size.Height) / 2;
            PdfPoint point = new PdfPoint(xOffset, yOffset);
            page.Content.DrawImage(image, point, image.Size);
            doc.Save(outputPath);
        }
    }
}
