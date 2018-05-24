using System;
using System.IO;
using System.Configuration;
using System.Xml.Schema;
using System.Collections.Generic;
using System.Globalization;

namespace XmlValidator
{
    class Program
    {
        const string ERR_FILE_NAME = "Нарушение структуры имени файла ЭС - Дата не соответствует текущей";
        const string ERR_NOT_ENOUGH_PRM = "Не достаточно параметров для запуска приложения";
        const string ERR_MASK = "Не указаны маски файлов.";

        //static private XmlSchemaSet schemaSet { get; set; }
        static private Dictionary<string, string> SchemaBinding { get; set; }
        static private List<string> validFiles;
        static private Dictionary<string,string> badFiles;
        static private string sourcePath;
        static private string validOkPath;
        static private string validNotPath;
        static private string errLog;

        static int Main(string[] args)
        {
            Console.WriteLine("Запуск программы XmlValidator");
            bool hasArgs = (args.Length == 3);
            if (!hasArgs)
            {
                Console.WriteLine(ERR_NOT_ENOUGH_PRM);
                Console.ReadLine();
                return 1;
            }
            sourcePath = args[0];
            validOkPath = args[1];
            validNotPath = args[2];

            ReadConfiguration();

            validFiles = new List<string>();
            badFiles = new Dictionary<string,string>();

            ValidateFiles(sourcePath);
            MoveFiles();
            Console.WriteLine("Проверено файлов - {0}", validFiles.Count + badFiles.Count);
            Console.WriteLine("Прверку прошли - {0} файлов", validFiles.Count);
            if (badFiles.Count > 0)
            {
                Console.WriteLine("Прoверку не прошли - {0} файлов ", badFiles.Count);
                Console.WriteLine(errLog);
            }
            if (badFiles.Count > 0) return 1;
            else return 0;

            //Console.ReadLine();
            //return 0;
        }

        static void ReadConfiguration()
        {
            SchemaBinding = new Dictionary<string, string>();
            try
            {
                var appSettings = ConfigurationManager.AppSettings;
                if (appSettings.Count == 0)
                {
                    Console.WriteLine(ERR_MASK);
                }
                else
                {
                    foreach (var key in appSettings.AllKeys)
                    {
                        string mask = key;
                        string schema = appSettings[key];
                        //Console.WriteLine("Key: {0}, Value: {1}", mask, schema);
                        SchemaBinding.Add(mask, schema);
                        //Console.WriteLine("Key: {0} Value: {1}", key, appSettings[key]);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        static private bool CheckSendDateFromFileName(string fileName)
        {
            DateTime sendDateValue;
            string sendDate = fileName.Substring(17, 8);
            if (DateTime.TryParseExact(sendDate, "yyyyMMdd", new CultureInfo("en-US"), DateTimeStyles.None, out sendDateValue))
            {
                return DateTime.Now.Date == sendDateValue;
            }
            else
            {
                return false;
            }
            
        }

        static private string WriteValidationErrToFile(string fileName, string errorDescription)
        {
            string errDescFileName = fileName + ".err";
            using (StreamWriter swErrDescFileName = new StreamWriter(errDescFileName, false, System.Text.Encoding.Default))
            {
                swErrDescFileName.WriteLine(errorDescription);
            }
            return errDescFileName;
        }

        static private void ValidateFiles(string folderPath)
        {
            //Get file mask stored in Dictionary
            foreach (string mask in SchemaBinding.Keys)
            {
                //Get files list by mask
                string[] files = Directory.GetFiles(folderPath, mask);
                //For each file in list  do validation   
                foreach (string file in files)
                {
                    //bool isFileNameGood = CheckSendDateFromFileName(Path.GetFileName(file));
                    bool isFileNameGood = true;

                    XmlSchemaSet schemaSet = new XmlSchemaSet();
                    schemaSet.Add(null, SchemaBinding[mask]);
                    XmlSchemaValidator xsv = new XmlSchemaValidator();
                    xsv.Validate(file, schemaSet);

                    if (!xsv.IsValidXml || !isFileNameGood)
                    {
                        string errDesc = isFileNameGood? xsv.ValidationError: xsv.ValidationError + ' ' + ERR_FILE_NAME;
                        string fileWithErrDesc = WriteValidationErrToFile(file, errDesc);
                        badFiles.Add(fileWithErrDesc, errDesc);
                        badFiles.Add(file, errDesc);
                        //Console.WriteLine("File name with err - {0}", Path.GetFileName(file));
                    }
                    else
                    {
                        validFiles.Add(file);
                        //Console.WriteLine("Valid XML file {0}", Path.GetFileName(file));
                    }
                }
            }
        }

        static private void FileSorting(string fileName, bool validationResult)
        {
            string destFile = validationResult ? Path.Combine(validOkPath, Path.GetFileName(fileName)) : Path.Combine(validNotPath, Path.GetFileName(fileName));
            FileInfo fileInf = new FileInfo(fileName);
            try
            {
                fileInf.MoveTo(destFile);
                //Console.WriteLine(destFile);
            }
            catch(Exception e)
            {
                //Console.WriteLine(e.Message);
            }
        }

        static private void MoveFiles()
        {
            foreach (string okFiles in validFiles)
            {
                FileSorting(okFiles, true);
            }
            foreach (string badFile in badFiles.Keys)
            {
                FileSorting(badFile, false);
                errLog += String.Format(" Имя файла - {0} {1} Ошибка - {2}", badFile,Environment.NewLine ,badFiles[badFile]);
            }
        }
    }
}
