using System;
using MySqlConnector;

namespace documentprocessor
{
    public interface IDatabase
    {
        int ExecuteNonQuery(string connectionString, string storedProcedure, params MySqlParameter[] parameters);
        object ExecuteScalar(string connectionString, string storedProcedure, params MySqlParameter[] parameters);
        int GetIntValue(object column);
        bool? GetNullableBooleanValue(object column);
        DateTime? GetNullableDateTimeValue(object column);
        decimal? GetNullableDecimalValue(object column);
        Guid? GetNullableGUIDValue(object column);
        int? GetNullableIntValue(object column);
        string GetStringFromReader(string column, MySqlDataReader reader);
        System.Data.DataSet RetrieveDataSet(string connectionString, string storedProcedure);
        System.Data.DataSet RetrieveDataSet(string connectionString, string storedProcedure, params MySqlParameter[] parameters);
    }
}
