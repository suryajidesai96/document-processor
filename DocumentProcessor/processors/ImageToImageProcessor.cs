using System;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Reflection;
using log4net;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace documentprocessor
{
    public class ImageToImageProcessor : GenericProcessor
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public ImageToImageProcessor(Factory factory) : base(factory)
        {
            name = MethodBase.GetCurrentMethod().DeclaringType.ToString().Replace(string.Concat( GetType().Namespace, "."),"");
        }

        public override void LocalProcess(string workId, Dictionary<string, string> requestInstructions, List<GenericProcessor> chain, List<DocumentInfo> documentInfoList, List<ProcessorResults> results)
        {
            List<DocumentInfo> documentsToProcess = new List<DocumentInfo>();
            List<String> documentPaths = new List<string>();

            int maxHeight = factory.Settings.DefaultMaxHeight;
            int maxWidth = factory.Settings.DefaultMaxWidth;
            log.Debug(string.Format("Max height is {0}; max width is {1}", maxHeight, maxWidth));

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
                if (stopRequested)
                {
                    log.Info("Aborting processing");
                    return;
                }

                string inputFile = documentPaths[i];
                string outputFileStart = factory.Utility.TempFileNameChange(inputFile, name);
                foreach (FileFormat fileFormat in targetFileFormats)
                {
                    if (FileFormats.GetFileFormatFamily(fileFormat) == FileFormatFamily.Image)
                    {
                        string extension = FileFormats.GetPreferredExtension(fileFormat);
                        string outputFile = string.Concat(outputFileStart, ".", extension);
                        switch (fileFormat)
                        {
                            case FileFormat.BMP:
                                ImageHelper.ResizeImage(inputFile, outputFile, ImageFormat.Png, new Size(maxWidth, maxHeight));
                                break;
                            case FileFormat.PNG:
                                ImageHelper.ResizeImage(inputFile, outputFile, ImageFormat.Png, new Size(maxWidth, maxHeight));
                                break;
                            case FileFormat.JPEG:
                                ImageHelper.ResizeImage(inputFile, outputFile, ImageFormat.Jpeg, new Size(maxWidth, maxHeight));
                                break;
                            case FileFormat.TIFF:
                                ImageHelper.ResizeImage(inputFile, outputFile, ImageFormat.Tiff, new Size(maxWidth, maxHeight));
                                break;
                            default:
                                throw new Exception("Image save type not supported or not implemented " + fileFormat);
                        }

                        ProcessorResults thisResult = new ProcessorResults(documentsToProcess[i], inputFile, name, outputFile, true, "image to correct sized " + extension);
                        results.Add(thisResult);
                    }
                }
            }
        }
    }
}
