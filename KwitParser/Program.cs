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

        //const string FILE_NAME = "D:\\Projects\\CB\\Alternative\\Example\\IZVTUB\\00007.xml";
        const string FILE_NAME = "D:\\Projects\\CB\\Alternative\\Example\\IZVTUB\\00008.xml";


        static void Main(string[] args)
        {
            ConnectToDataBase();
            //XmlParsing();
            InsertRootNode(1);
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

        static void InsertText(string kwitFileName)
        {
            string ins = INSERT_TEXT.Replace("{0}", FILE_NAME);
            Console.WriteLine(ins);
            try
            {
                SqlCommand sqlcmd = new SqlCommand(ins, DbSqlConnection);
                int number = sqlcmd.ExecuteNonQuery();
                Console.WriteLine(number);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        static void ReaderXmlParsing()
        {
            XmlTextReader reader = null;
            try
            {
                reader = new XmlTextReader(FILE_NAME);
                reader.WhitespaceHandling = WhitespaceHandling.None;
                while (reader.Read())
                {
                    if (reader.HasAttributes)
                    {
                        for (int i = 0; i < reader.AttributeCount; i++)
                        {
                            reader.MoveToAttribute(i);
                            Console.WriteLine("Attribute name - {0}; Attribute value - {1}", reader.Name, reader.Value);
                        }
                    }
                    Console.WriteLine(reader.NodeType.ToString());
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            Console.WriteLine("{0}",reader.Name);
                            break;
                        case XmlNodeType.Text:
                            Console.WriteLine("Node name {0} - Node value {1}", reader.Name, reader.Value);
                            break;
                        //case XmlNodeType.Attribute:
                        //    for(int i = 0; i< reader.AttributeCount;i++)
                        //    {
                        //        reader.MoveToAttribute(i);
                        //        Console.WriteLine("Attribute name - {0}; Attribute value - {1}", reader.Name, reader.Value);
                        //    }
                        //    break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }
        }



        static void InsertRootNode(int file_Id)
        {
            int newRecordId;
            SqlCommand sqlcmd = new SqlCommand();
            XmlDocument doc = new XmlDocument();
            doc.Load(FILE_NAME);
            XmlElement docNode = doc.DocumentElement;

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
                if (docNode.HasChildNodes && docNode.FirstChild.NodeType == XmlNodeType.Text)
                {
                    elemValue.Value = docNode.FirstChild.Value;
                    Console.WriteLine("Root node value - {0}", docNode.FirstChild.Value);
                }
                sqlcmd.Parameters.Add(elemValue);

                SqlParameter parentId = new SqlParameter("@parentId", -1);
                sqlcmd.Parameters.Add(parentId);

                SqlParameter newId = new SqlParameter
                {
                    ParameterName = "@id",
                    SqlDbType = SqlDbType.Int,
                    Direction = ParameterDirection.Output
                };
                sqlcmd.Parameters.Add(newId);


                int number = sqlcmd.ExecuteNonQuery();

                newRecordId = (int)newId.Value;
                Console.WriteLine("Parent Id in the table - {0}",newRecordId);
                Console.WriteLine(number);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Console.WriteLine("Root Node type - {0}", docNode.NodeType.ToString());
            Console.WriteLine("Root Node name - {0}", docNode.Name);

            if (docNode.HasAttributes)
            {
                foreach (XmlAttribute attr in docNode.Attributes)
                {
                    sqlcmd.Parameters.Clear();
                    Console.WriteLine("Root node attribute name - {0}; Root node attribut value - {1}", attr.Name, attr.Value);
                }
            }
            if (docNode.HasChildNodes && docNode.FirstChild.NodeType == XmlNodeType.Text)
            {
                Console.WriteLine("Root node value - {0}", docNode.FirstChild.Value);
            }
            else
            {
                Console.WriteLine("Root node value - {0}", "NO VALUE");
            }
        }

        static void XmlParsing()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(FILE_NAME);
            XmlElement docNode = doc.DocumentElement;
            
            Console.WriteLine("Root Node name - {0}", docNode.Name);

            if (docNode.HasAttributes)
            {
                foreach (XmlAttribute attr in docNode.Attributes)
                {
                    Console.WriteLine("Root node attribute name - {0}; Root node attribut value - {1}", attr.Name, attr.Value);
                }
            }
            if (docNode.HasChildNodes && docNode.FirstChild.NodeType == XmlNodeType.Text)
            {
                Console.WriteLine("Root node value - {0}", docNode.FirstChild.Value);
            }
            else
            {
                Console.WriteLine("Root node value - {0}", "NO VALUE");
            }
            
        }


        static void XmlPassing(XmlNode rootNode)
        {
            Console.WriteLine("Node name : {0}", rootNode.ParentNode.Name);
            if (rootNode.HasChildNodes)
            {
                rootNode = rootNode.FirstChild;
                while (rootNode != null)
                {
                    if (rootNode.Attributes!=null)
                    {
                        foreach (XmlAttribute attr in rootNode.Attributes)
                        {
                            Console.WriteLine("Node = {0}; Attribute name = {1}; Attribute value = {2}",rootNode.Name, attr.Name, attr.Value);
                        }
                    }
                    if (rootNode.NodeType == XmlNodeType.Text)
                    {
                        Console.WriteLine("Text = {0}", rootNode.Value);
                    }
                    else
                    {
                        Console.WriteLine("Node name : {0}",rootNode.Name);
                    }
                    XmlPassing(rootNode);
                    rootNode = rootNode.NextSibling;
                }
            }
        }

        static void SeparateXml()
        {
            XmlDocument kwit = new XmlDocument();
            kwit.Load(FILE_NAME);
            // Get root node
            XmlElement xRoot = kwit.DocumentElement;
            Console.WriteLine(xRoot.Name);
            XmlPassing(xRoot);

            //foreach (XmlElement xNode in xRoot)
            //{
            //    string nodeName = xNode.Name;
            //    Console.WriteLine("{0}", nodeName);
            //    // Get node attributes  and values
            //    foreach (XmlAttribute attr in xNode.Attributes)
            //    {
            //        Console.WriteLine("{0}-{1}",attr.Name,attr.Value);
            //    }
            //}

        }

        static void InsertXml(string kwitFileName)
        {
            //XmlDocument kwit = new XmlDocument();
            //kwit.Load(kwitFileName);
            //string s  = kwit.OuterXml;
            string ins = INSERT_XML.Replace("{0}", FILE_NAME);
            Console.WriteLine(ins);
            try
            {
                SqlCommand sqlcmd = new SqlCommand(ins, DbSqlConnection);
                //SqlParameter fileMaskPrm = new SqlParameter("@XXXML", s);
                //sqlcmd.Parameters.Add(fileMaskPrm);
                
                int number = sqlcmd.ExecuteNonQuery();
                Console.WriteLine(number);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            
        }

        static void ReadXml(string kwitFileName)
        {
            XmlDocument kwit = new XmlDocument();
            kwit.Load(kwitFileName);
            XmlElement xRoot = kwit.DocumentElement;
            foreach (XmlNode xNode in xRoot)
            {
                if (xNode.Attributes.Count > 0)
                {
                    foreach (XmlAttribute att in xNode.Attributes)
                    {
                        Console.WriteLine("{0} - {1}",att.Name,att.Value);
                    }
                }
            }
        }
        
    }
}
