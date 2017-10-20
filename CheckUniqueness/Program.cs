using System;
using System.Configuration;
using System.Collections.Generic;
using NLog;
using System.Data.SqlClient;
using System.Data;
using System.IO;

namespace CheckUniqueness
{
	class Program
	{
		private const string SQL_COUNT_FILE = "SELECT DISTINCT NAME FROM TFILES WHERE (NAME in ('{0}')) AND DATE = '{1}'";
        private const string SQL_GET_FILE_VERSION = "SELECT COUNT(F.NAME) AS VERSION FROM TFILES F WHERE F.NAME = '{0}' AND F.DATE = '{1}'";
		private const string VALUES_FOR_INSERT_NEW = "('{0}','{1}','{2}')";
		private const string VALUES_FOR_INSERT_DOUBLE = "('{0}','{1}','{2}', {3})";
		private const string SQL_INSERT_NEW_VALUES = "INSERT INTO TFILES (NAME,DATE,TIME) VALUES ";
		private const string SQL_INSERT_DOUBLE_VALUES = "INSERT INTO TFILES (NAME,DATE,TIME,VERSION) VALUES ";
        private const int    SIZE_INSERT_SQL_PART = 99;
        private const string DATE_PATERN = "yyyyMMdd";


        private static List<string> Inserts;                // Хранилище скриптов для Insert, 
                                                            // разделенная по SQL_INSERT_DOUBLE_VALUES записей
        private static string CBUtilsConnectionString;      // ConnectionString для подключения к BD, хранится в конфиг. файле 
		private static SqlConnection sqlConnection;         // Подключение к БД
		private static List<string> allFilesList;           // Хранилище списка файлов оригинал
		private static List<string> nonUniqueFiles;         // Хранилище списка файлов дубликатов
        private static DateTime FileDate;                   // Параметр для заполнения поля DATE в таблице TFILE 
        private static string InputFileList;                // Параметр путь к файлу со списком 
        private static string OutputFileList;               // Параметр путь к файлу со списком дублей

        private static Logger logger; 

        static int Main(string[] args)
		{
            logger = LogManager.GetCurrentClassLogger();
            logger.Trace("Проверяем наличие параметров для запуска");
            bool hasArgs = (args.Length == 3);
			if (!hasArgs)
			{
				Console.WriteLine("Не досаточно аргументов для запуска программы");
                logger.Error("Не досаточно аргументов для запуска программы");
                Console.WriteLine("Первый параметр - Дата файла");
				Console.WriteLine("Второй параметр - Имя файла со списком (полный путь к файлу) или -single или -repeat");
				Console.WriteLine("Третий параметр - Имя файла со списком дублей (полный путь) если второй параметр -single или -repeat, то имя файла для проверки");
				Console.ReadLine();
				return 1;
			}

			if (!DateTime.TryParseExact(args[0], DATE_PATERN, null,System.Globalization.DateTimeStyles.None, out FileDate))
			{
				Console.WriteLine("Не возможно преобразовать дату");
                logger.Error("Не возможно преобразовать дату");
                return -3;
			}
            logger.Info("Дата, за которую обрабатывается файл {0}", FileDate.ToString());
            InputFileList = args[1];
            logger.Info("Путь к файлу со списком {0}",InputFileList);
			OutputFileList = args[2];
            logger.Info("Путь к файлу со списком дублей {0}", OutputFileList);

            if (!ConnectToDataBase())
			{
				return -2;
			}

			ReadFileList(InputFileList);//Заполнение списка allFilesList
            GenerateNonUniqueList();// Заполнение  списка nonUniqueFiles
            PrepareUniqueList();// Получение списка уникальных имен файлов (удаление дублей из allFilesList)

            int insertedRec = InsertNewFilesInfo(); //Добавление уникальных значений в базу(возвращает количество добавленных данных)
            Console.WriteLine("Добавлено новых файлов - {0}", insertedRec.ToString());

            if (nonUniqueFiles.Count > 0)
            {
                int insertedDoubleRec = InsertDublicateFilesInfo();
                Console.WriteLine("Добавлено дубликатов {0}", insertedDoubleRec.ToString());
                CreateFileWithNonUnique(); //Создание файла со списком дублей
                //Console.ReadLine();
                return 1;
            }
            else
            {
                //Console.ReadLine();
                return 0;
            }
		}


