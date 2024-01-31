using System.ServiceProcess;
using log4net;
using System.Reflection;
using System.Threading;

namespace documentprocessor
{
    public partial class DocumentProcessingService : ServiceBase
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private DPMain documentProcessor;
        private Thread workerThread;

        public DocumentProcessingService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            log.Info("Service starting *************************************************");
            documentProcessor = new DPMain();
            workerThread = new Thread(new ThreadStart(documentProcessor.Start));
            workerThread.Start();
            log.Info("Service started");
        }

        protected override void OnStop()
        {
            log.Info("Service stopping");
            documentProcessor.Stop();
            workerThread.Join(22000);

            if (workerThread.ThreadState == System.Threading.ThreadState.Running || workerThread.ThreadState == System.Threading.ThreadState.WaitSleepJoin)
            {
                log.Warn("Killing document processor");
                documentProcessor.Kill();
                workerThread.Join(3000);
            }
            
            if (workerThread.ThreadState == System.Threading.ThreadState.Running || workerThread.ThreadState == System.Threading.ThreadState.WaitSleepJoin)
            {
                log.Warn("Aborting worker thread");
                workerThread.Abort();
                workerThread.Join(3000);
            }
            
            if (workerThread.ThreadState == System.Threading.ThreadState.Aborted || workerThread.ThreadState == System.Threading.ThreadState.Stopped)
            {
                log.Info("Worker thread has stopped");
            }
            else
            {
                log.Error("Dirty stop or failed to stop");
            }
            log.Info("Service stopped *************************************************");
        }

        protected override void OnShutdown()
        {
            log.Info("System shutdown");
        }
    }
}
