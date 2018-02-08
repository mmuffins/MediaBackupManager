using MediaBackupManager.SupportingClasses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBackupManager.ViewModel
{
    public class ExclusionListViewModel : ViewModelBase
    {
        #region Fields

        FileIndexViewModel index;
        string newExclusionText;
        string selectedExclusion;

        RelayCommand addExclusionCommand;
        RelayCommand closeOverlayCommand;
        RelayCommand removeExclusionCommand;

        #endregion

        #region Properties

        public ObservableCollection<string> Exclusions
        {
            get { return index.Exclusions; }
            set
            {
                if (value != index.Exclusions)
                {
                    index.Exclusions = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string NewExclusionText
        {
            get { return newExclusionText; }
            set
            {
                if (value != newExclusionText)
                {
                    newExclusionText = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string SelectedExclusion
        {
            get { return selectedExclusion; }
            set
            {
                if (value != selectedExclusion)
                {
                    selectedExclusion = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public RelayCommand AddExclusionCommand
        {
            get
            {
                if (addExclusionCommand == null)
                {
                    addExclusionCommand = new RelayCommand(
                        p => AddExclusionAsync(p.ToString()),
                        p => !String.IsNullOrWhiteSpace(NewExclusionText));
                }
                return addExclusionCommand;
            }
        }

        public RelayCommand RemoveExclusionCommand
        {
            get
            {
                if (removeExclusionCommand == null)
                {
                    removeExclusionCommand = new RelayCommand(
                        p => RemoveExclusionAsync(p.ToString()),
                        p => true);
                }
                return removeExclusionCommand;
            }
        }

        public RelayCommand CloseOverlayCommand
        {
            get
            {
                if (closeOverlayCommand == null)
                {
                    closeOverlayCommand = new RelayCommand(
                        p => MessageService.SendMessage(this, "DisposeOverlay", null),
                        p => true);
                }
                return closeOverlayCommand;
            }
        }


        #endregion

        #region Methods

        public ExclusionListViewModel(FileIndexViewModel index)
        {
            this.index = index;
        }

        private async void AddExclusionAsync(string exclusion)
        {
            await index.CreateFileExclusionAsync(exclusion);
            NewExclusionText = String.Empty;
        }

        private async void RemoveExclusionAsync(string exclusion)
        {
            await index.RemoveFileExclusionAsync(exclusion);
        }

        #endregion
    }
}
