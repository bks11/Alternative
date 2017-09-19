using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;


namespace DoRelation.Common
{
    public class DBHelper : IDisposable
    {
        private Logger _currentLogger = null;
        public Logger CurrentLogger
        {
            get
            {
                if (_currentLogger == null)
                {
                    _currentLogger = new Logger();
                }
                return _currentLogger;
            }
        }

        private const string RETURN_VALUE_NAME_PATTERN = "ReturnValue";

        private string _connectionString = null;
        private SqlConnection _connection = null;
        private bool _isCloseConnection = true;

        public DBHelper()
            : this(null)
        { }

        public DBHelper(string connectionString)
        {
            _connectionString = string.IsNullOrEmpty(connectionString) ?
                                            Const.DEFALUT_CONNECTION_NAME :
                                            connectionString;
        }

        public DBHelper(bool closeConnection)
        {
            _isCloseConnection = closeConnection;
        }

        #region IDisposable Members

        public void Dispose()
        {
            CloseConnectionNow();
        }

        #endregion

        #region String Helper Methods

        public string DBCommandTextToString(string commandText, params object[] args)
        {
            if (args == null || args.Length == 0) return string.Format("DB Query - {0}", commandText);

            StringBuilder messageBuilder = new StringBuilder();
            for (int i = 0; i < args.Length; i++)
            {
                messageBuilder.AppendFormat("{0},", args[i]);
            }
            return string.Format("DB Query - {0} ({1})", commandText, messageBuilder.ToString(0, messageBuilder.Length - 1));
        }
        public string DBCommandTextToString(string commandText, SqlParameterCollection args)
        {
            if (args == null || args.Count == 0) return string.Format("DB Query - {0}", commandText);

            StringBuilder messageBuilder = new StringBuilder();
            for (int i = 0; i < args.Count; i++)
            {
                messageBuilder.AppendFormat("{0},", args[i]);
            }
            return string.Format("DB Query - {0} ({1})", commandText, messageBuilder.ToString(0, messageBuilder.Length - 1));
        }

        #endregion

        #region Connection

        protected void OpenConnection()
        {
            try
            {
                if (_connection == null)
                {
                    _connection = new SqlConnection(_connectionString);
                }

                if (_connection.State == ConnectionState.Broken) _connection.Close();
                if (_connection.State != ConnectionState.Open) _connection.Open();
            }
            catch (Exception e)
            {
                _connection = null;
                CurrentLogger.Log(LogLevel.Error, e);
                throw new Exception(string.Format("Could not open database connection by connection string - {0}.", _connectionString), e);
            }
        }

        protected void CloseConnection()
        {
            if (_isCloseConnection) CloseConnectionNow();
        }

        public void CloseConnectionNow()
        {
            if (_connection != null && _connection.State != ConnectionState.Closed)
            {
                try
                {
                    _connection.Close();
                    _connection = null;
                }
                catch (Exception e)
                {
                    CurrentLogger.Log(LogLevel.Error, e);
                    throw new Exception("Could not close database connection.", e);
                }
            }
        }

        #endregion

        #region Command

        public bool TryMakeCommand(out SqlCommand command, string commandText, CommandType commandType, params object[] args)
        {
            try
            {
                command = GetCommand(commandText, commandType, args);
            }
            catch (Exception ex)
            {
                command = null;
                CurrentLogger.Log(LogLevel.Error, ex);
                throw new Exception("Could not create SqlCommand instance.", ex);
            }


            return (command != null);
        }

        public SqlCommand GetCommand(string commandText, CommandType commandType, params object[] args)
        {
            string spParams = (commandText.ToLower() != "usp_checkuser") ? String.Join("; ", args) : "login page params";
            //CurrentLogger.Log(LogLevel.Info, String.Format("Execute SP: {0}, params: {1}", commandText, spParams));
            SqlCommand command;
            try
            {
                OpenConnection();
                if (_connection == null) return null;


                command = new SqlCommand(commandText, _connection);
                command.CommandType = commandType;

                if (args != null && (args.Length % 2) == 0)
                {
                    for (int i = 0; i < args.Length; i += 2)
                    {
                        command.Parameters.AddWithValue(args[i].ToString(), args[i + 1]);
                    }
                }
            }
            catch (Exception e)
            {
                CloseConnectionNow();
                CurrentLogger.Log(LogLevel.Error, e);
                throw new Exception(string.Format("Could not create SqlCommand object for {0}.", DBCommandTextToString(commandText, args)), e);
            }

            return command;
        }

        #endregion

        #region Execute(Reader/NonQuery/ReturnValue) / Get Scalar

        #region ExecuteNonQuery

        public int ExecuteNonQuery(string commandText, CommandType commandType, params object[] args)
        {
            SqlCommand command;
            if (!TryMakeCommand(out command, commandText, commandType, args)) return 0;

            int result;

            try
            {
                result = command.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                CloseConnectionNow();
                CurrentLogger.Log(LogLevel.Error, e);
                throw new Exception(string.Format("Could not execute query - {0}.", DBCommandTextToString(commandText, args)), e);
            }

            CloseConnection();
            return result;
        }

        #endregion

        #region ExecuteReader

        public SqlDataReader ExecuteReader(string commandText, CommandType commandType, params object[] args)
        {
            SqlCommand command;
            if (!TryMakeCommand(out command, commandText, commandType, args)) return null;

            SqlDataReader sqlDataReader;
            try
            {
                sqlDataReader = command.ExecuteReader();
            }
            catch (Exception e)
            {
                CloseConnectionNow();
                CurrentLogger.Log(LogLevel.Error, e);
                throw new Exception(string.Format("Could not send command text - {0} and/or create SqlDataReader.", DBCommandTextToString(commandText, args)), e);
            }

            CloseConnection();
            return sqlDataReader;
        }

