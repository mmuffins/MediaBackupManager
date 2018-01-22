using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MediaBackupManager.Model
{
    /// <summary>
    /// Virtual representation of a file in one or multiple backup set.</summary>  
    class BackupFile : IEquatable<BackupFile>
    {
        public long Length { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime LastWriteTime { get; set;  }
        public string CheckSum { get; set; }
        public HashSet<FileNode> Nodes { get; }
        public int NodeCount { get => Nodes.Count; }
        public int BackupCount { get => Nodes.Select(x => x.BackupSet.Drive).Distinct().Count(); }

        public BackupFile()
        {
            this.Nodes = new HashSet<FileNode>();
        }

        public BackupFile(FileInfo fileInfo, string checkSum) : this()
        {
            this.Length = fileInfo.Length;
            this.CreationTime = fileInfo.CreationTime;
            this.LastWriteTime = fileInfo.LastWriteTime;
            this.CheckSum = checkSum;
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

        /// <summary>Adds a reference to a physical location for the file.</summary>  
        public void AddNode(FileNode node)
        {
            Nodes.Add(node);
            var a1 = Nodes.Select(x => x.BackupSet.Drive).Distinct();
        }

        /// <summary>Removes reference to a phyisical location for the file.</summary>  
        public void RemoveNode(FileNode node)
        {
            Nodes.Remove(node);

            if(Nodes.Count == 0)
                FileIndex.RemoveFile(this);
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
