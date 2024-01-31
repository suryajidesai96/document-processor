using System;
using System.Collections.Generic;
using log4net;
using System.Reflection;
using System.Threading;
using System.IO;


namespace documentprocessor
{
    public class DPMain
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private volatile bool stopping = false;
        private volatile bool processing = false;
        private bool processingError = false;
        private System.Timers.Timer mainTimer;
        private System.Timers.Timer flagCheckTimer;
        private ManualResetEvent signalEvent;
        private Factory factory;
        private List<FileSystemWatcher> watchers;
        private volatile bool checkIsDue = false;

        public void Start()
        {
            signalEvent = new ManualResetEvent(false);
            Thread processingThread = new Thread(new ThreadStart(Go));
            processingThread.Start();
            signalEvent.WaitOne();
            signalEvent.Reset();
        }

        private void Go()
        {
            try
            {
                factory = new Factory();
                factory.CreateTempDirIfMissing();

                if (factory.Settings.WatchFolders != null)
                {
                    watchers = new List<FileSystemWatcher>();
                    foreach (string folder in factory.Settings.WatchFolders)
                    {
                        if (Directory.Exists(folder))
                        {
                            FileSystemWatcher watcher = new FileSystemWatcher
                            {
                                Path = folder,
                                EnableRaisingEvents = true,
                                IncludeSubdirectories = true
                            };
                            watcher.Created += new FileSystemEventHandler(Watcher_Created);
                            watchers.Add(watcher);
                        }
                    }
                }
                
                mainTimer = new System.Timers.Timer();
                mainTimer.Elapsed += new System.Timers.ElapsedEventHandler(MainTimer_Elapsed);
                if (factory.Settings.TimerInterval > 0)
                {
                    mainTimer.Interval = factory.Settings.TimerInterval * 1000;
                    mainTimer.Enabled = true;
                }

                flagCheckTimer = new System.Timers.Timer
                {
                    Interval = 500
                };
                flagCheckTimer.Elapsed += new System.Timers.ElapsedEventHandler(FlagCheckTimer_Elapsed);
                flagCheckTimer.Enabled = true;

                checkIsDue = true;

            }
            catch (Exception e)
            {
                log.Error("Go failed", e);
                processing = false;
                stopping = true;
                CheckStop();
            }
        }

        void FlagCheckTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (checkIsDue && !processing && !stopping)
                {
                    DateTime fromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day).Add(factory.Config.ProcessStartTime);
                    DateTime toDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day).Add(factory.Config.ProcessEndTime);

                    if (fromDate < DateTime.Now && toDate > DateTime.Now)
                    {
                        Process();
                    }
                }
            } 
            catch(Exception ex)
            {
                log.Error("Exception in flagCheckTimer.", ex);
            }
        }

        void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            checkIsDue = true;
        }

        void MainTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            checkIsDue = true;
        }

        public void Stop()
        {
            stopping = true;
            try
            {
                factory.Processor.Stop();
            }
            catch (Exception) { }
            CheckStop();
        }

        public void Kill()
        {
            if (signalEvent != null)
                signalEvent.Set();
        }

        private void CheckStop()
        {
            if (stopping)
            {
                if (mainTimer != null)
                    mainTimer.Enabled = false;
                if (!processing && signalEvent != null)
                    signalEvent.Set();

            }
        }

        private void Process()
        {
            try
            {
                if (checkIsDue && !processing && !stopping)
                {
                    checkIsDue = false;
                    processing = true;
                    processingError = false;
                    Processor processor = factory.Processor;
                    processor.CheckForWork();
                    
                    while (processor.HasWork && !stopping)
                    {
                        bool success = processor.ProcessNextWorkItem();
                        processingError = processingError || !success;
                        factory.ReTool();

                        if (factory.Settings.TimerInterval == 0 && mainTimer.Enabled)
                        {
                            mainTimer.Enabled = false;
                        }
                        else if (factory.Settings.TimerInterval * 1000 != mainTimer.Interval)
                        {
                            mainTimer.Interval = factory.Settings.TimerInterval * 1000;
                            mainTimer.Enabled = true;
                        }
                        if (factory.Config.TestMode)
                        {
                            break;
                        }
                    }

                    processing = false;
                }
                else
                {
                    if (stopping)
                    {
                        log.DebugFormat("Do nothing because stopping, standing down.");
                        factory.StandDown();
                    }
                }
                CheckStop();

                // should go over errors again
                if (!stopping && processingError)
                {
                    log.Debug("There were some errors so check is due immediately");
                    checkIsDue = true;
                }
            }
            catch (Exception e)
            {
                log.Error("Exception during document processing", e);
                factory.StandDown();
                processing = false;
            }
        }
    }
}
