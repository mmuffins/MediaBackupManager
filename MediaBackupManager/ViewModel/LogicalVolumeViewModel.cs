using MediaBackupManager.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBackupManager.ViewModel
{
    public class LogicalVolumeViewModel : ViewModelBase.ViewModelBase
    {
        #region Fields

        LogicalVolume logicalVolume;

        #endregion

        #region Properties

        public string SerialNumber
        {
            get => logicalVolume.SerialNumber;
        }

        public ulong Size
        {
            get => logicalVolume.Size;
        }

        public System.IO.DriveType Type
        {
            get => logicalVolume.Type;
        }

        public string VolumeName
        {
            get => logicalVolume.VolumeName;
        }

        public string MountPoint
        {
            get => logicalVolume.MountPoint;
        }

        #endregion

        #region Methods

        public LogicalVolumeViewModel(LogicalVolume logicalVolume)
        {
            this.logicalVolume = logicalVolume;
        }

        #endregion
    }
}
