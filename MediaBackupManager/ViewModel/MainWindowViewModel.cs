using MediaBackupManager.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBackupManager.ViewModel
{
    class MainWindowViewModel
    {
        private FileIndex fileIndex;
        public FileIndex FileIndex
        {
            get { return fileIndex; }
            set { fileIndex = value; }
        }

        public MainWindowViewModel(FileIndex index)
        {
            this.FileIndex = index;
            PrepareDatabase(index);
        }

        private void PrepareDatabase(FileIndex index)
        {
            Database.FileIndex = index;
            Database.CreateDatabase();
            Database.PrepareDatabase();
        }
    }
}
