using Docnet.Core;
using Docnet.Core.Converters;
using Docnet.Core.Models;
using log4net;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace documentprocessor
{
    public class DocnetPdfToImage : GenericProcessor,  IDisposable
    {
        static readonly IDocLib docLib = DocLib.Instance;
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public DocnetPdfToImage(Factory factory) : base(factory)
        {
            name = MethodBase.GetCurrentMethod().DeclaringType.ToString().Replace(string.Concat(GetType().Namespace, "."), "");
        }

        public void ConvertPages(string inputPath, string outputPath)
        {
            using var docReader = docLib.GetDocReader(
                inputPath,
                new PageDimensions(1080, 1920));
            int pageCount = docReader.GetPageCount();
            for (int i = 0; i < pageCount; i++)
            {
                string pagePath = outputPath.Replace("%d", (i + 1).ToString());
                using var pageReader = docReader.GetPageReader(i);
                var rawBytes = pageReader.GetImage(new NaiveTransparencyRemover(255, 255, 255));
                var width = pageReader.GetPageWidth();
                var height = pageReader.GetPageHeight();

                using var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                AddBytes(bmp, rawBytes);
                bmp.Save(pagePath, ImageFormat.Png);
            }
        }

        private static void AddBytes(Bitmap bmp, byte[] rawBytes)
        {
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);

            var bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, bmp.PixelFormat);
            var pNative = bmpData.Scan0;

            Marshal.Copy(rawBytes, 0, pNative, rawBytes.Length);
            bmp.UnlockBits(bmpData);
        }

        public override void LocalProcess(string workId, Dictionary<string, string> requestInstructions, List<GenericProcessor> chain, List<DocumentInfo> documentInfoList, List<ProcessorResults> results)
        {
            List<DocumentInfo> documentsToProcess = new List<DocumentInfo>();
            List<String> documentPaths = new List<string>();

            foreach (ProcessorResults result in results)
            {
                if (FileFormats.GetFileFormatFamily(result.PathOutputExtension) == FileFormatFamily.PDF)
                {
                    documentsToProcess.Add(result.DocumentInfo);
                    documentPaths.Add(result.PathOutput);
                }
            }

            for (int i = 0; i < documentPaths.Count; i++)
            {
                foreach (FileFormat fileFormat in targetFileFormats)
                {
                    if (FileFormats.GetFileFormatFamily(fileFormat) == FileFormatFamily.Image)
                    {
                        if (stopRequested)
                        {
                            log.Info("Aborting processing");
                            return;
                        }

                        string inputFile = documentPaths[i];
                        string extension = FileFormats.GetPreferredExtension(fileFormat);
                        string outputFile = string.Concat(factory.Utility.TempFileName(name), factory.Config.PageSuffix, "%d.", extension);
                        string fileName = Path.GetFileName(outputFile);

                        ConvertPages(inputFile, outputFile);

                        DirectoryInfo directoryInfo = new DirectoryInfo(factory.Config.TempDirectory);
                        foreach (FileInfo fileInfo in directoryInfo.GetFiles(fileName.Replace("%d", "*")))
                        {
                            ProcessorResults thisResult = new ProcessorResults(documentsToProcess[i], inputFile, name, fileInfo.FullName, true, "pdf to " + extension);
                            results.Add(thisResult);
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (DocLib.Instance != null)
                {
                    DocLib.Instance.Dispose();
                }
            }
        }
    }
}