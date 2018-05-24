using System;
using System.Configuration;
using System.Data;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Data.SqlClient;

/// <summary>
///Занесение элементов XML  в таблицу 
/// </summary>
namespace KwitParser
{
    class Program
    {
        private static string DbConnectionString;
        private static string KwitFilePath { get; set; } //Путь к файлам квитовок. На пример:  c:\files.arh\2017\2017.09\2017.09.26\
        private static DateTime FileDate { get; set; }   //Дата а которую данный файл обрабаывается

        private static int totalRec = 0;
        private static List<string> filesList;              //Список файлов согласно списка масок fileMasks
        private static Dictionary<int,string> filesId;      //Список ID файлов и имени из таблицы TFiles
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
            
            FillXmlData();
            Console.WriteLine("Работает утилита KwitParser");
            Console.WriteLine("Файлов по маске {0} - {1}", String.Join(", ", fileMasks.ToArray()), filesList.Count);
            Console.WriteLine("Новых файлов - {0}", filesId.Count);
            Console.WriteLine("Добавлено записей в таблицу TXML_DATA - {0}", totalRec);
            //Console.ReadLine();
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
            } catch (Exception e)
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
        /// <summary>
        /// Генерация запроса по добавлению данных из XML
        /// </summary>
        /// <param name="newRecordId">Идентификатор родительской ноды в БД. Для корневой ноды значение -1</param>
        /// <param name="docNode">XmlNode - текщая нода, для которой формируется запрос</param>
        /// <param name="file_Id">Идентификатор обрабатываемого файла из TFILES</param>
        /// <returns>Идентификатор добавленной ноды  из TXML_DATA</returns>
        static int InsertRootNode(int newRecordId, XmlNode docNode, int file_Id)
        {
            SqlCommand sqlcmd = new SqlCommand();
            try
            {
                sqlcmd.CommandText = INSERT_XML_DATA;
                sqlcmd.Connection = DbSqlConnection;

                SqlParameter fileId = new SqlParameter("@fileId", file_Id);
                sqlcmd.Parameters.Add(fileId);

                SqlParameter elementtype = new SqlParameter("@elementtype", docNode.NodeType.ToString());
                sqlcmd.Parameters.Add(elementtype);

                SqlParameter elementName = new SqlParameter("@elementname", docNode.Name);
                sqlcmd.Parameters.Add(elementName);

                SqlParameter elemValue = new SqlParameter("@elemvalue", "");
                if (docNode.HasChildNodes)
                {
                    foreach (XmlNode childNode in docNode.ChildNodes)
                    {
                        if (childNode.NodeType == XmlNodeType.Text)
                        {
                            elemValue.Value += childNode.Value;
                            //Console.WriteLine("Node Name {0}; Node value - {1}", docNode.Name, childNode.Value);
                        }
                    }
                }
                sqlcmd.Parameters.Add(elemValue);

                SqlParameter parentId = new SqlParameter("@parentId", newRecordId);
                sqlcmd.Parameters.Add(parentId);

                SqlParameter newId = new SqlParameter
                {
                    ParameterName = "@id",
                    SqlDbType = SqlDbType.Int,
                    Direction = ParameterDirection.Output
                };
                sqlcmd.Parameters.Add(newId);

                int number;
                if (Convert.ToString(docNode.Name) != "#text")
                {
                    number = sqlcmd.ExecuteNonQuery();
                    newRecordId = (int)newId.Value;
                    totalRec++;
                    //Console.WriteLine("Parent Id in the table - {0}", newRecordId);
                    //Console.WriteLine(number);
                }                
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            //Console.WriteLine("Node type - {0}", docNode.NodeType.ToString());
            //Console.WriteLine("Node name - {0}", docNode.Name);

            if (docNode.NodeType != XmlNodeType.Text && docNode.Attributes.Count >0)
            {
                foreach (XmlAttribute attr in docNode.Attributes)
                {
                    sqlcmd.Parameters.Clear();
                    SqlParameter fileId = new SqlParameter("@fileId", file_Id);
                    sqlcmd.Parameters.Add(fileId);
        
                    SqlParameter elementtype = new SqlParameter("@elementtype", attr.NodeType.ToString());
                    sqlcmd.Parameters.Add(elementtype);

                    SqlParameter elementName = new SqlParameter("@elementname", attr.Name);
                    sqlcmd.Parameters.Add(elementName);

                    SqlParameter elemValue = new SqlParameter("@elemvalue", attr.Value);
                    sqlcmd.Parameters.Add(elemValue);

                    SqlParameter parentId = new SqlParameter("@parentId", newRecordId);
                    sqlcmd.Parameters.Add(parentId);

                    SqlParameter newId = new SqlParameter
                    {
                        ParameterName = "@id",
                        SqlDbType = SqlDbType.Int,
                        Direction = ParameterDirection.Output
                    };
                    sqlcmd.Parameters.Add(newId);
                    try
                    {
                        sqlcmd.ExecuteNonQuery();
                        totalRec++;
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    //Console.WriteLine("Root node attribute name - {0}; Root node attribut value - {1}", attr.Name, attr.Value);
                }
            }
            return newRecordId;
        }
        
        /// <summary>
        /// Проход по струтуре XML файла
        /// </summary>
        /// <param name="recId">Идентификатор родительской ноды из таблицы TXML_DATA
        /// При первоначальном вызове для корневой ноды значение -1
        /// </param>
        /// <param name="rootNode"> XmlNode - Обрабатываемая нода </param>
        static void XmlPassing(int recId, XmlNode rootNode, int kwitFileId)
        {
            int newId  = InsertRootNode(recId, rootNode, kwitFileId);
            if (rootNode.HasChildNodes)
            {
                rootNode = rootNode.FirstChild;
                while (rootNode != null)
                {
                    XmlPassing(newId, rootNode, kwitFileId);
                    rootNode = rootNode.NextSibling;
                }
            }
        }
        
        /// <summary>
        ///Подготовка списка файлов по маске для внесения данных о результате квитовки 
        /// </summary>
        /// <param name="FileMask">Путь и маска файлов для обработки</param>
        /// <returns>True -  если в список заполнен файлами; False - если список пустой</returns>
        static bool PrepareKwitList()
        {
            filesList = new List<string>();
            foreach (string fm in fileMasks)
            {
                string[] fl = Directory.GetFiles(KwitFilePath, fm); //fl - File list   список  найденных файлов  согласно маски fm (File Mask)
                foreach (string f in fl)
                {
                    filesList.Add(f);
                }

            }
            //Тернарная операция
            return filesList.Count > 0 ? true : false;
        }

        /// <summary>
        /// Заполняем словарь 
        /// </summary>
        /// <returns></returns>
        static int GetFileIdByName()
        {
            filesId     = new Dictionary<int, string>();
            notInTFiles = new List<string>();
            //Подготовка списка ID  файлов из TFiles(TFiles - имя таблицы) выбираем файлы по имени, которые есть в этой таблицы, что бы получить ID
            foreach (string fn in filesList)
            {
                string fileName = Path.GetFileName(fn);
                SqlCommand selectFileId = new SqlCommand(SELECT_FILE_ID, DbSqlConnection);
                SqlParameter file_Name = new SqlParameter("@filename", fileName);
                selectFileId.Parameters.Add(file_Name);
                SqlParameter file_Date = new SqlParameter("@filedate", FileDate);
                selectFileId.Parameters.Add(file_Date);
                try
                {
                    SqlDataReader reader = selectFileId.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            int id = (int)reader["ID"];
                            filesId.Add(id,fn);
                        }//end while
                    }
                    else
                    {
                        notInTFiles.Add(fn);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            return 0;
        }

        static void FillXmlData()
        {
            PrepareKwitList();
            GetFileIdByName();
            foreach (int fileIdKey in filesId.Keys)
            {
                string kwitFileName  = filesId[fileIdKey];
                XmlDocument doc = new XmlDocument();
                doc.Load(kwitFileName);
                XmlNode docNode = doc.DocumentElement;
                XmlPassing(-1, docNode, fileIdKey);
            }
        }        
    }
}
