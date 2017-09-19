using System;
using System.Configuration;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;

namespace FileCounter
{
	class Program
	{
		const string SELECT_FILE = "SELECT * FROM MESSAGECOUNTER WHERE FILEMASK = @filemask AND FILEDATE = @filedate";
		const string UPDATE_NUMBER = "UPDATE MESSAGECOUNTER SET SERIALNUMBER = @sn WHERE ID = @id";
		const string INSERT_RECORD = "INSERT INTO MESSAGECOUNTER (filemask,filedate,serialnumber) values (@fm, @dt,1)";

        private static string CONNECTION_STRING;
        private static int RecId { get; set; }
		private static SqlConnection sqlConnection;

		enum ExitCode : int
		{
			Success = 0,
			NotEnoughtArgs = 1,
			ConnectionError = 2,
			SelectError = 3,
			ConnectionCloseError = 4,
			UpdateError =5,
			ConvertError = 6,
			DateFormatError = 7,
			InsertError = 8
		}

		static int Main(string[] args)
		{
			bool hasArgs = (args.Length == 4);
			if (!hasArgs)
			{
				Console.WriteLine("Не досаточно аргументов для запуска программы");
				Console.WriteLine("Первый параметр маска файла");
				Console.WriteLine("Второй параметр дата отчета в формате yyyymmdd");
				Console.WriteLine("Третий параметр число соответствующее системе исчисления возвращаемого результата [10 или 36]");
				Console.WriteLine("Четвертый параметр число указывающее количество знаков возвращаемого результата (например 3 => 001)");
				//Console.ReadLine();
				return (int)ExitCode.NotEnoughtArgs;
			}
			//args[0]  - file mask
			string fileMask = args[0];
			
			//args[1] - file date
			DateTime dt;
			bool dateConvert = DateTime.TryParseExact(args[1].ToString(), "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);
			if (!dateConvert)
			{
				Console.WriteLine("Формат даты не соответствует -  yyyymmdd");
				//Console.ReadLine();
				return (int)ExitCode.DateFormatError;
			}
			//args[2] - number system translation
			int nst = 0;
			if (int.TryParse(args[2], out nst))
			{
				if (nst < 2 || nst > 36)
				{
					Console.WriteLine("Ошибка параметра. Данный параметр может принимать только числовое значение от 2 до 36");
					//Console.ReadLine();
					return (int)ExitCode.ConvertError;
				}
			}
			else
			{	
				Console.WriteLine("Ошибка параметра. Данный параметр может принимать только числовое значение от 2 до 36");
				//Console.ReadLine();
				return (int)ExitCode.ConvertError;
			}

			//args[3] - Leading zeros
			int lz;
			if (!int.TryParse(args[3], out lz))
			{
				Console.WriteLine("Ошибка параметра. Данный параметр может быть только целым числом");
				//Console.ReadLine();
				return (int)ExitCode.ConvertError;
			}

			int sn = UpdateFileNumber(fileMask, dt);

			string num = NumConvertor(sn, nst, lz);

			Console.WriteLine(num);

			//Console.WriteLine(sn);
			//Console.ReadLine();
			return ErrorLevel(sn);
				

		}

		static private string NumConvertor(int inpNumber, int systemTranslation, int leadZero)
		{
			string number = inpNumber.ToString();
			string result;

			if (number.TryToBase(10, systemTranslation, out result)) // from 10 to 36 
			{
				if (result.Length < leadZero)
				{
					string s = result;
					for (int i = 0; i < (leadZero - result.Length); i++)
					{
						s = "0" + s;
					}
					result = s;
				}
				return result;
			}
			else
			{
				Console.WriteLine("Неверный формат {0}", number);
				return "";
			}

			
		}

		static private int ErrorLevel(int errcode)
		{
			if (errcode == -1)
			{
				//Console.ReadLine();
				return (int)ExitCode.ConnectionError;
			}
			if (errcode == -2)
			{
				//Console.ReadLine();
				return (int)ExitCode.SelectError;
			}
			if (errcode == -3)
			{
				//Console.ReadLine();
				return (int)ExitCode.UpdateError;
			}
			if (errcode == -4 || errcode == -5)
			{
				//Console.ReadLine();
				return (int)ExitCode.InsertError;
			}
			if (errcode > 0)
			{
				return (int)ExitCode.Success;
			}
			return -100;
		}

        //Получаем строку соединения из конфигурационного файла
        private static void GetConnectionString()
        {
            ConnectionStringSettings CBUtils = ConfigurationManager.ConnectionStrings["ELODB"];
            if (CBUtils != null)
            {
                CONNECTION_STRING = CBUtils.ConnectionString;
            }
        }

        static private bool ConnectToDataBase()
		{
            GetConnectionString();
            sqlConnection = new SqlConnection(CONNECTION_STRING);
			try
			{
				sqlConnection.Open();
				//Console.WriteLine("Connection open");
				return true;
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				return false;
			}
		}

		static private int GetLastFileNumber(string mask, DateTime reportDate)
		{
			if (!ConnectToDataBase())
			{
				return -1;
			}

			int lastSerialNumber = 0;
			try
			{
				SqlCommand sqlcmd = new SqlCommand(SELECT_FILE, sqlConnection);
				SqlParameter fileMaskPrm = new SqlParameter("@filemask", mask);
				sqlcmd.Parameters.Add(fileMaskPrm);
				SqlParameter fileDatePrm = new SqlParameter("@filedate", reportDate);
				sqlcmd.Parameters.Add(fileDatePrm);
				SqlDataReader reader = sqlcmd.ExecuteReader();
				if (reader.HasRows)
				{
					while (reader.Read())
					{
						lastSerialNumber = (int)reader["SERIALNUMBER"];
						RecId = (int)reader["ID"];
					}
					return lastSerialNumber;
				}
				else return lastSerialNumber;
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				return -2;
			}
		}

		static private int UpdateFileNumber(string mask, DateTime reportDate)
		{
			int number;
			int lastNumber = GetLastFileNumber(mask, reportDate);
			if (lastNumber < 0)
			{
				return lastNumber;
			}
			if (lastNumber == 0)
			{
				try
				{
					SqlCommand sqlcmd = new SqlCommand(INSERT_RECORD, sqlConnection);
					SqlParameter fileMaskPrm = new SqlParameter("@fm", mask);
					sqlcmd.Parameters.Add(fileMaskPrm);
					SqlParameter fileDatePrm = new SqlParameter("@dt", reportDate);
					sqlcmd.Parameters.Add(fileDatePrm);
					number = sqlcmd.ExecuteNonQuery();
				}
				catch (Exception e)
				{
					Console.WriteLine(e.Message);
					return -4;
				}
				if (number > 0)
				{
					//Console.WriteLine("{0} - record(s) inserted", number);
					return 1;
				}
				else return -5;				
			}
			lastNumber += 1;
			try
			{
				SqlCommand sqlcmd = new SqlCommand(UPDATE_NUMBER, sqlConnection);
				SqlParameter fileMaskPrm = new SqlParameter("@sn", lastNumber);
				sqlcmd.Parameters.Add(fileMaskPrm);
				SqlParameter fileDatePrm = new SqlParameter("@id", RecId);
				sqlcmd.Parameters.Add(fileDatePrm);
				number = sqlcmd.ExecuteNonQuery();
				//Console.WriteLine("{0} - record(s) updated",number);	
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				return -2;
			}
			if (number > 0) return lastNumber;
			else return -3; 
		}
	}
}
