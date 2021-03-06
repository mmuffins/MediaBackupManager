﻿using System;
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
        private string[] dbTables = { "LogicalVolume", "FileHash", "FileNode", "Archive", "Exclusion" };


        private async Task<bool> ResetDatabase()
        {
            // Reset the database to a known state

            if (File.Exists(Database.FullName))
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
                    if (!File.Exists(Database.FullName))
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

            var archiveLabel = "BasicFileScanArchive";

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

            var refArchive = new Archive()
            {
                RootDirectoryPath = fileRootDir,
                Volume = dDrive,
                Index = refFi,
                Label = archiveLabel
            };
            refFi.Archives.Add(refArchive);

            var d1 = new FileDirectory()
            {
                Archive = refArchive,
                DirectoryName = Path.GetFullPath(testDirD).Substring(Path.GetPathRoot(testDirD).Length),
                Name = "dir1"
            };
            refArchive.RootDirectory = d1;

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
                Archive = refArchive,
                Parent = d1,
            };
            h1.AddNode(f1);
            refFi.Hashes.Add(h1);
            d1.FileNodes.Add(f1);

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
                Archive = refArchive,
                Parent = d1
            };
            h2.AddNode(f2);
            refFi.Hashes.Add(h2);
            d1.FileNodes.Add(f2);

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
                Archive = refArchive,
                Parent = d1
            };
            h3.AddNode(f3);
            refFi.Hashes.Add(h3);
            d1.FileNodes.Add(f3);

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
                Archive = refArchive,
                Parent = d1
            };
            h4.AddNode(f4);
            refFi.Hashes.Add(h4);
            d1.FileNodes.Add(f4);

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
                Archive = refArchive,
                Parent = d1
            };
            h5.AddNode(f5);
            refFi.Hashes.Add(h5);
            d1.FileNodes.Add(f5);

            // Act
            var diffFi = new FileIndex();
            var diffArchive = await diffFi.CreateArchiveAsync(new DirectoryInfo(targetDir), new CancellationTokenSource().Token, new Progress<int>(), new Progress<string>(), archiveLabel);

            // Several attributes are created on the fly, so we need to copy them
            // to the reference archive
            refArchive.Guid = diffArchive.Guid;

            // Assert
            Assert.AreEqual(refFi.Archives.Count, diffFi.Archives.Count, "Archive count incorrect.");

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

            Assert.AreEqual(refArchive.GetFileNodes().Count, diffArchive.GetFileNodes().Count, "FileNodes count incorrect.");

            foreach (var refNode in refArchive.GetFileNodes())
            {
                Assert.IsTrue(diffArchive.GetFileNodes().Contains(refNode), "FileNode not found.");
            }

            Assert.AreEqual(refArchive.GetFileDirectories().Count, diffArchive.GetFileDirectories().Count, "FileDirectory count incorrect.");

            foreach (var refNode in refArchive.GetFileDirectories())
            {
                Assert.IsTrue(diffArchive.GetFileDirectories().Contains(refNode), "FileDirectory not found.");
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
            await refFi.CreateArchiveAsync(new DirectoryInfo(targetDir), new CancellationTokenSource().Token, new Progress<int>(), new Progress<string>(), "testArchive");
            await refFi.AddFileExclusionAsync(exclusionString1, true);
            await refFi.AddFileExclusionAsync(exclusionString2, true);

            var diffFi = new FileIndex();
            await diffFi.LoadDataAsync();

            // Assert
            Assert.AreEqual(refFi.Archives.Count, diffFi.Archives.Count, "Archive count incorrect.");

            foreach (var refArchive in refFi.Archives)
            {
                Assert.IsTrue(diffFi.Archives.Contains(refArchive), "Archive not found.");

                var diffArchive = diffFi.Archives.FirstOrDefault(x => x.Equals(refArchive));


                Assert.AreEqual(refArchive.GetFileNodes().Count, diffArchive.GetFileNodes().Count, "FileNodes count incorrect.");

                foreach (var refNode in refArchive.GetFileNodes())
                {
                    Assert.IsTrue(diffArchive.GetFileNodes().Contains(refNode), "FileNode not found.");
                }

                Assert.AreEqual(refArchive.GetFileDirectories().Count, diffArchive.GetFileDirectories().Count, "FileDirectory count incorrect.");

                foreach (var refNode in refArchive.GetFileDirectories())
                {
                    Assert.IsTrue(diffArchive.GetFileDirectories().Contains(refNode), "FileDirectory not found.");
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
        [Description("Tests whether all data is properly cleaned up after deleting a archive.")]
        public async Task DeleteArchive()
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

            var refArchives = new List<Archive>();
            var archive1 = await refFi.CreateArchiveAsync(new DirectoryInfo(targetDir), new CancellationTokenSource().Token, new Progress<int>(), new Progress<string>(), "DeleteArchive1");
            refArchives.Add(archive1);

            // Archive on same volume       
            var archive2 = await refFi.CreateArchiveAsync(new DirectoryInfo(targetDir2), new CancellationTokenSource().Token, new Progress<int>(), new Progress<string>(), "DeleteArchive2");
            refArchives.Add(archive2);

            // Archive on different volume
            var archive3 = await refFi.CreateArchiveAsync(new DirectoryInfo(targetDir3), new CancellationTokenSource().Token, new Progress<int>(), new Progress<string>(), "DeleteArchive3");
            refArchives.Add(archive3);

            // Assert
            // Remove each archive from the index and check if all data
            // (and only data from the deleted archive) has been removed
            // from the file index in memory and the db after each deletion

            // Remove first archive
            await refFi.RemoveArchiveAsync(archive1, true);
            refArchives.Remove(archive1);
            archive1 = null;
            var diffFi = refFi;
            DeleteArchive_CheckEquality(refArchives, diffFi);

            diffFi = new FileIndex();
            await diffFi.LoadDataAsync();
            DeleteArchive_CheckEquality(refArchives, diffFi);

            // Remove second archive
            await refFi.RemoveArchiveAsync(archive2, true);
            refArchives.Remove(archive2);
            archive2 = null;
            diffFi = refFi;
            DeleteArchive_CheckEquality(refArchives, diffFi);

            diffFi = new FileIndex();
            await diffFi.LoadDataAsync();
            DeleteArchive_CheckEquality(refArchives, diffFi);

            // Remove third archive, all data should now be removed
            await refFi.RemoveArchiveAsync(archive3, true);
            refArchives.Remove(archive3);
            archive3 = null;
            diffFi = refFi;

            Assert.AreEqual(0, diffFi.Archives.Count, "Archive count incorrect.");
            Assert.AreEqual(0, diffFi.LogicalVolumes.Count, "LogicalVolume count incorrect.");
            Assert.AreEqual(0, diffFi.Hashes.Count, "FileHash count incorrect.");

            diffFi = new FileIndex();
            await diffFi.LoadDataAsync();
            Assert.AreEqual(0, diffFi.Archives.Count, "Archive count incorrect.");
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

        private void DeleteArchive_CheckEquality(List<Archive> refArchives, FileIndex diffFi)
        {
            Assert.AreEqual(refArchives.Count, diffFi.Archives.Count, "Archive count incorrect.");
            refArchives
                .ForEach(refArchive => Assert.IsTrue(diffFi.Archives.Contains(refArchive), "Archive not found."));

            // No need to check for file nodes since they are child elements of a archive

            var refVolumes = refArchives.Select(archive => archive.Volume).Distinct().ToList();

            Assert.AreEqual(refVolumes.Count, diffFi.LogicalVolumes.Count, "LogicalVolume count incorrect.");
            refVolumes
                .ForEach(refVolume => Assert.IsTrue(diffFi.LogicalVolumes.Contains(refVolume), "LogicalVolume not found."));


            var totalHashCount = refArchives.Sum(archive => archive.GetFileHashes().Count);
            Assert.AreEqual(totalHashCount, diffFi.Hashes.Count, "FileHash count incorrect.");

            refArchives.ForEach(refArchive =>
            {
                refArchive.GetFileHashes().ForEach(refHash =>
                {
                    Assert.IsTrue(diffFi.Hashes.Contains(refHash), "FileHash not found.");
                    Assert.AreEqual(refHash, diffFi.Hashes.FirstOrDefault(x => x.Equals(refHash)), "FileHash not equal.");
                });
            });
        }

        [TestMethod]
        [Description("Tests whether file node duplication is correctly counted between archives.")]
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
            await diffFi.CreateArchiveAsync(new DirectoryInfo(targetDir), new CancellationTokenSource().Token, new Progress<int>(), new Progress<string>(), "FileDuplicationArchive1");
            await diffFi.CreateArchiveAsync(new DirectoryInfo(targetDir2), new CancellationTokenSource().Token, new Progress<int>(), new Progress<string>(), "FileDuplicationArchive2");
            await diffFi.CreateArchiveAsync(new DirectoryInfo(targetDir3), new CancellationTokenSource().Token, new Progress<int>(), new Progress<string>(), "FileDuplicationArchive3");
            await diffFi.CreateArchiveAsync(new DirectoryInfo(targetDir4), new CancellationTokenSource().Token, new Progress<int>(), new Progress<string>(), "FileDuplicationArchive4");

            var allNodes = diffFi.Archives.SelectMany(x => x.GetFileNodes());
            var nodeCount11 = allNodes.FirstOrDefault(x => x.Name.Equals("KeyMap.txt"));
            var nodeCount12 = allNodes.FirstOrDefault(x => x.Name.Equals("0266554465.jpeg"));
            var nodeCount23 = allNodes.FirstOrDefault(x => x.Name.Equals("Nikon-1-V3-sample-photo.jpg"));
            var nodeCount33 = allNodes.FirstOrDefault(x => x.Name.Equals("randomExe.exe"));

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
            await diffFi.AddFileExclusionAsync(@"\.exe", true);
            await diffFi.AddFileExclusionAsync(@".*\\subdir1", true);

            await diffFi.CreateArchiveAsync(new DirectoryInfo(testDirD), new CancellationTokenSource().Token, new Progress<int>(), new Progress<string>(), "FileExclusionArchive");

            // Assert
            Assert.AreEqual(1, diffFi.Hashes.Count, "FileHash count incorrect.");

            var hash = diffFi.Hashes.FirstOrDefault();
            Assert.AreEqual(1, hash.NodeCount, "Node count incorrect.");
            var node = hash.Nodes.FirstOrDefault();

            Assert.AreEqual(Path.Combine(targetDir, "KeyMap.txt"), node.FullSessionName, "Node count incorrect.");
        }

        [TestMethod]
        [Description("Tests whether changes to Archive labels are correctly updated in the DB.")]
        public async Task ChangeArchiveLabel()
        {
            // Arrange
            // Prepare DB & files
            Assert.IsTrue(PrepareDirectories());
            Assert.IsTrue(await ResetDatabase(), "Could not create database");

            var targetDir = Path.Combine(testDirD, "dir1");

            File.Copy(Path.GetFullPath(Path.Combine(testFileDir, "KeyMap.txt")), Path.Combine(targetDir, "KeyMap.txt"));

            var oldLabel = "oldLabel";
            var newLabel = "newLabel";

            // Act
            var refFi = new FileIndex();
            var refArchive = await refFi.CreateArchiveAsync(new DirectoryInfo(targetDir), new CancellationTokenSource().Token, new Progress<int>(), new Progress<string>(), oldLabel);

            await refArchive.UpdateLabel(newLabel);

            var diffFi = new FileIndex();
            await diffFi.LoadDataAsync();

            var diffArchive = diffFi.Archives.FirstOrDefault(x => x.Guid.Equals(refArchive.Guid));

            // Assert
            Assert.AreEqual(newLabel, refArchive.Label, "Label was not updated correctly.");
            Assert.AreEqual(newLabel, diffArchive.Label, "New Label was not correctly written to DB.");


        }

        [TestMethod]
        [Description("Tests whether the update function properly updates archives")]
        public async Task UpdateArchive()
        {
            // Arrange
            // Prepare DB & files
            Assert.IsTrue(PrepareDirectories());
            Assert.IsTrue(await ResetDatabase(), "Could not create database");

            var targetDir1 = Path.Combine(testDirD, "dir1");
            var targetDir2 = Path.Combine(testDirD, "dir2");

            // f1 stays the same
            File.Copy(Path.GetFullPath(Path.Combine(testFileDir, "KeyMap.txt")), Path.Combine(targetDir1, "KeyMap.txt"));

            // f2 gets moved from dir1 to dir2 after scanning
            File.Copy(Path.GetFullPath(Path.Combine(testFileDir, "0266554465.jpeg")), Path.Combine(targetDir1, "0266554465.jpeg"));

            // f3 is removed after scanning
            File.Copy(Path.GetFullPath(Path.Combine(testFileDir, "Nikon-1-V3-sample-photo.jpg")), Path.Combine(targetDir1, "Nikon-1-V3-sample-photo.jpg"));

            var fi1 = new FileIndex();
            var archive1 = await fi1.CreateArchiveAsync(new DirectoryInfo(testDirD), new CancellationTokenSource().Token, new Progress<int>(), new Progress<string>(), "archive1");
            var archive1Guid = archive1.Guid;

            var f1 = archive1.GetFileNodes().FirstOrDefault(x => x.FullSessionName.Equals(Path.Combine(targetDir1, "KeyMap.txt")));
            var f2 = archive1.GetFileNodes().FirstOrDefault(x => x.FullSessionName.Equals(Path.Combine(targetDir1, "0266554465.jpeg")));
            var f3 = archive1.GetFileNodes().FirstOrDefault(x => x.FullSessionName.Equals(Path.Combine(targetDir1, "Nikon-1-V3-sample-photo.jpg")));

            var f1Hash = f1.Checksum;
            var f2Hash = f2.Checksum;
            var f3Hash = f3.Checksum;

            // Act

            // f4 is newly added
            File.Copy(Path.GetFullPath(Path.Combine(testFileDir, "umlaut_äü(&テスト.txt")), Path.Combine(targetDir1, "umlaut_äü(&テスト.txt"));

            File.Move(Path.Combine(targetDir1, "0266554465.jpeg"), Path.Combine(targetDir2, "0266554465.jpeg"));
            File.Delete(Path.Combine(targetDir1, "Nikon-1-V3-sample-photo.jpg"));

            await fi1.UpdateArchiveAsync(archive1, new CancellationTokenSource().Token, new Progress<int>(), new Progress<string>());
            archive1 = fi1.Archives.FirstOrDefault(x => x.Guid.Equals(archive1Guid));

            // Also check if the changes are correctly written to the DB
            var fi2 = new FileIndex();
            await fi2.LoadDataAsync();
            var archive2 = fi2.Archives.FirstOrDefault(x => x.Guid.Equals(archive1Guid));

            // Assert
            Assert.IsNotNull(archive1, "Archive Guid was changed");
            Assert.IsNotNull(archive2, "Archive Guid was changed");

            Assert.AreEqual(3, archive1.GetFileNodes().Count(), "FileNode count incorrect.");
            Assert.AreEqual(1, archive1.GetFileNodes().Where(x => x.FullSessionName.Equals(Path.Combine(targetDir1, "KeyMap.txt"))).Count(), "FileNode not found.");
            Assert.AreEqual(1, archive1.GetFileNodes().Where(x => x.FullSessionName.Equals(Path.Combine(targetDir2, "0266554465.jpeg"))).Count(), "FileNode not found.");
            Assert.AreEqual(0, archive1.GetFileNodes().Where(x => x.FullSessionName.Equals(Path.Combine(targetDir1, "Nikon-1-V3-sample-photo.jpg"))).Count(), "FileNode not found.");
            Assert.AreEqual(1, archive1.GetFileNodes().Where(x => x.FullSessionName.Equals(Path.Combine(targetDir1, "umlaut_äü(&テスト.txt"))).Count(), "FileNode not found.");


            Assert.AreEqual(3, archive2.GetFileNodes().Count(), "FileNode count incorrect.");
            Assert.AreEqual(1, archive2.GetFileNodes().Where(x => x.FullSessionName.Equals(Path.Combine(targetDir1, "KeyMap.txt"))).Count(), "FileNode not found.");
            Assert.AreEqual(1, archive2.GetFileNodes().Where(x => x.FullSessionName.Equals(Path.Combine(targetDir2, "0266554465.jpeg"))).Count(), "FileNode not found.");
            Assert.AreEqual(0, archive2.GetFileNodes().Where(x => x.FullSessionName.Equals(Path.Combine(targetDir1, "Nikon-1-V3-sample-photo.jpg"))).Count(), "FileNode not found.");
            Assert.AreEqual(1, archive2.GetFileNodes().Where(x => x.FullSessionName.Equals(Path.Combine(targetDir1, "umlaut_äü(&テスト.txt"))).Count(), "FileNode not found.");


            Assert.AreEqual(3, fi1.Hashes.Count, "FileHash count incorrect.");
            Assert.AreEqual(1, fi1.Hashes.Where(x => x.Checksum.Equals(f1Hash)).Count(), "FileHash not found.");
            Assert.AreEqual(1, fi1.Hashes.Where(x => x.Checksum.Equals(f2Hash)).Count(), "FileHash not found.");
            Assert.AreEqual(0, fi1.Hashes.Where(x => x.Checksum.Equals(f3Hash)).Count(), "FileHash not found.");

            Assert.AreEqual(3, fi2.Hashes.Count, "FileHash count incorrect.");
            Assert.AreEqual(1, fi2.Hashes.Where(x => x.Checksum.Equals(f1Hash)).Count(), "FileHash not found.");
            Assert.AreEqual(1, fi2.Hashes.Where(x => x.Checksum.Equals(f2Hash)).Count(), "FileHash not found.");
            Assert.AreEqual(0, fi2.Hashes.Where(x => x.Checksum.Equals(f3Hash)).Count(), "FileHash not found.");
        }
    }
}
