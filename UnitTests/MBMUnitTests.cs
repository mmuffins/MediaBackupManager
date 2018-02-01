using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MediaBackupManager.Model;
using System.IO;
using System.Data.SQLite;
using System.Threading.Tasks;
using System.Data;
using UnitTests.Properties;
using System.Linq;

namespace UnitTests
{
    [TestClass]
    public class MBMUnitTests
    {
        #region files


        //var cDrive = new LogicalVolume()
        //{
        //    SerialNumber = "B8917EA9",
        //    Size = 255043366912,
        //    Type = DriveType.Fixed,
        //    VolumeName = "",
        //    MountPoint = "C:\\"
        //};

        //var dDrive = new LogicalVolume()
        //{
        //    SerialNumber = "2822F77D",
        //    Size = 499971518464,
        //    Type = DriveType.Fixed,
        //    VolumeName = "Games",
        //    MountPoint = "D:\\"
        //};

        //var fDrive = new LogicalVolume()
        //{
        //    SerialNumber = "963265DC",
        //    Size = 3000457228288,
        //    Type = DriveType.Fixed,
        //    VolumeName = "Data",
        //    MountPoint = "F:\\"
        //};

        //var h1 = new FileHash()
        //{
        //    Checksum = "F03F01D5778DFB6DC499BFFC11C26EF7",
        //    Length = 56199,
        //    CreationTime = DateTime.Parse("2018-02-01 20:54:52.4866447"),
        //    LastWriteTime = DateTime.Parse("2018-01-31 22:27:16.2543275")
        //};

        //var f1 = new FileNode()
        //{
        //    DirectoryName = @"indexdir\unit",
        //    Name = "0266554465.jpeg",
        //    Extension = ".jpeg",
        //    Checksum = "F03F01D5778DFB6DC499BFFC11C26EF7",
        //    Hash = h1
        //};
        //h1.AddNode(f1);

        //var h2 = new FileHash()
        //{
        //    Checksum = "9BFCF0A5F4660C7251F487F085C2580B",
        //    Length = 12155,
        //    CreationTime = DateTime.Parse("2018-02-01 20:54:52.5009427"),
        //    LastWriteTime = DateTime.Parse("2018-01-31 22:25:31.9274553")
        //};

        //var f2 = new FileNode()
        //{
        //    DirectoryName = @"indexdir\unit",
        //    Name = "KeyMap.txt",
        //    Extension = ".txt",
        //    Checksum = "9BFCF0A5F4660C7251F487F085C2580B",
        //    Hash = h2
        //};
        //h2.AddNode(f2);

        //var h3 = new FileHash()
        //{
        //    Checksum = "C9C02F785EE42EFACE21B3164BE718C2",
        //    Length = 75349,
        //    CreationTime = DateTime.Parse("2018-02-01 20:54:52.51192"),
        //    LastWriteTime = DateTime.Parse("2018-01-31 22:25:54.6826964")
        //};

        //var f3 = new FileNode()
        //{
        //    DirectoryName = @"indexdir\unit",
        //    Name = "Nikon-1-V3-sample-photo.jpg",
        //    Extension = ".jpg",
        //    Checksum = "C9C02F785EE42EFACE21B3164BE718C2",
        //    Hash = h3
        //};
        //h3.AddNode(f3);

        //var h4 = new FileHash()
        //{
        //    Checksum = "F6BA3E6C9CA1D37B980536ECF4075C77",
        //    Length = 13824,
        //    CreationTime = DateTime.Parse("2018-02-01 20:54:52.5334261"),
        //    LastWriteTime = DateTime.Parse("2018-01-31 22:28:23.013479")
        //};

        //var f4 = new FileNode()
        //{
        //    DirectoryName = @"indexdir\unit",
        //    Name = "randomExe.exe",
        //    Extension = ".exe",
        //    Checksum = "F6BA3E6C9CA1D37B980536ECF4075C77",
        //    Hash = h4
        //};
        //h4.AddNode(f4);

        //var h5 = new FileHash()
        //{
        //    Checksum = "D41D8CD98F00B204E9800998ECF8427E",
        //    Length = 0,
        //    CreationTime = DateTime.Parse("2018-02-01 20:54:52.5399261"),
        //    LastWriteTime = DateTime.Parse("2018-01-31 22:27:04.4650235")
        //};

        //var f5 = new FileNode()
        //{
        //    DirectoryName = @"indexdir\unit",
        //    Name = "umlaut_äü(&テスト.txt",
        //    Extension = ".txt",
        //    Checksum = "D41D8CD98F00B204E9800998ECF8427E",
        //    Hash = h5
        //};
        //h5.AddNode(f5);

        #endregion

        private const string testDirC = @"C:\indexdir\unit";
        private const string testDirD = @"D:\indexdir\unit";
        private const string testDirF = @"F:\indexdir\unit";

