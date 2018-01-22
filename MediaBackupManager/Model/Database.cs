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
                    "CheckSum TEXT PRIMARY KEY" +
                    ", Length INTEGER" +
                    ", CreationTime TEXT" +
                    ", LastWriteTime TEXT" +
                    ")";
                sqlCmd.ExecuteNonQuery();

                //sqlCmd.CommandText = "CREATE TABLE FileDirectory (" +
                //    "Id TEXT PRIMARY KEY" +
                //    ", Name TEXT" +
                //    ", Drive TEXT" +
                //    ")";
                //sqlCmd.ExecuteNonQuery();

                sqlCmd.CommandText = "CREATE TABLE FileNode (" +
                    "BackupSet TEXT NOT NULL" +
                    ", DirectoryName TEXT NOT NULL" +
                    ", Name TEXT" +
                    ", Extension TEXT" +
                    ", File TEXT" +
                    ", NodeType INTEGER" +
                    ", PRIMARY KEY (BackupSet, DirectoryName, Name)" +
                    ")";
                sqlCmd.ExecuteNonQuery();

                sqlCmd.CommandText = "CREATE TABLE BackupSet (" +
                    "Guid TEXT PRIMARY KEY" +
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
            var res = new HashSet<LogicalVolume>();

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

                        res.Add(new LogicalVolume()
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

            return res;
        }

        public static List<BackupSet> GetBackupSet()
        {
            var res = new List<BackupSet>();

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
                        var newSet = new BackupSet()
                        {
                            Guid = new Guid(reader["Guid"].ToString()),
                            RootDirectory = reader["RootDirectory"].ToString()
                        };

                        res.Add(newSet);
                    }
                }
            }

            return res;
        }

        public static List<BackupFile> GetBackupFile()
        {
            var res = new List<BackupFile>();

            using (var dbConn = new SQLiteConnection(GetConnectionString()))
            {
                var sqlCmd = new SQLiteCommand(dbConn);

                sqlCmd.CommandText = "SELECT * FROM BackupFile";
                sqlCmd.CommandType = CommandType.Text;

                dbConn.Open();
                using (var reader = sqlCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        res.Add(new BackupFile()
                        {
                            CheckSum = reader["CheckSum"].ToString(),
                            CreationTime = DateTime.Parse(reader["CreationTime"].ToString()),
                            LastWriteTime = DateTime.Parse(reader["LastWriteTime"].ToString()),
                            Length = long.Parse(reader["CheckSum"].ToString())
                        });
                    }
                }
            }

            return res;
        }

        public static List<FileDirectory> GetFileNode()
        {
            var res = new List<FileDirectory>();

            using (var dbConn = new SQLiteConnection(GetConnectionString()))
            {
                var sqlCmd = new SQLiteCommand(dbConn);

                sqlCmd.CommandText = "SELECT * FROM FileNode";
                sqlCmd.CommandType = CommandType.Text;

                dbConn.Open();
                using (var reader = sqlCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var node = new FileDirectory();

                        if(int.Parse(reader["NodeType"].ToString()) == 1)  // 0 => Directory, 1 => Node
                        {
                            // Filenode and FileDirectory are stored in the same table,
                            // based on the nodetype we need to cast to the correct data type
                            node = new FileNode();
                            ((FileNode)node).Name = reader["DirectoryName"].ToString();
                            ((FileNode)node).Extension = reader["Extension"].ToString();
                        }

                        node.DirectoryName = reader["DirectoryName"].ToString();
                        res.Add(node);
                    }
                }
            }

            return res;
        }

        public static void InsertFileNode(FileDirectory fileNode)
        {
            using (var dbConn = new SQLiteConnection(GetConnectionString()))
            {
                var sqlCmd = new SQLiteCommand(dbConn);

                sqlCmd.CommandText = "INSERT INTO FileNode (" +
                    "BackupSet" +
                    ", DirectoryName" +
                    ", Name" +
                    ", Extension" +
                    ", File" +
                    ", NodeType" +
                    ") VALUES (" +
                    "@BackupSet" +
                    ", @DirectoryName" +
                    ", @Name" +
                    ", @Extension" +
                    ", @File" +
                    ", @NodeType" +
                    ")";

                sqlCmd.CommandType = CommandType.Text;

                sqlCmd.Parameters.Add(new SQLiteParameter("@DirectoryName", DbType.String));
                sqlCmd.Parameters.Add(new SQLiteParameter("@Name", DbType.String));
                sqlCmd.Parameters.Add(new SQLiteParameter("@Extension", DbType.String));
                sqlCmd.Parameters.Add(new SQLiteParameter("@File", DbType.String));
                sqlCmd.Parameters.Add(new SQLiteParameter("@BackupSet", DbType.String));
                sqlCmd.Parameters.Add(new SQLiteParameter("@NodeType", DbType.Int16));

                sqlCmd.Parameters["@DirectoryName"].Value = fileNode.DirectoryName;
                sqlCmd.Parameters["@BackupSet"].Value = fileNode.BackupSet.Guid;
                sqlCmd.Parameters["@Name"].Value = "";
                sqlCmd.Parameters["@Extension"].Value = "";
                sqlCmd.Parameters["@File"].Value = "";
                sqlCmd.Parameters["@NodeType"].Value = 0; // 0 => Directory, 1 => Node

                if (fileNode is FileNode)
                {
                    sqlCmd.Parameters["@Name"].Value = (fileNode as FileNode).Name;
                    sqlCmd.Parameters["@Extension"].Value = (fileNode as FileNode).Extension;
                    sqlCmd.Parameters["@File"].Value = (fileNode as FileNode).File.CheckSum;
                    sqlCmd.Parameters["@BackupSet"].Value = (fileNode as FileNode).BackupSet.Guid;
                    sqlCmd.Parameters["@NodeType"].Value = 1;
                }


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

        public static void InsertBackupSet(BackupSet backupSet)
        {
            using (var dbConn = new SQLiteConnection(GetConnectionString()))
            {
                var sqlCmd = new SQLiteCommand(dbConn);

                sqlCmd.CommandText = "INSERT INTO BackupSet (" +
                    "Guid" +
                    ", Drive" +
                    ", RootDirectory" +
                    ") VALUES (" +
                    "@Guid" +
                    ", @Drive " +
                    ", @RootDirectory" +
                    ")";

                sqlCmd.CommandType = CommandType.Text;

                sqlCmd.Parameters.Add(new SQLiteParameter("@Guid", DbType.String));
                sqlCmd.Parameters.Add(new SQLiteParameter("@Drive", DbType.String));
                sqlCmd.Parameters.Add(new SQLiteParameter("@RootDirectory", DbType.String));

                sqlCmd.Parameters["@Guid"].Value = backupSet.Guid;
                sqlCmd.Parameters["@Drive"].Value = backupSet.Drive.VolumeSerialNumber;
                sqlCmd.Parameters["@RootDirectory"].Value = backupSet.RootDirectory;

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

        public static void InsertBackupFile(BackupFile backupFile)
        {
            using (var dbConn = new SQLiteConnection(GetConnectionString()))
            {
                var sqlCmd = new SQLiteCommand(dbConn);

                sqlCmd.CommandText = "INSERT INTO BackupFile (" +
                    "CheckSum" +
                    ", Length" +
                    ", CreationTime" +
                    ", LastWriteTime" +
                    ") VALUES (" +
                    "@CheckSum" +
                    ", @Length" +
                    ", @CreationTime" +
                    ", @LastWriteTime" +
                    ")";

                sqlCmd.CommandType = CommandType.Text;

                sqlCmd.Parameters.Add(new SQLiteParameter("@CheckSum", DbType.String));
                sqlCmd.Parameters.Add(new SQLiteParameter("@Length", DbType.Int64));
                sqlCmd.Parameters.Add(new SQLiteParameter("@CreationTime", DbType.DateTime));
                sqlCmd.Parameters.Add(new SQLiteParameter("@LastWriteTime", DbType.DateTime));

                sqlCmd.Parameters["@CheckSum"].Value = backupFile.CheckSum;
                sqlCmd.Parameters["@Length"].Value = backupFile.Length;
                sqlCmd.Parameters["@CreationTime"].Value = backupFile.CreationTime;
                sqlCmd.Parameters["@LastWriteTime"].Value = backupFile.LastWriteTime;

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
    }
}
