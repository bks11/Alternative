using System;
using System.Configuration;
using System.Data;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using System.Xml;
using System.Data.SqlClient;
using System.Linq;

/// <summary>
///Занесение элементов XML  в таблицу 
/// </summary>
namespace XmlStore
{
    class Program
    {
        static Dictionary<string, Dictionary<string, List<string>>> XmlElementsFromConfigByFileMask;


        private static string DbConnectionString;
        private static string KwitFilePath { get; set; } //Путь к файлам квитовок. На пример:  c:\files.arh\2017\2017.09\2017.09.26\
        private static DateTime FileDate { get; set; }   //Дата а которую данный файл обрабаывается

        private static Dictionary<string, List<string>> cfgStore;

        private static int totalRec = 0;
        private static List<string> filesList;              //Список файлов согласно списка масок fileMasks
        private static Dictionary<int, string> filesId;      //Список ID файлов и имени из таблицы TFiles
        private static Dictionary <string, string[]> configStore;
        private static List<string> fileMasks;              //Список масок файлов, которые хранятся в конфигурационном файле
        private static List<string> notInTFiles;
        private static SqlConnection DbSqlConnection;

        const string DATE_PATERN = "yyyyMMdd";
        const string INSERT_XML = "INSERT INTO TTESTXML (XMLTEXT) SELECT * FROM OPENROWSET(BULK '{0}',SINGLE_BLOB) AS XMLTEXT";
        const string INSERT_TEXT = "INSERT INTO TTESTXML (KWITTEXT) SELECT * FROM OPENROWSET(BULK '{0}',SINGLE_BLOB) AS KWITTEXT";
        const string INSERT_XML_DATA = "INSERT INTO [dbo].[TXML_DATA]([TFILE_ID],[TYPE],[NAME],[VALUE],[TXML_DATA_ID])VALUES(@fileId,@elementtype,@elementname,@elemvalue,@parentId); set @id = SCOPE_IDENTITY()";
        const string SELECT_FILE_ID = "SELECT F.ID FROM TFILES F LEFT JOIN TXML_DATA X on X.TFILE_ID  = F.ID WHERE F.NAME = @filename AND F.DATE = @filedate AND X.ID IS NULL";
        const string FILE_NAME = "D:\\Projects\\CB\\Alternative\\Example\\IZVTUB\\00008.xml";


        static void Main(string[] args)
        {
            bool hasArgs = (args.Length == 2);
            string[] testConfig = new string[] { "a*.xml;TIME;NAME_ES;NAME_REC[nRec,test1,test2,test3];NAME[RecId,test4];TEST5;TEST8", "b*.xml;TIME_;NAME_ES_;NAME_REC_[nRec_,test1_,test2_,test3_];NAME_[RecId_,test4_];TEST5_;TEST8_" };
            if (!hasArgs)
            {
                Console.WriteLine("Не достаточно параметров для запуска приложения");
                Console.ReadLine();
                return;
            }
            //Получаем аргументы
            if (!DateTime.TryParseExact(args[0], DATE_PATERN, null, System.Globalization.DateTimeStyles.None, out DateTime fileDate))
            {
                Console.WriteLine("Не возможно преобразовать дату");
                return;
            }
            FileDate = fileDate;

            KwitFilePath = args[1];

            ConnectToDataBase();

            if (fileMasks.Count == 0)
            {
                Console.WriteLine("В конфигурационном файле не указаны маски файлов.");
                Console.ReadLine();
                return;
            }


            SaveNodeStore(testConfig);


            Console.ReadLine();
        }



        private static string[] ParseConfig(string inputString)
        {
            return inputString.Split(';');
        }
        protected static string GetFileMask(string[] configString)
        {
            return configString[0];
        }



        private static void SaveNodeStore(string[] configString)
        {
            cfgStore = new Dictionary<string, List<string>>();
            List<string> element = new List<string>();
            List<List<string>> attributes = new List<List<string>>();
            if (configString.Length > 0)
            {
                for (int i = 1; i < configString.Length; i++)
                {
                    if (configString[i].IndexOf('[') < 0)
                    {
                        element.Add(configString[i]);
                        cfgStore.Add(configString[i], new List<string>());
                    }
                    else
                    {
                        int startIndex = configString[i].IndexOf('[');
                        int endIndex = configString[i].IndexOf(']');

                        element.Add(configString[i].Substring(0, startIndex));

                        string attr = configString[i].Substring(startIndex + 1, endIndex - (startIndex + 1));
                        List<string> attrib = attr.Split(',').ToList();
                        cfgStore.Add(configString[i].Substring(0, startIndex), attrib);
                    }
                }
            }
        }

        private static void WorkWithDictionary()
        {
            foreach (string s in cfgStore.Keys)
            {
                Console.WriteLine("Element - {0}", s);
                foreach (string a in cfgStore[s])
                {
                    Console.WriteLine("    Attribute - {0}", a);
                }
            }
        }

        private static void GetParametersFromConfigString(string[] configLinesForFile)
        {
            XmlElementsFromConfigByFileMask = new Dictionary<string, Dictionary<string, List<string>>>();

            foreach (string configLineForFile in configLinesForFile)
            {
                string xmlFileMask;
                List<string> configLineParts = configLineForFile.Split(';').ToList();
                xmlFileMask = configLineParts[0];

                var XmlElementsFromConfig = new Dictionary<string, List<string>>();
                List<List<string>> attributes = new List<List<string>>();
                if (configLineParts.Count > 0)
                {
                    for (int i = 1; i < configLineParts.Count; i++)
                    {
                        if (configLineParts[i].IndexOf('[') < 0)
                        {
                            XmlElementsFromConfig.Add(configLineParts[i], new List<string>());
                        }
                        else
                        {
                            int startIndex = configLineParts[i].IndexOf('[');
                            int endIndex = configLineParts[i].IndexOf(']');

                            string tagName = configLineParts[i].Substring(0, startIndex);

                            string attrString = configLineParts[i].Substring(startIndex + 1, endIndex - (startIndex + 1));
                            List<string> tagAttributes = attrString.Split(',').ToList();

                            XmlElementsFromConfig.Add(tagName, tagAttributes);
                        }
                    }
                    XmlElementsFromConfigByFileMask.Add(xmlFileMask, XmlElementsFromConfig);
                }
            }
        }



        #region DO CONNECT

        /// <summary>
        /// Получение ConnectionString и маски файлов квитовок
        /// </summary>
        private static void ReadApplicationSettings()
        {
            fileMasks = new List<string>();
            //Получаем строку соединения из конфигурационного файла
            try
            {
                ConnectionStringSettings CBUtils = ConfigurationManager.ConnectionStrings["CBUtils"];
                if (CBUtils != null)
                {
                    DbConnectionString = CBUtils.ConnectionString;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }
            try
            {
                var appSettings = ConfigurationManager.AppSettings;
                if (appSettings.Count == 0)
                {
                    Console.WriteLine("Не указаны маски файлов.");
                }
                else
                {
                    foreach (var key in appSettings.AllKeys)
                    {
                        fileMasks.Add(appSettings[key]);
                        //Console.WriteLine("Key: {0} Value: {1}", key, appSettings[key]);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

        }
        //Соединение с БД
        private static bool ConnectToDataBase()
        {
            ReadApplicationSettings();
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
      
    }
}