        private async Task<bool> ResetDatabase()
        {
            // Reset the database to a known state

            try
            {
                File.Delete(Database.GetFullName());
                if(File.Exists(Database.GetFullName()))
                    return false;
            }
            catch (Exception)
            {
                return false;
            }

            try
            {
                Database.CreateDatabase();
                if (!File.Exists(Database.GetFullName()))
                    return false;

                await Database.PrepareDatabaseAsync();
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        private async Task<int> ExecuteNonQueryAsync(SQLiteCommand command)
        {
            using (var dbConn = new SQLiteConnection(Database.GetConnectionString(), true))
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

        private bool PrepareDirectories()
        {

            string[] testDirs = { testDirC, testDirD, testDirF };

            foreach (var dir in testDirs)
            {
                // Remove existing test directories
                if (Directory.Exists(dir))
                    Directory.Delete(dir, true);

                if (Directory.Exists(dir))
                    return false;

                // Recreate test directories
                Directory.CreateDirectory(dir);
                if (!Directory.Exists(dir))
                    return false;

                try
                {
                    Directory.CreateDirectory(Path.Combine(dir, "dir1"));
                    Directory.CreateDirectory(Path.Combine(dir, @"dir2\subdir1"));
                    Directory.CreateDirectory(Path.Combine(dir, @"dir2\subdir2"));
                    Directory.CreateDirectory(Path.Combine(dir, "dir2"));
                    Directory.CreateDirectory(Path.Combine(dir, "emptydir"));
                }
                catch (Exception)
                {
                    return false;
                }
            }



            return true;
        }

        [TestMethod]
        [Description("Tests whether the database is created and initialized correctly.")]
        public async Task PrepareDatabase()
        {
            Assert.IsTrue(await ResetDatabase(), "Could not create database");

            string[] tables = { "LogicalVolume", "FileHash", "FileNode", "BackupSet", "Exclusion" };

            // Check if all tables are created correctly
            using (var dbConn = new SQLiteConnection(Database.GetConnectionString(), true))
            {
                await dbConn.OpenAsync();

                foreach (var tableName in tables)
                {
                    var sqlCmd = new SQLiteCommand("SELECT count(*) AS tableCount " +
                        "FROM sqlite_master " +
                        "WHERE type='table' " +
                        "AND name=@TableName"
                        , dbConn);

                    sqlCmd.Parameters.Add(new SQLiteParameter("@TableName", DbType.String));
                    sqlCmd.Parameters["@TableName"].Value = tableName;

                    using (var reader = await sqlCmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            Assert.AreEqual(1, int.Parse(reader["tableCount"].ToString()), "Table " + tableName +  " was not found.");
                        }
                    }
                }

                if (dbConn.State == ConnectionState.Open)
                {
                    dbConn.Close();
                }
            }
        }

        [TestMethod]
        [Description("Performs a basic file scan and tests wheter all objects are created correctly.")]
        public async Task BasicFileScan()
        {
            // Arrange
            // Prepare DB & files
            Assert.IsTrue(PrepareDirectories());
            Assert.IsTrue(await ResetDatabase(), "Could not create database");

            var targetDir = Path.Combine(testDirC, "dir1");

            File.Copy(Path.GetFullPath(@"..\..\testfiles/KeyMap.txt"), Path.Combine(targetDir, "KeyMap.txt"));
            File.Copy(Path.GetFullPath(@"..\..\testfiles/0266554465.jpeg"), Path.Combine(targetDir, "0266554465.jpeg"));
            File.Copy(Path.GetFullPath(@"..\..\testfiles/Nikon-1-V3-sample-photo.jpg"), Path.Combine(targetDir, "Nikon-1-V3-sample-photo.jpg"));
            File.Copy(Path.GetFullPath(@"..\..\testfiles/randomExe.exe"), Path.Combine(targetDir, "randomExe.exe"));
            File.Copy(Path.GetFullPath(@"..\..\testfiles/umlaut_äü(&テスト.txt"), Path.Combine(targetDir, "umlaut_äü(&テスト.txt"));

            // Create reference file index
            var fileRootDir = Path.GetFullPath(targetDir).Substring(Path.GetPathRoot(targetDir).Length);

            var refFi = new FileIndex();
            var cDrive = new LogicalVolume()
            {
                SerialNumber = "B8917EA9",
                Size = 255043366912,
                Type = DriveType.Fixed,
                VolumeName = "",
                MountPoint = "C:\\"
            };
            refFi.LogicalVolumes.Add(cDrive);

            var refSet = new BackupSet()
            {
                RootDirectory = fileRootDir,
                Volume = cDrive,
                Index = refFi
            };
            refFi.BackupSets.Add(refSet);

            var d1 = new FileDirectory()
            {
                BackupSet = refSet,
                DirectoryName = fileRootDir,
                Name = "dir1"
            };
            refSet.FileNodes.Add(d1);

            var h1 = new FileHash()
            {
                Checksum = "F03F01D5778DFB6DC499BFFC11C26EF7",
                Length = 56199,
                CreationTime = DateTime.Parse("2018-02-01 20:54:52.4866447"),
                LastWriteTime = DateTime.Parse("2018-01-31 22:27:16.2543275")
            };
            
            var f1 = new FileNode()
            {
                DirectoryName = fileRootDir,
                Name = "0266554465.jpeg",
                Extension = ".jpeg",
                Checksum = "F03F01D5778DFB6DC499BFFC11C26EF7",
                Hash = h1,
                BackupSet = refSet
            };
            h1.AddNode(f1);
            refFi.Hashes.Add(h1.Checksum, h1);
            refSet.FileNodes.Add(f1);

            var h2 = new FileHash()
            {
                Checksum = "9BFCF0A5F4660C7251F487F085C2580B",
                Length = 12155,
                CreationTime = DateTime.Parse("2018-02-01 20:54:52.5009427"),
                LastWriteTime = DateTime.Parse("2018-01-31 22:25:31.9274553")
            };

            var f2 = new FileNode()
            {
                DirectoryName = fileRootDir,
                Name = "KeyMap.txt",
                Extension = ".txt",
                Checksum = "9BFCF0A5F4660C7251F487F085C2580B",
                Hash = h2,
                BackupSet = refSet
            };
            h2.AddNode(f2);
            refFi.Hashes.Add(h2.Checksum, h2);
            refSet.FileNodes.Add(f2);

            var h3 = new FileHash()
            {
                Checksum = "C9C02F785EE42EFACE21B3164BE718C2",
                Length = 75349,
                CreationTime = DateTime.Parse("2018-02-01 20:54:52.51192"),
                LastWriteTime = DateTime.Parse("2018-01-31 22:25:54.6826964")
            };

            var f3 = new FileNode()
            {
                DirectoryName = fileRootDir,
                Name = "Nikon-1-V3-sample-photo.jpg",
                Extension = ".jpg",
                Checksum = "C9C02F785EE42EFACE21B3164BE718C2",
                Hash = h3,
                BackupSet = refSet
            };
            h3.AddNode(f3);
            refFi.Hashes.Add(h3.Checksum, h3);
            refSet.FileNodes.Add(f3);

            var h4 = new FileHash()
            {
                Checksum = "F6BA3E6C9CA1D37B980536ECF4075C77",
                Length = 13824,
                CreationTime = DateTime.Parse("2018-02-01 20:54:52.5334261"),
                LastWriteTime = DateTime.Parse("2018-01-31 22:28:23.013479")
            };

            var f4 = new FileNode()
            {
                DirectoryName = fileRootDir,
                Name = "randomExe.exe",
                Extension = ".exe",
                Checksum = "F6BA3E6C9CA1D37B980536ECF4075C77",
                Hash = h4,
                BackupSet = refSet
            };
            h4.AddNode(f4);
            refFi.Hashes.Add(h4.Checksum, h4);
            refSet.FileNodes.Add(f4);

            var h5 = new FileHash()
            {
                Checksum = "D41D8CD98F00B204E9800998ECF8427E",
                Length = 0,
                CreationTime = DateTime.Parse("2018-02-01 20:54:52.5399261"),
                LastWriteTime = DateTime.Parse("2018-01-31 22:27:04.4650235")
            };

            var f5 = new FileNode()
            {
                DirectoryName = fileRootDir,
                Name = "umlaut_äü(&テスト.txt",
                Extension = ".txt",
                Checksum = "D41D8CD98F00B204E9800998ECF8427E",
                Hash = h5,
                BackupSet = refSet
            };
            h5.AddNode(f5);
            refFi.Hashes.Add(h5.Checksum, h5);
            refSet.FileNodes.Add(f5);

            // Act
            var diffFi = new FileIndex();
            await diffFi.CreateBackupSetAsync(new DirectoryInfo(targetDir));

            // Several attributes are created on the fly, so we need to copy them
            // to the reference set
            var diffSet = diffFi.BackupSets.FirstOrDefault(x => x.RootDirectory.Equals(refSet.RootDirectory));
            refSet.Guid = diffSet.Guid;
            refSet.Label = diffSet.Label;


            // Assert
            Assert.AreEqual(refFi.BackupSets.Count, diffFi.BackupSets.Count, "BackupSet count incorrect.");

            Assert.AreEqual(refFi.LogicalVolumes.Count, diffFi.LogicalVolumes.Count, "LogicalVolume count incorrect.");

            foreach (var refVolume in refFi.LogicalVolumes)
            {
                Assert.IsTrue(diffFi.LogicalVolumes.Contains(refVolume), "LogicalVolume not found.");
            }

            Assert.AreEqual(refFi.Hashes.Count, diffFi.Hashes.Count, "FileHash count incorrect.");
            foreach (var refHash in refFi.Hashes)
            {
                Assert.IsTrue(diffFi.Hashes.ContainsKey(refHash.Key), "FileHash not found.");
                Assert.AreEqual(refHash.Value, diffFi.Hashes[refHash.Key], "FileHash not equal.");
            }

            Assert.AreEqual(refSet.FileNodes.Count, diffSet.FileNodes.Count, "FileNodes count incorrect.");

            foreach (var refNode in refSet.FileNodes)
            {
                Assert.IsTrue(diffSet.FileNodes.Contains(refNode), "FileNode not found.");
            }

            //Assert.AreEqual(refFi.fi.Count, diffFi.Hashes.Count, "FileNode count incorrect.");


        }
    }
}
