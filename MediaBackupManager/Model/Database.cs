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
                    "SerialNumber TEXT PRIMARY KEY" +
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
                    ", Volume TEXT" +
                    ", RootDirectory TEXT" +
                    ")";
                sqlCmd.ExecuteNonQuery();
            }
        }

        /// <summary>Execute the command and return the number of rows inserted/affected by it.</summary>
        /// <param name="command">The command object that will be executed.</param>
        private static int ExecuteNonQuery(SQLiteCommand command)
        {
            using (var dbConn = new SQLiteConnection(GetConnectionString()))
            {
                try
                {
                    command.Connection = dbConn;
                    dbConn.Open();
                    return command.ExecuteNonQuery();
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

        /// <summary>Retrieves the specified LogicalVolume objects from the database.</summary>
        /// <param name="serialNumber">Volume Serial Number of the object that should be retrieved.</param>
        public static List<LogicalVolume> GetLogicalVolume(string serialNumber = "")
        {
            var res = new List<LogicalVolume>();

            using (var dbConn = new SQLiteConnection(GetConnectionString()))
            {
                var sqlCmd = new SQLiteCommand(dbConn);
                var cmdText = new StringBuilder("SELECT * FROM LogicalVolume");
                if (!string.IsNullOrWhiteSpace(serialNumber))
                {
                    cmdText.Append(" WHERE SerialNumber = @SerialNumber");
                    sqlCmd.Parameters.Add(new SQLiteParameter("@SerialNumber", DbType.String));
                    sqlCmd.Parameters["@SerialNumber"].Value = serialNumber;
                }

                sqlCmd.CommandText = cmdText.ToString();
                sqlCmd.CommandType = CommandType.Text;

                dbConn.Open();
                using (var reader = sqlCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        res.Add(new LogicalVolume()
                        {
                            Label = reader["Label"].ToString(),
                            Size = long.Parse(reader["Size"].ToString()),
                            Type = (DriveType)Enum.Parse(typeof(DriveType), reader["Type"].ToString()),
                            VolumeName = reader["VolumeName"].ToString(),
                            SerialNumber = reader["SerialNumber"].ToString()
                        });
                    }
                }
            }
            return res;
        }

        /// <summary>Retrieves the specified BackupSet objects from the database and creates related child objects.</summary>
        /// <param name="guid">Guid of the object that should be retrieved.</param>
        public static List<BackupSet> GetBackupSet(string guid = "")
        {
            // To build a complete backup set:
            // load base data from Backupset
            // load volume
            // load file nodes/directories

            var res = new List<BackupSet>();

            using (var dbConn = new SQLiteConnection(GetConnectionString()))
            {
                var sqlCmd = new SQLiteCommand(dbConn);

                var cmdText = new StringBuilder("SELECT * FROM BackupSet");
                if (!string.IsNullOrWhiteSpace(guid))
                {
                    cmdText.Append(" WHERE Guid = @Guid");
                    sqlCmd.Parameters.Add(new SQLiteParameter("@Guid", DbType.String));
                    sqlCmd.Parameters["@Guid"].Value = guid;
                }

                sqlCmd.CommandText = cmdText.ToString();
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

                        // Load related objects
                        newSet.Volume = GetLogicalVolume(reader["Volume"].ToString()).FirstOrDefault();

                        // Don't use the AddFileNode function as it would 
                        // write the nodes that we just downloaded 
                        // right back to the database causing duplicate errors
                        GetFileNode(newSet).ForEach(n => newSet.FileNodes.Add(n));

                        res.Add(newSet);
                    }
                }
            }

            return res;
        }

        /// <summary>Retrieves list of all backup files from the database.</summary>
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
                            Length = long.Parse(reader["Length"].ToString())
                        });
                    }
                }
            }

            return res;
        }

        /// <summary>Retrieves the specified FileDirectory and FileNode objects from the database.</summary>
        /// <param name="backupSet">Parent backup set of the nodes.</param>
        public static List<FileDirectory> GetFileNode(BackupSet backupSet)
        {
            var res = new List<FileDirectory>();

            using (var dbConn = new SQLiteConnection(GetConnectionString()))
            {
                var sqlCmd = new SQLiteCommand(dbConn);
                sqlCmd.CommandText = "SELECT * FROM FileNode WHERE BackupSet = @Guid";
                sqlCmd.CommandType = CommandType.Text;
                sqlCmd.Parameters.Add(new SQLiteParameter("@Guid", DbType.String));
                sqlCmd.Parameters["@Guid"].Value = backupSet.Guid;

                dbConn.Open();
                using (var reader = sqlCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var node = new FileDirectory();

                        if (int.Parse(reader["NodeType"].ToString()) == 0)  // 0 => Directory, 1 => Node
                        {
                            node.DirectoryName = reader["DirectoryName"].ToString();
                            node.BackupSet = backupSet;
                        }
                        else
                        {
                            // Filenode and FileDirectory are stored in the same table,
                            // based on the nodetype we need to cast to the correct data type
                            node = new FileNode();

                            node.DirectoryName = reader["DirectoryName"].ToString();
                            node.BackupSet = backupSet;
                            ((FileNode)node).Name = reader["DirectoryName"].ToString();
                            ((FileNode)node).Extension = reader["Extension"].ToString();

                            // Make sure to also properly set the relations between nodes and files
                            BackupFile file;
                            var crc = reader["File"].ToString();
                            FileIndex.Files.TryGetValue(crc, out file);

                            if(!(file is null))
                            {
                                ((FileNode)node).File = file;
                                file.AddNode((FileNode)node);
                            }
                        }


                        res.Add(node);
                    }
                }
            }

            return res;
        }

        /// <summary>Inserts the specified FileDirectory or FileNode object to the database.</summary>
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
        
        /// <summary>Inserts the specified object to the database.</summary>
        public static void InsertBackupSet(BackupSet backupSet)
        {
            using (var dbConn = new SQLiteConnection(GetConnectionString()))
            {
                var sqlCmd = new SQLiteCommand(dbConn);

                sqlCmd.CommandText = "INSERT INTO BackupSet (" +
                    "Guid" +
                    ", Volume" +
                    ", RootDirectory" +
                    ") VALUES (" +
                    "@Guid" +
                    ", @Volume " +
                    ", @RootDirectory" +
                    ")";

                sqlCmd.CommandType = CommandType.Text;

                sqlCmd.Parameters.Add(new SQLiteParameter("@Guid", DbType.String));
                sqlCmd.Parameters.Add(new SQLiteParameter("@Volume", DbType.String));
                sqlCmd.Parameters.Add(new SQLiteParameter("@RootDirectory", DbType.String));

                sqlCmd.Parameters["@Guid"].Value = backupSet.Guid;
                sqlCmd.Parameters["@Volume"].Value = backupSet.Volume.SerialNumber;
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

        /// <summary>Inserts the specified object to the database.</summary>
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

        /// <summary>Inserts the specified object to the database.</summary>
        public static void InsertLogicalVolume(LogicalVolume logicalVolume)
        {
            using (var dbConn = new SQLiteConnection(GetConnectionString()))
            {
                var sqlCmd = new SQLiteCommand(dbConn);

                sqlCmd.CommandText = "INSERT INTO LogicalVolume (" +
                    "SerialNumber" +
                    ", Size" +
                    ", Type" +
                    ", VolumeName" +
                    ", Label" +
                    ") VALUES (" +
                    "@SerialNumber " +
                    ", @Size" +
                    ", @Type" +
                    ", @VolumeName" +
                    ", @Label" +
                    ")";

                sqlCmd.CommandType = CommandType.Text;

                sqlCmd.Parameters.Add(new SQLiteParameter("@SerialNumber", DbType.String));
                sqlCmd.Parameters.Add(new SQLiteParameter("@Size", DbType.UInt64));
                sqlCmd.Parameters.Add(new SQLiteParameter("@Type", DbType.Int16));
                sqlCmd.Parameters.Add(new SQLiteParameter("@VolumeName", DbType.String));
                sqlCmd.Parameters.Add(new SQLiteParameter("@Label", DbType.String));

                sqlCmd.Parameters["@SerialNumber"].Value = logicalVolume.SerialNumber;
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
                    if (dbConn.State == ConnectionState.Open)
                    {
                        dbConn.Close();
                    }
                }
            }
        }

        /// <summary>Deletes the specified object from the database.</summary>
        public static void DeleteBackupFile(BackupFile backupFile)
        {
            var sqlCmd = new SQLiteCommand();
            sqlCmd.CommandText = "DELETE FROM BackupFile WHERE CheckSum = @CheckSum";
            sqlCmd.CommandType = CommandType.Text;

            sqlCmd.Parameters.Add(new SQLiteParameter("@CheckSum", DbType.String));
            sqlCmd.Parameters["@CheckSum"].Value = backupFile.CheckSum;

            ExecuteNonQuery(sqlCmd);
        }

        /// <summary>Deletes the specified object from the database.</summary>
        public static void DeleteLogicalVolume(LogicalVolume logicalVolume)
        {
            var sqlCmd = new SQLiteCommand();
            sqlCmd.CommandText = "DELETE FROM LogicalVolume WHERE SerialNumber = @SerialNumber";
            sqlCmd.CommandType = CommandType.Text;

            sqlCmd.Parameters.Add(new SQLiteParameter("@SerialNumber", DbType.String));
            sqlCmd.Parameters["@SerialNumber"].Value = logicalVolume.SerialNumber;

            ExecuteNonQuery(sqlCmd);
        }

        /// <summary>Deletes the specified object from the database.</summary>
        public static void DeleteBackupSet(BackupSet backupSet)
        {
            var sqlCmd = new SQLiteCommand();
            sqlCmd.CommandText = "DELETE FROM BackupSet WHERE Guid = @Guid";
            sqlCmd.CommandType = CommandType.Text;

            sqlCmd.Parameters.Add(new SQLiteParameter("@Guid", DbType.String));
            sqlCmd.Parameters["@Guid"].Value = backupSet.Guid;

            ExecuteNonQuery(sqlCmd);
        }

        /// <summary>Deletes the specified object from the database.</summary>
        public static void DeleteFileNode(FileDirectory fileNode)
        {
            var sqlCmd = new SQLiteCommand();
            var cmdText = new StringBuilder("DELETE FROM FileNode WHERE");
            cmdText.Append(" BackupSet = @BackupSet");
            cmdText.Append(" AND DirectoryName = @DirectoryName");


            sqlCmd.Parameters.Add(new SQLiteParameter("@BackupSet", DbType.String));
            sqlCmd.Parameters.Add(new SQLiteParameter("@DirectoryName", DbType.String));

            sqlCmd.Parameters["@BackupSet"].Value = fileNode.BackupSet.Guid;
            sqlCmd.Parameters["@DirectoryName"].Value = fileNode.DirectoryName;

            // Only add name parameter if the provided object is of type fileNode
            if(fileNode is FileNode)
            {
                cmdText.Append(" AND Name = @Name");
                sqlCmd.Parameters.Add(new SQLiteParameter("@Name", DbType.String));
                sqlCmd.Parameters["@Name"].Value = ((FileNode)fileNode).Name;
            }

            sqlCmd.CommandText = cmdText.ToString();
            sqlCmd.CommandType = CommandType.Text;

            ExecuteNonQuery(sqlCmd);
        }
    }
}
