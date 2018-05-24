using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Globalization;
using System.Data.SqlClient;
using System.Configuration;
using System.Data;

namespace CountFilesInFolder
{
    class Program
    {
        private static string PathForControl { get; set; }
        private static List<string> MasksOfFiles { get; set; }
        private static DateTime ControlDate { get; set; }
        private static string RecordType { get; set; }
        private static int SubFolders { get; set; }
        private static int RecId { get; set; }
        private static int MaskArrayLengthIncludeExclude { get;set;}

        //static List<string> FindFilesList;
        static int TotalFiles;
        static List<string> MasksListInclude;
        static List<string> MasksListExclude;

        static string DbConnectionString;
        static SqlConnection DbSqlConnection;
        const string SELECT_FILE = "SELECT * FROM MESSAGECOUNTER WHERE FILEMASK = @filemask AND FILEDATE = @filedate";
        const string UPDATE_NUMBER = "UPDATE MESSAGECOUNTER SET SERIALNUMBER = @sn WHERE ID = @id";
        const string INSERT_RECORD = "INSERT INTO MESSAGECOUNTER (filemask,filedate,serialnumber) values (@fm, @dt,@tf)";

        static int Main(string[] args)
        {
            Console.WriteLine("Запуск программы CountFilesInFolder");
            bool hasArgs = (args.Length == 5);
            if (!hasArgs)
            {
                Console.WriteLine("Не досаточно аргументов для запуска программы");
                Console.WriteLine("Путь к папке");
                Console.WriteLine("Список масок файлов в формате b*.*#a*.*#c*.*/t*.* - через слеш описываются маски, которые нужно исключить из подсчета");
                Console.WriteLine("Дата в формате yyyymmdd");
                Console.WriteLine("Тип записи в BD");
                Console.WriteLine("Проверять вложенные каталоги (да - 1/нет - 0)");
                //Console.ReadLine();
                return -1;
            }

            if (!CheckArgs(args))
            {
                //Console.WriteLine("");
                return -1;
            }

            TotalFiles = FindFilesByMask(SubFolders);

            if (ConnectToDataBase())
            {
                Console.WriteLine("Подключение к БД прошло успешно.");
                UpdateFileNumber(RecordType, ControlDate);
            }

            Console.WriteLine("В каталоге {0} - {1} файлов", MasksOfFiles, TotalFiles);
            //Console.ReadLine();
            return TotalFiles;
        }

        private static bool IsPathValid(string path)
        {
            if (!Directory.Exists(path))
            {
                Console.WriteLine("Not valid path - {0} !", path);
                return false;
            }
            else
            {
                return true;
            }
        }

        

