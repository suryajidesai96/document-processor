using GemBox.Pdf;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace documentprocessor
{
    public class GemPdfToImageProcessor : GenericProcessor
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        const int PDFIMAGE_SIZE = 1080;

        public GemPdfToImageProcessor(Factory factory) : base(factory)
        {
            name = MethodBase.GetCurrentMethod().DeclaringType.ToString().Replace(string.Concat( GetType().Namespace, "."),"");
        }

        [HandleProcessCorruptedStateExceptions]
        public void ConvertPages(string inputPath, string outputPath)
        {
            using PdfDocument document = PdfDocument.Load(inputPath);
            int pageCount = document.Pages.Count;
            
            for (int i = 0; i < pageCount; i++)
            {
                string pagePath = outputPath.Replace("%d", (i + 1).ToString());
                ImageSaveOptions options = new ImageSaveOptions(ImageSaveFormat.Png)
                {
                    PageNumber = i,
                    PixelFormat = PixelFormat.Rgb24,
                    Width = PDFIMAGE_SIZE
                };
                
                // Handle and log errors on specific pages
                // to allow other pages to render
                try
                {
                    document.Save(pagePath, options);
                }
                catch (Exception ex)
                {
                    log.Warn($"Failed to render page {i + 1} of document {inputPath}.", ex);
                }                
            }
        }

        public override void LocalProcess(string workId, Dictionary<string, string> requestInstructions, List<GenericProcessor> chain, List<DocumentInfo> documentInfoList, List<ProcessorResults> results)
        {
            List<DocumentInfo> documentsToProcess = new List<DocumentInfo>();
            List<string> documentPaths = new List<string>();

            foreach (ProcessorResults result in results)
            {
                if (FileFormats.GetFileFormatFamily(result.PathOutputExtension) == FileFormatFamily.PDF)
                {
                    documentsToProcess.Add(result.DocumentInfo);
                    documentPaths.Add(result.PathOutput);
                }
            }

            for (int i = 0; i < documentPaths.Count; i++ ) 
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
    }
}
