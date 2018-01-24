using MediaBackupManager.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBackupManager.ViewModel
{
    class FileIndexViewModel : ViewModelBase.ViewModelBase
    {
        FileIndex index = new FileIndex();
        public FileIndex Index { get; set; }

    }
}
