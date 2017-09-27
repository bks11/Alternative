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

namespace KwitParser
{
    class Program
    {
        private static string DbConnectionString;
        private static string FileMask { get; set; }     //Путь и маска файла квитовок. На пример:  c:\files.arh\2017\2017.09\2017.09.26\IZVTUB*.*
        private static DateTime FileDate { get; set; }   //Дата а которую данный файл обрабаывается

        private static string[] filesList;              //Список файлов согласно маски fileMask
        private static Dictionary<int,string> filesId;
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
            //Получаем аргументы
            //if (!DateTime.TryParseExact(args[0], DATE_PATERN, null, System.Globalization.DateTimeStyles.None, out DateTime fileDate))
            //{
            //    Console.WriteLine("Не возможно преобразовать дату");
            //    return;
            //}
            //FileDate = fileDate;

            //FileMask = args[1];

            FileMask = "D:\\Projects\\CB\\Alternative\\Example\\IZVTUB";
            FileDate = DateTime.Parse("26.09.2017");

            ConnectToDataBase();
            FillXmlData();
            //LoadXmlFile();
            Console.ReadLine();
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
                            Console.WriteLine("Node Name {0}; Node value - {1}", docNode.Name, childNode.Value);
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
                    Console.WriteLine("Parent Id in the table - {0}", newRecordId);
                    Console.WriteLine(number);
                }                
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Console.WriteLine("Node type - {0}", docNode.NodeType.ToString());
            Console.WriteLine("Node name - {0}", docNode.Name);

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
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    Console.WriteLine("Root node attribute name - {0}; Root node attribut value - {1}", attr.Name, attr.Value);
                }
            }
            return newRecordId;
        }
        
        /// <summary>
        /// Загрузка XML файла
        /// </summary>
        static void LoadXmlFile()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(FILE_NAME);
            XmlNode docNode = doc.DocumentElement;
            XmlPassing(-1, docNode, 31306);
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
            string filePath;
            string fileMask;
            filePath = Path.GetFullPath(FileMask);
            fileMask = "IZVTUB*.XML";
            filesList = Directory.GetFiles(filePath,fileMask);
            //Тернарная операция
            return filesList.Length > 0 ? true : false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        static int GetFileIdByName()
        {
            filesId     = new Dictionary<int, string>();
            notInTFiles = new List<string>();
            //Подготовка списка ID  файлов из TFiles
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
