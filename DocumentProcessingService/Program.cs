using System.ServiceProcess;

namespace documentprocessor
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] 
			{ 
				new DocumentProcessingService() 
			};
            ServiceBase.Run(ServicesToRun);
        }
    }
}
