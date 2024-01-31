using System;
using System.Reflection;
using GemBox.Document;
using System.Collections.Generic;
using log4net;
using System.IO;

namespace documentprocessor
{
    public class GemWordToPdfProcessor : GenericProcessor
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public GemWordToPdfProcessor(Factory factory) : base(factory)
        {
            name = MethodBase.GetCurrentMethod().DeclaringType.ToString().Replace(string.Concat( GetType().Namespace, "."),"");
        }

        public override void LocalProcess(string workId, Dictionary<string, string> requestInstructions, List<GenericProcessor> chain, List<DocumentInfo> documentInfoList, List<ProcessorResults> results)
        {
            foreach (DocumentInfo documentInfo in documentInfoList)
            {
                ConvertToPdf(documentInfo, ref results);
            }
        }

        private void ConvertToPdf(DocumentInfo documentInfo, ref List<ProcessorResults> results)
        {
            string filePath = null;

            foreach (ProcessorResults result in results)
            {
                if (result.DocumentInfo == documentInfo)
                {
                    // find Word version
                    FileFormatFamily fileFormatFamily = FileFormats.GetFileFormatFamily(result.PathOutputExtension);
                    if (fileFormatFamily == FileFormatFamily.Word)
                    {
                        filePath = result.PathOutput;
                    }
                }
            }

            // If we don't have a word version to use
            if (filePath == null) return;

            // We only support conversion to PDF
            List<FileFormatFamily> targetFileFormatFamilies = FileFormats.GetFileFormatFamilies(targetFileFormats);
            if (!targetFileFormatFamilies.Contains(FileFormatFamily.PDF)) return;

            if (stopRequested)
            {
                log.Info("Aborting processing");
                return;
            }

            DocumentModel document = DocumentModel.Load(filePath);
            string extension = FileFormats.GetPreferredExtension(FileFormat.PDF);
            string outputFile = string.Concat(factory.Utility.TempFileName(name), ".", extension);

            document.Save(outputFile);

            results.Add(new ProcessorResults(documentInfo, documentInfo.Path, name, outputFile, true, "whole doc"));
        }
    }
}
