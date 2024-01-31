using System;
using System.Collections.Generic;
using System.Reflection;
using log4net;
using System.Text.RegularExpressions;
using System.IO;

namespace documentprocessor
{
    public class FinalProcessor:GenericProcessor
    {    
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public FinalProcessor(Factory factory) : base(factory)
        {
            name = MethodBase.GetCurrentMethod().DeclaringType.ToString().Replace(string.Concat( GetType().Namespace, "."),"");;
        }

        public override void LocalProcess(string workId, Dictionary<string, string> requestInstructions, List<GenericProcessor> chain, List<DocumentInfo> documentInfoList, List<ProcessorResults> results)
        {
            string filename;
            if (stopRequested)
            {
                log.Info("Aborting processing");
                return;
            }

            switch (processType) {
                case ProcessType.Convert :
                    foreach (DocumentInfo documentInfo in documentInfoList)
                    {
                        List<string> outputFiles = new List<string>();
                        List<ProcessorResults> resultsForThisDoc = new List<ProcessorResults>();
                        foreach (ProcessorResults result in results)
                        {
                            if (result.DocumentInfo == documentInfo)
                            {
                                resultsForThisDoc.Add(result);
                            }
                        }

                        foreach (ProcessorResults relevantResult in resultsForThisDoc)
                        {
                            if (targetFileFormats.Contains(FileFormats.GetFileFormat(relevantResult.PathOutputExtension))) {
                                if (FileFormats.GetFileFormatFamily(relevantResult.PathOutputExtension) == FileFormatFamily.Image)
                                {
                                    // only accept output from image finisher 
                                    if (relevantResult.Processor == "ImageToImageProcessor")
                                    {
                                        outputFiles.Add(relevantResult.PathOutput);
                                    }
                                }
                                else
                                {
                                    outputFiles.Add(relevantResult.PathOutput);
                                }
                            }
                        }

                        foreach (string outputFile in outputFiles)
                        {
                            if (stopRequested)
                            {
                                log.Info("Aborting processing");
                                return;
                            }

                            Regex pagedFileRegex = new Regex(string.Concat(".*", factory.Config.PageSuffix, "([0-9]*)[.][^.]*"));
                            Match match = pagedFileRegex.Match(outputFile);
                            string page = "";
                            if (match.Success)
                            {
                                page = match.Groups[1].Value;
                            }

                            string targetPath = null;
                            string targetFileName;
                            string dir = Path.GetDirectoryName(documentInfo.Path);  // TODO support defaultoutputdirectory and overrides in requests?
                            filename = Path.GetFileNameWithoutExtension(documentInfo.Path);
                            string extension = Path.GetExtension(outputFile);

                            if (match.Success)
                            {
                                targetFileName = factory.Settings.DefaultOutputPagedFilename;
                            }
                            else
                            {
                                targetFileName = factory.Settings.DefaultOutputFilename;
                            }
                            targetFileName = targetFileName.Replace("{filename}", filename).Replace(".{extension}", extension).Replace("{page}", page);
                            targetPath = Path.Combine(dir, targetFileName);

                            File.Copy(outputFile, targetPath, true);
                            int? pageInt = string.IsNullOrEmpty(page) ? null : (int?)Int32.Parse(page);
                            results.Add(new ProcessorResults(documentInfo, outputFile, name, targetPath, true, null, pageInt));
                        }
                    }
                    break;
                
            }

            // delete temp files
            if (!factory.Config.TestMode) 
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(factory.Config.TempDirectory);
                FileInfo[] tempFiles = directoryInfo.GetFiles();
                foreach (FileInfo fileInfo in tempFiles)
                {
                    try
                    {
                        fileInfo.Delete();
                    }
                    catch (Exception)
                    {
                        log.Warn("Failed to delete temporary file " + fileInfo.FullName);
                    }
                }
            }
        }
    }
}
