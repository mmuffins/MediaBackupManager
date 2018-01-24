using MediaBackupManager.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBackupManager.ViewModel
{
    class BackupSetViewModel : ViewModelBase.ViewModelBase
    {
        ObservableCollection<BackupSet> backupSets = new ObservableCollection<BackupSet>();
        public ObservableCollection<BackupSet> BackupSets { get => backupSets; }

        public BackupSetViewModel()
        {
            //FileIndex.LoadData();
            this.backupSets = new ObservableCollection<BackupSet>(FileIndex.BackupSets);
            //var ab
        }
    }
}
