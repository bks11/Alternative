using System;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace _550
{
	class Program
	{
		const string XML_SOURCE = @"D:\Projects\CB\Alternative\Example\550\In";

		static _550Answer answer;

		static List<string> esNumbers;
		static int recNumbers;
		static List<string> Diasoft;
		static List<string> Catalog;
		static List<string> Diff;


		static void Main(string[] args)
		{
            //answer = new _550Answer();
            //CompareList();
            //FindXmlFiles("");
            //GenerateFileName();
            //CopyDiff();
            //string fn = @"D:\Projects\CB\Alternative\Example\Source\AFN_MIFNS00_3510123_20170829_00001.ARJ.114253.list";
            //MoveRenameFile(fn);
            //string s = GetFileParts("AFN_MIFNS00_3510123_20170829_00001.ARJ.114253.list ", '.');
            CompareList();
            //Console.WriteLine(s);
            Console.ReadLine();
		}

        private static void MoveRenameFile(string fname)
        {
            //@"D:\Projects\CB\Alternative\Example\Source\AFN_MIFNS00_3510123_20170829_00001.ARJ.114253.list";
            string pathTo = "D:\\Projects\\CB\\Alternative\\Example\\Destination\\";
            string newFileName = Path.Combine(pathTo, "!" + Path.GetFileName(fname));
            FileInfo fileInf = new FileInfo(fname);
            if (fileInf.Exists)
            {
                fileInf.MoveTo(newFileName);
            }
        }

        private static string GetFileParts(string fName, char delimSymbol)
        {
            string fn = Path.GetFileName(fName);
            string[] fileParts = fn.Split(delimSymbol);
            if (fileParts.Length > 2)
            {
                return fileParts[0] + delimSymbol + fileParts[1];
            }
            else return "";
        }

        static void CompareList()
		{
			Diasoft = new List<string>();
			Catalog = new List<string>();
			Diff = new List<string>();
			 
			using (StreamReader sr = new StreamReader("D:\\Temp\\440_3\\13.list", System.Text.Encoding.Default))
			{
				string fileNameLine;
				while ((fileNameLine = sr.ReadLine()) != null)
				{
					Diasoft.Add(fileNameLine);
				}
			}
            //using (StreamReader sr = new StreamReader("D:\\Projects\\CB\\Alternative\\Example\\440\\2908\\AllIn.txt", System.Text.Encoding.Default))
            //{
            //	string fileNameLine;
            //	while ((fileNameLine = sr.ReadLine()) != null)
            //	{
            //		Catalog.Add(fileNameLine);
            //	}
            //}
            //for (int i = 0; i <= Diasoft.Count - 1; i++)
            //	for (int j = 0; j <= Catalog.Count - 1; j++)
            //		if (Diasoft[i] == Catalog[j])
            //		{
            //			Diff.Add(Diasoft[i]);
            //			break;
            //		}

            //Catalog.RemoveAll(fn => Diasoft.Exists(nfn => fn == nfn)); //remove  double names from list 

            //Diff = Catalog.Where(c => Diasoft.Contains(c)).ToList();

            //foreach (string cfn in Diasoft)
            //{
            //	if (!Catalog.Exists(dfn => dfn == cfn)) Diff.Add(cfn);
            //}

            //using (StreamWriter sw = new StreamWriter("D:\\Projects\\CB\\Alternative\\Example\\440\\2908\\lost.txt"))
            //{
            //	foreach (string fn in Diff)
            //	{
            //		Console.WriteLine(fn);
            //		sw.WriteLine(fn);
            //	}
            //}

            foreach (string s in Diasoft)
            {
                string path = "D:\\Projects\\CB\\Alternative\\Example\\!!!!!!!!!!!\\" + s + ".vrb";
                string newPath = "D:\\Projects\\CB\\Alternative\\Example\\!!!!!!!!!!!\\New\\" + s + ".vrb";
                FileInfo fi = new FileInfo(path);
                if (fi.Exists)
                {
                    fi.CopyTo(newPath);
                }
            }

            //using (StreamWriter sw = new StreamWriter("D:\\Projects\\CB\\Alternative\\Example\\diff.txt", false, System.Text.Encoding.Default))
            //{
            //	foreach (string s in Diff)
            //	{
            //		Console.WriteLine(s);
            //		sw.WriteLine(s);
            //	}
            //}

        }

        static void CopyDiff()
		{
			List<string> rList = new List<string>();
			using (StreamReader sr = new StreamReader("D:\\Projects\\CB\\Alternative\\Example\\440\\3008\\df.txt", System.Text.Encoding.Default))
			{
				string fileNameLine;
				while ((fileNameLine = sr.ReadLine()) != null)
				{
					rList.Add(fileNameLine);
				}
			}

			foreach (string s in rList)
			{
				string path = "D:\\!\\440\\!!!\\All\\" + s;
				string newPath = "D:\\!\\440\\!!!\\Copy\\" + s ;
				FileInfo fi = new FileInfo(path);
				if (fi.Exists)
				{
					//Console.WriteLine(s);
					fi.CopyTo(newPath);
				}
				else
				{
					Console.WriteLine(s);
				}
			}
		}

		static string GenerateFileName()
		{
			string answerName = string.Format("UV_{0}_{1}",answer.IDNOR,answer.ES);
			Console.WriteLine(answerName);
			return answerName;
		}

		static void FindXmlFiles(string xmlPath)
		{
			string[] files = Directory.GetFiles(XML_SOURCE,"*.xml");
			foreach (string fileName in files)
			{
				LoadXml(fileName);
			}
		}

		static void LoadXml(string xmlFile)
		{
			XmlDocument xDoc = new XmlDocument();
			xDoc.Load(xmlFile);
			XmlElement xRoot = xDoc.DocumentElement;
			esNumbers = new List<string>();
			
			recNumbers = 0;
			answer.IDNOR = "2490_0000";
			answer.ES = Path.GetFileName(xmlFile);
			FileInfo fi = new FileInfo(xmlFile);
			answer.SIZE_ES = fi.Length;

			foreach (XmlElement xNode in xRoot)
			{
				foreach (XmlElement childNode in xNode)
				{
					if(childNode.Name == "ДатаСообщения")
					{
						answer.DATE_ES = childNode.InnerText;
						Console.WriteLine(childNode.InnerText);
						
					}
					if (childNode.Name == "Раздел2")
					{
						foreach (XmlElement subChildNode in childNode)
						{
							if (subChildNode.Name == "НомерЗаписи")
							{
								esNumbers.Add(subChildNode.InnerText);
								recNumbers++;
								Console.WriteLine(subChildNode.InnerText);
							}
						}
					}
				}
			}
			answer.ES_REC = esNumbers;
			answer.RECNO_ES = recNumbers;
		}
	}
}