        static bool CheckArgs(string[] args)
        {

            //Fist argument path for control
            if (IsPathValid(args[0]))
            {
                PathForControl = args[0];
            }
            else
            {
                return false;
            }

            // Second arguent files mask list
            string masks = args[1];
            MaskArrayLengthIncludeExclude = CreateIncludeExcludeMaskList(masks);

            // Third argument control date (current day)
            DateTime dt;
            bool dateConvert = DateTime.TryParseExact(args[2].ToString(), "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);
            if (!dateConvert)
            {
                Console.WriteLine("Input date {0} not valid  -  yyyymmdd", args[3]);
                return false;
            }
            ControlDate = dt;

            //Fourth argument Record type (type for DB)
            RecordType = args[3];

            //Fifth argument - include subfolders (yes/no)
            int searchOptions;
            if (Int32.TryParse(args[4], out searchOptions))
            {
                SubFolders = searchOptions;
            }
            else
            {
                Console.WriteLine("Invalid search options - must be 1 (Incluse subfolders) or 0 {Not include subfolder} ");
                return false;
            }          
            return true; 
        }
        
        /// <summary>
        /// Create two list of file masks include and exclude
        /// </summary>
        /// <param name="masks">File masks list from parametr</param>
        /// <returns>Length. If length > 1 we have include and exclude  file mask list 
        /// else only include list</returns>
      
        static int CreateIncludeExcludeMaskList(string masks)
        {
            try {
                string[] separateList = masks.Split('/');
                if (separateList.Length > 1)
                {
                    MasksListInclude = new List<string>();
                    MasksListInclude = separateList[0].Split('#').ToList();
                    MasksListExclude = new List<string>();
                    MasksListExclude = separateList[1].Split('#').ToList();
                }
                else
                {
                    MasksListInclude = new List<string>();
                    MasksListInclude = separateList[0].Split('#').ToList();
                }
                //Console.WriteLine("Количество файлов включенных в список - {0}", MasksListInclude.Count());
                //Console.WriteLine("Количество файлов исключенных из списка - {0}", MasksListExclude.Count());
                return separateList.Length;
            }
            catch {
                Console.WriteLine("Ошибка создания списка масок файлов!");
                return -1;
            }   
        }

        static int FindFilesByMask(int searchOption)
        {
            SearchOption so;

            switch (searchOption)
            {
                case 0:
                    so = SearchOption.TopDirectoryOnly;
                    break;
                case 1:
                    so = SearchOption.AllDirectories;
                    break;
                default:
                    so = SearchOption.TopDirectoryOnly;
                    break;
            }

            int excludeFilesCount = 0;
            List<string> findIncludeFilesList = new List<string>();
            foreach (string mask in MasksListInclude)
            {
                List<string> fn = Directory.GetFiles(PathForControl, mask, so).ToList();
                findIncludeFilesList.AddRange(fn);
            }
            IEnumerable<string> incDistinct = findIncludeFilesList.Distinct();
            if (MaskArrayLengthIncludeExclude > 1)
            {
                List<string> findExcludeFilesList = new List<string>();
                foreach (string mask in MasksListExclude)
                {
                    List<string> fn = Directory.GetFiles(PathForControl, mask, so).ToList();
                    findExcludeFilesList.AddRange(fn);
                }
                IEnumerable<string> excDistinct = findExcludeFilesList.Distinct();
                excludeFilesCount = excDistinct.Count();
            }


            return incDistinct.Count() - excludeFilesCount;
        }

        #region Work with DB

        //Получаем строку соединения из конфигурационного файла
        private static void GetConnectionString()
        {
            ConnectionStringSettings CBUtils = ConfigurationManager.ConnectionStrings["CBUtils"];
            if (CBUtils != null)
            {
                DbConnectionString = CBUtils.ConnectionString;
            }
        }
        //Соединение с БД
        private static bool ConnectToDataBase()
        {
            GetConnectionString();
            DbSqlConnection = new SqlConnection(DbConnectionString);
            try
            {
                DbSqlConnection.Open();
                //Console.WriteLine("Sql connection open!");
            }
            catch (Exception e)
            {
                Console.WriteLine("{0} Exception caught.", e);
                return false;
            }
            return (DbSqlConnection.State == ConnectionState.Open) ? true : false;
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
                    SqlCommand sqlcmd = new SqlCommand(INSERT_RECORD, DbSqlConnection);
                    SqlParameter fileMaskPrm = new SqlParameter("@fm", mask);
                    sqlcmd.Parameters.Add(fileMaskPrm);
                    SqlParameter fileDatePrm = new SqlParameter("@dt", reportDate);
                    sqlcmd.Parameters.Add(fileDatePrm);
                    SqlParameter totalFilesInArcPrm = new SqlParameter("@tf", TotalFiles);
                    sqlcmd.Parameters.Add(totalFilesInArcPrm);
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
                SqlCommand sqlcmd = new SqlCommand(UPDATE_NUMBER, DbSqlConnection);
                SqlParameter fileMaskPrm = new SqlParameter("@sn", TotalFiles);
                sqlcmd.Parameters.Add(fileMaskPrm);
                SqlParameter fileDatePrm = new SqlParameter("@id", RecId);
                sqlcmd.Parameters.Add(fileDatePrm);
                number = sqlcmd.ExecuteNonQuery();
                Console.WriteLine("{0} - record(s) updated",number);	
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return -2;
            }
            if (number > 0) return lastNumber;
            else return -3;
        }


        static private int GetLastFileNumber(string arcType, DateTime reportDate)
        {
            if (DbSqlConnection.State != ConnectionState.Open)
            {
                return -1;
            }

            int lastSerialNumber = 0;
            try
            {
                SqlCommand sqlcmd = new SqlCommand(SELECT_FILE, DbSqlConnection);
                SqlParameter fileMaskPrm = new SqlParameter("@filemask", arcType);
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
                    return 1;
                }
                else return lastSerialNumber;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return -2;
            }
        }

        #endregion DO CONNECT

    }
}
