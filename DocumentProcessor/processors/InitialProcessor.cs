using System;
using System.Collections.Generic;
using log4net;
using System.Reflection;
using System.IO;

namespace documentprocessor
{
    public class InitialProcessor:GenericProcessor
    {    
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public InitialProcessor(Factory factory) : base(factory)
        {
            name = MethodBase.GetCurrentMethod().DeclaringType.ToString().Replace(string.Concat( GetType().Namespace, "."),"");;
        }

        public override void LocalProcess(string workId, Dictionary<string, string> requestInstructions, List<GenericProcessor> chain, List<DocumentInfo> documentInfoList, List<ProcessorResults> results)
        {
            switch (processType)
            {
                case ProcessType.Convert:
                    chain.Insert(1, factory.ConverterProcessor);
                    break;
                case ProcessType.Index:
                    log.Debug("index action not yet supported");
                    break;
                default:
                    throw new Exception("Invalid request: unrecognised action in request");
            }
            
            if (targetFileFormats.Contains(FileFormat.Unrecognised))
            {
                throw new Exception("Unrecognised format in " + requestInstructions["format"]);
            }

            if (documentInfoList == null || documentInfoList.Count == 0)
            {
                throw new Exception("No documents to work on");
            }

            foreach (DocumentInfo documentInfo in documentInfoList)
            {
                if (FileFormats.GetFileFormat(documentInfo.Extension) == FileFormat.Unrecognised)
                {
                    throw new Exception(string.Concat("Unrecognised source format for extension: ", documentInfo.Extension));
                }
            }            

            // copy files to work on - including 1:1 alternate formats
            foreach (DocumentInfo documentInfo in documentInfoList)
            {
                if (stopRequested)
                {
                    log.Info("Aborting processing");
                    return;
                }

                FileInfo sourceFileInfo = new FileInfo(documentInfo.Path);
                string target = string.Concat(factory.Utility.TempFileName(name), sourceFileInfo.Extension);
                sourceFileInfo.CopyTo(target, true);

                results.Add(new ProcessorResults(documentInfo, sourceFileInfo.FullName, name, target, true));
            }
        }
    }
}
