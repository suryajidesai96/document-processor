using System;
using log4net;
using System.Reflection;
using System.Threading;

namespace documentprocessor
{
    class Program
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static DPMain documentProcessor;
        private static Thread workerThread;

        static void Main(string[] args)
        {
            log.Info("Command starting *************************************************");
            Console.CancelKeyPress += delegate(object sender, ConsoleCancelEventArgs e)
            {
                e.Cancel = true;
                Stop();
            }; 

            documentProcessor = new DPMain();
            workerThread = new Thread(new ThreadStart(documentProcessor.Start));
            workerThread.Start();
            log.Info("Command started");
            workerThread.Join();

            Console.ReadLine();
            log.Info("Command worker thread ended");
        }

        static void Stop()
        {
            const int MAX_STOP_WAIT = 20;
            const int MAX_KILL_WAIT = 5;
            const int MAX_THREADABORT_WAIT = 5;

            log.Info("Command stopping");
            documentProcessor.Stop();
            int counter = 0;
            while (workerThread.ThreadState == System.Threading.ThreadState.WaitSleepJoin  && counter < MAX_STOP_WAIT)
            {
                log.Info("Waiting for worker thread to stop");
                Thread.Sleep(1000);
                counter++;
            }

            counter = 0;
            while (workerThread.ThreadState == System.Threading.ThreadState.WaitSleepJoin && counter < MAX_KILL_WAIT)
            {
                if (counter == 0)
                {
                    documentProcessor.Kill();
                }
                log.Info("Waiting for worker thread to die");
                Thread.Sleep(1000);
                counter++;
            }
            if (workerThread.ThreadState == System.Threading.ThreadState.WaitSleepJoin)
            {
                log.Info("Aborting worker thread");
                workerThread.Abort();
            }
            counter = 0;
            while (workerThread.ThreadState != System.Threading.ThreadState.Aborted && workerThread.ThreadState != System.Threading.ThreadState.Stopped && counter < MAX_THREADABORT_WAIT)
            {
                log.Info("Waiting for worker thread to abort");
                Thread.Sleep(1000);
                counter++;
            }
            log.Info("Command stopped *************************************************");
        }
    }
}