		#region DO CONNECT

		private static void GetConnectionString()
		{
            try
            {
                ConnectionStringSettings CBUtils = ConfigurationManager.ConnectionStrings["CBUtils"];
                if (CBUtils != null)
                {
                    CBUtilsConnectionString = CBUtils.ConnectionString;
                    logger.Info("Получили ConnectionString  из конфигурационного файла");
                }
            }
            catch (Exception e)
            {
                logger.Error("При получении ConnectionString из конфигурационного файла произошла ошибка: {0}",e.Message);
            }
            
		}

		private static bool ConnectToDataBase()
		{
			GetConnectionString();
			sqlConnection = new SqlConnection(CBUtilsConnectionString);
			try
			{
                sqlConnection.Open();
                logger.Info("Соединение с БД установленно.");
            }
			catch (Exception e)
			{
                logger.Error("При установке соединеения с БД произошла ошибка: {0}", e.Message);
                Console.WriteLine("{0} Exception caught.", e);
				return false;
			}
			return (sqlConnection.State == ConnectionState.Open) ? true : false;
		}

        #endregion DO CONNECT

        //Подготовка списка имен файлов, которые будут вносится в базу.
        #region READ_FILE 

        /// <summary>
        /// Первичное заполнение списка allFilesList
        /// </summary>
        /// <param name="pathToFilesListStore"></param>
        private static void ReadFileList(string pathToFilesListStore)
		{
            logger.Info("Считываем данные из файла в List");
            allFilesList = new List<string>();
			try
			{
                ///Читаем имена файлов из списка и сохраняем их в List (allFilesList)
                using (StreamReader sr = new StreamReader(pathToFilesListStore, System.Text.Encoding.Default))
				{
					string fileName;
                    while ((fileName = sr.ReadLine()) != null)
					{
						if (!String.IsNullOrEmpty(fileName))
						{
                            allFilesList.Add(fileName);
                            logger.Info("Имя файла, который добавлен в List - {0}", fileName);
                        }
					}
				}
			}
			catch (Exception e)
			{
                logger.Error("Во время заполнения List файлами из списка произошла ошибка - {0}", e.Message);
                Console.WriteLine(e.Message);
			}
		}
        
        #endregion READ_FILE

        #region CHECK_UNIQUE
        
        /// <summary>
        /// Подготовка запроса для проверки на дублирование файла
        /// SELECT DISTINCT NAME FROM TFILES WHERE (NAME in ('список имен файлов из allFilesList')) AND DATE = 'Дата внесения данных'
        /// Имена, которые попадут в выборку считаются дублями
        /// </summary>
        /// <param name="dt">Дата внесения данных</param>
        /// <returns></returns>
        private static string PrepareCheckSql(DateTime dt)
		{
			string separateList = String.Join("','", allFilesList);
			string sqlSelect    = SQL_COUNT_FILE.Replace("{0}", separateList);
			sqlSelect           = sqlSelect.Replace("{1}", dt.ToString(DATE_PATERN));
            return sqlSelect;
		}

        /// <summary>
        /// Заполнение списка именами файлов, которые  являются дублями (nonUniqueFiles)
        /// </summary>
		private static void GenerateNonUniqueList()
		{
			string queryText = PrepareCheckSql(FileDate);
            logger.Info("Запрос для проверки на дублирование файла - {0}", queryText);
            try
			{
				SqlCommand sqlcmd = new SqlCommand(queryText, sqlConnection);
				SqlDataReader reader = sqlcmd.ExecuteReader();
				nonUniqueFiles = new List<string>();
				if (reader.HasRows)
				{
					while (reader.Read())
					{
						string fileName = (string)reader["NAME"];
						nonUniqueFiles.Add(fileName);
                        logger.Info("Файлы дубли, которые добавлены в List(nonUniqueFiles) - {0}", fileName);
                    }//end while
				}
			}
			catch(Exception e)
			{
                logger.Error("Во время выполнения запроса на поиск дублей произошла ошибка- {0}", e.Message);
                Console.WriteLine(e.Message);
			}
		}
       
