using System;
using System.Collections.Generic;
using log4net;
using System.Reflection;

namespace documentprocessor
{
    public enum ProcessType {
        Unrecognised,
        Convert,
        Index,
    }

    public class GenericProcessor
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        protected bool stopRequested = false;
        protected string name = "GenericProcessor";
        protected List<GenericProcessor> chain;
        protected Factory factory;
        protected ProcessType processType;
        protected List<FileFormat> targetFileFormats = new List<FileFormat>();

        public GenericProcessor(Factory factory)
        {
            this.factory = factory;
        }

        public void Stop()
        {
            if (!stopRequested)
            {
                stopRequested = true;
                log.Info("Stop requested in " + name);
                if (chain != null && chain.Count > 0)
                {
                    foreach (GenericProcessor processor in chain)
                    {
                        if (processor != this)
                        {
                            processor.Stop();
                        }                            
                    }
                }
            }
        }

        public void Process(string workId, Dictionary<string,string> requestInstructions, List<GenericProcessor> chain, List<DocumentInfo> documentInfoList, List<ProcessorResults> results)
        {
            this.chain = chain;
            ParseInstructions(requestInstructions);

            if (!stopRequested) 
            {
                log.Debug(string.Format("Processing work id {0}, processor {1}", workId, name));
                LocalProcess(workId, requestInstructions, chain, documentInfoList, results);
            }

            if (!stopRequested && chain != null && chain.Count > 1)
            {
                this.chain = new List<GenericProcessor>(chain);
                this.chain.Remove(this);
                this.chain[0].Process(workId, requestInstructions, this.chain, documentInfoList, results);
            }

            if (stopRequested)
            {
                log.Info(string.Format("Stop requested in work id {0}, processor {1}", workId, name));
            }
        }

        public virtual void LocalProcess(string workId, Dictionary<string,string> requestInstructions, List<GenericProcessor> chain, List<DocumentInfo> documentInfoList, List<ProcessorResults> results) {

        }

        protected virtual void ParseInstructions(Dictionary<string,string> requestInstructions) {
            if (!requestInstructions.ContainsKey("action"))
            {
                throw new Exception("No action specified in request");
            }

            processType = (requestInstructions["action"]) switch
            {
                "convert" => ProcessType.Convert,
                "index" => ProcessType.Index,
                _ => ProcessType.Unrecognised,
            };
            if (requestInstructions.ContainsKey("format")) {
                targetFileFormats = FileFormats.GetFileFormats(requestInstructions["format"]);
            }
        }
    }
}
