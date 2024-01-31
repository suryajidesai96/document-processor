using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Diagnostics;
using MySqlConnector;
using log4net;
using System.Reflection;

namespace documentprocessor
{    
    public class Database : IDatabase
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Config config;

        public Database(Config config)
        {
            this.config = config;
        }

        public DataSet RetrieveDataSet(string connectionString, string storedProcedure)
        {
            return RetrieveDataSet(connectionString, storedProcedure, null);
        }

        public DataSet RetrieveDataSet(string connectionString, string storedProcedure, params MySqlParameter[] parameters)
        {
            return DatabaseHelper.RetrieveDataSet(connectionString, storedProcedure, parameters);
        }

        public object ExecuteScalar(string connectionString, string storedProcedure, params MySqlParameter[] parameters)
        {
            return DatabaseHelper.ExecuteScalar(connectionString, storedProcedure, parameters);
        }

        public int ExecuteNonQuery(string connectionString, string storedProcedure, params MySqlParameter[] parameters)
        {
            return DatabaseHelper.ExecuteNonQuery(connectionString, storedProcedure, parameters);
        }

        public string GetStringFromReader(string column, MySqlDataReader reader)
        {
            string value = null;

            int index = reader.GetOrdinal(column);

            if (!reader.IsDBNull(index))
            {
                value = reader.GetValue(index).ToString().Trim();
                if (string.IsNullOrEmpty(value))
                {
                    value = null;
                }
            }

            return value;
        }

        public int GetIntValue(object column)
        {
            if (column != DBNull.Value)
                return Convert.ToInt32(column);
            else
                return 0;
        }

        public int? GetNullableIntValue(object column)
        {
            if (column != DBNull.Value)
                return Convert.ToInt32(column);
            else
                return null;
        }

        public DateTime? GetNullableDateTimeValue(object column)
        {
            if (column != DBNull.Value)
                return Convert.ToDateTime(column);
            else
                return null;
        }

        public Boolean? GetNullableBooleanValue(object column)
        {
            if (column != DBNull.Value)
            {
                return (Convert.ToBoolean(column));
            }
            else
                return null;
        }

        public Guid? GetNullableGUIDValue(object column)
        {
            if (column != DBNull.Value)
            {
                return (new Guid(column.ToString()));
            }
            else
                return null;
        }

        public Decimal? GetNullableDecimalValue(object column)
        {
            if (column != DBNull.Value)
            {
                return (Convert.ToDecimal(column));
            }
            else
                return null;
        }

        public void Close()
        {

        }
    }
}
