using log4net;
using MySqlConnector.Logging;
using MySqlConnector;
using System;
using System.Data;
using System.Reflection;
using System.Text;

namespace documentprocessor
{
    public class DatabaseHelper
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        static DatabaseHelper()
        {
        }

        public static DataSet RetrieveDataSet(string connectionString, string query)
        {
            using MySqlConnection conn = new MySqlConnection(connectionString);
            conn.Open();

            using MySqlCommand comm = conn.CreateCommand();
            comm.CommandText = query;
            comm.CommandType = CommandType.Text;

            using MySqlDataAdapter adapter = new MySqlDataAdapter(comm);
            DataSet data = new DataSet();
            adapter.Fill(data);
            return data;
        }

        public static DataSet RetrieveDataSet(string connectionString, string storedProcedure, params MySqlParameter[] parameters)
        {
            using MySqlConnection conn = new MySqlConnection(connectionString);
            conn.Open();

            using MySqlCommand comm = conn.CreateCommand();
            comm.CommandText = storedProcedure;
            comm.CommandType = CommandType.StoredProcedure;
            if (parameters != null && parameters.Length > 0)
            {
                comm.Parameters.AddRange(parameters);
            }

            using MySqlDataAdapter adapter = new MySqlDataAdapter(comm);
            DataSet data = new DataSet();
            adapter.Fill(data);
            return data;
        }

        public static object ExecuteScalar(string connectionString, string storedProcedure, params MySqlParameter[] parameters)
        {
            using MySqlConnection conn = new MySqlConnection(connectionString);
            conn.Open();

            using MySqlCommand comm = conn.CreateCommand();
            comm.CommandText = storedProcedure;
            comm.CommandType = CommandType.StoredProcedure;
            if (parameters != null && parameters.Length > 0)
            {
                comm.Parameters.AddRange(parameters);
            }
            return comm.ExecuteScalar();
        }

        public static int ExecuteNonQuery(string connectionString, string storedProcedure, params MySqlParameter[] parameters)
        {
            using MySqlConnection conn = new MySqlConnection(connectionString);
            conn.Open();

            using MySqlCommand comm = conn.CreateCommand();
            comm.CommandText = storedProcedure;
            comm.CommandType = CommandType.StoredProcedure;
            if (parameters != null && parameters.Length > 0)
            {
                comm.Parameters.AddRange(parameters);
            }
            return comm.ExecuteNonQuery();
        }

        /// <summary>
        /// Executes the specified stored procedure and returns the results as a MySqlDataReader.
        /// Connection must be open.
        /// </summary>
        /// <param name="connection">Full MySqlConnector compatible Connection String</param>
        /// <param name="storedProcedure">Name only of the stored procedure</param>
        /// <param name="parameters">Parameters to call the stored procedure with</param>
        /// <returns>An open MySqlDataReader that must be closed once caller is finished with it</returns>
        public static MySqlDataReader ExecuteReader(MySqlConnection connection, string storedProcedure, params MySqlParameter[] parameters)
        {
            MySqlCommand comm = connection.CreateCommand();
            comm.CommandText = storedProcedure;
            comm.CommandType = CommandType.StoredProcedure;
            if (parameters != null && parameters.Length > 0)
            {
                comm.Parameters.AddRange(parameters);
            }
            return comm.ExecuteReader();
        }

        public static string GetStringFromReader(string column, MySqlDataReader reader)
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

        public static int GetIntValue(object column)
        {
            if (column != DBNull.Value)
                return Convert.ToInt32(column);
            else
                return 0;
        }

        public static int? GetNullableIntValue(object column)
        {
            if (column != DBNull.Value)
                return Convert.ToInt32(column);
            else
                return null;
        }

        public static DateTime? GetNullableDateTimeValue(object column)
        {
            if (column != DBNull.Value)
                return Convert.ToDateTime(column);
            else
                return null;
        }

        public static Boolean? GetNullableBooleanValue(object column)
        {
            if (column != DBNull.Value)
            {
                return (Convert.ToBoolean(column));
            }
            else
                return null;
        }

        public static Guid? GetNullableGUIDValue(object column)
        {
            if (column != DBNull.Value)
            {
                return (new Guid(column.ToString()));
            }
            else
                return null;
        }

        public static Decimal? GetNullableDecimalValue(object column)
        {
            if (column != DBNull.Value)
            {
                return (Convert.ToDecimal(column));
            }
            else
                return null;
        }
    }
}
