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
        const string FILE_NAME = "D:\\Projects\\CB\\Alternative\\Example\\IZVTUB\\00001.xml";


        static void Main(string[] args)
        {
            ConnectToDataBase();
            //ReadXml("D:\\Projects\\CB\\Alternative\\Example\\IZVTUB\\IZVTUB_AFN_3510123_MIFNS00_20170918_00003.xml");
            InsertXml("D:\\Projects\\CB\\Alternative\\Example\\IZVTUB\\00001.xml");
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
