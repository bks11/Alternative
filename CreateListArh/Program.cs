using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;
//
namespace CreateListArh
{
    class Program
    {
        static string archiveName = "";
        static string pathToFileList = "";
        static List<string> arhContent;
        
        static int Main(string[] args)
        {
            int repeatCount = 0;
            bool result = false;
            if (!CheckArgs(args))
            {
                return 1;
            }
            if (PrepareArhContent() > 0 && repeatCount < 100)
            {
                result = CreateFileWithArhContetnt();
            }
            else
            {
                repeatCount++;
            }

            int i = result ? 0 : 1;
            Console.ReadLine();
            return i;
        }


        static bool CheckArgs(string[] args)
        {
            try
            {
                if (args.Length < 2) return false;
                //string arhType = Path.GetExtension(args[0]);
                string arhType = "Arj";
                if (!Enum.IsDefined(typeof(KnownSevenZipFormat), arhType)) return false;
                archiveName = args[0];
                pathToFileList = args[1];
                return true;
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }

        static bool CreateFileWithArhContetnt()
        {
            try
            {
                if (arhContent.Count > 0)
                {
                    using (StreamWriter sw = new StreamWriter(pathToFileList))
                    {
                        foreach (string fn in arhContent)
                        {
                            Console.WriteLine(fn);
                            sw.WriteLine(fn);
                        }
                    }
                }
                return true;
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
            
        }

        static int PrepareArhContent()
        {
            arhContent = new List<string>();
            using (SevenZipFormat Format = new SevenZipFormat(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "7z.dll")))
            {
                IInArchive Archive = Format.CreateInArchive(SevenZipFormat.GetClassIdFromKnownFormat(KnownSevenZipFormat.Arj));
                if (Archive == null)
                {
                    return 0;
                }
                try
                {
                    using (InStreamWrapper ArchiveStream = new InStreamWrapper(File.OpenRead(archiveName)))
                    {
                        ulong checkPos = 32 * 1024;
                        if (Archive.Open(ArchiveStream, ref checkPos, null) != 0) Console.WriteLine("Error!!!");
                        Console.Write("Archive: ");
                        Console.WriteLine(archiveName);
                        uint Count = Archive.GetNumberOfItems();
                        for (uint i = 0; i < Count; i++)
                        {
                            PropVariant Name = new PropVariant();
                            Archive.GetProperty(i, ItemPropId.kpidPath, ref Name);
                            arhContent.Add(Name.GetObject().ToString());
                            Console.Write(i);
                            Console.Write(' ');
                            Console.WriteLine(Name.GetObject());
                        }
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(Archive);
                }
            }
            return arhContent.Count;
        }
        class ArchiveCallback : IArchiveExtractCallback
        {
            private uint FileNumber;
            private string FileName;
            private OutStreamWrapper FileStream;

            public ArchiveCallback(uint fileNumber, string fileName)
            {
                this.FileNumber = fileNumber;
                this.FileName = fileName;
            }

            #region IArchiveExtractCallback Members

            public void SetTotal(ulong total)
            {

            }

            public void SetCompleted(ref ulong completeValue)
            {

            }

            public int GetStream(uint index, out ISequentialOutStream outStream, AskMode askExtractMode)
            {
                if ((index == FileNumber) && (askExtractMode == AskMode.kExtract))
                {
                    string FileDir = Path.GetDirectoryName(FileName);
                    if (!string.IsNullOrEmpty(FileDir))
                        Directory.CreateDirectory(FileDir);
                    FileStream = new OutStreamWrapper(File.Create(FileName));

                    outStream = FileStream;
                }
                else
                    outStream = null;

                return 0;
            }

            public void PrepareOperation(AskMode askExtractMode)
            {

            }

            public void SetOperationResult(OperationResult resultEOperationResult)
            {
                FileStream.Dispose();
                Console.WriteLine(resultEOperationResult);
            }

            #endregion
        }
    }
}
