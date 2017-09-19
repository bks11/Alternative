using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompareLists
{
	class Program
	{
		public static string pathToListOne;
		public static string pathToListTwo;
		public static string pathToListDif;

		public static List<string> listOne;
		public static List<string> listTwo;
		public static List<string> listDif;

		static void Main(string[] args)
		{
			bool hasArgs = (args.Length == 3);
			if (!hasArgs)
			{
				Console.WriteLine("Не досаточно аргументов для запуска программы");
				Console.WriteLine("Первый параметр - Путь к исходному листу");
				Console.WriteLine("Второй параметр - Путь к листу с дублями");
				Console.WriteLine("Третий параметр - Путь к файлу с результатми сравнения");
				Console.ReadLine();
			}
			pathToListOne = args[0];
			pathToListTwo = args[1];
			pathToListDif = args[2];

			ReadListOne();
			ReadListTwo();
			CompareLst();
			SaveToFile();
		}

		private static int ReadListOne()
		{
			using (StreamReader sr = new StreamReader(pathToListOne, System.Text.Encoding.Default))
			{
				listOne = new List<string>();
				string fileNameLine;
				while ((fileNameLine = sr.ReadLine()) != null)
				{
					listOne.Add(fileNameLine);
				}
			}
			return listOne.Count;
		}

		private static int ReadListTwo()
		{
			listTwo = new List<string>();
			using (StreamReader sr = new StreamReader(pathToListTwo, System.Text.Encoding.Default))
			{
				string fileNameLine;
				while ((fileNameLine = sr.ReadLine()) != null)
				{
					listTwo.Add(fileNameLine);
				}
			}
			return listTwo.Count;
		}

		private static int CompareLst()
		{
			listDif = new List<string>();
			foreach (string fn in listOne)
			{
				if (!listTwo.Exists(dfn => dfn == fn)) listDif.Add(fn);
			}
			return listDif.Count;
		}
		private static void SaveToFile()
		{
			using (StreamWriter sw = new StreamWriter(pathToListDif))
			{
				foreach (string fn in listDif)
				{
					Console.WriteLine(fn);
					sw.WriteLine(fn);
				}
			}
		}
	}
}
