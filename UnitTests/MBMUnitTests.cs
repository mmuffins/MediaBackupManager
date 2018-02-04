using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MediaBackupManager.Model;
using System.IO;
using System.Data.SQLite;
using System.Threading.Tasks;
using System.Data;
using UnitTests.Properties;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

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
        private const string testFileDir = @"..\..\testfiles";
        private string[] dbTables = { "LogicalVolume", "FileHash", "FileNode", "BackupSet", "Exclusion" };


        private async Task<bool> ResetDatabase()
        {
            // Reset the database to a known state

            if (File.Exists(Database.GetFullName()))
            {
                using (var dbConn = new SQLiteConnection(Database.GetConnectionString(), true))
                {
                    try
                    {
                        await dbConn.OpenAsync();

                        foreach (var tableName in dbTables)
                        {
                            var sqlCmd = new SQLiteCommand("DROP TABLE " + tableName, dbConn);
                            await sqlCmd.ExecuteNonQueryAsync();
                        }
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                    finally
                    {
                        if (dbConn.State == ConnectionState.Open)
                            dbConn.Close();
                    }
                }
            }
            else
            {
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
            }

            await Database.PrepareDatabaseAsync();
            return true;
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

            // Check if all tables are created correctly
            using (var dbConn = new SQLiteConnection(Database.GetConnectionString(), true))
            {
                await dbConn.OpenAsync();

                foreach (var tableName in dbTables)
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

            var targetDir = Path.Combine(testDirD, "dir1");

            File.Copy(Path.GetFullPath(Path.Combine(testFileDir, "KeyMap.txt")), Path.Combine(targetDir, "KeyMap.txt"));
            File.Copy(Path.GetFullPath(Path.Combine(testFileDir, "0266554465.jpeg")), Path.Combine(targetDir, "0266554465.jpeg"));
            File.Copy(Path.GetFullPath(Path.Combine(testFileDir, "Nikon-1-V3-sample-photo.jpg")), Path.Combine(targetDir, "Nikon-1-V3-sample-photo.jpg"));
            File.Copy(Path.GetFullPath(Path.Combine(testFileDir, "randomExe.exe")), Path.Combine(targetDir, "randomExe.exe"));
            File.Copy(Path.GetFullPath(Path.Combine(testFileDir, "umlaut_äü(&テスト.txt")), Path.Combine(targetDir, "umlaut_äü(&テスト.txt"));

            var setLabel = "BasicFileScanSet";

            // Create reference file index
            var fileRootDir = Path.GetFullPath(targetDir).Substring(Path.GetPathRoot(targetDir).Length);

            var refFi = new FileIndex();
            var dDrive = new LogicalVolume()
            {
                SerialNumber = "2822F77D",
                Size = 499971518464,
                Type = DriveType.Fixed,
                VolumeName = "Games",
                MountPoint = "D:\\"
            };
            refFi.LogicalVolumes.Add(dDrive);

            var refSet = new BackupSet()
            {
                RootDirectory = fileRootDir,
                Volume = dDrive,
                Index = refFi,
                Label = setLabel
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
            refFi.Hashes.Add(h1);
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
            refFi.Hashes.Add(h2);
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
            refFi.Hashes.Add(h3);
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
            refFi.Hashes.Add(h4);
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
            refFi.Hashes.Add(h5);
            refSet.FileNodes.Add(f5);

            // Act
            var diffFi = new FileIndex();
            await diffFi.CreateBackupSetAsync(new DirectoryInfo(targetDir), new CancellationTokenSource().Token, new Progress<int>(), new Progress<string>(), setLabel);

            // Several attributes are created on the fly, so we need to copy them
            // to the reference set
            var diffSet = diffFi.BackupSets.FirstOrDefault(x => x.Label.Equals(refSet.Label));
            refSet.Guid = diffSet.Guid;

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
                Assert.IsTrue(diffFi.Hashes.Contains(refHash), "FileHash not found.");
                Assert.AreEqual(refHash, diffFi.Hashes.FirstOrDefault(x => x.Equals(refHash)), "FileHash not equal.");
            }

            Assert.AreEqual(refSet.FileNodes.Count, diffSet.FileNodes.Count, "FileNodes count incorrect.");

            foreach (var refNode in refSet.FileNodes)
            {
                Assert.IsTrue(diffSet.FileNodes.Contains(refNode), "FileNode not found.");
            }
        }

        [TestMethod]
        [Description("Tests whether data is correctly written to and read from the database.")]
        public async Task WriteToDatabase()
        {
            // Arrange
            // Prepare DB & files
            Assert.IsTrue(PrepareDirectories());
            Assert.IsTrue(await ResetDatabase(), "Could not create database");

            var targetDir = Path.Combine(testDirD, "dir1");

            File.Copy(Path.GetFullPath(Path.Combine(testFileDir, "KeyMap.txt")), Path.Combine(targetDir, "KeyMap.txt"));
            File.Copy(Path.GetFullPath(Path.Combine(testFileDir, "0266554465.jpeg")), Path.Combine(targetDir, "0266554465.jpeg"));
            File.Copy(Path.GetFullPath(Path.Combine(testFileDir, "Nikon-1-V3-sample-photo.jpg")), Path.Combine(targetDir, "Nikon-1-V3-sample-photo.jpg"));
            File.Copy(Path.GetFullPath(Path.Combine(testFileDir, "randomExe.exe")), Path.Combine(targetDir, "randomExe.exe"));
            File.Copy(Path.GetFullPath(Path.Combine(testFileDir, "umlaut_äü(&テスト.txt")), Path.Combine(targetDir, "umlaut_äü(&テスト.txt"));

            var exclusionString1 = @"\.exe";
            var exclusionString2 = @".*\subdir1";

            // Act
            var refFi = new FileIndex();
            await refFi.CreateBackupSetAsync(new DirectoryInfo(targetDir), new CancellationTokenSource().Token, new Progress<int>(), new Progress<string>(), "testSet");
            await refFi.CreateFileExclusionAsync(exclusionString1);
            await refFi.CreateFileExclusionAsync(exclusionString2);

            var diffFi = new FileIndex();
            await diffFi.LoadDataAsync();

            // Assert
            Assert.AreEqual(refFi.BackupSets.Count, diffFi.BackupSets.Count, "BackupSet count incorrect.");

            foreach (var refSet in refFi.BackupSets)
            {
                Assert.IsTrue(diffFi.BackupSets.Contains(refSet), "BackupSet not found.");

                var diffSet = diffFi.BackupSets.FirstOrDefault(x => x.Equals(refSet));

                Assert.AreEqual(refSet.FileNodes.Count, diffSet.FileNodes.Count, "FileNodes count incorrect.");
                foreach (var refNode in refSet.FileNodes)
                {
                    Assert.IsTrue(diffSet.FileNodes.Contains(refNode), "FileNode not found.");
                }
            }

            Assert.AreEqual(refFi.LogicalVolumes.Count, diffFi.LogicalVolumes.Count, "LogicalVolume count incorrect.");

            foreach (var refVolume in refFi.LogicalVolumes)
            {
                Assert.IsTrue(diffFi.LogicalVolumes.Contains(refVolume), "LogicalVolume not found.");
            }

            Assert.AreEqual(refFi.Hashes.Count, diffFi.Hashes.Count, "FileHash count incorrect.");
            foreach (var refHash in refFi.Hashes)
            {
                Assert.IsTrue(diffFi.Hashes.Contains(refHash), "FileHash not found.");
                Assert.AreEqual(refHash, diffFi.Hashes.FirstOrDefault(x => x.Equals(refHash)), "FileHash not equal.");
            }
            Assert.IsTrue(diffFi.Exclusions.Contains(exclusionString1), "Exclusion not found.");
            Assert.IsTrue(diffFi.Exclusions.Contains(exclusionString2), "Exclusion not found.");

        }

        [TestMethod]
        [Description("Tests whether all data is properly cleaned up after deleting a backup set.")]
        public async Task DeleteBackupSet()
        {
            // Arrange
            // Prepare DB & files
            Assert.IsTrue(PrepareDirectories());
            Assert.IsTrue(await ResetDatabase(), "Could not create database");

            var targetDir = Path.Combine(testDirD, "dir1");
            File.Copy(Path.GetFullPath(Path.Combine(testFileDir, "KeyMap.txt")), Path.Combine(targetDir, "KeyMap.txt"));
            File.Copy(Path.GetFullPath(Path.Combine(testFileDir, "0266554465.jpeg")), Path.Combine(targetDir, "0266554465.jpeg"));
            File.Copy(Path.GetFullPath(Path.Combine(testFileDir, "Nikon-1-V3-sample-photo.jpg")), Path.Combine(targetDir, "Nikon-1-V3-sample-photo.jpg"));
            File.Copy(Path.GetFullPath(Path.Combine(testFileDir, "randomExe.exe")), Path.Combine(targetDir, "randomExe.exe"));
            File.Copy(Path.GetFullPath(Path.Combine(testFileDir, "umlaut_äü(&テスト.txt")), Path.Combine(targetDir, "umlaut_äü(&テスト.txt"));

            var targetDir2 = Path.Combine(testDirD, "dir2");
            File.Copy(Path.GetFullPath(Path.Combine(testFileDir, "KeyMap.txt")), Path.Combine(targetDir2, "KeyMap.txt"));

            var targetDir3 = Path.Combine(testDirF, "dir1");
            File.Copy(Path.GetFullPath(Path.Combine(testFileDir, "0266554465.jpeg")), Path.Combine(targetDir3, "0266554465.jpeg"));


            // Act
            var refFi = new FileIndex();

            List<BackupSet> refSets = new List<BackupSet>();
            var set1 = await refFi.CreateBackupSetAsync(new DirectoryInfo(targetDir), new CancellationTokenSource().Token, new Progress<int>(), new Progress<string>(), "DeleteBackupSet1");
            refSets.Add(set1);

            // Backupset on same volume       
            var set2 = await refFi.CreateBackupSetAsync(new DirectoryInfo(targetDir2), new CancellationTokenSource().Token, new Progress<int>(), new Progress<string>(), "DeleteBackupSet2");
            refSets.Add(set2);

            // Backupset on different volume
            var set3 = await refFi.CreateBackupSetAsync(new DirectoryInfo(targetDir3), new CancellationTokenSource().Token, new Progress<int>(), new Progress<string>(), "DeleteBackupSet3");
            refSets.Add(set3);

            // Assert
            // Remove each set from the index and check if all data
            // (and only data from the deleted set) has been removed
            // from the file index in memory and the db after each deletion

            // Remove first set
            await refFi.RemoveBackupSetAsync(set1, true);
            refSets.Remove(set1);
            set1 = null;
            var diffFi = refFi;
            DeleteBackupSet_CheckEquality(refSets, diffFi);

            diffFi = new FileIndex();
            await diffFi.LoadDataAsync();
            DeleteBackupSet_CheckEquality(refSets, diffFi);

            // Remove second set
            await refFi.RemoveBackupSetAsync(set2, true);
            refSets.Remove(set2);
            set2 = null;
            diffFi = refFi;
            DeleteBackupSet_CheckEquality(refSets, diffFi);

            diffFi = new FileIndex();
            await diffFi.LoadDataAsync();
            DeleteBackupSet_CheckEquality(refSets, diffFi);

            // Remove third set, all data should now be removed
            await refFi.RemoveBackupSetAsync(set3, true);
            refSets.Remove(set3);
            set3 = null;
            diffFi = refFi;

            Assert.AreEqual(0, diffFi.BackupSets.Count, "BackupSet count incorrect.");
            Assert.AreEqual(0, diffFi.LogicalVolumes.Count, "LogicalVolume count incorrect.");
            Assert.AreEqual(0, diffFi.Hashes.Count, "FileHash count incorrect.");

            diffFi = new FileIndex();
            await diffFi.LoadDataAsync();
            Assert.AreEqual(0, diffFi.BackupSets.Count, "BackupSet count incorrect.");
            Assert.AreEqual(0, diffFi.LogicalVolumes.Count, "LogicalVolume count incorrect.");
            Assert.AreEqual(0, diffFi.Hashes.Count, "FileHash count incorrect.");


            // Also make sure that all data was removed from the database

            using (var dbConn = new SQLiteConnection(Database.GetConnectionString(), true))
            {
                await dbConn.OpenAsync();

                foreach (var tableName in dbTables)
                {
                    var sqlCmd = new SQLiteCommand("SELECT count(*) AS tableCount FROM " + tableName, dbConn);

                    using (var reader = await sqlCmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            Assert.AreEqual(0, int.Parse(reader["tableCount"].ToString()), "Not all entries were deleted in table " + tableName);
                        }
                    }
                }

                if (dbConn.State == ConnectionState.Open)
                {
                    dbConn.Close();
                }
            }
        }

        private void DeleteBackupSet_CheckEquality(List<BackupSet> refSets, FileIndex diffFi)
        {
            Assert.AreEqual(refSets.Count, diffFi.BackupSets.Count, "BackupSet count incorrect.");
            refSets
                .ForEach(refSet => Assert.IsTrue(diffFi.BackupSets.Contains(refSet), "BackupSet not found."));

            // No need to check for file nodes since they are child elements of a backup set

            var refVolumes = refSets.Select(set => set.Volume).Distinct().ToList();

            Assert.AreEqual(refVolumes.Count, diffFi.LogicalVolumes.Count, "LogicalVolume count incorrect.");
            refVolumes
                .ForEach(refVolume => Assert.IsTrue(diffFi.LogicalVolumes.Contains(refVolume), "LogicalVolume not found."));


            var totalHashCount = refSets.Sum(set => set.GetFileHashes().Count);
            Assert.AreEqual(totalHashCount, diffFi.Hashes.Count, "FileHash count incorrect.");

            refSets.ForEach(refSet =>
            {
                refSet.GetFileHashes().ForEach(refHash =>
                {
                    Assert.IsTrue(diffFi.Hashes.Contains(refHash), "FileHash not found.");
                    Assert.AreEqual(refHash, diffFi.Hashes.FirstOrDefault(x => x.Equals(refHash)), "FileHash not equal.");
                });
            });
        }

        [TestMethod]
        [Description("Tests whether file node duplication is correctly counted between backup sets.")]
        public async Task FileDuplication()
        {
            // Arrange
            // Prepare DB & files
            Assert.IsTrue(PrepareDirectories());
            Assert.IsTrue(await ResetDatabase(), "Could not create database");


            var targetDir = Path.Combine(testDirC, "dir1");
            var targetDir2 = Path.Combine(testDirD, "dir1");
            var targetDir3 = Path.Combine(testDirD, "dir2");
            var targetDir4 = Path.Combine(testDirF, "dir1");

            // node count 1
            File.Copy(Path.GetFullPath(Path.Combine(testFileDir, "KeyMap.txt")), Path.Combine(targetDir, "KeyMap.txt"));
            File.Copy(Path.GetFullPath(Path.Combine(testFileDir, "0266554465.jpeg")), Path.Combine(targetDir2, "0266554465.jpeg"));
            File.Copy(Path.GetFullPath(Path.Combine(testFileDir, "0266554465.jpeg")), Path.Combine(targetDir3, "0266554465.jpeg"));

            // node count 2
            File.Copy(Path.GetFullPath(Path.Combine(testFileDir, "Nikon-1-V3-sample-photo.jpg")), Path.Combine(targetDir2, "Nikon-1-V3-sample-photo.jpg"));
            File.Copy(Path.GetFullPath(Path.Combine(testFileDir, "Nikon-1-V3-sample-photo.jpg")), Path.Combine(targetDir3, "Nikon-1-V3-sample-photo.jpg"));
            File.Copy(Path.GetFullPath(Path.Combine(testFileDir, "Nikon-1-V3-sample-photo.jpg")), Path.Combine(targetDir4, "Nikon-1-V3-sample-photo.jpg"));

            // node count 3
            File.Copy(Path.GetFullPath(Path.Combine(testFileDir, "randomExe.exe")), Path.Combine(targetDir, "randomExe.exe"));
            File.Copy(Path.GetFullPath(Path.Combine(testFileDir, "randomExe.exe")), Path.Combine(targetDir2, "randomExe.exe"));
            File.Copy(Path.GetFullPath(Path.Combine(testFileDir, "randomExe.exe")), Path.Combine(targetDir4, "randomExe.exe"));


            // Act
            var diffFi = new FileIndex();
            await diffFi.CreateBackupSetAsync(new DirectoryInfo(targetDir), new CancellationTokenSource().Token, new Progress<int>(), new Progress<string>(), "FileDuplicationSet1");
            await diffFi.CreateBackupSetAsync(new DirectoryInfo(targetDir2), new CancellationTokenSource().Token, new Progress<int>(), new Progress<string>(), "FileDuplicationSet2");
            await diffFi.CreateBackupSetAsync(new DirectoryInfo(targetDir3), new CancellationTokenSource().Token, new Progress<int>(), new Progress<string>(), "FileDuplicationSet3");
            await diffFi.CreateBackupSetAsync(new DirectoryInfo(targetDir4), new CancellationTokenSource().Token, new Progress<int>(), new Progress<string>(), "FileDuplicationSet4");

            var allNodes = diffFi.BackupSets.SelectMany(x => x.FileNodes);
            var nodeCount11 = (FileNode)allNodes.FirstOrDefault(x => x.Name.Equals("KeyMap.txt"));
            var nodeCount12 = (FileNode)allNodes.FirstOrDefault(x => x.Name.Equals("0266554465.jpeg"));
            var nodeCount23 = (FileNode)allNodes.FirstOrDefault(x => x.Name.Equals("Nikon-1-V3-sample-photo.jpg"));
            var nodeCount33 = (FileNode)allNodes.FirstOrDefault(x => x.Name.Equals("randomExe.exe"));

            // Assert
            Assert.AreEqual(4, diffFi.Hashes.Count, "FileHash count incorrect.");
            Assert.AreEqual(1, nodeCount11.Hash.BackupCount, "Backup count incorrect.");
            Assert.AreEqual(1, nodeCount12.Hash.BackupCount, "Backup count incorrect.");
            Assert.AreEqual(2, nodeCount23.Hash.BackupCount, "Backup count incorrect.");
            Assert.AreEqual(3, nodeCount33.Hash.BackupCount, "Backup count incorrect.");

            Assert.AreEqual(1, nodeCount11.Hash.NodeCount, "Node count incorrect.");
            Assert.AreEqual(2, nodeCount12.Hash.NodeCount, "Node count incorrect.");
            Assert.AreEqual(3, nodeCount23.Hash.NodeCount, "Node count incorrect.");
            Assert.AreEqual(3, nodeCount33.Hash.NodeCount, "Node count incorrect.");

        }

        [TestMethod]
        [Description("Tests whether files are excluded from scans if they match an entry on the exclusion list.")]
        public async Task FileExclusion()
        {
            // Arrange
            // Prepare DB & files
            Assert.IsTrue(PrepareDirectories());
            Assert.IsTrue(await ResetDatabase(), "Could not create database");

            var targetDir = Path.Combine(testDirD, "dir1");
            var targetDir2 = Path.Combine(testDirD, @"dir2\subdir1");

            // should be scanned
            File.Copy(Path.GetFullPath(Path.Combine(testFileDir, "KeyMap.txt")), Path.Combine(targetDir, "KeyMap.txt"));

            // should not be scanned
            File.Copy(Path.GetFullPath(Path.Combine(testFileDir, "randomExe.exe")), Path.Combine(targetDir, "randomExe.exe"));
            File.Copy(Path.GetFullPath(Path.Combine(testFileDir, "0266554465.jpeg")), Path.Combine(targetDir2, "0266554465.jpeg"));
            File.Copy(Path.GetFullPath(Path.Combine(testFileDir, "Nikon-1-V3-sample-photo.jpg")), Path.Combine(targetDir2, "Nikon-1-V3-sample-photo.jpg"));
            File.Copy(Path.GetFullPath(Path.Combine(testFileDir, "umlaut_äü(&テスト.txt")), Path.Combine(targetDir2, "umlaut_äü(&テスト.txt"));

            // Act
            var diffFi = new FileIndex();
            await diffFi.CreateFileExclusionAsync(@"\.exe");
            await diffFi.CreateFileExclusionAsync(@".*\\subdir1");

            await diffFi.CreateBackupSetAsync(new DirectoryInfo(testDirD), new CancellationTokenSource().Token, new Progress<int>(), new Progress<string>(), "FileExclusionSet");

            // Assert
            Assert.AreEqual(1, diffFi.Hashes.Count, "FileHash count incorrect.");

            var hash = diffFi.Hashes.FirstOrDefault();
            Assert.AreEqual(1, hash.NodeCount, "Node count incorrect.");
            var node = hash.Nodes.FirstOrDefault();

            Assert.AreEqual(Path.Combine(targetDir, "KeyMap.txt"), node.FullSessionName, "Node count incorrect.");
        }
    }
}
