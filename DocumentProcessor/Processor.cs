using System;
using System.Collections.Generic;
using log4net;
using System.Reflection;
using System.Data;
using System.IO;

namespace documentprocessor
{
    public class Processor
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly Factory factory;
        private readonly Model model;
        private bool stopRequested = false;
        private DataTable workItems = null;
        private int nextItem;
        private List<GenericProcessor> chain;

        public Processor(Factory factory)
        {
            log.Debug("Creating processor");
            this.factory = factory;
            this.model = factory.Model;
        }

        public void CheckForWork()
        {
            if (!factory.Config.TestMode)
            {
                int includeLowPriority = factory.Utility.TimeBetween(factory.Settings.LowPriorityStart, factory.Settings.LowPriorityEnd) ? 1 : 0;
                Dictionary<string, string> paramValues = new Dictionary<string, string>(1)
                {
                    { "includelowpriority", includeLowPriority.ToString() },
                    { "even", factory.Config.WorklistEvenFilter }
                };
                workItems = model.GetData(factory.Settings.StoredProcedureGetOrderedWorkList, paramValues).Tables[0];
            }
            nextItem = 0;
        }

        public bool HasWork
        {
            get
            {
                bool result;
                if (factory.Config.TestMode)
                {
                    log.Debug("Test mode - hasWork always true");
                    result = true;
                }
                else
                {
                    result = workItems.Rows.Count >= 1 && workItems.Rows.Count >= nextItem + 1;
                    if (result)
                    {
                        log.Debug(workItems.Rows.Count.ToString() + " work item/subitem rows found");
                    }
                }
                return result;
            }
        }

        public bool ProcessNextWorkItem()
        {
            bool success = true;
            if (HasWork)
            {
                success = false;
                string id = null;
                chain = new List<GenericProcessor>();
                List<DocumentInfo> documentInfoList = new List<DocumentInfo>();
                List<ProcessorResults> results = new List<ProcessorResults>();
                Dictionary<string, string> requestInstructions = new Dictionary<string, string>();
                string request;
                string response = string.Empty;
                try
                {
                    int offset = 0;

                    if (factory.Config.TestMode)
                    {
                        log.Warn("TEST MODE");
                        id = "test mode";
                        request = factory.Config.TestRequest;
                        string[] testDocs = factory.Config.TestDocuments;
                        int counter = 1;
                        foreach (string testDoc in testDocs)
                        {
                            DocumentInfo documentInfo = new DocumentInfo("test-" + counter,
                                    "testdocumentid-" + counter, testDoc, Path.GetExtension(testDoc).Replace(".", ""));
                            documentInfoList.Add(documentInfo);
                        }
                    }
                    else
                    {
                        DataRow currentRequestRow = workItems.Rows[nextItem];
                        id = model.ByteArrayToHexString(currentRequestRow["id"]);
                        request = currentRequestRow["request"].ToString();

                        while (nextItem + offset < workItems.Rows.Count &&
                           model.ByteArrayToHexString(workItems.Rows[nextItem + offset]["id"]) == id)
                        {
                            DataRow currentDataRow = workItems.Rows[nextItem + offset];
                            DocumentInfo documentInfo = new DocumentInfo(
                                model.ByteArrayToHexString(currentDataRow["dpitemid"]),
                                model.ByteArrayToHexString(currentDataRow["documentid"]), 
                                currentDataRow["path"].ToString(), 
                                currentDataRow["extension"].ToString());
                            documentInfoList.Add(documentInfo);
                            offset++;
                        }
                        nextItem += offset;
                    }
                    log.Info(string.Concat("Request is: ", request,
                        "; document count is: ", documentInfoList.Count, "; first document is: ",
                        (documentInfoList.Count > 0 ? documentInfoList[0].Path : "not specified")));

                    string[] requestDetails = request.Split(';');

                    foreach (string requestDetail in requestDetails)
                    {
                        string[] nameValue = requestDetail.Split('=');
                        if (nameValue.Length == 2)
                        {
                            requestInstructions.Add(nameValue[0], nameValue[1]);
                        }
                    }

                    if (!stopRequested)
                    {
                        if (requestInstructions.ContainsKey("action"))
                        {
                            chain.Add(factory.InitialProcessor);
                            chain.Add(factory.FinalProcessor);
                            chain[0].Process(id, requestInstructions, chain, documentInfoList, results);
                            if (!stopRequested)
                            {
                                success = true;
                            }                                
                        }
                        else
                        {
                            throw new Exception("Invalid request: action not specified in " + request);
                        }
                    }

                }
                catch (Exception e)
                {
                    log.Error($"Exception processing work item {id}", e);
                }

                if (success)
                {
                    foreach (ProcessorResults result in results)
                    {
                        log.Debug(string.Concat(result.DocumentInfo.Path, " -> ", result.Processor, " -> ", result.PathOutput));
                        if (!result.Success)
                        {
                            response += "Incompletion in " + result.Processor;
                            success = false;
                        }
                    }
                }

                if (success)
                {
                    log.Debug(string.Format("Success processing work item {0}", id));
                }
                else if (!stopRequested)
                {
                    log.Debug(string.Format("Failure processing work item {0}, work details below", id));
                    foreach (ProcessorResults result in results)
                    {
                        log.Debug(string.Concat(result.DocumentInfo.Path, " -> ", result.Processor, " -> ", result.PathOutput));
                    }
                }

                if (!factory.Config.TestMode)
                {
                    if (success)
                    {
                        int counter = 1;
                        foreach (ProcessorResults result in results)
                        {
                            if (result.Processor == "FinalProcessor")
                            {
                                // write documentprocessingitemresponse
                                Dictionary<string, string> paramValues = new Dictionary<string, string>(6)
                                {
                                    { "dpiid", result.DocumentInfo.DpItemId },
                                    { "sequence", counter.ToString() },
                                    { "page", result.Page.HasValue ? result.Page.Value.ToString() : null },
                                    { "filename", String.IsNullOrEmpty(result.PathOutput) ? null : Path.GetFileName(result.PathOutput) },
                                    { "path", result.PathOutput },
                                    { "extension", result.PathOutputExtension }
                                };
                                model.Execute(factory.Settings.StoredProcedureCreateItemResponse, paramValues);
                                counter++;
                            }
                        }
                        response += string.Concat((counter - 1).ToString(), " output items");
                    }
                    else
                    {
                        response += " failed";
                    }

                    if (!stopRequested)
                    {
                        Dictionary<string, string> mainParamValues = new Dictionary<string, string>(3)
                        {
                            { "id", id },
                            { "success", success ? "1" : "0" },
                            { "response", response }
                        };
                        model.Execute(factory.Settings.StoredProcedureUpdateWorkStatus, mainParamValues);
                    }
                }
            }
            return success;
        }

        public void Stop()
        {
            log.Debug("Stop requested");
            stopRequested = true;
            if (chain != null)
            {
                foreach (GenericProcessor processor in chain)
                {
                    processor.Stop();
                }
            }
        }
    }
}
