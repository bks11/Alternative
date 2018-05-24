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

namespace CountFilesInArh
{
    class Program
    {
       
        private static string   ArchivePath { get; set; }
        private static string   MasksOfArchive { get; set; }
        private static DateTime ArchiveDate { get; set; }
        private static string   ArchiveType { get; set; }
        private static int RecId { get; set; }

        static int TotalFiles;
        static List<string> MasksList;
        static List<string> FindFilesList;

        //static string archiveName = "";
        static string pathToFileList = "";
        static List<string> arhContent;

        static string DbConnectionString;
        static SqlConnection DbSqlConnection;
        const string SELECT_FILE = "SELECT * FROM MESSAGECOUNTER WHERE FILEMASK = @filemask AND FILEDATE = @filedate";
        const string UPDATE_NUMBER = "UPDATE MESSAGECOUNTER SET SERIALNUMBER = @sn WHERE ID = @id";
        const string INSERT_RECORD = "INSERT INTO MESSAGECOUNTER (filemask,filedate,serialnumber) values (@fm, @dt,@tf)";

        static int Main(string[] args)
        {
            Console.WriteLine("Запуск программы CountFilesInArc");
            bool hasArgs = (args.Length == 4);
            if (!hasArgs)
            {
                Console.WriteLine("Не досаточно аргументов для запуска программы");
                Console.WriteLine("Путь к архивам");
                Console.WriteLine("Дата в формате yyyymmdd");
                Console.WriteLine("Маски файлов архивов");
                Console.WriteLine("Тип файла");
                //Console.ReadLine();
                return -1;
            }

            if (!CheckArgs(args)) {
                //Console.WriteLine("");
                return -1;
            }            

            GetTotalFilesInArchive();
            if (ConnectToDataBase())
            {
                Console.WriteLine("Подключение к БД прошло успешно.");
                UpdateFileNumber(ArchiveType, ArchiveDate);
            }

            Console.WriteLine("В архивах по маскам {0} - {1} файлов", MasksOfArchive, TotalFiles);
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

            if (IsPathValid(args[0]))
            {
                ArchivePath = args[0];
            }
            else
            {
                return false;
            }

            MasksOfArchive = args[1];

            ArchiveType = args[2];

            DateTime dt;
            bool dateConvert = DateTime.TryParseExact(args[3].ToString(), "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);
            ArchiveDate = dt;
            if (!dateConvert)
            {
                Console.WriteLine("Input date {0} not valid  -  yyyymmdd", args[3]);
                return false;
            }

            try
            {
                string arhType = "Arj";
                if (!Enum.IsDefined(typeof(KnownSevenZipFormat), arhType)) return false;
                //archiveName = args[0];
                //pathToFileList = args[1];
                //return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
            FillMasksList();
            return true;
        }

        static void FillMasksList()
        {
            MasksList = new List<string>();
            MasksList = MasksOfArchive.Split('#').ToList();
        }
       
        static int FindFilesByMask()
        {
            FindFilesList = new List<string>();
            foreach (string mask in MasksList)
            {
                List<string> fn = Directory.GetFiles(ArchivePath,mask).ToList();
                FindFilesList.AddRange(fn);
            }
            
            return FindFilesList.Count();
        }

        static bool CreateFileWithArhContetnt()
        {
            try
            {
                if (arhContent.Count > 0)
                {
                    using (StreamWriter sw = new StreamWriter(pathToFileList))
                    {
                        foreach (string fn in arhContent)
                        {
                            Console.WriteLine(fn);
                            sw.WriteLine(fn);
                        }
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }

        }

        static int CountFilesInArchive(string arcName)
        {
            arhContent = new List<string>();
            using (SevenZipFormat Format = new SevenZipFormat(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "7z.dll")))
            {
                IInArchive Archive = Format.CreateInArchive(SevenZipFormat.GetClassIdFromKnownFormat(KnownSevenZipFormat.Arj));
                if (Archive == null)
                {
                    return 0;
                }
                try
                {
                    using (InStreamWrapper ArchiveStream = new InStreamWrapper(File.OpenRead(arcName)))
                    {
                        ulong checkPos = 32 * 1024;
                        if (Archive.Open(ArchiveStream, ref checkPos, null) != 0) Console.WriteLine("Error!!!");
                        uint Count = Archive.GetNumberOfItems();
                        for (uint i = 0; i < Count; i++)
                        {
                            PropVariant Name = new PropVariant();
                            Archive.GetProperty(i, ItemPropId.kpidPath, ref Name);
                            arhContent.Add(Name.GetObject().ToString());
                        }
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(Archive);
                }
            }
            return arhContent.Count;
        }

        static void GetTotalFilesInArchive()
        {
            FindFilesByMask();
            int totalFilesInArc = 0;
            foreach (string archiveName in FindFilesList)
            {
                totalFilesInArc += CountFilesInArchive(archiveName);
            }
            TotalFiles = totalFilesInArc;
        }

        static int PrepareArhContent()
        {
            string archiveName = "";
            arhContent = new List<string>();
            using (SevenZipFormat Format = new SevenZipFormat(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "7z.dll")))
            {
                IInArchive Archive = Format.CreateInArchive(SevenZipFormat.GetClassIdFromKnownFormat(KnownSevenZipFormat.Arj));
                if (Archive == null)
                {
                    return 0;
                }
                try
                {
                    using (InStreamWrapper ArchiveStream = new InStreamWrapper(File.OpenRead(archiveName)))
                    {
                        ulong checkPos = 32 * 1024;
                        if (Archive.Open(ArchiveStream, ref checkPos, null) != 0) Console.WriteLine("Error!!!");
                        //Console.Write("Archive: ");
                        //Console.WriteLine(archiveName);
                        uint Count = Archive.GetNumberOfItems();
                        for (uint i = 0; i < Count; i++)
                        {
                            PropVariant Name = new PropVariant();
                            Archive.GetProperty(i, ItemPropId.kpidPath, ref Name);
                            arhContent.Add(Name.GetObject().ToString());
                            //Console.Write(i);
                            //Console.Write(' ');
                            //Console.WriteLine(Name.GetObject());
                        }
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(Archive);
                }
            }
            return arhContent.Count;
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


        #region DO CONNECT

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

        

        #endregion DO CONNECT


        #region IArchiveExtractCallBack

        class ArchiveCallback : IArchiveExtractCallback
        {
            private uint FileNumber;
            private string FileName;
            private OutStreamWrapper FileStream;

            public ArchiveCallback(uint fileNumber, string fileName)
            {
                this.FileNumber = fileNumber;
                this.FileName = fileName;
            }

            


            public void SetTotal(ulong total)
            {

            }

            public void SetCompleted(ref ulong completeValue)
            {

            }

            public int GetStream(uint index, out ISequentialOutStream outStream, AskMode askExtractMode)
            {
                if ((index == FileNumber) && (askExtractMode == AskMode.kExtract))
                {
                    string FileDir = Path.GetDirectoryName(FileName);
                    if (!string.IsNullOrEmpty(FileDir))
                        Directory.CreateDirectory(FileDir);
                    FileStream = new OutStreamWrapper(File.Create(FileName));

                    outStream = FileStream;
                }
                else
                    outStream = null;

                return 0;
            }

            public void PrepareOperation(AskMode askExtractMode)
            {

            }

            public void SetOperationResult(OperationResult resultEOperationResult)
            {
                FileStream.Dispose();
                Console.WriteLine(resultEOperationResult);
            }

            #endregion
        }
    }
}
