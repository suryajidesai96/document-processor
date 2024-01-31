using System;
using System.Data;
using System.Collections.Generic;
using MySqlConnector;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using log4net;
using System.Reflection;
using System.Web;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace documentprocessor
{
    public struct Param
    {
        public string Name;
        public string Type;
        public int Length;
    }

    public class SP
    {
        public string Name;
        public Dictionary<string, Param> Params;
    }

    public class Model
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        readonly Factory factory;
        private Dictionary<string, SP> sps = null;
        private DataSet settings = null;


        public Model(Factory factory)
        {
            this.factory = factory;
            SetupStoredProcs();
        }

        private void SetupStoredProcs()
        {
            string versionNumber = (string)factory.Database.ExecuteScalar(factory.Config.ConnectionString, factory.Config.mySQLVersionSP, null);
            if (versionNumber.StartsWith("8."))
            {
                SetupStoredProcs_80();
                return;
            }
            SetupStoredProcs_56_57();
        }

        private void SetupStoredProcs_80()
        {
            sps = new Dictionary<string, SP>();
            DataSet ds = GetSystemData(factory.Config.storedProcedureStoredProcedure80Name, null);
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    string spName = row[0].ToString();
                    string paramName = row[1].ToString();
                    string dataType = row[2].ToString();

                    Param p = new()
                    {
                        Name = paramName,
                        Type = dataType.ToLowerInvariant()
                    };
                    if (!sps.ContainsKey(spName))
                    {
                        SP sp = new()
                        {
                            Name = spName,
                            Params = new Dictionary<string, Param>()
                        };
                        sps.Add(spName, sp);
                    }
                    Dictionary<string, Param> dictParams = sps[spName].Params;
                    dictParams[p.Name] = p;
                }
            }
        }

        private void SetupStoredProcs_56_57()
        {
            sps = new Dictionary<string, SP>();
            DataSet ds = GetSystemData(factory.Config.storedProcedureStoredProcedure5657Name, null);
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    Dictionary<string, Param> dictParams = new();
                    string spName = row[0].ToString();
                    DataColumn column = ds.Tables[0].Columns[1];
                    string spParamsData;
                    if (column.DataType == typeof(string))
                        spParamsData = row[1].ToString();
                    else
                        spParamsData = Encoding.UTF8.GetString((byte[])(row[1]));
                    string[] spParams = spParamsData.Split(',');
                    foreach (string spParam in spParams)
                    {
                        Regex regexParamParts = new(@"\W*(\w+)\W+(\w+)\W*(\d*)\W*");
                        Match match = regexParamParts.Match(spParam);
                        if (match.Success)
                        {
                            string spParamName = match.Groups[1].Value;
                            string spParamType = match.Groups[2].Value;
                            int spParamLength = 0;
                            if (match.Groups.Count > 3)
                                Int32.TryParse(match.Groups[3].Value, out spParamLength);
                            Param p = new()
                            {
                                Name = spParamName,
                                Type = spParamType.ToLowerInvariant()
                            };
                            dictParams[p.Name] = p;
                        }
                    }

                    SP sp = new()
                    {
                        Name = spName,
                        Params = dictParams
                    };
                    sps.Add(sp.Name, sp);
                }

            }
        }

        private SP GetSP(string name) 
        {
            if (sps.ContainsKey(name))
            {
                return sps[name];
            }

            return new SP
            {
                Name = name,
                Params = new()
            };
        }


        public Dictionary<string,string> GetSettings()
        {
            settings = GetSystemData(factory.Config.DocSettingsStoredProcedureName, null);
            DataRowCollection rows = settings.Tables[0].Rows;
            Dictionary<string,string> result = new Dictionary<string, string>();
            foreach (DataRow row in rows)
            {
                string name = row["name"].ToString();
                string value = row["value"].ToString();
                result[name] = value;
            }
            return result;
        }

        public Dictionary<string, SharedFolder> GetSharedFolders()
        {
            Dictionary<string, SharedFolder> folders = new Dictionary<string, SharedFolder>();
            DataSet shareSettings = GetSystemData(factory.Config.SettingsStoredProcedureName, new Dictionary<string, string> {
                { "xgroup", "share" }
            });
            DataRowCollection rows = shareSettings.Tables[0].Rows;
            foreach (DataRow row in rows)
            {
                string name = row["name"].ToString();
                string value = row["value"].ToString();
                if (!folders.ContainsKey(name))
                {
                    folders.Add(name, new SharedFolder(name));
                }

                switch (row["subgroup"].ToString())
                {
                    case "username":
                        folders[name].UserName = value;
                        break;
                    case "password":
                        folders[name].Password = value;
                        break;
                    case "uncpath":
                        folders[name].UncPath = value;
                        break;
                }
            }

            return folders;
        }

        public bool IsDecimal(Type datatype)
        {
            if (datatype == typeof(decimal) || datatype == typeof(Single) || datatype == typeof(double) || datatype == typeof(float) || datatype == typeof(decimal))
                return true;
            return false;
        }

        public bool IsString(Type datatype)
        {
            return (datatype == typeof(string));
        }

        protected MySqlParameter[] GetParams(string storedProc, Dictionary<string, string> passedParamValues, bool systemCall)
        {
            if (storedProc == factory.Config.storedProcedureStoredProcedure80Name || storedProc == factory.Config.storedProcedureStoredProcedure5657Name)
            {
                MySqlParameter[] spspparameters = new MySqlParameter[1];
                spspparameters[0] = new MySqlParameter("xuserid", MySqlDbType.Binary, 16)
                {
                    Value = HexStringToByteArray(factory.Config.SystemEntityId)
                };
                return spspparameters;
            }

            Dictionary<string, string> actualParamValues = new Dictionary<string, string>();
            SP sp = null;

            // todo log param mismatches as warnings or raise errors??
            sp = GetSP(storedProc);
            if (sp == null)
                throw new ArgumentException(string.Format("Stored procedure {0} does not exist", storedProc));

            // created standard-named versions of params (remove componentname and underscore then add x prefix)
            if (passedParamValues != null)
            {
                foreach (KeyValuePair<string, string> paramValuePair in passedParamValues)
                {
                    string standardKey = paramValuePair.Key;
                    if (standardKey != "_")
                    {
                        int underscorePos = standardKey.IndexOf('_');
                        if (underscorePos >= 0)
                        {
                            standardKey = standardKey.Substring(underscorePos + 1);
                        }
                        if (!standardKey.StartsWith("x"))
                        {
                            standardKey = "x" + standardKey;
                        }
                    }
                    actualParamValues[standardKey] = paramValuePair.Value;
                    if (standardKey != paramValuePair.Key)
                        actualParamValues[paramValuePair.Key] = paramValuePair.Value;
                }
            }

            if (sp.Params.ContainsKey("xuserid") || systemCall)
            {
               actualParamValues["xuserid"] = factory.Config.SystemEntityId;
            }
            MySqlParameter[] parameters = new MySqlParameter[sp.Params.Count];
            int count = 0;
            foreach (KeyValuePair<string, Param> pair in sp.Params)
            {
                object nullValue = DBNull.Value;

                switch (pair.Value.Type)
                {
                    case "char":
                        parameters[count] = new MySqlParameter(pair.Key, MySqlDbType.String, pair.Value.Length)
                        {
                            Value = actualParamValues.ContainsKey(pair.Key) ? actualParamValues[pair.Key] : nullValue
                        };
                        break;
                    case "varchar":
                        parameters[count] = new MySqlParameter(pair.Key, MySqlDbType.VarChar, pair.Value.Length)
                        {
                            Value = actualParamValues.ContainsKey(pair.Key) ? actualParamValues[pair.Key] : nullValue
                        };
                        break;
                    case "guid":
                        parameters[count] = new MySqlParameter(pair.Key, MySqlDbType.Guid)
                        {
                            Value = actualParamValues.ContainsKey(pair.Key) ? actualParamValues[pair.Key] : nullValue
                        };
                        break;
                    case "int":
                        parameters[count] = new MySqlParameter(pair.Key, MySqlDbType.Int32)
                        {
                            Value = actualParamValues.ContainsKey(pair.Key) && actualParamValues[pair.Key] != string.Empty ? Convert.ToInt32(actualParamValues[pair.Key]) : nullValue
                        };
                        break;
                    case "tinyint":
                        parameters[count] = new MySqlParameter(pair.Key, MySqlDbType.Int16)
                        {
                            Value = actualParamValues.ContainsKey(pair.Key) && actualParamValues[pair.Key] != string.Empty ? Convert.ToInt16(actualParamValues[pair.Key]) : nullValue
                        };
                        break;
                    case "float":
                        parameters[count] = new MySqlParameter(pair.Key, MySqlDbType.Float)
                        {
                            Value = actualParamValues.ContainsKey(pair.Key) && actualParamValues[pair.Key] != string.Empty ? float.Parse(actualParamValues[pair.Key]) : nullValue
                        };
                        break;
                    case "bit":
                    case "boolean":
                        parameters[count] = new MySqlParameter(pair.Key, MySqlDbType.Bit);
                        object bitValue = nullValue;
                        if (actualParamValues.ContainsKey(pair.Key))
                        {
                            bool value = actualParamValues[pair.Key] == "true" || actualParamValues[pair.Key] == "1";
                            bitValue = value ? 1 : 0;
                        }
                        parameters[count].Value = bitValue;
                        break;
                    case "datetime":
                        parameters[count] = new MySqlParameter(pair.Key, MySqlDbType.DateTime)
                        {
                            Value = actualParamValues.ContainsKey(pair.Key) && actualParamValues[pair.Key] != "null" ? actualParamValues[pair.Key] : nullValue
                        };
                        break;
                    case "text":
                        parameters[count] = new MySqlParameter(pair.Key, MySqlDbType.Text)
                        {
                            Value = actualParamValues.ContainsKey(pair.Key) ? actualParamValues[pair.Key] : nullValue
                        };
                        break;
                    case "mediumtext":
                        parameters[count] = new MySqlParameter(pair.Key, MySqlDbType.MediumText)
                        {
                            Value = actualParamValues.ContainsKey(pair.Key) ? actualParamValues[pair.Key] : nullValue
                        };
                        break;
                    case "tinytext":
                        parameters[count] = new MySqlParameter(pair.Key, MySqlDbType.TinyText)
                        {
                            Value = actualParamValues.ContainsKey(pair.Key) && actualParamValues[pair.Key] != "null" ? actualParamValues[pair.Key] : nullValue
                        };
                        break;
                    case "binary":
                        parameters[count] = new MySqlParameter(pair.Key, MySqlDbType.Binary)
                        {
                            Value = actualParamValues.ContainsKey(pair.Key) && actualParamValues[pair.Key] != "null" ? HexStringToByteArray(actualParamValues[pair.Key]) : nullValue
                        };
                        break;
                    default:
                        throw new InvalidDataException(string.Format("Unrecognised parameter data type '{0}'", pair.Value.Type));
                }
                count++;
            }
            return parameters;
        }

        public DataSet GetSystemData(string storedProc, Dictionary<string, string> paramValues)
        {
            DataSet result = factory.Database.RetrieveDataSet(factory.Config.ConnectionString, storedProc, GetParams(storedProc, paramValues, true));
            return result;
        }

        public string Execute(string storedProc, Dictionary<string, string> paramValues)
        {
            string result = factory.Database.ExecuteNonQuery(factory.Config.ConnectionString, storedProc, GetParams(storedProc, paramValues, false)).ToString();
            return result;
        }

        public string ExecuteScalar(string storedProc, Dictionary<string, string> paramValues)
        {
            string result = (string)factory.Database.ExecuteScalar(factory.Config.ConnectionString, storedProc, GetParams(storedProc, paramValues, false)).ToString();
            return result;
        }

        public DataSet GetData(string storedProc, Dictionary<string, string> paramValues)
        {
            DataSet result = factory.Database.RetrieveDataSet(factory.Config.ConnectionString, storedProc, GetParams(storedProc, paramValues, false));
            return result;
        }

        public bool IsByteArray(Type datatype)
        {
            return (datatype == typeof(byte[]));
        }

        public string ByteArrayToHexString(Object data)
        {
            if (data is System.DBNull || data == null)
                return null;

            byte[] barray = (byte[])data;
            char[] c = new char[barray.Length * 2];
            byte b;
            for (int i = 0; i < barray.Length; ++i)
            {
                b = ((byte)(barray[i] >> 4));
                c[i * 2] = (char)(b > 9 ? b + 0x57 : b + 0x30);
                b = ((byte)(barray[i] & 0xF));
                c[i * 2 + 1] = (char)(b > 9 ? b + 0x57 : b + 0x30);
            }

            // put dashes in if uuid
            if (barray.Length == 16)
            {
                char[] cu = new char[36];
                int i = 0;
                foreach (char ci in c)
                {
                    if (i == 8 || i == 13 || i == 18 || i == 23)
                    {
                        cu[i] = '-';
                        i++;
                    }
                    cu[i] = ci;
                    i++;
                }
                return new string(cu);
            }

            return new string(c);
        }

        public Byte[] HexStringToByteArray(string hexString)
        {
            if (string.IsNullOrEmpty(hexString))
                return null;

            hexString = hexString.Replace("-", string.Empty).ToUpperInvariant();
            int hexStringLength = hexString.Length;
            byte[] b = new byte[hexStringLength / 2];
            for (int i = 0; i < hexStringLength; i += 2)
            {
                int topChar = (hexString[i] > 0x40 ? hexString[i] - 0x37 : hexString[i] - 0x30) << 4;
                int bottomChar = hexString[i + 1] > 0x40 ? hexString[i + 1] - 0x37 : hexString[i + 1] - 0x30;
                b[i / 2] = Convert.ToByte(topChar + bottomChar);
            }
            return b;
        }

        public string ColumnStringValue(DataRow row, DataColumn column)
        {
            if (IsByteArray(column.DataType))
            {
                return ByteArrayToHexString(row[column]);
            }
            else
            {
                return row[column].ToString();
            }
        }
    }
}
