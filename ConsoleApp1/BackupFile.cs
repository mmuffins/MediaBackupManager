using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    /// <summary>
    /// Virtual representation of a file in one or multiple backup set.</summary>  
    class BackupFile : IEquatable<BackupFile>
    {
        public long Length { get; }
        public DateTime CreationTime { get; }
        public DateTime CreationTimeUtc { get; }
        public DateTime LastWriteTime { get; }
        public DateTime LastWriteTimeUtc { get; }
        public string CheckSum { get; }
        public HashSet<FileNode> Nodes { get; }
        public int BackupCount { get => Nodes.Select(x => x.Directory.Drive).Distinct().Count(); }

        public BackupFile(FileInfo fileInfo, string checkSum)
        {
            this.Nodes = new HashSet<FileNode>();
            this.Length = fileInfo.Length;
            this.CreationTime = fileInfo.CreationTime;
            this.CreationTimeUtc = fileInfo.CreationTimeUtc;
            this.LastWriteTime = fileInfo.LastWriteTime;
            this.LastWriteTimeUtc = fileInfo.LastWriteTimeUtc;
            this.CheckSum = checkSum;
            this.Nodes = new HashSet<FileNode>();
        }

        public BackupFile(string fileName, string checkSum) : this(new FileInfo(fileName), checkSum) { }


        /// <summary>Generates MD5 hash</summary>  
        /// <param name="filePath">The fully qualified or relative name of a physical file</param>
        public static string CalculateChecksum(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", String.Empty);
                }
            }
        }

        /// <summary>Adds a new physical location to the current file</summary>  
        public void AddNode(FileNode node)
        {
            Nodes.Add(node);
        }

        public override int GetHashCode()
        {
            return CheckSum.GetHashCode();
        }

        public bool Equals(BackupFile other)
        {
            if (other == null)
                return false;

            return this.CheckSum.Equals(other.CheckSum);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            BackupFile otherObj = obj as BackupFile;
            if (otherObj == null)
                return false;
            else
                return Equals(otherObj);
        }

        public override string ToString()
        {
            return CheckSum;
        }
    }
}
