using MediaBackupManager.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBackupManager.ViewModel
{
    class FileIndexViewModel : ViewModelBase.ViewModelBase
    {

        #region Fields

        FileIndex index = new FileIndex();
        private ObservableCollection<BackupSetViewModel> backupSets;

        #endregion

        #region Properties

        public FileIndex Index { get; set; }

        public ObservableCollection<BackupSetViewModel> BackupSets
        {
            get { return backupSets; }
            set
            {
                if (value != backupSets)
                {
                    backupSets = value;
                    OnPropertyChanged("");
                }
            }
        }


        #endregion

        #region Methods

        public FileIndexViewModel(FileIndex index)
        {
            this.Index = index;
        }

        #endregion

        #region Implementations

        #endregion


    }
}
