using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
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

        public static string GetPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDoc‌​uments), folderName);
        }

        public static string GetName()
        {
            return fileName;
        }

        public static string GetFullName()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDoc‌​uments), folderName, fileName);
        }

        public static string GetConnectionString()
        {
            return (new SQLiteConnectionStringBuilder()
            {
                DataSource = GetFullName(),
                Version = 3,
                UseUTF16Encoding = true,
                ForeignKeys = true
            }).ConnectionString;
        }

        public static void CreateDatabase()
        {
            string dbPath = GetFullName();

            if (File.Exists(dbPath))
                return;

            if (!Directory.Exists(GetPath()))
            {
                Directory.CreateDirectory(GetPath());
            }

            using (var dbConn = new SQLiteConnection(GetConnectionString()))
            {
                var sqlCmd = new SQLiteCommand(dbConn);

                dbConn.Open();

                sqlCmd.CommandText = "CREATE TABLE LogicalVolume (" +
                    "VolumeSerialNumber TEXT PRIMARY KEY" +
                    ", Size INTEGER" +
                    ", Type INTEGER" +
                    ", VolumeName TEXT" +
                    ", Label TEXT" +
                    ")";
                sqlCmd.ExecuteNonQuery();

                sqlCmd.CommandText = "CREATE TABLE BackupFile (" +
                    "Id INTEGER PRIMARY KEY AUTOINCREMENT" +
                    ", Length TEXT" +
                    ", CreationTime TEXT" +
                    ", CreationTimeUtc TEXT" +
                    ", LastWriteTime TEXT" +
                    ", LastWriteTimeUtc TEXT" +
                    ", CheckSum TEXT" +
                    ", MyCar INTEGER" +
                    ")";
                sqlCmd.ExecuteNonQuery();

                sqlCmd.CommandText = "CREATE TABLE FileDirectory (" +
                    "Id TEXT PRIMARY KEY" +
                    ", Name TEXT" +
                    ", Drive TEXT" +
                    ")";
                sqlCmd.ExecuteNonQuery();

                sqlCmd.CommandText = "CREATE TABLE FileNode (" +
                    "File TEXT PRIMARY KEY" +
                    ", Directory TEXT" +
                    ", Name TEXT" +
                    ", Extension TEXT" +
                    ")";
                sqlCmd.ExecuteNonQuery();

                sqlCmd.CommandText = "CREATE TABLE BackupSet (" +
                    "Id INTEGER PRIMARY KEY AUTOINCREMENT" +
                    ", Drive TEXT" +
                    ", RootDirectory TEXT" +
                    ")";
                sqlCmd.ExecuteNonQuery();
            }
        }

        public static void InsertLogicalVolume(LogicalVolume logicalVolume)
        {
            using (var dbConn = new SQLiteConnection(GetConnectionString()))
            {
                var sqlCmd = new SQLiteCommand(dbConn);

                sqlCmd.CommandText = "INSERT INTO LogicalVolume (" +
                    "VolumeSerialNumber" +
                    ", Size" +
                    ", Type" +
                    ", VolumeName" +
                    ", Label" +
                    ") VALUES (" +
                    "@VolumeSerialNumber " +
                    ", @Size" +
                    ", @Type" +
                    ", @VolumeName" +
                    ", @Label" +
                    ")";

                sqlCmd.CommandType = CommandType.Text;

                sqlCmd.Parameters.Add(new SQLiteParameter("@VolumeSerialNumber", DbType.String));
                sqlCmd.Parameters.Add(new SQLiteParameter("@Size", DbType.UInt64));
                sqlCmd.Parameters.Add(new SQLiteParameter("@Type", DbType.Int16));
                sqlCmd.Parameters.Add(new SQLiteParameter("@VolumeName", DbType.String));
                sqlCmd.Parameters.Add(new SQLiteParameter("@Label", DbType.String));

                sqlCmd.Parameters["@VolumeSerialNumber"].Value = logicalVolume.VolumeSerialNumber;
                sqlCmd.Parameters["@Size"].Value = logicalVolume.Size;
                sqlCmd.Parameters["@Type"].Value = (int)logicalVolume.Type;
                sqlCmd.Parameters["@VolumeName"].Value = logicalVolume.VolumeName;
                sqlCmd.Parameters["@Label"].Value = logicalVolume.Label;

                try
                {
                    dbConn.Open();
                    sqlCmd.ExecuteNonQuery();
                }
                catch (Exception)
                {

                    throw;
                }
                finally
                {
                    if(dbConn.State == ConnectionState.Open)
                    {
                        dbConn.Close();
                    }
                } 
            }
        }

        public static HashSet<LogicalVolume> GetLogicalVolume()
        {
            var volumes = new HashSet<LogicalVolume>();

            using (var dbConn = new SQLiteConnection(GetConnectionString()))
            {
                var sqlCmd = new SQLiteCommand(dbConn);

                sqlCmd.CommandText = "SELECT * FROM LogicalVolume";
                sqlCmd.CommandType = CommandType.Text;

                dbConn.Open();
                using (var reader = sqlCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var ab = reader["VolumeSerialNumber"];
                        var de = reader["Size"];
                        var ef = reader["Type"];
                        var cd = reader["Label"];
                        var ee = reader["VolumeName"];

                        volumes.Add(new LogicalVolume()
                        {
                            Label = reader["Label"].ToString(),
                            Size = long.Parse(reader["Size"].ToString()),
                            Type = (DriveType)Enum.Parse(typeof(DriveType), reader["Type"].ToString()),
                            VolumeName = reader["VolumeName"].ToString(),
                            VolumeSerialNumber = reader["VolumeSerialNumber"].ToString()
                        });
                    }
                }
            }

            return volumes;
        }

        public static void InsertBackupSet(BackupSet backupSet)
        {
            using (var dbConn = new SQLiteConnection(GetConnectionString()))
            {
                var sqlCmd = new SQLiteCommand(dbConn);

                sqlCmd.CommandText = "INSERT INTO BackupSet (" +
                    "Drive" +
                    ", RootDirectory" +
                    ") VALUES (" +
                    "@Drive " +
                    ", @RootDirectory" +
                    ")";

                sqlCmd.CommandType = CommandType.Text;

                sqlCmd.Parameters.Add(new SQLiteParameter("@Drive", DbType.String));
                sqlCmd.Parameters.Add(new SQLiteParameter("@RootDirectory", DbType.String));

                sqlCmd.Parameters["@Drive"].Value = backupSet.Drive.VolumeSerialNumber;
                sqlCmd.Parameters["@RootDirectory"].Value = backupSet.RootDirectory.Name;

                try
                {
                    dbConn.Open();
                    sqlCmd.ExecuteNonQuery();
                }
                catch (Exception)
                {

                    throw;
                }
                finally
                {
                    if (dbConn.State == ConnectionState.Open)
                    {
                        dbConn.Close();
                    }
                }
            }
        }

        public static List<BackupSet> GetBackupSet()
        {
            var sets = new List<BackupSet>();

            using (var dbConn = new SQLiteConnection(GetConnectionString()))
            {
                var sqlCmd = new SQLiteCommand(dbConn);

                sqlCmd.CommandText = "SELECT * FROM BackupSet";
                sqlCmd.CommandType = CommandType.Text;

                dbConn.Open();
                using (var reader = sqlCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var ab = reader["VolumeSerialNumber"];
                        var de = reader["Size"];
                        var ef = reader["Type"];
                        var cd = reader["Label"];
                        var ee = reader["VolumeName"];

                        sets.Add(new BackupSet()
                        {
                            //Label = reader["Label"].ToString(),
                            //Size = long.Parse(reader["Size"].ToString()),
                            //Type = (DriveType)Enum.Parse(typeof(DriveType), reader["Type"].ToString()),
                            //VolumeName = reader["VolumeName"].ToString(),
                            //VolumeSerialNumber = reader["VolumeSerialNumber"].ToString(),
                        });
                    }
                }
            }

            return sets;
        }
    }
}
