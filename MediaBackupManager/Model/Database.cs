using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBackupManager.Model
{
    static class Database
    {
        private const string fileName = "db.sqlite";
        private const string folderName = "MediaBackupManager";



        static string GetPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDoc‌​uments), folderName);
        }

        static string GetName()
        {
            return fileName;
        }

        static string GetFullName()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDoc‌​uments), folderName, fileName);
        }

        static string GetConnectionString()
        {
            return "Data Source = " + GetFullName() + "; Version = 3; UTF8Encoding=True";
        }

        static void CreateDatabase()
        {
            string dbPath = GetFullName();

            if (File.Exists(dbPath))
                return;

            if (!Directory.Exists(GetPath()))
            {
                Directory.CreateDirectory(GetPath());
            }



        }

    }
}
