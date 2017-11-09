using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FillJournal
{
    class Program
    {
        const string LOG_FILE_MASK = "*.log";
        const string KEY_NAME_TMP_PATH_FOR_LOG = "TMP_PATH_FOR_LOG";

        private static string tmpFolderPath;
        private static string tmpFolderName; //HHmmssfff
        private static string[] logFilesList;
        private static string pathToLogFiles;

        public static string[] LogFilesList
        {
            get
            {
                return logFilesList;
            }
            set
            {
                logFilesList = value;
            }
        }
        static void Main(string[] args)
        {
            pathToLogFiles = @"D:\Projects\CB\Alternative\Example\2017.10.25\";
            ReadConfiguration();
            MoveLogFiles();
            //CreateTmpFolder();
            Console.WriteLine(tmpFolderName);
        }
#region Move log files to tmp directory

        private static void ReadConfiguration()
        {
            try
            {
                var appSettings = ConfigurationManager.AppSettings;
                if (appSettings.Count == 0)
                {
                    Console.WriteLine("Не указана временный каталог для обработки файлов.");
                }
                else
                {
                    tmpFolderPath = appSettings[KEY_NAME_TMP_PATH_FOR_LOG];
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static void CreateLogFilesList()
        {
            LogFilesList = Directory.GetFiles(pathToLogFiles, LOG_FILE_MASK);
        }

        private static bool CreateTmpFolder()
        {
            
            try
            {
                DirectoryInfo dirInfo = new DirectoryInfo(tmpFolderPath);
                if (dirInfo.Exists)
                {
                    string folder = DateTime.Now.ToString("HHmmssfff");
                    dirInfo.CreateSubdirectory(folder);
                    tmpFolderName = Path.Combine(tmpFolderPath, folder);
                    Console.WriteLine(tmpFolderName);
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return Directory.Exists(tmpFolderName);
        }

        private static void MoveLogFileForProcessing(string filePath)
        {
            try
            {
                if (filePath == null)
                {
                    throw new ArgumentNullException(nameof(filePath));
                }
                FileInfo fileLogInfo = new FileInfo(filePath);
                string destinationFileName = Path.Combine(tmpFolderName, Path.GetFileName(filePath));
                fileLogInfo.MoveTo(destinationFileName);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public static void MoveLogFiles()
        {
            CreateLogFilesList();
            CreateTmpFolder();
            foreach (string fileName in LogFilesList)
            {
                MoveLogFileForProcessing(fileName);
            }
        }
        #endregion

        #region Process log file

        private void ReadLogFile(string fileName)
        {

        }


        #endregion



    }
}
