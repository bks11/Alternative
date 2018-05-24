using System;
using System.Diagnostics;
using System.IO;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;

namespace TransactionPassport
{
    class Program
    {
        const string SQL_REP_TYPE = "select d.VALUE from TFILES f Right join TXML_DATA d on d.TFILE_ID = f.ID  where f.NAME = @filename and d.NAME = @reptype";
        const string SELECT_FILE = "SELECT * FROM MESSAGECOUNTER WHERE FILEMASK = @filemask AND FILEDATE = @filedate";
        const string INSERT_RECORD = "INSERT INTO MESSAGECOUNTER (filemask,filedate,serialnumber) values (@fm, @dt,1)";
        const string UPDATE_NUMBER = "UPDATE MESSAGECOUNTER SET SERIALNUMBER = @sn WHERE ID = @id";

        const string TIME_PATERN = "HHmmss";
        const string DATE_PATERN = "yyyyMMdd";
        const string XML_DATA_ELEMENT_NAME = "RepType";
        const string XML_REP_TYPE_PS_KR3 = "ps_kr4";
        const string XML_REP_TYPE_PS_EI3 = "ps_ei4";
        const string FIELD_NAME = "VALUE";
        const string ERR_NOT_ENOUGH_PRM = "Не достаточно параметров для запуска";
        const string CFG_KEY = "PS";
        const string CFG_ARC = "ARJ";
        private static string pathToArchiver;
        public static string FileMask { get; private set; }
        public static string OutPath { get; private set; }
        public static string InPath { get; private set; }
        public static List<string> Ps_kr3_files;
        public static List<string> Ps_ei3_files;
        static DateTime ArcDate;
        static string[] FilesByMask;

        static void Main(string[] args)
        {
            bool hasArgs = (args.Length == 2);
            if (!hasArgs)
            {
                Console.WriteLine(ERR_NOT_ENOUGH_PRM);
                //Console.ReadLine();
                return;
            }
            else
            {
                ArcDate = DateTime.Today;
                InPath = args[0];
                OutPath = args[1];

                //if (!DateTime.TryParseExact(args[1], DATE_PATERN, null, System.Globalization.DateTimeStyles.None, out ArcDate))
                //{
                //    Console.WriteLine("Формат даты отличается от yyyyMMdd");
                //    return;
                //}
            }

            if (!ConnectToDataBase())
            {
                return;
            }

            GetFileMask();
            //Get files by mask fill array FilesByMask[]
            GetFilesListByMask();
            if (!DoArj())
            {
                Console.WriteLine("Не удалось создать архив");
                //Console.ReadLine();
            }
            
        }

        #region Do connect
        private static SqlConnection DbSqlConnection;
        private static string DbConnectionString;

        private static void GetFileMask()
        {
            try
            {
                var appSettings = ConfigurationManager.AppSettings;
                if (appSettings.Count == 0)
                {
                    Console.WriteLine("Не указаны маски файлов.");
                }
                else
                {
                    FileMask = appSettings[CFG_KEY];
                    pathToArchiver = appSettings[CFG_ARC];
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            if (string.IsNullOrEmpty(pathToArchiver))
            {
                Console.WriteLine("Не указан путь к архиватору");
            };
        }

        private static void GetConnectionString()
        {
            ConnectionStringSettings CBUtils = ConfigurationManager.ConnectionStrings["CBUtils"];
            if (CBUtils != null)
            {
                DbConnectionString = CBUtils.ConnectionString;
            }
        }
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
                Console.WriteLine("Ошибкак подключения к БД {0}", e);
                return false;
            }
            return (DbSqlConnection.State == ConnectionState.Open) ? true : false;
        }
        #endregion

        #region FileProcessor

