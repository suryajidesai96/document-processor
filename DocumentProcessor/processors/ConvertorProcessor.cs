using System;
using System.Collections.Generic;
using System.Reflection;
using log4net;

namespace documentprocessor
{
    public class ConvertorProcessor:GenericProcessor
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public ConvertorProcessor(Factory factory) : base(factory)
        {
            name = MethodBase.GetCurrentMethod().DeclaringType.ToString().Replace(string.Concat( GetType().Namespace, "."),"");;
        }

        public override void LocalProcess(string workId, Dictionary<string,string> requestInstructions, List<GenericProcessor> chain, List<DocumentInfo> documentInfoList, List<ProcessorResults> results) 
        {
            if (targetFileFormats.Count == 0)
                throw new Exception("No format given for convert work item");

            bool wordToPdf = false;
            bool imageToPdf = false;
            bool pdfToImage = false;
            bool imageProcessing = false;

            // conversion overwrites previous conversions so just look at source types to construct the right chain
            foreach (DocumentInfo documentInfo in documentInfoList)
            {
                string sourceExtension = documentInfo.Extension;
                FileFormat sourceFormat = FileFormats.GetFileFormat(sourceExtension);
                if (sourceFormat == FileFormat.Unrecognised)
                    throw new Exception("Unrecognised format for source extension " + sourceExtension);

                //List<FileFormat> targetFileFormats = fileFormats.GetFileFormats(requestInstructions["format"]);
                foreach (FileFormat targetFileFormat in targetFileFormats)
                {
                    if (stopRequested)
                    {
                        log.Info("Aborting processing");
                        return;
                    }

                    if (targetFileFormat == FileFormat.Unrecognised)
                        throw new Exception("Unrecognised format for target extension in " + requestInstructions["format"]);

                    if (sourceFormat != targetFileFormat || FileFormats.GetFileFormatFamily(sourceFormat) == FileFormatFamily.Image) // process image sources always because of resizing
                    {
                        FileFormatFamily targetFileFormatFamily = FileFormats.GetFileFormatFamily(targetFileFormat);
                        FileFormatFamily sourceFileFormatFamily = FileFormats.GetFileFormatFamily(sourceFormat);
                        switch (sourceFileFormatFamily)
                        {
                            case FileFormatFamily.Word:
                                if (targetFileFormatFamily == FileFormatFamily.PDF)
                                {
                                    wordToPdf = true;
                                }
                                else if (targetFileFormatFamily == FileFormatFamily.Image)
                                {
                                    wordToPdf = true;
                                    pdfToImage = true;
                                    imageProcessing = true;
                                }
                                else
                                {
                                    throw new Exception(string.Concat("Unsupported conversion from Word to ", targetFileFormat.ToString()));
                                }
                                break;
                            case FileFormatFamily.PDF:
                                if (targetFileFormatFamily == FileFormatFamily.Image)
                                {
                                    pdfToImage = true;
                                    imageProcessing = true;
                                }
                                else
                                {
                                    throw new Exception(string.Concat("Unsupported conversion from PDF to ", targetFileFormat.ToString()));
                                }
                                break;
                            case FileFormatFamily.Image:
                                if (targetFileFormatFamily == FileFormatFamily.Image)
                                {
                                    imageProcessing = true;
                                }
                                else if (targetFileFormatFamily == FileFormatFamily.PDF)
                                {
                                    imageToPdf = true;
                                }
                                else
                                {
                                    throw new Exception(string.Concat("Unsupported conversion from image to ", targetFileFormat.ToString()));
                                }
                                break;
                            default :
                                throw new Exception(string.Concat("Unsupported conversion from ", sourceFileFormatFamily.ToString(), " to ", targetFileFormat.ToString()));
                        }
                    }
                }
            }

            int index = chain.IndexOf(this);

            if (wordToPdf)
            {
                index++;
                chain.Insert(index, factory.WordToPdfProcessor);
            }

            if (imageToPdf)
            {
                index++;
                chain.Insert(index, factory.ImageToPdfProcessor);
            }

            if (pdfToImage)
            {
                index++;
                chain.Insert(index, factory.PdfToImageProcessor);
            }

            if (imageProcessing)
            {
                index++;
                chain.Insert(index, factory.ImageToImageProcessor);
            }
        }
    }
}
