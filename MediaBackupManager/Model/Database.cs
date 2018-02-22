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
    /// <summary>
    /// Support class used for database access.</summary>
    public static class Database
    {
        #region Fields

        private const string fileName = "db.sqlite";
        private const string directory = "MediaBackupManager";

        #endregion

        #region Properties

        /// <summary>
        /// Gets the file name of the database.</summary>  
        public static string FileName
        {
            get => fileName;
        }

        /// <summary>
        /// Gets the directory name of the database.</summary>  
        public static string Directory
        {
            get => directory;
        }

        /// <summary>
        /// Gets the file path to the database.</summary>  
        public static string FilePath
        {
            get => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDoc‌​uments), Directory);
        }

        /// <summary>
        /// Gets the full file name of the database.</summary>  
        public static string FullName
        {
            get => Path.Combine(FilePath, FileName);
        }


        #endregion

        #region Methods

        /// <summary>
        /// Returns the connection string of the database.</summary>  
        public static string GetConnectionString()
        {
            return (new SQLiteConnectionStringBuilder()
            {
                DataSource = FullName,
                Version = 3,
                UseUTF16Encoding = true,
                ForeignKeys = true
            }).ConnectionString;
        }

        /// <summary>
        /// Ensures that the database exists.</summary>
        /// <returns>Returns true if a new database was created.</returns>
        public static bool CreateDatabase()
        {
            string dbPath = FullName;

            if (!System.IO.Directory.Exists(FilePath))
                System.IO.Directory.CreateDirectory(FilePath);

            if (!(File.Exists(dbPath)))
            {
                SQLiteConnection.CreateFile(dbPath);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Ensures that all needed database objects are created.</summary>
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
                    "Archive TEXT NOT NULL" +
                    ", DirectoryName TEXT" +
                    ", Name TEXT" +
                    ", Extension TEXT" +
                    ", Checksum TEXT" +
                    ", NodeType INTEGER" +
                    ", PRIMARY KEY (Archive, DirectoryName, Name)" +
                    ")";
                await sqlCmd.ExecuteNonQueryAsync();

                sqlCmd.CommandText = "CREATE TABLE IF NOT EXISTS Archive (" +
                    "Guid TEXT PRIMARY KEY" +
                    ", Volume TEXT" +
                    ", RootDirectoryPath TEXT" +
                    ", Label TEXT" +
                    ", LastScanDate TEXT" +
                    ")";
                await sqlCmd.ExecuteNonQueryAsync();

                sqlCmd.CommandText = "CREATE TABLE IF NOT EXISTS Exclusion (" +
                    "Value TEXT PRIMARY KEY" +
                    ")";
                await sqlCmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Execute the command and return the number of rows inserted/affected by it.</summary>
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

        /// <summary>
        /// Retrieves the specified LogicalVolume objects from the database.</summary>
        /// <param name="guid">Guid of the Archive containing the logical volume.</param>
        public static async Task<List<LogicalVolume>> GetLogicalVolumeAsync(string guid = "")
        {
            var res = new List<LogicalVolume>();

            using (var dbConn = new SQLiteConnection(GetConnectionString(), true))
            {
                var sqlCmd = new SQLiteCommand(dbConn);
                var cmdText = new StringBuilder("SELECT v.*FROM LogicalVolume AS v " +
                    "INNER JOIN Archive AS s " +
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

        /// <summary>
        /// Retrieves the specified Archive objects from the database.</summary>
        /// <param name="guid">Guid of the object that should be retrieved.</param>
        public static async Task<List<Archive>> GetArchiveAsync(string guid = "")
        {
            // To build a complete archive:
            // load base data from Archive
            // load volume
            // load file nodes/directories

            var res = new List<Archive>();

            using (var dbConn = new SQLiteConnection(GetConnectionString(), true))
            {
                var sqlCmd = new SQLiteCommand(dbConn);

                var cmdText = new StringBuilder("SELECT * FROM Archive");
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
                        var newArchive = new Archive()
                        {
                            Guid = new Guid(reader["Guid"].ToString()),
                            RootDirectoryPath = reader["RootDirectoryPath"].ToString(),
                            Label = reader["Label"].ToString(),
                            LastScanDate = DateTime.Parse(reader["LastScanDate"].ToString()),
                        };

                        // Load related objects
                        res.Add(newArchive);
                    }
                }
            }

            return res;
        }

        /// <summary>
        /// Retrieves the specified FileHash objects from the database.</summary>
        /// <param name="guid">Guid of the Archive containing the File hashes.</param>
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
                    cmdText.Append(" INNER JOIN Archive AS s ON n.Archive = s.Guid ");

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

        /// <summary>
        /// Retrieves list of all file exclusions from the database.</summary>
        public static async Task<List<string>> GetExclusionsAsync()
        {
            var res = new List<string>();

            using (var dbConn = new SQLiteConnection(GetConnectionString(), true))
            {
                var sqlCmd = new SQLiteCommand(dbConn)
                {
                    CommandText = "SELECT * FROM Exclusion",
                    CommandType = CommandType.Text
                };

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

        /// <summary>
        /// Retrieves the specified FileDirectory and FileNode objects from the database.</summary>
        /// <param name="guid">Guid of the Archive containing the File nodes.</param>
        public static async Task<List<FileDirectory>> GetFileNodeAsync(string guid = "")
        {
            var res = new List<FileDirectory>();

            using (var dbConn = new SQLiteConnection(GetConnectionString(), true))
            {
                var sqlCmd = new SQLiteCommand(dbConn)
                {
                    CommandText = "SELECT * FROM FileNode WHERE Archive = @Guid",
                    CommandType = CommandType.Text
                };
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
                            node = new FileNode
                            {
                                DirectoryName = reader["DirectoryName"].ToString()
                            };
                            ((FileNode)node).Name = reader["Name"].ToString();
                            ((FileNode)node).Extension = reader["Extension"].ToString();
                            ((FileNode)node).Checksum = reader["Checksum"].ToString();
                        }

                        res.Add(node);
                    }
                }
            }

            return res;
        }

        /// <summary>
        /// Inserts the specified FileDirectory or FileNode object to the database.</summary>
        public static async Task InsertFileNodeAsync(FileDirectory fileNode)
        {
            var sqlCmd = new SQLiteCommand
            {
                CommandText = "INSERT INTO FileNode (" +
                "Archive" +
                ", DirectoryName" +
                ", Name" +
                ", Extension" +
                ", File" +
                ", NodeType" +
                ") VALUES (" +
                "@Archive" +
                ", @DirectoryName" +
                ", @Name" +
                ", @Extension" +
                ", @File" +
                ", @NodeType" +
                ")",

                CommandType = CommandType.Text
            };

            sqlCmd.Parameters.Add(new SQLiteParameter("@DirectoryName", DbType.String));
            sqlCmd.Parameters.Add(new SQLiteParameter("@Name", DbType.String));
            sqlCmd.Parameters.Add(new SQLiteParameter("@Extension", DbType.String));
            sqlCmd.Parameters.Add(new SQLiteParameter("@File", DbType.String));
            sqlCmd.Parameters.Add(new SQLiteParameter("@Archive", DbType.String));
            sqlCmd.Parameters.Add(new SQLiteParameter("@NodeType", DbType.Int16));

            sqlCmd.Parameters["@DirectoryName"].Value = fileNode.DirectoryName;
            sqlCmd.Parameters["@Archive"].Value = fileNode.Archive.Guid;
            sqlCmd.Parameters["@Name"].Value = fileNode.Name;
            sqlCmd.Parameters["@Extension"].Value = "";
            sqlCmd.Parameters["@File"].Value = "";
            sqlCmd.Parameters["@NodeType"].Value = 0; // 0 => Directory, 1 => Node

            if (fileNode is FileNode)
            {
                sqlCmd.Parameters["@Extension"].Value = (fileNode as FileNode).Extension;
                sqlCmd.Parameters["@File"].Value = (fileNode as FileNode).Hash.Checksum;
                sqlCmd.Parameters["@Archive"].Value = (fileNode as FileNode).Archive.Guid;
                sqlCmd.Parameters["@NodeType"].Value = 1;
            }

            await ExecuteNonQueryAsync(sqlCmd);
        }

        /// <summary>
        /// Inserts the specified object to the database.</summary>
        public static async Task InsertArchiveAsync(Archive archive)
        {
            var sqlCmd = new SQLiteCommand
            {
                CommandText = "INSERT INTO Archive (" +
                "Guid" +
                ", Volume" +
                ", RootDirectoryPath" +
                ", Label" +
                ", LastScanDate" +
                ") VALUES (" +
                "@Guid" +
                ", @Volume " +
                ", @RootDirectoryPath" +
                ", @Label" +
                ", @LastScanDate" +
                ")",

                CommandType = CommandType.Text
            };

            sqlCmd.Parameters.Add(new SQLiteParameter("@Guid", DbType.String));
            sqlCmd.Parameters.Add(new SQLiteParameter("@Volume", DbType.String));
            sqlCmd.Parameters.Add(new SQLiteParameter("@RootDirectoryPath", DbType.String));
            sqlCmd.Parameters.Add(new SQLiteParameter("@Label", DbType.String));
            sqlCmd.Parameters.Add(new SQLiteParameter("@LastScanDate", DbType.DateTime));

            sqlCmd.Parameters["@Guid"].Value = archive.Guid;
            sqlCmd.Parameters["@Volume"].Value = archive.Volume.SerialNumber;
            sqlCmd.Parameters["@RootDirectoryPath"].Value = archive.RootDirectoryPath;
            sqlCmd.Parameters["@Label"].Value = archive.Label;
            sqlCmd.Parameters["@LastScanDate"].Value = archive.LastScanDate;

            await ExecuteNonQueryAsync(sqlCmd);
        }

        /// <summary>
        /// Inserts the specified object to the database.</summary>
        public static async Task InsertFileHashAsync(FileHash hash)
        {
            var sqlCmd = new SQLiteCommand
            {
                CommandText = "INSERT INTO FileHash (" +
                "Checksum" +
                ", Length" +
                ", CreationTime" +
                ", LastWriteTime" +
                ") VALUES (" +
                "@Checksum" +
                ", @Length" +
                ", @CreationTime" +
                ", @LastWriteTime" +
                ")",

                CommandType = CommandType.Text
            };

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

        /// <summary>
        /// Inserts the specified object to the database.</summary>
        public static async Task InsertLogicalVolumeAsync(LogicalVolume logicalVolume)
        {
            var sqlCmd = new SQLiteCommand
            {
                CommandText = "INSERT INTO LogicalVolume (" +
                "SerialNumber" +
                ", Size" +
                ", Type" +
                ", VolumeName" +
                ") VALUES (" +
                "@SerialNumber " +
                ", @Size" +
                ", @Type" +
                ", @VolumeName" +
                ")",

                CommandType = CommandType.Text
            };

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

        /// <summary>
        /// Inserts the specified object to the database.</summary>
        public static async Task InsertExclusionAsync(string exclusion)
        {
            var sqlCmd = new SQLiteCommand
            {
                CommandText = "INSERT INTO Exclusion (" +
                "Value" +
                ") VALUES (" +
                "@Value" +
                ")",

                CommandType = CommandType.Text
            };

            sqlCmd.Parameters.Add(new SQLiteParameter("@Value", DbType.String));
            sqlCmd.Parameters["@Value"].Value = exclusion;

            await ExecuteNonQueryAsync(sqlCmd);
        }

        /// <summary>
        /// Deletes the specified object from the database.</summary>
        public static async Task DeleteFileHashAsync(FileHash hash)
        {
            var sqlCmd = new SQLiteCommand
            {
                CommandText = "DELETE FROM FileHash WHERE Checksum = @Checksum",
                CommandType = CommandType.Text
            };

            sqlCmd.Parameters.Add(new SQLiteParameter("@Checksum", DbType.String));
            sqlCmd.Parameters["@Checksum"].Value = hash.Checksum;

            await ExecuteNonQueryAsync(sqlCmd);
        }

        /// <summary>
        /// Deletes the specified object from the database.</summary>
        public static async Task DeleteLogicalVolumeAsync(LogicalVolume logicalVolume)
        {
            var sqlCmd = new SQLiteCommand
            {
                CommandText = "DELETE FROM LogicalVolume WHERE SerialNumber = @SerialNumber",
                CommandType = CommandType.Text
            };

            sqlCmd.Parameters.Add(new SQLiteParameter("@SerialNumber", DbType.String));
            sqlCmd.Parameters["@SerialNumber"].Value = logicalVolume.SerialNumber;

            await ExecuteNonQueryAsync(sqlCmd);
        }

        /// <summary>
        /// Deletes the specified object from the database.</summary>
        public static async Task DeleteArchiveAsync(Archive archive)
        {
            var sqlCmd = new SQLiteCommand
            {
                CommandText = "DELETE FROM Archive WHERE Guid = @Guid",
                CommandType = CommandType.Text
            };

            sqlCmd.Parameters.Add(new SQLiteParameter("@Guid", DbType.String));
            sqlCmd.Parameters["@Guid"].Value = archive.Guid;

            await ExecuteNonQueryAsync(sqlCmd);
        }

        /// <summary>
        /// Deletes the specified object from the database.</summary>
        public static async Task DeleteFileNodeAsync(FileDirectory fileNode)
        {
            var sqlCmd = new SQLiteCommand();
            var cmdText = new StringBuilder("DELETE FROM FileNode WHERE");
            cmdText.Append(" Archive = @Archive");
            cmdText.Append(" AND DirectoryName = @DirectoryName");
            cmdText.Append(" AND Name = @Name");


            sqlCmd.Parameters.Add(new SQLiteParameter("@Archive", DbType.String));
            sqlCmd.Parameters.Add(new SQLiteParameter("@DirectoryName", DbType.String));
            sqlCmd.Parameters.Add(new SQLiteParameter("@Name", DbType.String));

            sqlCmd.Parameters["@Archive"].Value = fileNode.Archive.Guid;
            sqlCmd.Parameters["@DirectoryName"].Value = fileNode.DirectoryName;
            sqlCmd.Parameters["@Name"].Value = "";

            // Only add name parameter if the provided object is of type fileNode
            if (fileNode is FileNode)
                sqlCmd.Parameters["@Name"].Value = ((FileNode)fileNode).Name;

            sqlCmd.CommandText = cmdText.ToString();
            sqlCmd.CommandType = CommandType.Text;

            await ExecuteNonQueryAsync(sqlCmd);
        }

        /// <summary>
        /// Deletes the specified object from the database.</summary>
        public static async Task DeleteExclusionAsync(string exclusion)
        {
            var sqlCmd = new SQLiteCommand
            {
                CommandText = "DELETE FROM Exclusion WHERE Value = @Value",
                CommandType = CommandType.Text
            };

            sqlCmd.Parameters.Add(new SQLiteParameter("@Value", DbType.String));
            sqlCmd.Parameters["@Value"].Value = exclusion;

            await ExecuteNonQueryAsync(sqlCmd);
        }

        /// <summary>
        /// Updates the label of the provided Archive.</summary>
        public static async Task UpdateArchiveLabel(Archive archive, string newLabel)
        {
            var sqlCmd = new SQLiteCommand
            {
                CommandText = "UPDATE Archive" +
                " SET Label = @Label" +
                " WHERE Guid = @Guid",
                CommandType = CommandType.Text
            };

            sqlCmd.Parameters.Add(new SQLiteParameter("@Guid", DbType.String));
            sqlCmd.Parameters.Add(new SQLiteParameter("@Label", DbType.String));

            sqlCmd.Parameters["@Guid"].Value = archive.Guid;
            sqlCmd.Parameters["@Label"].Value = newLabel;

            await ExecuteNonQueryAsync(sqlCmd);
        }

        /// <summary>
        /// Deletes the specified list from the database via transaction.</summary>
        public static async Task BatchDeleteFileNodeAsync(List<FileDirectory> nodes)
        {
            var commandText = "DELETE FROM FileNode WHERE" +
            " Archive = @Archive" +
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
                            var sqlCmd = new SQLiteCommand(commandText, dbConn, transaction)
                            {
                                CommandType = CommandType.Text
                            };

                            sqlCmd.Parameters.Add(new SQLiteParameter("@Archive", DbType.String));
                            sqlCmd.Parameters.Add(new SQLiteParameter("@DirectoryName", DbType.String));
                            sqlCmd.Parameters.Add(new SQLiteParameter("@Name", DbType.String));

                            sqlCmd.Parameters["@Archive"].Value = node.Archive.Guid;
                            sqlCmd.Parameters["@DirectoryName"].Value = node.DirectoryName;
                            sqlCmd.Parameters["@Name"].Value = node.Name;

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
                            var sqlCmd = new SQLiteCommand(commandText, dbConn, transaction)
                            {
                                CommandType = CommandType.Text
                            };

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
        public static async Task BatchInsertArchiveAsync(List<Archive> archives)
        {
            var commandText = "INSERT INTO Archive (" +
                "Guid" +
                ", Volume" +
                ", RootDirectoryPath" +
                ", Label" +
                ", LastScanDate" +
                ") VALUES (" +
                "@Guid" +
                ", @Volume " +
                ", @RootDirectoryPath" +
                ", @Label" +
                ", @LastScanDate" +
                ")";

            var dbConn = new SQLiteConnection(GetConnectionString(), true);

            using (dbConn.OpenAsync())
            {
                using (var transaction = dbConn.BeginTransaction())
                {
                    try
                    {
                        foreach (var archive in archives)
                        {
                            var sqlCmd = new SQLiteCommand(commandText, dbConn, transaction)
                            {
                                CommandType = CommandType.Text
                            };

                            sqlCmd.Parameters.Add(new SQLiteParameter("@Guid", DbType.String));
                            sqlCmd.Parameters.Add(new SQLiteParameter("@Volume", DbType.String));
                            sqlCmd.Parameters.Add(new SQLiteParameter("@RootDirectoryPath", DbType.String));
                            sqlCmd.Parameters.Add(new SQLiteParameter("@Label", DbType.String));
                            sqlCmd.Parameters.Add(new SQLiteParameter("@LastScanDate", DbType.DateTime));

                            sqlCmd.Parameters["@Guid"].Value = archive.Guid;
                            sqlCmd.Parameters["@Volume"].Value = archive.Volume.SerialNumber;
                            sqlCmd.Parameters["@RootDirectoryPath"].Value = archive.RootDirectoryPath;
                            sqlCmd.Parameters["@Label"].Value = archive.Label;
                            sqlCmd.Parameters["@LastScanDate"].Value = archive.LastScanDate;

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
                            var sqlCmd = new SQLiteCommand(commandText, dbConn, transaction)
                            {
                                CommandType = CommandType.Text
                            };

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
                "Archive" +
                ", DirectoryName" +
                ", Name" +
                ", Extension" +
                ", Checksum" +
                ", NodeType" +
                ") VALUES (" +
                "@Archive" +
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
                            var sqlCmd = new SQLiteCommand(commandText, dbConn, transaction)
                            {
                                CommandType = CommandType.Text
                            };

                            sqlCmd.Parameters.Add(new SQLiteParameter("@DirectoryName", DbType.String));
                            sqlCmd.Parameters.Add(new SQLiteParameter("@Name", DbType.String));
                            sqlCmd.Parameters.Add(new SQLiteParameter("@Extension", DbType.String));
                            sqlCmd.Parameters.Add(new SQLiteParameter("@Checksum", DbType.String));
                            sqlCmd.Parameters.Add(new SQLiteParameter("@Archive", DbType.String));
                            sqlCmd.Parameters.Add(new SQLiteParameter("@NodeType", DbType.Int16));

                            sqlCmd.Parameters["@DirectoryName"].Value = node.DirectoryName;
                            sqlCmd.Parameters["@Archive"].Value = node.Archive.Guid;
                            sqlCmd.Parameters["@Name"].Value = node.Name;
                            sqlCmd.Parameters["@Extension"].Value = "";
                            sqlCmd.Parameters["@Checksum"].Value = "";
                            sqlCmd.Parameters["@NodeType"].Value = 0; // 0 => Directory, 1 => Node

                            if (node is FileNode)
                            {
                                sqlCmd.Parameters["@Extension"].Value = (node as FileNode).Extension;
                                sqlCmd.Parameters["@Archive"].Value = (node as FileNode).Archive.Guid;
                                sqlCmd.Parameters["@NodeType"].Value = 1;

                                if((node as FileNode).Hash != null)
                                    sqlCmd.Parameters["@Checksum"].Value = (node as FileNode).Hash.Checksum;
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
                            var sqlCmd = new SQLiteCommand(commandText, dbConn, transaction)
                            {
                                CommandType = CommandType.Text
                            };

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
