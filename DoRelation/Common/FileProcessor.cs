using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoRelation.Common
{
     public class FileProcessor
    {
        public static string fileName { get; set; }
        public static string fileMask { get; set; }
        public static  string pathFrom { get; set; }
        public static string pathTo { get; set; }
        public static List<string> childFilesList { get; set; }

        /// <summary>
        /// Перемещение файла с изменением имени
        /// </summary>
        /// <param name="fname">Полный путь с именем файла, который будет  перемещаться</param>
        /// <param name="addSymbol">Символ, который будет добавлен к имени файла после переименования</param>
        private static void MoveRenameFile(string fname, string addSymbol)
        {
            string newFileName = Path.Combine(pathTo, addSymbol + Path.GetFileName(fname));
            FileInfo fileInf = new FileInfo(fname);
            if (fileInf.Exists)
            {
                fileInf.MoveTo(newFileName);
            }
        }
        /// <summary>
        /// Получает имя архива из имени файла с расширением list 
        /// </summary>
        /// <param name="fName">Имя файла листа</param>
        /// <param name="delimSymbol">Символ разделяющий  части файла </param>
        /// <returns>Возвращает имя архива 
        /// Например из AFN_MIFNS00_3510123_20170829_00001.ARJ.114253.list
        /// Вернется значение AFN_MIFNS00_3510123_20170829_00001.ARJ
        /// </returns>
        private static string GetParentFileName(string fName, char delimSymbol)
        {
            string fn = Path.GetFileName(fName);
            string[] fileParts = fn.Split(delimSymbol);
            if (fileParts.Length > 2)
            {
                return fileParts[0] + delimSymbol + fileParts[1];
            }
            else return "";
        }

        /// <summary>
        /// Получение списка имен файлов потомков из листа в свойство класса childFilesList
        /// </summary>
        /// <param name="parentFileName">Имя файла с расширением *.list,  в котором находится список файлов потомков</param>
        private static void GetChildList(string parentFileName)
        {
            childFilesList = new List<string>();

            try
            {
                using (StreamReader sr = new StreamReader(parentFileName, System.Text.Encoding.Default))
                {
                    string childFileName;// Имя файла из списка
                    while ((childFileName = sr.ReadLine()) != null)
                    {
                        //Console.WriteLine(fileName);
                        if (!String.IsNullOrEmpty(childFileName))
                        {
                            childFilesList.Add(childFileName);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {

            }

        }

        /// <summary>
        /// Обработка файла архива и заполнение свойств класса для дальнейшего внесения данныхв БД
        /// Сохраняет имя файла предка и список  потомков, которые описаны внутри файла
        /// </summary>
        /// <param name="parentFileName">Имя файла архива</param>
        public static void PrepareFileInfo(string parentFileName)
        {
            fileName = GetParentFileName(parentFileName, '.');// Получение имени файла архива
            GetChildList(parentFileName); // Заполнение листа именами файлов потом
            
            //MoveRenameFile(parentFileName, addSymbol);
            //if (String.IsNullOrEmpty(fileMask)) return false;
            //int sourceFileCount = 0;
            //int movedFileCount = 0;
            //try
            //{
            //    string[] files = Directory.GetFiles(pathFrom,fileMask);
            //    sourceFileCount = files.Length;
            //    foreach (string f in files)
            //    {
            //        fileName = GetParentFileName(f,'.');// Получение имени файла архива
            //        GetChildList(f); // Заполнение листа именами файлов потом
            //        MoveRenameFile(f, delimSymbol);
            //        movedFileCount++;
            //    }
            //}
            //catch(Exception e)
            //{
            //    Console.WriteLine(e.Message);
            //    return false;
            //}
            //if (sourceFileCount == movedFileCount)
            //{
            //    return true;
            //}
            //else
            //{
            //    return false;
            //}
        }
    }
}
