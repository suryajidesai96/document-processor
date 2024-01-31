using System.Configuration;
using System;
using log4net;

namespace documentprocessor
{
    public class Config : IDBConfig
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public readonly string mySQLVersionSP = "mysql_version";
		public readonly string storedProcedureStoredProcedure5657Name = "plusstoredprocedures_get";
		public readonly string storedProcedureStoredProcedure80Name = "plusstoredprocedures80_get";
        public readonly string DocSettingsStoredProcedureName = "documentprocessingsettings_get";
        public readonly string SettingsStoredProcedureName = "settings_get";

        public string ConnectionStringName
        {
            get { return ConfigurationManager.AppSettings["ConnectionStringName"].ToString(); }
        }

        public string ConnectionString
        {
            get { return ConfigurationManager.ConnectionStrings[ConnectionStringName].ConnectionString; }
        }

        public string SystemEntityId
        {
            get { return ConfigurationManager.AppSettings["EntityId"].ToString(); }
        }

        public string GemboxPdfLicence
        {
            get { return ConfigurationManager.AppSettings["GemboxPdfLicence"].ToString(); }
        }

        public string GemboxDocumentLicence
        {
            get { return ConfigurationManager.AppSettings["GemboxDocumentLicence"].ToString(); }
        }

        public string TempDirectory
        {
            get { return ConfigurationManager.AppSettings["TempDirectory"].ToString(); }
        }

        public string PageSuffix
        {
            get { return "-page-"; }
        }

        public string[] TestDocuments
        {
            get
            { 
                try
                {
                    string args = ConfigurationManager.AppSettings["TestDocuments"].ToString();
                    return args.Split(' ');
                }
                catch
                {
                    return null;
                }
            }
        }

        public string TestRequest
        {
            get
            {
                string result = null;
                try
                {
                    result = ConfigurationManager.AppSettings["TestRequest"].ToString();
                }
                catch { }
                return result;
            }
        }

        public bool TestMode
        {
            get
            {
                bool result = false;
                try
                {
                    result = ConfigurationManager.AppSettings["TestMode"].ToString().ToLowerInvariant() == "true";
                }
                catch { }
                return result;
            }
        }

        public bool WordVisible
        {
            get
            {
                bool result = false;
                try
                {
                    result = ConfigurationManager.AppSettings["WordVisible"]?.ToString().ToLowerInvariant() == "true";
                }
                catch { }
                return result;
            }
        }

        public TimeSpan ProcessStartTime
        {
            get
            {
                TimeSpan startTime;
                if (!TimeSpan.TryParse(ConfigurationManager.AppSettings["ProcessStartTime"], out startTime))
                {
                    startTime = new TimeSpan(0, 0, 0);
                }
                return startTime;
            }
        }

        public TimeSpan ProcessEndTime
        {
            get
            {
                TimeSpan endTime;
                if (!TimeSpan.TryParse(ConfigurationManager.AppSettings["ProcessEndTime"], out endTime))
                {
                    endTime = new TimeSpan(23, 59, 59);
                }
                return endTime;
            }
        }

        public string WorklistEvenFilter
        {
            get { 
                // 1 or even = even records only, 0 or odd = odd records only, anything else all records
                string result = "";
                try
                {
                    result = ConfigurationManager.AppSettings["WorkListEvenFilter"].ToString();
                    result = (result.ToLowerInvariant()) switch
                    {
                        "even" or "1" => "1",
                        "odd" or "0" => "0",
                        _ => "",
                    };
                }
                catch (Exception e)
                {
                    log.WarnFormat("Exception reading config value WorkListEvenFilter: {0}", e.Message);
                }
                return result;
            }
        }
    }
}
