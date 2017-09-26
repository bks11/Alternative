using System;
using System.Configuration;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Data.SqlClient;

namespace KwitParser
{
    class Program
    {
        static string DbConnectionString;
        static SqlConnection DbSqlConnection;
        const string INSERT_XML = "INSERT INTO TTESTXML (XMLTEXT) SELECT * FROM OPENROWSET(BULK '{0}',SINGLE_BLOB) AS XMLTEXT";
        const string INSERT_TEXT = "INSERT INTO TTESTXML (KWITTEXT) SELECT * FROM OPENROWSET(BULK '{0}',SINGLE_BLOB) AS KWITTEXT";
        const string INSERT_XML_DATA = "INSERT INTO [dbo].[TXML_DATA]([TFILE_ID],[TYPE],[NAME],[VALUE],[TXML_DATA_ID])VALUES(@fileId,@elementtype,@elementname,@elemvalue,@parentId); set @id = SCOPE_IDENTITY()";

        const string FILE_NAME = "D:\\Projects\\CB\\Alternative\\Example\\IZVTUB\\00008.xml";


        static void Main(string[] args)
        {
            ConnectToDataBase();
            LoadXmlFile();
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

        static void LoadXmlFile()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(FILE_NAME);
            XmlNode docNode = doc.DocumentElement;
            XmlPassing(-1, docNode);
        }


        static void XmlPassing(int recId, XmlNode rootNode)
        {
            int newId  = InsertRootNode(recId, rootNode, 3);
            if (rootNode.HasChildNodes)
            {
                rootNode = rootNode.FirstChild;
                while (rootNode != null)
                {
                    XmlPassing(newId, rootNode);
                    rootNode = rootNode.NextSibling;
                }
            }
        }
    }
}