        #endregion

        #region ExecuteReturnValue

        protected void AddReturnParameterToCommand(SqlCommand command)
        {
            if (command == null) return;

            SqlParameter sqlParameter = new SqlParameter();
            sqlParameter.Direction = ParameterDirection.ReturnValue;
            sqlParameter.ParameterName = RETURN_VALUE_NAME_PATTERN;
            command.Parameters.Add(sqlParameter);
        }

        public T ExecuteReturnValue<T>(string commandText, CommandType commandType, params object[] args) where T : IConvertible
        {
            SqlCommand command;
            if (!TryMakeCommand(out command, commandText, commandType, args)) return default(T);

            AddReturnParameterToCommand(command);
            object returnValue = null;
            try
            {
                command.ExecuteNonQuery();
                returnValue = command.Parameters[RETURN_VALUE_NAME_PATTERN].Value;
            }
            catch (Exception e)
            {
                CloseConnectionNow();
                CurrentLogger.Log(LogLevel.Error, e);
                throw new Exception(string.Format("Could not execute query - {0} and return value.", DBCommandTextToString(commandText, args)), e);
            }
            finally
            {
                CloseConnection();
                if (returnValue == null) returnValue = default(T);
            }

            return (T)returnValue;
        }

        public int ExecuteReturnInt(string commandText, CommandType commandType, params object[] args)
        {
            try
            {
                return ExecuteReturnValue<int>(commandText, commandType, args);
            }
            catch (Exception e)
            {
                CurrentLogger.Log(LogLevel.Error, e);
                throw new Exception(string.Format("Could not execute query - {0} and return valiue as Int32.", DBCommandTextToString(commandText, args)), e);
            }
        }

        #endregion

        #region GetScalar

        public T GetScalar<T>(string commandText, CommandType commandType, params object[] args) where T : IConvertible
        {
            SqlCommand command;
            if (!TryMakeCommand(out command, commandText, commandType, args)) return default(T);

            object scalar = null;
            try
            {
                scalar = command.ExecuteScalar();
            }
            catch (Exception e)
            {
                CloseConnectionNow();
                CurrentLogger.Log(LogLevel.Error, e);
                throw new Exception(string.Format("Could not execute query - {0}, and returns scalar.", DBCommandTextToString(commandText, args)), e);
            }
            finally
            {
                CloseConnection();

                if (scalar == null) scalar = default(T);
            }

            return (T)Convert.ChangeType(scalar, typeof(T));
        }

        #endregion

        #endregion

        #region GetDataSet

        public DataSet GetDataSet(string commandText, CommandType commandType, params object[] args)
        {
            SqlCommand command;
            if (!TryMakeCommand(out command, commandText, commandType, args)) return null;

            DataSet dataSet = GetDataSet(command);

            CloseConnection();
            return dataSet;
        }

        public DataSet GetDataSet(SqlCommand command)
        {
            if (command == null) return null;

            DataSet dataSet = new DataSet();
            SqlDataAdapter dataAdapter = new SqlDataAdapter();

            try
            {
                dataAdapter.SelectCommand = command;
                dataAdapter.Fill(dataSet);
            }
            catch (Exception e)
            {
                CloseConnectionNow();
                CurrentLogger.Log(LogLevel.Error, e);
                throw new Exception(string.Format("Could not fill DataSet by query - {0}.", DBCommandTextToString(command.CommandText, command.Parameters)), e);
            }

            CloseConnection();
            return dataSet;
        }

        #endregion

        #region GetDataTable

        public DataTable GetDataTable(DataSet dataSet)
        {
            return GetDataTable(dataSet, 0);
        }

        public DataTable GetDataTable(DataSet dataSet, int tableIndex)
        {
            DataTable dataTable = null;
            if ((dataSet != null) && (dataSet.Tables.Count - 1 >= tableIndex))
            {
                dataTable = dataSet.Tables[tableIndex];
            }
            return dataTable;
        }

        public DataTable GetDataTable(string commandText, CommandType commandType, params object[] args)
        {
            SqlCommand command;
            if (!TryMakeCommand(out command, commandText, commandType, args)) return null;

            DataTable dataTable = GetDataTable(command);

            CloseConnection();
            return dataTable;
        }

        public DataTable GetDataTable(SqlCommand command)
        {
            if (command == null) return null;

            DataTable dataTable = new DataTable();
            SqlDataAdapter dataAdapter = new SqlDataAdapter();
            try
            {
                dataAdapter.SelectCommand = command;
                dataAdapter.Fill(dataTable);
            }
            catch (Exception e)
            {
                CloseConnectionNow();
                CurrentLogger.Log(LogLevel.Error, e);
                throw new Exception(string.Format("Could not fill DataTable by query - {0}.", DBCommandTextToString(command.CommandText, command.Parameters)), e);
            }
            CloseConnection();
            return dataTable;
        }

        #endregion

        #region GetDataRow

        public DataRow GetDataRow(string commandText, CommandType commandType, params object[] args)
        {
            DataTable dataTable = GetDataTable(commandText, commandType, args);
            if (dataTable == null || dataTable.Rows == null || dataTable.Rows.Count == 0) return null;

            return dataTable.Rows[0];
        }

        public DataRow GetDataRow(SqlCommand command)
        {
            DataTable dataTable = GetDataTable(command);
            if (dataTable == null || dataTable.Rows == null || dataTable.Rows.Count == 0) return null;

            return dataTable.Rows[0];
        }

        #endregion
    }
}
