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
    /// A unique checksum for a file which can be related to one or more file nodes.</summary>  
    public class FileHash : IEquatable<FileHash>
    {
        #region Properties

        /// <summary>
        /// Gets or sets the file length of the current file hash.</summary>  
        public long Length { get; set; }

        /// <summary>
        /// Gets or sets the creation time of the current file hash.</summary>  
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// Gets or sets the last write time of the current file hash.</summary>  
        public DateTime LastWriteTime { get; set; }

        /// <summary>
        /// Gets or sets the checksum of the current file hash.</summary>  
        public string Checksum { get; set; }

        /// <summary>
        /// Gets a list of all file nodes related to the current file hash.</summary>  
        public ObservableHashSet<FileNode> Nodes { get; }

        /// <summary>
        /// Gets the count of all file nodes related to the current file hash.</summary>  
        public int NodeCount { get => Nodes.Count; }

        /// <summary>
        /// Gets the count logical volumes containing the current file hash.</summary>  
        public int BackupCount { get => Nodes.Select(x => x.Archive.Volume).Distinct().Count(); }

        #endregion

        #region Methods

        public FileHash()
        {
            this.Nodes = new ObservableHashSet<FileNode>();
        }

        public FileHash(FileInfo fileInfo, string checkSum) : this()
        {
            this.Length = fileInfo.Length;
            this.CreationTime = fileInfo.CreationTime;
            this.LastWriteTime = fileInfo.LastWriteTime;
            this.Checksum = checkSum;
        }

        public FileHash(string fileName, string checkSum) : this(new FileInfo(fileName), checkSum) { }

        /// <summary>Generates MD5 hash</summary>  
        /// <param name="filePath">The fully qualified or relative name of a physical file</param>
        public static string CalculateChecksum(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = new BufferedStream(File.OpenRead(filePath), 120000))
                {
                    return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", String.Empty);
                }
            }
        }


        /// <summary>
        /// Adds a reference to a physical location for the file.</summary>  
        public void AddNode(FileNode node)
        {
            Nodes.Add(node);
        }

        /// <summary>
        /// Removes reference to a phyisical location for the file.</summary>  
        public void RemoveNode(FileNode node)
        {
            Nodes.Remove(node);

            //if(Nodes.Count == 0)
            //    FileIndex.RemoveFile(this);
        }

        #endregion

        #region Implementations

        public override int GetHashCode()
        {
            return Checksum.GetHashCode();
        }

        public bool Equals(FileHash other)
        {
            if (other == null)
                return false;

            return this.Checksum.Equals(other.Checksum);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            FileHash otherObj = obj as FileHash;
            if (otherObj == null)
                return false;
            else
                return Equals(otherObj);
        }

        public override string ToString()
        {
            return Checksum;
        }

        #endregion
    }
}
