﻿using System;
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
    public static class Database
    {
        #region Fields

        private const string fileName = "db.sqlite";
        private const string folderName = "MediaBackupManager";

        #endregion

        #region Properties

        public static FileIndex Index { get; set; }

        #endregion

        #region Methods

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
            return Path.Combine(GetPath(), fileName);
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

        /// <summary>Ensures that the database exists.</summary>
        /// <returns>Returns true if a new database was created.</returns>
        public static bool CreateDatabase()
        {
            string dbPath = GetFullName();

            if (!Directory.Exists(GetPath()))
                Directory.CreateDirectory(GetPath());

            if (!(File.Exists(dbPath)))
            {
                SQLiteConnection.CreateFile(dbPath);
                return true;
            }
            return false;
        }

        /// <summary>Ensures that all needed database objects are created.</summary>
        public static async Task PrepareDatabaseAsync()
        {
            using (var dbConn = new SQLiteConnection(GetConnectionString(),true))
            {
                var sqlCmd = new SQLiteCommand(dbConn);

                await dbConn.OpenAsync();

                sqlCmd.CommandText = "CREATE TABLE IF NOT EXISTS LogicalVolume (" +
                    "SerialNumber TEXT PRIMARY KEY" +
                    ", Size INTEGER" +
                    ", Type INTEGER" +
                    ", VolumeName TEXT" +
                    ")";

                await sqlCmd.ExecuteNonQueryAsync();

                sqlCmd.CommandText = "CREATE TABLE IF NOT EXISTS FileHash (" +
                    "Checksum TEXT PRIMARY KEY" +
                    ", Length INTEGER" +
                    ", CreationTime TEXT" +
                    ", LastWriteTime TEXT" +
                    ")";
                await sqlCmd.ExecuteNonQueryAsync();

                sqlCmd.CommandText = "CREATE TABLE IF NOT EXISTS FileNode (" +
                    "BackupSet TEXT NOT NULL" +
                    ", DirectoryName TEXT NOT NULL" +
                    ", Name TEXT" +
                    ", Extension TEXT" +
                    ", Checksum TEXT" +
                    ", NodeType INTEGER" +
                    ", PRIMARY KEY (BackupSet, DirectoryName, Name)" +
                    ")";
                await sqlCmd.ExecuteNonQueryAsync();

                sqlCmd.CommandText = "CREATE TABLE IF NOT EXISTS BackupSet (" +
                    "Guid TEXT PRIMARY KEY" +
                    ", Volume TEXT" +
                    ", RootDirectory TEXT" +
                    ", Label TEXT" +
                    ")";
                await sqlCmd.ExecuteNonQueryAsync();

                sqlCmd.CommandText = "CREATE TABLE IF NOT EXISTS Exclusion (" +
                    "Value TEXT PRIMARY KEY" +
                    ")";
                await sqlCmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>Execute the command and return the number of rows inserted/affected by it.</summary>
        /// <param name="command">The command object that will be executed.</param>
        private static async Task<int> ExecuteNonQueryAsync(SQLiteCommand command)
        {
            using (var dbConn = new SQLiteConnection(GetConnectionString(),true))
            {
                try
                {
                    command.Connection = dbConn;
                    dbConn.Open();
                    var rowCount = await command.ExecuteNonQueryAsync();
                    return rowCount;
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
        /// <param name="guid">Guid of the Backupset containing the logical volume.</param>
        public static async Task<List<LogicalVolume>> GetLogicalVolumeAsync(string guid = "")
        {
            var res = new List<LogicalVolume>();

            using (var dbConn = new SQLiteConnection(GetConnectionString(), true))
            {
                var sqlCmd = new SQLiteCommand(dbConn);
                var cmdText = new StringBuilder("SELECT v.*FROM LogicalVolume AS v " +
                    "INNER JOIN BackupSet AS s " +
                    "ON v.SerialNumber = s.Volume");

                if (!string.IsNullOrWhiteSpace(guid))
                {
                    cmdText.Append(" WHERE s.Guid = @Guid");
                    sqlCmd.Parameters.Add(new SQLiteParameter("@Guid", DbType.String));
                    sqlCmd.Parameters["@Guid"].Value = guid;
                }

                sqlCmd.CommandText = cmdText.ToString();
                sqlCmd.CommandType = CommandType.Text;

                await dbConn.OpenAsync();
                using (var reader = await sqlCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        res.Add(new LogicalVolume()
                        {
                            Size = ulong.Parse(reader["Size"].ToString()),
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
        public static async Task<List<BackupSet>> GetBackupSetAsync(string guid = "")
        {
            // To build a complete backup set:
            // load base data from Backupset
            // load volume
            // load file nodes/directories

            var res = new List<BackupSet>();

            using (var dbConn = new SQLiteConnection(GetConnectionString(), true))
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

                await dbConn.OpenAsync();
                using (var reader = await sqlCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var newSet = new BackupSet()
                        {
                            Guid = new Guid(reader["Guid"].ToString()),
                            RootDirectory = reader["RootDirectory"].ToString(),
                            Label = reader["Label"].ToString(),
                            Index = Database.Index
                        };

                        // Load related objects
                        //newSet.Volume = (await GetLogicalVolumeAsync(reader["Volume"].ToString())).FirstOrDefault();
                        res.Add(newSet);
                    }
                }
            }

            return res;
        }

        /// <summary>Retrieves the specified FileHash objects from the database.</summary>
        /// <param name="guid">Guid of the Backupset containing the File hashes.</param>
        public static async Task<List<FileHash>> GetFileHashAsync(string guid = "")
        {
            var res = new List<FileHash>();

            using (var dbConn = new SQLiteConnection(GetConnectionString(), true))
            {
                var sqlCmd = new SQLiteCommand(dbConn);

                var cmdText = new StringBuilder("SELECT h.* FROM FileHash AS h");

                if (!string.IsNullOrWhiteSpace(guid))
                {
                    cmdText.Append(" INNER JOIN FileNode As n ON h.Checksum = n.Checksum");
                    cmdText.Append(" INNER JOIN BackupSet AS s ON n.BackupSet = s.Guid ");

                    cmdText.Append(" WHERE s.Guid = @Guid");
                    sqlCmd.Parameters.Add(new SQLiteParameter("@Guid", DbType.String));
                    sqlCmd.Parameters["@Guid"].Value = guid;
                }

                sqlCmd.CommandText = cmdText.ToString();
                sqlCmd.CommandType = CommandType.Text;

                await dbConn.OpenAsync();
                using (var reader = await sqlCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        res.Add(new FileHash()
                        {
                            Checksum = reader["Checksum"].ToString(),
                            CreationTime = DateTime.Parse(reader["CreationTime"].ToString()),
                            LastWriteTime = DateTime.Parse(reader["LastWriteTime"].ToString()),
                            Length = long.Parse(reader["Length"].ToString())
                        });
                    }
                }
            }

            return res;
        }

        /// <summary>Retrieves list of all file exclusions from the database.</summary>
        public static async Task<List<string>> GetExclusionsAsync()
        {
            var res = new List<string>();

            using (var dbConn = new SQLiteConnection(GetConnectionString(), true))
            {
                var sqlCmd = new SQLiteCommand(dbConn);

                sqlCmd.CommandText = "SELECT * FROM Exclusion";
                sqlCmd.CommandType = CommandType.Text;

                await dbConn.OpenAsync();
                using (var reader = await sqlCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        res.Add(reader["Value"].ToString());
                    }
                }
            }

            return res;
        }

        /// <summary>Retrieves the specified FileDirectory and FileNode objects from the database.</summary>
        /// <param name="guid">Guid of the Backupset containing the File nodes.</param>
        public static async Task<List<FileDirectory>> GetFileNodeAsync(string guid = "")
        {
            var res = new List<FileDirectory>();

            using (var dbConn = new SQLiteConnection(GetConnectionString(), true))
            {
                var sqlCmd = new SQLiteCommand(dbConn);
                sqlCmd.CommandText = "SELECT * FROM FileNode WHERE BackupSet = @Guid";
                sqlCmd.CommandType = CommandType.Text;
                sqlCmd.Parameters.Add(new SQLiteParameter("@Guid", DbType.String));
                sqlCmd.Parameters["@Guid"].Value = guid;

                await dbConn.OpenAsync();
                using (var reader = await sqlCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var node = new FileDirectory();

                        if (int.Parse(reader["NodeType"].ToString()) == 0)  // 0 => Directory, 1 => Node
                        {
                            node.DirectoryName = reader["DirectoryName"].ToString();
                            node.Name = reader["Name"].ToString();
                        }
                        else
                        {
                            // Filenode and FileDirectory are stored in the same table,
                            // based on the nodetype we need to cast to the correct data type
                            node = new FileNode();

                            node.DirectoryName = reader["DirectoryName"].ToString();
                            ((FileNode)node).Name = reader["Name"].ToString();
                            ((FileNode)node).Extension = reader["Extension"].ToString();
                            ((FileNode)node).Checksum = reader["Checksum"].ToString();

                            // Make sure to also properly set the relations between nodes and files
                            //FileHash file;
                            //var crc = reader["File"].ToString();
                            //Index.Hashes.TryGetValue(crc, out file);

                            //if (!(file is null))
                            //{
                            //    ((FileNode)node).Hash = file;
                            //    file.AddNode((FileNode)node);
                            //}
                        }

                        res.Add(node);
                    }
                }
            }

            return res;
        }

        /// <summary>Populates the specified BackupSet with filenodes from the database.</summary>
        public static void LoadBackupSetNodes(BackupSet backupSet)
        {
            using (var dbConn = new SQLiteConnection(GetConnectionString(), true))
            {
                var sqlCmd = new SQLiteCommand(dbConn);
                sqlCmd.CommandText = "SELECT h.*, n.DirectoryName, n.Name, n.Extension, n.NodeType  FROM FileNode n" +
                    " INNER JOIN FileHash h ON n.Checksum = h.Checksum" +
                    " WHERE BackupSet = @Guid";

                sqlCmd.CommandType = CommandType.Text;
                sqlCmd.Parameters.Add(new SQLiteParameter("@Guid", DbType.String));
                sqlCmd.Parameters["@Guid"].Value = backupSet.Guid;

                dbConn.Open();
                using (var reader = sqlCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // Each line contains both a file node and the related has
                        // make sure that the hash is added to the index before creating the node

                        FileHash hash;

                        if (!Index.Hashes.TryGetValue(reader["Checksum"].ToString(), out hash))
                        {
                            // Only create a new hash if it doesn't exist in the index yet
                            hash = new FileHash()
                            {
                                Checksum = reader["Checksum"].ToString(),
                                CreationTime = DateTime.Parse(reader["CreationTime"].ToString()),
                                LastWriteTime = DateTime.Parse(reader["LastWriteTime"].ToString()),
                                Length = long.Parse(reader["Length"].ToString())
                            };

                            Index.Hashes.Add(hash.Checksum, hash);
                        }

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
                            ((FileNode)node).Name = reader["Name"].ToString();
                            ((FileNode)node).Extension = reader["Extension"].ToString();

                            // Make sure to also properly set the relations between nodes and files
                            //FileHash file;
                            //var crc = reader["File"].ToString();
                            //Index.Hashes.TryGetValue(crc, out file);

                            //if (!(file is null))
                            //{
                            //    ((FileNode)node).File = file;
                            //    file.AddNode((FileNode)node);
                            //}
                            ((FileNode)node).Hash = hash;
                            hash.AddNode(((FileNode)node));

                        }

                        // Don't use the AddFileNode function as it would 
                        // try to rescan the nodes that we just downloaded 
                        // right back to the database causing duplicate errors
                        backupSet.FileNodes.Add(node);
                    }
                }
            }
        }

        /// <summary>Inserts the specified FileDirectory or FileNode object to the database.</summary>
        public static async Task InsertFileNodeAsync(FileDirectory fileNode)
        {
            var sqlCmd = new SQLiteCommand();
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
            sqlCmd.Parameters["@Name"].Value = fileNode.Name;
            sqlCmd.Parameters["@Extension"].Value = "";
            sqlCmd.Parameters["@File"].Value = "";
            sqlCmd.Parameters["@NodeType"].Value = 0; // 0 => Directory, 1 => Node

            if (fileNode is FileNode)
            {
                sqlCmd.Parameters["@Extension"].Value = (fileNode as FileNode).Extension;
                sqlCmd.Parameters["@File"].Value = (fileNode as FileNode).Hash.Checksum;
                sqlCmd.Parameters["@BackupSet"].Value = (fileNode as FileNode).BackupSet.Guid;
                sqlCmd.Parameters["@NodeType"].Value = 1;
            }

            await ExecuteNonQueryAsync(sqlCmd);
        }

        /// <summary>Inserts the specified object to the database.</summary>
        public static async Task InsertBackupSetAsync(BackupSet backupSet)
        {
            var sqlCmd = new SQLiteCommand();
            sqlCmd.CommandText = "INSERT INTO BackupSet (" +
                "Guid" +
                ", Volume" +
                ", RootDirectory" +
                ", Label" +
                ") VALUES (" +
                "@Guid" +
                ", @Volume " +
                ", @RootDirectory" +
                ", @Label" +
                ")";

            sqlCmd.CommandType = CommandType.Text;

            sqlCmd.Parameters.Add(new SQLiteParameter("@Guid", DbType.String));
            sqlCmd.Parameters.Add(new SQLiteParameter("@Volume", DbType.String));
            sqlCmd.Parameters.Add(new SQLiteParameter("@RootDirectory", DbType.String));
            sqlCmd.Parameters.Add(new SQLiteParameter("@Label", DbType.String));

            sqlCmd.Parameters["@Guid"].Value = backupSet.Guid;
            sqlCmd.Parameters["@Volume"].Value = backupSet.Volume.SerialNumber;
            sqlCmd.Parameters["@RootDirectory"].Value = backupSet.RootDirectory;
            sqlCmd.Parameters["@Label"].Value = backupSet.Label;

            await ExecuteNonQueryAsync(sqlCmd);
        }

        /// <summary>Inserts the specified object to the database.</summary>
        public static async Task InsertFileHashAsync(FileHash hash)
        {
            var sqlCmd = new SQLiteCommand();
            sqlCmd.CommandText = "INSERT INTO FileHash (" +
                "Checksum" +
                ", Length" +
                ", CreationTime" +
                ", LastWriteTime" +
                ") VALUES (" +
                "@Checksum" +
                ", @Length" +
                ", @CreationTime" +
                ", @LastWriteTime" +
                ")";

            sqlCmd.CommandType = CommandType.Text;

            sqlCmd.Parameters.Add(new SQLiteParameter("@Checksum", DbType.String));
            sqlCmd.Parameters.Add(new SQLiteParameter("@Length", DbType.Int64));
            sqlCmd.Parameters.Add(new SQLiteParameter("@CreationTime", DbType.DateTime));
            sqlCmd.Parameters.Add(new SQLiteParameter("@LastWriteTime", DbType.DateTime));

            sqlCmd.Parameters["@Checksum"].Value = hash.Checksum;
            sqlCmd.Parameters["@Length"].Value = hash.Length;
            sqlCmd.Parameters["@CreationTime"].Value = hash.CreationTime;
            sqlCmd.Parameters["@LastWriteTime"].Value = hash.LastWriteTime;

            await ExecuteNonQueryAsync(sqlCmd);
        }

        /// <summary>Inserts the specified object to the database.</summary>
        public static async Task InsertLogicalVolumeAsync(LogicalVolume logicalVolume)
        {
            var sqlCmd = new SQLiteCommand();

            sqlCmd.CommandText = "INSERT INTO LogicalVolume (" +
                "SerialNumber" +
                ", Size" +
                ", Type" +
                ", VolumeName" +
                ") VALUES (" +
                "@SerialNumber " +
                ", @Size" +
                ", @Type" +
                ", @VolumeName" +
                ")";

            sqlCmd.CommandType = CommandType.Text;

            sqlCmd.Parameters.Add(new SQLiteParameter("@SerialNumber", DbType.String));
            sqlCmd.Parameters.Add(new SQLiteParameter("@Size", DbType.UInt64));
            sqlCmd.Parameters.Add(new SQLiteParameter("@Type", DbType.Int16));
            sqlCmd.Parameters.Add(new SQLiteParameter("@VolumeName", DbType.String));

            sqlCmd.Parameters["@SerialNumber"].Value = logicalVolume.SerialNumber;
            sqlCmd.Parameters["@Size"].Value = logicalVolume.Size;
            sqlCmd.Parameters["@Type"].Value = (int)logicalVolume.Type;
            sqlCmd.Parameters["@VolumeName"].Value = logicalVolume.VolumeName;

            await ExecuteNonQueryAsync(sqlCmd);
        }

        /// <summary>Inserts the specified object to the database.</summary>
        public static async Task InsertExclusionAsync(string exclusion)
        {
            var sqlCmd = new SQLiteCommand();
            sqlCmd.CommandText = "INSERT INTO Exclusion (" +
                "Value" +
                ") VALUES (" +
                "@Value" +
                ")";

            sqlCmd.CommandType = CommandType.Text;

            sqlCmd.Parameters.Add(new SQLiteParameter("@Value", DbType.String));
            sqlCmd.Parameters["@Value"].Value = exclusion;

            await ExecuteNonQueryAsync(sqlCmd);
        }

        /// <summary>Deletes the specified object from the database.</summary>
        public static async Task DeleteFileHashAsync(FileHash hash)
        {
            var sqlCmd = new SQLiteCommand();
            sqlCmd.CommandText = "DELETE FROM FileHash WHERE Checksum = @Checksum";
            sqlCmd.CommandType = CommandType.Text;

            sqlCmd.Parameters.Add(new SQLiteParameter("@Checksum", DbType.String));
            sqlCmd.Parameters["@Checksum"].Value = hash.Checksum;

            await ExecuteNonQueryAsync(sqlCmd);
        }

        /// <summary>Deletes the specified object from the database.</summary>
        public static async Task DeleteLogicalVolumeAsync(LogicalVolume logicalVolume)
        {
            var sqlCmd = new SQLiteCommand();
            sqlCmd.CommandText = "DELETE FROM LogicalVolume WHERE SerialNumber = @SerialNumber";
            sqlCmd.CommandType = CommandType.Text;

            sqlCmd.Parameters.Add(new SQLiteParameter("@SerialNumber", DbType.String));
            sqlCmd.Parameters["@SerialNumber"].Value = logicalVolume.SerialNumber;

            await ExecuteNonQueryAsync(sqlCmd);
        }

        /// <summary>Deletes the specified object from the database.</summary>
        public static async Task DeleteBackupSetAsync(BackupSet backupSet)
        {
            var sqlCmd = new SQLiteCommand();
            sqlCmd.CommandText = "DELETE FROM BackupSet WHERE Guid = @Guid";
            sqlCmd.CommandType = CommandType.Text;

            sqlCmd.Parameters.Add(new SQLiteParameter("@Guid", DbType.String));
            sqlCmd.Parameters["@Guid"].Value = backupSet.Guid;

            await ExecuteNonQueryAsync(sqlCmd);
        }

        /// <summary>Deletes the specified object from the database.</summary>
        public static async Task DeleteFileNodeAsync(FileDirectory fileNode)
        {
            var sqlCmd = new SQLiteCommand();
            var cmdText = new StringBuilder("DELETE FROM FileNode WHERE");
            cmdText.Append(" BackupSet = @BackupSet");
            cmdText.Append(" AND DirectoryName = @DirectoryName");
            cmdText.Append(" AND Name = @Name");


            sqlCmd.Parameters.Add(new SQLiteParameter("@BackupSet", DbType.String));
            sqlCmd.Parameters.Add(new SQLiteParameter("@DirectoryName", DbType.String));
            sqlCmd.Parameters.Add(new SQLiteParameter("@Name", DbType.String));

            sqlCmd.Parameters["@BackupSet"].Value = fileNode.BackupSet.Guid;
            sqlCmd.Parameters["@DirectoryName"].Value = fileNode.DirectoryName;
            sqlCmd.Parameters["@Name"].Value = "";

            // Only add name parameter if the provided object is of type fileNode
            if (fileNode is FileNode)
                sqlCmd.Parameters["@Name"].Value = ((FileNode)fileNode).Name;

            sqlCmd.CommandText = cmdText.ToString();
            sqlCmd.CommandType = CommandType.Text;

            await ExecuteNonQueryAsync(sqlCmd);
        }

        /// <summary>Deletes the specified object from the database.</summary>
        public static async Task DeleteExclusionAsync(string exclusion)
        {
            var sqlCmd = new SQLiteCommand();
            sqlCmd.CommandText = "DELETE FROM Exclusion WHERE Value = @Value";
            sqlCmd.CommandType = CommandType.Text;

            sqlCmd.Parameters.Add(new SQLiteParameter("@Value", DbType.String));
            sqlCmd.Parameters["@Value"].Value = exclusion;

            await ExecuteNonQueryAsync(sqlCmd);
        }

        /// <summary>
        /// Deletes the specified list from the database via transaction.</summary>
        public static async Task BatchDeleteFileNodeAsync(List<FileDirectory> nodes)
        {
            var commandText = "DELETE FROM FileNode WHERE" +
            " BackupSet = @BackupSet" +
            " AND DirectoryName = @DirectoryName" +
            " AND Name = @Name";

            var dbConn = new SQLiteConnection(GetConnectionString(), true);

            using (dbConn.OpenAsync())
            {
                using (var transaction = dbConn.BeginTransaction())
                {
                    try
                    {
                        foreach (var node in nodes)
                        {
                            var sqlCmd = new SQLiteCommand(commandText, dbConn, transaction);
                            sqlCmd.CommandType = CommandType.Text;

                            sqlCmd.Parameters.Add(new SQLiteParameter("@BackupSet", DbType.String));
                            sqlCmd.Parameters.Add(new SQLiteParameter("@DirectoryName", DbType.String));
                            sqlCmd.Parameters.Add(new SQLiteParameter("@Name", DbType.String));

                            sqlCmd.Parameters["@BackupSet"].Value = node.BackupSet.Guid;
                            sqlCmd.Parameters["@DirectoryName"].Value = node.DirectoryName;
                            sqlCmd.Parameters["@Name"].Value = "";

                            if (node is FileNode)
                                sqlCmd.Parameters["@Name"].Value = (node as FileNode).Name;

                            await sqlCmd.ExecuteNonQueryAsync();
                        }
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }

                    transaction.Commit();
                }
            }
        }

        /// <summary>
        /// Deletes the specified list from the database via transaction.</summary>
        public static async Task BatchDeleteFileHashAsync(List<FileHash> hashes)
        {
            var commandText = "DELETE FROM FileHash WHERE" +
            " Checksum = @Checksum";

            var dbConn = new SQLiteConnection(GetConnectionString(), true);

            using (dbConn.OpenAsync())
            {
                using (var transaction = dbConn.BeginTransaction())
                {
                    try
                    {
                        foreach (var hash in hashes)
                        {
                            var sqlCmd = new SQLiteCommand(commandText, dbConn, transaction);
                            sqlCmd.CommandType = CommandType.Text;

                            sqlCmd.Parameters.Add(new SQLiteParameter("@Checksum", DbType.String));
                            sqlCmd.Parameters["@Checksum"].Value = hash.Checksum;

                            await sqlCmd.ExecuteNonQueryAsync();
                        }
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }

                    transaction.Commit();
                }
            }
        }

        /// <summary>
        /// Inserts the specified list into the database via transaction.</summary>
        public static async Task BatchInsertBackupSetAsync(List<BackupSet> sets)
        {
            var commandText = "INSERT INTO BackupSet (" +
                "Guid" +
                ", Volume" +
                ", RootDirectory" +
                ", Label" +
                ") VALUES (" +
                "@Guid" +
                ", @Volume " +
                ", @RootDirectory" +
                ", @Label" +
                ")";

            var dbConn = new SQLiteConnection(GetConnectionString(), true);

            using (dbConn.OpenAsync())
            {
                using (var transaction = dbConn.BeginTransaction())
                {
                    try
                    {
                        foreach (var set in sets)
                        {
                            var sqlCmd = new SQLiteCommand(commandText, dbConn, transaction);
                            sqlCmd.CommandType = CommandType.Text;

                            sqlCmd.Parameters.Add(new SQLiteParameter("@Guid", DbType.String));
                            sqlCmd.Parameters.Add(new SQLiteParameter("@Volume", DbType.String));
                            sqlCmd.Parameters.Add(new SQLiteParameter("@RootDirectory", DbType.String));
                            sqlCmd.Parameters.Add(new SQLiteParameter("@Label", DbType.String));

                            sqlCmd.Parameters["@Guid"].Value = set.Guid;
                            sqlCmd.Parameters["@Volume"].Value = set.Volume.SerialNumber;
                            sqlCmd.Parameters["@RootDirectory"].Value = set.RootDirectory;
                            sqlCmd.Parameters["@Label"].Value = set.Label;

                            await sqlCmd.ExecuteNonQueryAsync();
                        }
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }

                    transaction.Commit();
                }
            }
        }

        /// <summary>
        /// Inserts the specified list into the database via transaction.</summary>
        public static async Task BatchInsertFileHashAsync(List<FileHash> hashes)
        {
            var commandText = "INSERT INTO FileHash (" +
                "Checksum" +
                ", Length" +
                ", CreationTime" +
                ", LastWriteTime" +
                ") VALUES (" +
                "@Checksum" +
                ", @Length" +
                ", @CreationTime" +
                ", @LastWriteTime" +
                ")";

            var dbConn = new SQLiteConnection(GetConnectionString(), true);

            using (dbConn.OpenAsync())
            {
                using (var transaction = dbConn.BeginTransaction())
                {
                    try
                    {
                        foreach (var hash in hashes)
                        {
                            var sqlCmd = new SQLiteCommand(commandText, dbConn, transaction);
                            sqlCmd.CommandType = CommandType.Text;

                            sqlCmd.Parameters.Add(new SQLiteParameter("@Checksum", DbType.String));
                            sqlCmd.Parameters.Add(new SQLiteParameter("@Length", DbType.Int64));
                            sqlCmd.Parameters.Add(new SQLiteParameter("@CreationTime", DbType.DateTime));
                            sqlCmd.Parameters.Add(new SQLiteParameter("@LastWriteTime", DbType.DateTime));

                            sqlCmd.Parameters["@Checksum"].Value = hash.Checksum;
                            sqlCmd.Parameters["@Length"].Value = hash.Length;
                            sqlCmd.Parameters["@CreationTime"].Value = hash.CreationTime;
                            sqlCmd.Parameters["@LastWriteTime"].Value = hash.LastWriteTime;

                            await sqlCmd.ExecuteNonQueryAsync();
                        }
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }

                    transaction.Commit();
                }
            }
        }

        /// <summary>
        /// Inserts the specified list into the database via transaction.</summary>
        public static async Task BatchInsertFileNodeAsync(List<FileDirectory> nodes)
        {
            var commandText = "INSERT INTO FileNode (" +
                "BackupSet" +
                ", DirectoryName" +
                ", Name" +
                ", Extension" +
                ", Checksum" +
                ", NodeType" +
                ") VALUES (" +
                "@BackupSet" +
                ", @DirectoryName" +
                ", @Name" +
                ", @Extension" +
                ", @Checksum" +
                ", @NodeType" +
                ")";


            var dbConn = new SQLiteConnection(GetConnectionString(), true);

            using (dbConn.OpenAsync())
            {
                using (var transaction = dbConn.BeginTransaction())
                {
                    try
                    {
                        foreach (var node in nodes)
                        {
                            var sqlCmd = new SQLiteCommand(commandText, dbConn, transaction);
                            sqlCmd.CommandType = CommandType.Text;

                            sqlCmd.Parameters.Add(new SQLiteParameter("@DirectoryName", DbType.String));
                            sqlCmd.Parameters.Add(new SQLiteParameter("@Name", DbType.String));
                            sqlCmd.Parameters.Add(new SQLiteParameter("@Extension", DbType.String));
                            sqlCmd.Parameters.Add(new SQLiteParameter("@Checksum", DbType.String));
                            sqlCmd.Parameters.Add(new SQLiteParameter("@BackupSet", DbType.String));
                            sqlCmd.Parameters.Add(new SQLiteParameter("@NodeType", DbType.Int16));

                            sqlCmd.Parameters["@DirectoryName"].Value = node.DirectoryName;
                            sqlCmd.Parameters["@BackupSet"].Value = node.BackupSet.Guid;
                            sqlCmd.Parameters["@Name"].Value = node.Name;
                            sqlCmd.Parameters["@Extension"].Value = "";
                            sqlCmd.Parameters["@Checksum"].Value = "";
                            sqlCmd.Parameters["@NodeType"].Value = 0; // 0 => Directory, 1 => Node

                            if (node is FileNode)
                            {
                                sqlCmd.Parameters["@Extension"].Value = (node as FileNode).Extension;
                                sqlCmd.Parameters["@Checksum"].Value = (node as FileNode).Hash.Checksum;
                                sqlCmd.Parameters["@BackupSet"].Value = (node as FileNode).BackupSet.Guid;
                                sqlCmd.Parameters["@NodeType"].Value = 1;
                            }

                            await sqlCmd.ExecuteNonQueryAsync();
                        }
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }

                    transaction.Commit();
                }
            }
        }

        /// <summary>
        /// Inserts the specified list into the database via transaction.</summary>
        public static async Task BatchInsertLogicalVolumeAsync(List<LogicalVolume> volumes)
        {
            var commandText = "INSERT INTO LogicalVolume (" +
                "SerialNumber" +
                ", Size" +
                ", Type" +
                ", VolumeName" +
                ") VALUES (" +
                "@SerialNumber " +
                ", @Size" +
                ", @Type" +
                ", @VolumeName" +
                ")";

            var dbConn = new SQLiteConnection(GetConnectionString(), true);

            using (dbConn.OpenAsync())
            {
                using (var transaction = dbConn.BeginTransaction())
                {
                    try
                    {
                        foreach (var volume in volumes)
                        {
                            var sqlCmd = new SQLiteCommand(commandText, dbConn, transaction);
                            sqlCmd.CommandType = CommandType.Text;

                            sqlCmd.Parameters.Add(new SQLiteParameter("@SerialNumber", DbType.String));
                            sqlCmd.Parameters.Add(new SQLiteParameter("@Size", DbType.UInt64));
                            sqlCmd.Parameters.Add(new SQLiteParameter("@Type", DbType.Int16));
                            sqlCmd.Parameters.Add(new SQLiteParameter("@VolumeName", DbType.String));

                            sqlCmd.Parameters["@SerialNumber"].Value = volume.SerialNumber;
                            sqlCmd.Parameters["@Size"].Value = volume.Size;
                            sqlCmd.Parameters["@Type"].Value = (int)volume.Type;
                            sqlCmd.Parameters["@VolumeName"].Value = volume.VolumeName;

                            await sqlCmd.ExecuteNonQueryAsync();
                        }
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }

                    transaction.Commit();
                }
            }
        }

        #endregion
    }
}