        /// <summary>
        /// Удаление из списка дубликатов
        /// На выходе получаем список с уникальными именами
        /// </summary>
        private static void PrepareUniqueList()
        {
            allFilesList.RemoveAll(fn => nonUniqueFiles.Exists(nfn => fn == nfn)); //remove  double names from list 
            logger.Info("Создание списка с уникальными именами List");
        }

        /// <summary>
        /// Делит список с значениями для Insert 
        /// на порции, которые указаны в параметре
        /// </summary>
        /// <param name="items">Подготовленный список с значениями для Insert </param>
        /// <returns>Возвращает список состоящий из порций</returns>
        private static List<List<string>> DivideList(List<string> items)
        {
            List<List<string>> separatedList = new List<List<string>>();
            int recCount = items.Count;
            int partsCount = recCount / SIZE_INSERT_SQL_PART;//Вычисляем количество частей хранилища. Делим количество записей всего на количество записей одной порции 
            for (int i = 0; i <= partsCount; i++)
            {
                List<string> insertVal = new List<string>();
                for (int j = 0; j < SIZE_INSERT_SQL_PART; j++)
                {
                    int k = i * SIZE_INSERT_SQL_PART + j;
                    if (k < recCount)
                    {
                        insertVal.Add(items[k]);
                    } 
                }
                separatedList.Add(insertVal);
            }
            separatedList.Add(items);
            items.Clear();
            return separatedList;
        }

        /// <summary>
        /// Подготовка скрипта для добавления дубликатов
        /// </summary>
        /// <returns></returns>
        private static string PrepareSqlInsertForDouble()
        {
            if (Inserts == null)
            {
                Inserts = new List<string>();// Хранилище готовых иинсертов
            }
            Inserts.Clear();

            List<List<string>> divValues = new List<List<string>>();//Хранилище значений для инсертов, которые разделили на порции
            List<string> values = new List<string>(); //Полный список в котором хранятся заполненные данные для Insert VALUES_FOR_INSERT_DOUBLE = "('{0}','{1}','{2}', {3})"

            foreach (string fn in nonUniqueFiles)
            {
                string insertValues;
                string selectVersionSql;
                int fileVersion = 0;

                selectVersionSql = SQL_GET_FILE_VERSION.Replace("{0}",fn);
                selectVersionSql = selectVersionSql.Replace("{1}", FileDate.ToString(DATE_PATERN));
                //Получаем версию дубля в переменную fileVersion
                logger.Info("Запрос на получение версии дубля - {0}", selectVersionSql);
                try
                {
                    SqlCommand sqlcmd = new SqlCommand(selectVersionSql, sqlConnection);
                    SqlDataReader reader = sqlcmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            fileVersion = (int)reader["VERSION"];
                            logger.Info("Версия дубля - {0}", fileVersion);
                        }//end while
                    }
                }
                catch (Exception e)
                {
                    logger.Error("При получении версии дубля - {0}", e.Message);
                    Console.WriteLine(e.Message);
                }
                ///Готовим данные для Insert VALUES_FOR_INSERT_DOUBLE = "('{0}','{1}','{2}', {3})";
                insertValues = VALUES_FOR_INSERT_DOUBLE.Replace("{0}", fn); //fn - file name  
                insertValues = insertValues.Replace("{1}", FileDate.ToString(DATE_PATERN));
                insertValues = insertValues.Replace("{2}", DateTime.Now.ToLongTimeString());
                insertValues = insertValues.Replace("{3}", fileVersion.ToString());

