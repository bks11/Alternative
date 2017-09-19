using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GetFilesName
{
	class Program
	{
		enum ExitCode : int
		{
			Success = 0,
			NoFiles = 1,
			IncorrectPath = 2,
			SearchFileError = 3
		}
		public static List<string> filesList;
		public static List<int> numberForFolderName;

		static int Main(string[] args)
		{
			bool hasArgs = (args.Length > 1);
			if (!hasArgs)
			{
				Console.WriteLine("Укажите путь к файлам и регулярное выражения для идентификации файлов, правила выбора(необязательный аргумент)");
				Console.ReadLine();
				return (int)ExitCode.IncorrectPath;
			}

			string inPath = args[0];
			string regular = args[1];

			string outputformat= "";
			if (args.Length == 3) outputformat = args[2];
			outputformat = (outputformat.Trim()).ToLower();


			GetFilesInFolder(inPath, regular);

			if (filesList.Count == 0)
			{
				return (int)ExitCode.NoFiles;
			}

			if (String.IsNullOrEmpty(outputformat))
			{
				Regex regex = new Regex(regular);
				MatchCollection matches = regex.Matches(filesList[0]);
				foreach (Match match in matches)
				{
					Console.WriteLine(match.Groups[1]);
				}
				//Console.ReadLine();
				return (int)ExitCode.Success;
			} 
			if (outputformat == "all")
			{
				foreach (string fn in filesList)
				{
					Regex regex = new Regex(regular);
					MatchCollection matches = regex.Matches(fn);
					foreach (Match match in matches)
					{
						Console.WriteLine(match.Groups[1]);
					}
				}
				//Console.ReadLine();
				//return (int)ExitCode.Success;
			}
			if (outputformat == "allinline")
			{
				string resultLine = "";
				for (int i = 0; i < filesList.Count; i++)
				{
					Regex regex = new Regex(regular);
					MatchCollection matches = regex.Matches(filesList[i]);
					if (matches.Count > 0)
					{
						if (i == filesList.Count - 1)
						{
							resultLine = resultLine + matches[0].Groups[1];
						}
						else
						{
							resultLine = resultLine + matches[0].Groups[1]+ ", ";
						}
						
					}
				}
				Console.WriteLine(resultLine);
			}
			//Console.ReadLine();
			return (int)ExitCode.Success;
		}

		static private int GetFilesInFolder(string folderPath, string regular)
		{
			try
			{
				Regex regex = new Regex(regular);  //new Regex(@"^(?<name>\d+).pdf"); // @"\d{5,}"

				filesList = new List<string>();
				string[] files = Directory.GetFiles(folderPath, "*.*");

				foreach (string s in files)
				{
					string fileName = Path.GetFileName(s);
					MatchCollection matches = regex.Matches(fileName);
					if (matches.Count > 0) filesList.Add(Path.GetFileName(fileName));
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				return (int)ExitCode.SearchFileError;
			}
			filesList.Sort();
			return filesList.Count;
		}

		static int NumericFolderName()
		{
			numberForFolderName = new List<int>();
			foreach (string fn in filesList)
			{
				int i = Convert.ToInt32(Path.GetFileNameWithoutExtension(fn));
				numberForFolderName.Add(i);
			}

			int min = int.MaxValue;
			foreach (int n in numberForFolderName)
			{
				if (n < min)
				{
					min = n;
				}
			}
			return min;
		}
	}
}
