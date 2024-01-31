using System;
using System.Collections.Generic;
using log4net;

namespace documentprocessor
{
    public class Settings
    {
        private readonly int defaultMaxHeight;
        private readonly int defaultMaxWidth;
        private readonly string defaultOutputDirectory;
        private readonly string defaultOutputFilename;
        private readonly string defaultOutputPagedFilename;
        private readonly DateTime lowPriorityStart;
        private readonly DateTime lowPriorityEnd;
        private readonly int timerInterval;
        private readonly string[] watchFolders;

        public readonly string StoredProcedureGetOrderedWorkList = "documentprocessingworklist_get";
        public readonly string StoredProcedureCreateItemResponse = "documentprocessingitemresponse_post";
        public readonly string StoredProcedureUpdateWorkStatus = "documentprocessingupdate_put";

        public string[] WatchFolders
        {
            get { return watchFolders; }
        } 

        public int TimerInterval
        {
            get { return timerInterval; }
        } 

        public DateTime LowPriorityEnd
        {
            get { return lowPriorityEnd; }
        } 

        public DateTime LowPriorityStart
        {
            get { return lowPriorityStart; }
        } 

        public string DefaultOutputPagedFilename
        {
            get { return defaultOutputPagedFilename; }
        } 

        public string DefaultOutputFilename
        {
            get { return defaultOutputFilename; }
        } 

        public string DefaultOutputDirectory
        {
            get { return defaultOutputDirectory; }
        } 

        public int DefaultMaxWidth
        {
            get { return defaultMaxWidth; }
        } 

        public int DefaultMaxHeight
        {
            get { return defaultMaxHeight; }
        }

        public Settings(Factory factory)
        {
            Dictionary<string, string> settingValues = factory.Model.GetSettings();
            if (settingValues.ContainsKey("defaultmaxheight")) {
                Int32.TryParse(settingValues["defaultmaxheight"], out defaultMaxHeight);
            }
            if (settingValues.ContainsKey("defaultmaxwidth"))
            {
                Int32.TryParse(settingValues["defaultmaxwidth"], out defaultMaxWidth);
            }
            if (settingValues.ContainsKey("defaultoutputdirectory"))
            {
                defaultOutputDirectory = settingValues["defaultoutputdirectory"];
            }
            if (settingValues.ContainsKey("defaultoutputfilename"))
            {
                defaultOutputFilename = settingValues["defaultoutputfilename"];
            }
            if (settingValues.ContainsKey("defaultoutputpagedfilename"))
            {
                defaultOutputPagedFilename = settingValues["defaultoutputpagedfilename"];
            }
            if (settingValues.ContainsKey("lowprioritystart"))
            {
                DateTime.TryParse(settingValues["lowprioritystart"], out lowPriorityStart);
                lowPriorityStart = factory.Utility.ToTimeOnly(lowPriorityStart);
            }
            if (settingValues.ContainsKey("lowpriorityend"))
            {
                DateTime.TryParse(settingValues["lowpriorityend"], out lowPriorityEnd);
                lowPriorityEnd = factory.Utility.ToTimeOnly(lowPriorityEnd);
            }
            if (settingValues.ContainsKey("timerinterval"))
            {
                Int32.TryParse(settingValues["timerinterval"], out timerInterval);
            }
            if (settingValues.ContainsKey("watchfolders"))
            {
                watchFolders = settingValues["watchfolders"].Split(';');
            }

        }
    }
}