                values.Add(insertValues);
            }
            divValues = DivideList(values);
            foreach (var divValue in divValues)
            {
                string insVal = String.Join(",", divValue);
                if (!String.IsNullOrEmpty(insVal))
                {
                    //Готовим скрипт для Insert  и добавляем его в List "Insert"
                    // Шаблон - SQL_INSERT_DOUBLE_VALUES = "INSERT INTO TFILES (NAME,DATE,TIME,VERSION) VALUES ";
                    Inserts.Add(SQL_INSERT_DOUBLE_VALUES + insVal);
                }
            }
            if (Inserts.Count == 0) return ""; else return Inserts[0];
        }

        /// <summary>
        ///Подготовка запросов для добавления новых данных 
        ///Запросы сохраняются в List по имени "Inserts"
        /// </summary>
        /// <returns>Возвращает текст запроса</returns>
        private static string PrepareSqlInsertForUnique()
		{
            if (Inserts == null)
            {
                Inserts = new List<string>();// Хранилище готовых иинсертов
            }
            Inserts.Clear();

            List<List<string>> divValues = new List<List<string>>();//Хранилище значений для инсертов, которые разделили на порции
			List<string> values = new List<string>(); //Полный список в котором хранятся заполненные  VALUES_FOR_INSERT_NEW - ('{0}','{1}','{2}')

            //  список в котором хранятся заполненные  VALUES_FOR_INSERT_NEW - ('{0}','{1}','{2}')
            foreach (string ufn in allFilesList)
            {
                string insertValues;
                insertValues = VALUES_FOR_INSERT_NEW.Replace("{0}", ufn); //ufn - unique file name  
                insertValues = insertValues.Replace("{1}", FileDate.ToString(DATE_PATERN));
                insertValues = insertValues.Replace("{2}", DateTime.Now.ToLongTimeString());
                values.Add(insertValues);
            }
            //Разбиваем массив на куски по SIZE_INSERT_SQL_PART записей
            divValues = DivideList(values);
       
            foreach (var divValue in divValues)
            {
                string insVal = String.Join(",", divValue);
                if (!String.IsNullOrEmpty(insVal))
                {
                    Inserts.Add(SQL_INSERT_NEW_VALUES + insVal);
                }
            }
            if (Inserts.Count == 0) return ""; else return Inserts[0];
		}
        
        /// <summary>
        /// Исполнение запроса по добавлению дубликатов в базу
        /// </summary>
        /// <returns>Количество добавленных файлов</returns>
        private static int InsertDublicateFilesInfo()
        {
            if (nonUniqueFiles.Count == 0)
            {
                return 0;
            }
            PrepareSqlInsertForDouble();

            if (Inserts.Count == 0)
            {
                return 0;
            }

            int ri = 0;

            foreach (var insert in Inserts)
            {
                try
                {
                    SqlCommand sqlcmd = new SqlCommand(insert, sqlConnection);
                    ri = ri + sqlcmd.ExecuteNonQuery();
                    logger.Info("Скрипт добавления дубликатов - {0}", insert);
                }
                catch (Exception e)
                {
                    logger.Error("Во время выполнения запроса по добавлению дублей {0} произошла ошибка - {1}", insert, e.Message);
                    Console.WriteLine(e.Message);
                }
            }
            logger.Info("Добавлено {0} дубликатов ", ri);
            return ri;
        }

        /// <summary>
        /// Добавляет в базу новые файлы из списка allFilesList
        /// Данный список содержит уникальные имена файлов
        /// </summary>
        /// <returns>Возвращаем количество добавленных строк</returns>
		private static int InsertNewFilesInfo()
		{
            if (allFilesList.Count == 0)
            {
                return 0;
            }

            string queryText = PrepareSqlInsertForUnique();

            if (Inserts.Count == 0)
            {
                return 0;
            }

            int ri = 0;

            foreach (var insert in Inserts)
            {
                try
                {
                    SqlCommand sqlcmd = new SqlCommand(insert, sqlConnection);
                    ri = ri + sqlcmd.ExecuteNonQuery();
                    logger.Info("Скрипт добавления новых записей - {0}", insert);
                }
                catch (Exception e)
                {
                    logger.Error("Во время выполнения запроса по добавлению дублей {0} произошла ошибка - {1}", insert, e.Message);
                    Console.WriteLine(e.Message);
                }
            }
			return ri;
		}

        /// <summary>
        /// Создание файла содержащего список дубликатов
        /// </summary>
        /// <returns>Количество дублей</returns>
		private static int CreateFileWithNonUnique()
		{
			if (File.Exists(OutputFileList))
			{
				File.Delete(OutputFileList);
			}

			try
			{
				using (StreamWriter sw = new StreamWriter(OutputFileList))
				{
					foreach (string fn in nonUniqueFiles)
					{
                        logger.Info("В файл со списком дубликатов добавлен - {0}", fn);
                        sw.WriteLine(fn);
					}
				}
			}
			catch (Exception e)
			{
                logger.Error("Во время создания файла со списком дублей произошла ошибка - {0}", e.Message);
                Console.WriteLine(e.Message);
			}
			return nonUniqueFiles.Count;
		}

		#endregion CHECK_UNIQUE
	}
}