        public static bool GetFilesListByMask()
        {
            try
            {
                FilesByMask = Directory.GetFiles(InPath, FileMask);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
            return FilesByMask.Length > 0;
        }

        public static bool DoArj()
        {
            Ps_kr3_files = new List<string>();
            Ps_ei3_files = new List<string>();
            foreach (string file in FilesByMask)
            {
                try
                {
                    if (!Directory.Exists(OutPath))
                    {
                        Directory.CreateDirectory(OutPath);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("При создании дирректории {0} произошла ошибка {1}",OutPath,e.Message);
                    return false;
                }
                
                string fileName = Path.GetFileName(file);
                string reptype = GetReportTypeFromXmlData(fileName);
                if (!string.IsNullOrEmpty(reptype))
                {
                    if (reptype == XML_REP_TYPE_PS_KR3)
                    {
                        Ps_kr3_files.Add(file);
                    }

                    if (reptype == XML_REP_TYPE_PS_EI3)
                    {
                        Ps_ei3_files.Add(file);
                    }

                    //string archiveName = GenerateArchiveName(reptype);
                    //string fullArchiveName = Path.Combine(OutPath, archiveName);

                    //string cmdPrm = string.Format("m -e {0} {1}", fullArchiveName,file);

                    //Process arjStartProcess = new Process();
                    //arjStartProcess.StartInfo.FileName = pathToArchiver;
                    //arjStartProcess.StartInfo.Arguments = cmdPrm;
                    ////arjStartProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    //arjStartProcess.Start();
                    //arjStartProcess.WaitForExit();

                    //Console.WriteLine(archiveName);
                }
                else
                {
                    Console.WriteLine("Файл {0} не зарегистрирован в таблице TFIles", fileName);
                }
            }
            //
            if (Ps_kr3_files.Count > 0)
            {
                string kr3FilesToArch = String.Join(" ", Ps_kr3_files.ToArray());
                
                string archiveName = GenerateArchiveName(XML_REP_TYPE_PS_KR3);
                string fullArchiveName = Path.Combine(OutPath, archiveName);
                string cmdPrm = string.Format("m -e {0} {1}", fullArchiveName, kr3FilesToArch);
                try
                {
                    Process arjStartProcess = new Process();
                    arjStartProcess.StartInfo.FileName = pathToArchiver;
                    arjStartProcess.StartInfo.Arguments = cmdPrm;
                    //arjStartProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    arjStartProcess.Start();
                    arjStartProcess.WaitForExit();
                    Console.WriteLine(arjStartProcess.ExitCode);
                    Console.WriteLine(archiveName);

                }
                catch(Exception e)
                {
                    Console.WriteLine("Во время создания архива ps_kr3 произошла ошибка {0}", e.Message);
                    return false;
                }
            }

            if (Ps_ei3_files.Count > 0)
            {
                string ei3FilesToArch = String.Join(" ", Ps_ei3_files.ToArray());
               
                string archiveName = GenerateArchiveName(XML_REP_TYPE_PS_EI3);
                string fullArchiveName = Path.Combine(OutPath, archiveName);
                string cmdPrm = string.Format("m -e {0} {1}", fullArchiveName, ei3FilesToArch);
                try
                {
                    Process arjStartProcess = new Process();
                    arjStartProcess.StartInfo.FileName = pathToArchiver;
                    arjStartProcess.StartInfo.Arguments = cmdPrm;
                    arjStartProcess.Start();
                    arjStartProcess.WaitForExit();
                    Console.WriteLine(archiveName);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Во время создания архива ps_ei3 произошла ошибка {0}", e.Message);
                    return false;
                }
            }

            return true;
        }

        private static string GetReportTypeFromXmlData(string fileName)
        {
            SqlCommand selectFileId = new SqlCommand(SQL_REP_TYPE, DbSqlConnection);

            SqlParameter file_Name = new SqlParameter("@filename", fileName);
            selectFileId.Parameters.Add(file_Name);
            SqlParameter AttrName = new SqlParameter("@reptype", XML_DATA_ELEMENT_NAME);
            selectFileId.Parameters.Add(AttrName);

            string reportType = "";
            try
            {
                SqlDataReader reader = selectFileId.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        reportType = (string)reader[FIELD_NAME];
                    }//end while
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return reportType;
        }



        private static int GeneratePackageNumber()
        {
            int recId = 0;
            int lastSerialNumber = 0;
            try
            {
                SqlCommand sqlcmd = new SqlCommand(SELECT_FILE, DbSqlConnection);
                SqlParameter fileMaskPrm = new SqlParameter("@filemask", "f364");
                sqlcmd.Parameters.Add(fileMaskPrm);
                SqlParameter fileDatePrm = new SqlParameter("@filedate", ArcDate);
                sqlcmd.Parameters.Add(fileDatePrm);
                SqlDataReader reader = sqlcmd.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        lastSerialNumber = (int)reader["SERIALNUMBER"];
                        recId = (int)reader["ID"];
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            int number = 0;

            if (lastSerialNumber == 0)
            {
                try
                {
                    SqlCommand sqlcmd = new SqlCommand(INSERT_RECORD, DbSqlConnection);
                    SqlParameter fileMaskPrm = new SqlParameter("@fm", "f364");
                    sqlcmd.Parameters.Add(fileMaskPrm);
                    SqlParameter fileDatePrm = new SqlParameter("@dt", ArcDate);
                    sqlcmd.Parameters.Add(fileDatePrm);
                    number = sqlcmd.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                return lastSerialNumber + 1;
            }

            lastSerialNumber += 1;
            try
            {
                SqlCommand sqlcmd = new SqlCommand(UPDATE_NUMBER, DbSqlConnection);
                SqlParameter prmFileMask = new SqlParameter("@sn", lastSerialNumber);
                sqlcmd.Parameters.Add(prmFileMask);
                SqlParameter prmRecordId = new SqlParameter("@id", recId);
                sqlcmd.Parameters.Add(prmRecordId);
                number = sqlcmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            if (number > 0) return lastSerialNumber; else return -1;
        }

        static private string NumConvertor(int inpNumber, int leadZero)
        {
            string strNumber = inpNumber.ToString();
            string result = strNumber;
            if (strNumber.Length < leadZero)
            {
                for (int i = 0; i < (leadZero - strNumber.Length); i++)
                {
                    result = "0" + result;
                }
            }
            return result;
        }

        private static string GenerateArchiveName(string reportType)
        {
            string archiveName = "";
            string currentDate = DateTime.Now.ToString("yyyyMMdd");

            int pNumber = GeneratePackageNumber();
            string packageNumber = NumConvertor(pNumber, 3);

            if (reportType == XML_REP_TYPE_PS_KR3)
            {
                archiveName = "PSKR_2490_0000_" + currentDate + "_" + packageNumber + ".arj";
            }
            if (reportType == XML_REP_TYPE_PS_EI3)
            {
                archiveName = "PSEI_2490_0000_" + currentDate + "_" + packageNumber+".arj";
            }
            return archiveName;
        }
        #endregion
    }
}
