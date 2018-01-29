using MediaBackupManager.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBackupManager.ViewModel
{
    class DirectoryBrowserViewModel : ViewModelBase.ViewModelBase
    {
        #region Fields

        private FileIndexViewModel index;

        #endregion

        public FileIndexViewModel Index
        {
            get { return index; }
            set { index = value; }
        }

        #region Methods

        public DirectoryBrowserViewModel(FileIndex index)
        {
            this.Index = new FileIndexViewModel(index);
            //this.Index = index;
        }

        #endregion
    }
}
