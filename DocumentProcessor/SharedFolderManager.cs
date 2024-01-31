using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using log4net;
using System.Reflection;

namespace documentprocessor
{
    public class SharedFolderManager
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        readonly Factory factory = null;

        Dictionary<string, SharedFolder> shares = null;

        public SharedFolderManager(Factory factory)
        {
            this.factory = factory;
        }

        public void Setup()
        {
            shares = this.factory.Model.GetSharedFolders();
            foreach (SharedFolder folder in shares.Values)
            {

                try
                {
                    NetworkConnection.Connect(folder);
                }
                catch (NetworkConnectionException e)
                {
                    log.Error(e);
                }
            }
        }
    }
}