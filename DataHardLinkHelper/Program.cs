using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace DataHardLinkHelper
{
    class Program
    {
        public static string workdir = Directory.GetCurrentDirectory();
        public static string dbdir = workdir + @"\DB";
        public static string xmldir = workdir + @"\XML";
        
        public static void Main()
        {
            if (Directory.Exists(dbdir) == false)
            {
                Directory.CreateDirectory(dbdir);
            }
            if (Directory.Exists(xmldir) == false)
            {
                Directory.CreateDirectory(xmldir);
            }
            int usr = 99;
            while (usr != 0)
            {
                Console.Clear();
                Console.WriteLine(
                    "Menu:\n" +
                    "[1] Add an instance\n" +
                    "[2] Initiate hard links\n" +
                    "[3] Copy files from DB\n" +
                    "[4] Delete unused DB files\n" +
                    "[0] Exit"
                    );
                usr = Convert.ToInt32(Console.ReadLine());
                if (usr == 1)
                {
                    NewVer();
                }
                else if (usr == 2)
                {
                    MakeHardLink();
                }
                else if (usr == 3)
                {
                    CopyFromInstance();
                }
                else if (usr == 4)
                {
                    DeleteUnusedFiles();
                }
            }
            Environment.Exit(1);
        }

        public static void NewVer()
        {
            Console.WriteLine("Instance name (will be saved as yourname.xml):");
            string name = Console.ReadLine();
            DataTable table = new("Data");
            table.Columns.Add("File");
            table.Columns.Add("MD5");
            table.Columns.Add("FilePath");
            Console.WriteLine(@"Instance data path (these files will be added to currentdir\DB, example input: D:\IIDX 20\contents\data):");
            string sourcepath = Console.ReadLine();
            Console.WriteLine(@"Move or copy files? [M\C]");
            string or = Console.ReadLine().ToString().ToLower();
            DirectoryInfo dir = new(sourcepath);
            int xmlfilecount = 0;
            int newdbfilecount = 0;
            foreach (var file in dir.GetFiles("", SearchOption.AllDirectories))
            {
                string hash = GetMD5Checksum(file.FullName);
                string filePath = file.Directory.FullName.Replace(sourcepath, "");
                Console.WriteLine($"Adding File:{file.Name} MD5:{hash + file.Extension} FilePath:{filePath} to {name}.xml");
                table.Rows.Add(file.Name, hash + file.Extension, filePath);
                if (File.Exists($@"{dbdir}\{hash}{file.Extension}") == false)
                {
                    Console.WriteLine($"Adding {file.Name} as {hash+file.Extension} to the DB");
                    MoveOrCopy(or, file.FullName, $@"{dbdir}\{hash}{file.Extension}");
                    newdbfilecount++;
                }
                else
                {
                    Console.WriteLine($"File: {hash+file.Extension} already exists in the DB");
                }
                xmlfilecount++;
            }
            Console.WriteLine($"{xmlfilecount} files registered in {name}.xml\n" +
                $"{newdbfilecount} new files added to the main DB\n" +
                $"That's {xmlfilecount - newdbfilecount} files already in the main DB!");
            table.WriteXml(xmldir + $@"\{name}.xml", XmlWriteMode.WriteSchema);
            Console.WriteLine("Press a key to continue...");
            Console.ReadKey();

        }
        public static void MakeHardLink()
        {
            Console.WriteLine("For which instance would you like to initiate hard links?\n" +
                "Input a name of an existing instance (example: tricoro):");
            string input = Console.ReadLine();
            Console.WriteLine(@"Input link destination (example: D:\IIDX 20\contents\data):");
            string dest = Console.ReadLine();
            DataTable table = new("Data");
            table.ReadXml(xmldir + @"\" + input + ".xml");
            Console.WriteLine("Creating folders...");
            var distinctNames = (from row in table.AsEnumerable()
                                 select row.Field<string>("FilePath")).Distinct();

            foreach (var name in distinctNames)
            {
                if (name != "")
                {
                    Directory.CreateDirectory(dest + name);
                }
            }
            foreach (var row in table.AsEnumerable())
            {
                string source = dbdir + @"\" + row.Field<string>("MD5");
                string target = dest + row.Field<string>("Filepath") + @"\" + row.Field<string>("File");
                Console.WriteLine($@"Target: {target} Source: {source}");
                CreateHardLink(target, source, IntPtr.Zero);
            }
            Console.WriteLine("Done, press a key to continue...");
            Console.ReadKey();
        }
        public static void CopyFromInstance()
        {
            Console.WriteLine("Input instance name:");
            string input = Console.ReadLine();
            Console.WriteLine("Input destination:");
            string dest = Console.ReadLine();
            DataTable table = new();
            table.ReadXml(xmldir + @"\" + input + ".xml");
            Console.WriteLine("Creating folders...");
            var distinctNames = (from row in table.AsEnumerable()
                                 select row.Field<string>("FilePath")).Distinct();

            foreach (var name in distinctNames)
            {
                if (name != "")
                {
                    Directory.CreateDirectory(dest + name);
                }
            }
            foreach (var row in table.AsEnumerable())
            {
                string source = dbdir + @"\" + row.Field<string>("MD5");
                string target = dest + row.Field<string>("Filepath") + @"\" + row.Field<string>("File");
                Console.WriteLine($@"Target: {target} Source: {source}");
                File.Copy(source, target);
            }
            Console.WriteLine("Done, press a key to continue...");
            Console.ReadKey();
        }
        public static void DeleteUnusedFiles()
        {
            Console.WriteLine("This will delete files which are not used in any instance xml, press a key to continue...");
            Console.ReadKey();
            DirectoryInfo dir = new(xmldir);           
            HashSet<string> xmlEntries = new();
            DirectoryInfo _dbdir = new(dbdir);
            Console.WriteLine("Parsing DB files...");
            foreach (var file in _dbdir.GetFiles())
            {
                xmlEntries.Add(file.Name);
            }
            foreach (var file in dir.GetFiles())
            {
                DataTable dt = new();
                dt.ReadXml(file.FullName);
                Console.WriteLine($"Reading from {file.Name}");
                var distinctMD5 = (from row in dt.AsEnumerable() 
                                   select row.Field<string>("MD5")).Distinct();
                foreach (var dis in distinctMD5)
                {
                    xmlEntries.Remove(dis.ToString());
                }
            }
            Console.WriteLine($"Found {xmlEntries.Count} unused files.");
            foreach (var entry in xmlEntries)
            {
                Console.WriteLine(entry);
            }
            Console.WriteLine(@"Do you want to delete these files? [Y\N]");
            string usr = Console.ReadLine().ToString().ToLower();
            if (usr == "y")
            {
                foreach (var entry in xmlEntries)
                {
                    Console.WriteLine($"Deleting {entry}");
                    File.Delete(dbdir + @"\" + entry);
                }
            }
            Console.WriteLine("Done, press a key to continue...");
            Console.ReadKey();
        }

        public static string GetMD5Checksum(string filename)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filename);
            var hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "");
        }
        public static void MoveOrCopy(string or, string source, string target)
        {
            if (or == "m")
            {
                File.Move(source, target);
            }
            else if (or == "c")
            {
                File.Copy(source, target);
            }
            else
            {
                Console.WriteLine("Very funny, you probably broke it");
            }
        }
        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        static extern bool CreateHardLink(
            string lpFileName,
            string lpExistingFileName,
            IntPtr lpSecurityAttributes
            );
    }
}
