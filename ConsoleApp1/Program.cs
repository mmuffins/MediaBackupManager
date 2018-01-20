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
            var backupFiles = new FileIndex();

            var dirInfo = new DirectoryInfo(@"F:\indexdir\main\images2");

            backupFiles.AddDirectory(new DirectoryInfo(@"D:\indexdir\dd"));

            backupFiles.AddDirectory(new DirectoryInfo(@"F:\indexdir\main\images"));
            backupFiles.AddDirectory(new DirectoryInfo(@"F:\indexdir\main\images2"));
            backupFiles.AddDirectory(new DirectoryInfo(@"D:\indexdir"));
        }
    }
}
