﻿using System;
using System.Data;
using System.Configuration;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using NLog;

namespace DoRelation
{
    class Program
    {
        const string ERR_NOT_ENOUGH_PARAMS = "Не достаточно параметров для запуска";
        const string ERR_CAN_NOT_CONVERT_DATA = "Не возможно преобразовать дату";
        const string FILE_MASK = "*.list";

        const string SELECT_FILE_ID = "select top(1) ID from TFILES  where NAME ='{0}' and DATE = '{1}' and TIME <= '{2}'";
        const string TIME_PATERN = "HHmmss";
        const string DATE_PATERN = "yyyyMMdd";
        const string SP_ADDRELATION = "[dbo].[SP_ADDRELATION]";

        private static Logger logger = LogManager.GetCurrentClassLogger();
        static string SourcePath;
        static DateTime FileDate;
        static string DbConnectionString;
        static SqlConnection DbSqlConnection;
        static int totalListFilesCounter;
        static int totalRelationsCounter;
        static int totalRecordsAdded;
        static int totalErrors;

        public static List<string> ArjFilesList;
        public static Dictionary<string,List<string>> InitFileList; //Первоначальный список, состоящий из имени архива и его содержимого
        public static Dictionary<string, int> ArjWithIdList;
        public static Dictionary<int, List<int>> FileRelationsDict;


        static void Main(string[] args)
        {
            Console.WriteLine("Работает программа DoRelations");
            //logger.Info("Запуск программы DoRelations");
            bool hasArgs = (args.Length == 2);
            if (!hasArgs)
            {
                Console.WriteLine(ERR_NOT_ENOUGH_PARAMS);
                logger.Error(ERR_NOT_ENOUGH_PARAMS);
                Console.ReadLine();
                return;
            }
            //Получаем аргументы

            if (!DateTime.TryParseExact(args[0], DATE_PATERN, null, System.Globalization.DateTimeStyles.None, out FileDate))
            {
                Console.WriteLine(ERR_CAN_NOT_CONVERT_DATA);
                return;
            }
            SourcePath = args[1];
            
            //Создаем все Lists and Dictionaries
            InitList();
            FillArjDictWithId();
            Console.WriteLine("Всего файлов *.list");
            Console.WriteLine("в директории {0} - {1}", SourcePath, totalListFilesCounter);
            Console.WriteLine("Количество записей в файлах *.list - {0}", totalRelationsCounter);
            Console.WriteLine("Связей добавлено - {0}", totalRecordsAdded);
            Console.WriteLine("Произошло ошибок во время добавления - {0}", totalErrors);

            //Console.ReadLine();
        }

