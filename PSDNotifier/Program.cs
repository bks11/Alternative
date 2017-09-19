using System;
using System.Configuration;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace PSDNotifier
{
	class Program
	{
		const string SQL_SELECT_ERR = "SELECT U.USRNAME,e.TIME_,e.ERRCOD,e.ERRTEXT  FROM ELO_USR_ERR E INNER JOIN ELO_USERS U ON E.USERID = U.USRID WHERE DATEDIFF(SECOND,TIME_,getdate())< {0} ORDER BY TIME_ DESC";
		const string SQL_SELECT_EMAIL = "SELECT  vol FROM LDV_ADD_INFO WHERE code = '$execmail$' AND vol IS NOT NULL AND vol != '' AND SUBSTRING(unicode, 7, 3) NOT IN (SELECT usr.USRID FROM ELO_USERS AS usr LEFT OUTER JOIN ELO_USER_JOB AS job ON usr.USRID = job.USERID WHERE job.JOBID = -1 and job.DOSTUP = 1 )";

		public static SqlConnection sqlConnection;
		public static List<string> resultStore;
		public static int Interval { get; set; }
		public static string ExecuteOptions { get; set; }
		public static string Delimeter { get; set; }
		public static string ElodbConnectionString;


		enum ExitCode : int
		{
			NoAlarms = 0,
			Alarm = 1,
			ConnectionError = 2,
			QueryExecuteError = 3,
			ConnectionCloseError = 4,
			ConvertError = 5
		}

		static int Main(string[] args)
		{
			bool hasArgs = (args.Length > 0);
			if (!hasArgs)
			{
				Console.WriteLine("Не указаны параметры запуска");
				Console.WriteLine("Два варианта запуска");
				Console.WriteLine("PSDNotifier[целочисленное  цифровое значение количества секунд] -для  получения информации о наличии ошибок");
				Console.WriteLine("PSDNotifier email - Для получения списка электронных адресов пользователей ПТК ПСД");
				Console.ReadLine();
			}
			else
			{
				ParseArguments(args);
			}
			string sql = PrepareSqlScript();
			
			Console.WriteLine(ShowResult());

			CloseConnection();
			//Console.ReadLine();

			if (resultStore.Count > 0 && ExecuteOptions == "notifier")
			{
				return (int)ExitCode.Alarm; 
			} else
				return (int)ExitCode.NoAlarms;
		}

		private static void GetConnectionString()
		{
			ConnectionStringSettings elodbSettings = ConfigurationManager.ConnectionStrings["elodb"];
			if (elodbSettings != null)
			{
				ElodbConnectionString = elodbSettings.ConnectionString;
			}
		}

		private static string ShowResult()
		{
			string resultString = "";
			if (ExecuteOptions == "email")
			{
				for(int i = 0; i <= resultStore.Count -1; i++)
				{
					if (i == resultStore.Count - 1)
					{
						resultString = resultString + resultStore[i];
						resultString.Replace(" ","");
					}
					else
					{
						resultString = resultString + resultStore[i] + "#";
						resultString.Replace(" ", "");
					}
				}
			}
			if (ExecuteOptions == "notifier")
			{
				foreach (string s in resultStore)
				{
					resultString = resultString + s;// + Environment.NewLine;
				}
			}
			return resultString;
		}

		private static string PrepareSqlScript()
		{
			string sql = "";
			switch (ExecuteOptions)
			{
				case "email":
					sql = SQL_SELECT_EMAIL;
					ExecuteQuery(sql);
					break;
				case "notifier":
					sql = SQL_SELECT_ERR.Replace("{0}", Interval.ToString());
					ExecuteQuery(sql);
					break;
				default:
					sql = SQL_SELECT_ERR.Replace("{0}", "7");
					ExecuteQuery(sql);
					break;
			}
			return sql;
		}

		private static void ParseArguments(string[] args)
		{
			ExecuteOptions = (args[0].Trim()).ToLower();
			int interval;

			if (int.TryParse(ExecuteOptions, out interval))
			{
				Interval = interval;
				ExecuteOptions = "notifier";
				return;
			}
			if (ExecuteOptions == "email")
			{
				ExecuteOptions = "email";
				return;
			}
		}

		private static void CloseConnection()
		{
			if (sqlConnection.State != System.Data.ConnectionState.Open)
			{
				try
				{
					sqlConnection.Close();
				}
				catch (Exception e)
				{
					Console.WriteLine(e.Message);
					Console.ReadLine();
				}
			}
		}

		private static bool ConnectToDb()
		{
			GetConnectionString();
			sqlConnection = new SqlConnection(ElodbConnectionString);
			try
			{
				sqlConnection.Open();
				//Console.WriteLine("Connection open");
				return true;
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				//Console.ReadLine();
				return false;
			}
		}

		static int ExecuteQuery(string queryText)
		{

			if (!ConnectToDb())
			{

			};
			int recordsCount = 0;
			if (sqlConnection.State != System.Data.ConnectionState.Open)
			{
				return -2;
			}
			try
			{
				resultStore = new List<string>();
				SqlCommand sqlcmd = new SqlCommand(queryText, sqlConnection);
				SqlDataReader reader = sqlcmd.ExecuteReader();
				if (reader.HasRows)
				{
					while (reader.Read())
					{
						recordsCount++;

						string resultStr = "";
						for (int i = 0; i <= reader.FieldCount - 1; i++)
						{
							if (i == reader.FieldCount - 1)
							{
								resultStr = resultStr + reader[i].ToString();
							}
							else
							{
								resultStr = resultStr + reader[i].ToString() + " ";
							}
						}
						resultStore.Add(resultStr);
						//Console.WriteLine(resultStr);
					}//end while
				}
			} catch (Exception e)
			{
				Console.WriteLine(e.Message);
				//Console.ReadLine();
				return -1;
			}
			return recordsCount;
		}
			
		static int ExecuteQuery(int interval)
		{
			if (sqlConnection.State != System.Data.ConnectionState.Open)
			{
				return -2;
			}
			int recordsCount = 0;
			try
			{
				string selectQuery = SQL_SELECT_ERR.Replace("{0}", interval.ToString());
				SqlCommand sqlcmd = new SqlCommand(selectQuery, sqlConnection);
				SqlDataReader reader = sqlcmd.ExecuteReader();
				if (reader.HasRows)
				{
					while (reader.Read())
					{
						recordsCount++;

						string resultStr = "";
						for (int i = 0; i <= reader.FieldCount - 1; i++)
						{
							resultStr = resultStr + reader[i].ToString() + " ";
						}
						Console.WriteLine(resultStr);

						//object username = reader[0];
						//object errTime = reader["TIME_"];
						//object errCode = reader["ERRCOD"];
						//object errText = reader["ERRTEXT"];
						//Console.WriteLine("{0} \t{1} \t{2} \t{3}", username, errTime, errCode, errText);
					}
				}
				return recordsCount;
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				//Console.ReadLine();
				return -1;
			}
		}
	}
}
