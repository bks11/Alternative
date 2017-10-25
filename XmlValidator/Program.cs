using System;
using System.IO;
using System.Xml;
using System.Configuration;
using System.Xml.Schema;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XmlValidator
{
    class Program
    {
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
            bool hasArgs = (args.Length == 3);
            if (!hasArgs)
            {
                Console.WriteLine("Не достаточно параметров для запуска приложения");
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
                Console.WriteLine("Прверку не прошли - {0} файлов", badFiles.Count);
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
                    Console.WriteLine("Не указаны маски файлов.");
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
                    XmlSchemaSet schemaSet = new XmlSchemaSet();
                    schemaSet.Add(null, SchemaBinding[mask]);
                    XmlSchemaValidator xsv = new XmlSchemaValidator();
                    xsv.Validate(file, schemaSet);
                    if (!xsv.IsValidXml)
                    {
                        badFiles.Add(file, xsv.ValidationError);
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
                errLog += String.Format("Имя файла - {0} {1} Ошибка - {2}", badFile,Environment.NewLine ,badFiles[badFile]);
            }
        }
    }
}
