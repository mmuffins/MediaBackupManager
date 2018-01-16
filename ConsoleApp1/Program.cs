using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {

            var backupFiles = new BackupList();
            backupFiles.Add(new DirectoryInfo(@"C:\"));
            backupFiles.Add(new DirectoryInfo(@"C:\"));
            backupFiles.Add(new DirectoryInfo(@"E:\"));

        }
    }
}