        private static void InitList()
        {
            ConnectToDataBase();
            ArjFilesList = new List<string>();
            InitFileList = new Dictionary<string, List<string>>();
            ArjWithIdList = new Dictionary<string, int>();
            FileRelationsDict = new Dictionary<int, List<int>>();
            totalListFilesCounter = 0;
            totalRelationsCounter = 0;
            totalRecordsAdded = 0;
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


        #region WorkWithArj
        /// <summary>
        /// Преобразование строки во время
        /// </summary>
        /// <param name="listTime">строковое значение времени</param>
        /// <param name="outTime">Возвращаемый параметр  типа DateTime</param>
        /// <returns>Результат конвертации</returns>
        private static void TimeParser(string listTime, out DateTime outTime )
        {
            outTime = DateTime.ParseExact(listTime, TIME_PATERN, CultureInfo.InvariantCulture);    
        }

        /// <summary>
        /// Получает имя архива из имени файла с расширением list 
        /// </summary>
        /// <param name="fName">Имя файла листа</param>
        /// <param name="delimSymbol">Символ разделяющий  части файла </param>
        /// <param name="listTime">Возвращаемый параметр строка времени</param>
        /// <returns>Возвращает имя архива 
        /// Например из AFN_MIFNS00_3510123_20170829_00001.ARJ.114253.list
        /// Вернется значение AFN_MIFNS00_3510123_20170829_00001.ARJ
        /// </returns>
        private static string GetParentFileName(string fName, char delimSymbol, out string listTime)
        {
            string fn = Path.GetFileName(fName);
            string[] fileParts = fn.Split(delimSymbol);
            if (fileParts.Length > 2)
            {
                DateTime ft;
                TimeParser(fileParts[2], out ft);
                listTime = ft.ToString("HH:mm:ss");
                return fileParts[0] + delimSymbol + fileParts[1];
            }
            else
            {
                listTime = "";
                return "";
            }
        }

        private static void FillArjDictWithId()
        {
            string sqlGetParentId;
            int parentFileId = 0;
            string[] files = Directory.GetFiles(SourcePath, FILE_MASK);
            foreach (string file in files)
            {
                var childFilesIdList = new List<int>();
                string listTime;
                string parentOriginalName = GetParentFileName(Path.GetFileName(file), '.',out listTime);
                sqlGetParentId = SELECT_FILE_ID.Replace("{0}", parentOriginalName);
                sqlGetParentId = sqlGetParentId.Replace("{1}", FileDate.ToString(DATE_PATERN));
                sqlGetParentId = sqlGetParentId.Replace("{2}", listTime);
                try
                {
                    parentFileId = 0;
                    SqlCommand sqlcmd = new SqlCommand(sqlGetParentId, DbSqlConnection);
                    SqlDataReader reader = sqlcmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            parentFileId = (int)reader["ID"];
                        }//end while
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                if (parentFileId > 0)
                {
                    using (StreamReader sr = new StreamReader(file, System.Text.Encoding.Default))
                    {
                        childFilesIdList.Clear();
                        string fileNameLine;
                        while ((fileNameLine = sr.ReadLine()) != null)
                        {
                            int childFileId = 0;
                            string sqlGetChildId = SELECT_FILE_ID.Replace("{0}", fileNameLine);
                            sqlGetChildId = sqlGetChildId.Replace("{1}", FileDate.ToString(DATE_PATERN));
                            sqlGetChildId = sqlGetChildId.Replace("{2}", listTime);
                            try
                            {
                                SqlCommand sqlcmd = new SqlCommand(sqlGetChildId, DbSqlConnection);
                                SqlDataReader reader = sqlcmd.ExecuteReader();
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        childFileId = (int)reader["ID"];
                                    }//end while
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                            }
                            if (childFileId > 0)
                            {
                                childFilesIdList.Add(childFileId);
                            }
                        }
                    }
                    if (parentFileId > 0 && childFilesIdList.Count > 0)
                    {
                        try
                        {
                            FileRelationsDict.Add(parentFileId, childFilesIdList);
                        }
                        catch(Exception e)
                        {
                            logger.Error(e.Message);
                        }
                        
                    }
                    AddRelation();
                    FileRelationsDict.Clear();
                }
                totalListFilesCounter++;
            }
        }

        private static void AddRelation()
        {
            int totalRelationsInList = 0; //Общее количество связей св файлах *.List
            int recordsAded = 0; //Количество успешно добавленных новых связей
            int err = 0; // Количество ошибок  при добавлении связи
            foreach (int key in FileRelationsDict.Keys)
            {
                List<int> a = new List<int>();
                a = FileRelationsDict[key];
                foreach (int i in a)
                {
                    try
                    {
                        SqlCommand sqlcmd = new SqlCommand(SP_ADDRELATION, DbSqlConnection);
                        sqlcmd.CommandType = CommandType.StoredProcedure;
                        SqlParameter prmIdFile = new SqlParameter
                        {
                            ParameterName = "@FileId",
                            Value = i
                        };
                        SqlParameter prmIdParentFile = new SqlParameter
                        {
                            ParameterName = "@ParentId",
                            Value = key
                        };
                        SqlParameter prmType = new SqlParameter
                        {
                            ParameterName = "@type",
                            Value = "arj"
                        };
                        SqlParameter returnParameter = new SqlParameter("@returnVal",SqlDbType.Int);
                        returnParameter.Direction = ParameterDirection.ReturnValue;

                        sqlcmd.Parameters.Add(prmIdFile);
                        sqlcmd.Parameters.Add(prmIdParentFile);
                        sqlcmd.Parameters.Add(prmType);
                        sqlcmd.Parameters.Add(returnParameter);

                        sqlcmd.ExecuteNonQuery();

                        int recAdded =Convert.ToInt32(returnParameter.Value);
                        //Console.WriteLine("rec added - {0}", recAdded);
                        recordsAded += recAdded;
                        totalRelationsInList++;
                        //Console.WriteLine("ParentId = {0}, ChildId = {1} Type = {2} ",key,i,"arj");
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(e.Message);
                        err++;
                    }
                }
            }
            totalRelationsCounter += totalRelationsInList;
            totalRecordsAdded += recordsAded;
            totalErrors += err;
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        #endregion WorkWithArj

    }
}
