using MediaBackupManager.SupportingClasses;
using MediaBackupManager.View.Popups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MediaBackupManager.ViewModel.Popups
{
    public class YesNoPopupViewModel : ViewModelBase
    {
        #region Fields

        string title;
        string message;
        bool isVisible;
        DialogResult result;
        YesNoPopup window;

        RelayCommand yesCommand;
        RelayCommand noCommand;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the title text of the popup.</summary>  
        public string Title
        {
            get { return title; }
            set
            {
                if (value != title)
                {
                    title = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the message text of the popup.</summary>  
        public string Message
        {
            get { return message; }
            set
            {
                if (value != message)
                {
                    message = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the visibility of the popup.</summary>  
        public bool IsVisible
        {
            get { return isVisible; }
            set
            {
                if (value != isVisible)
                {
                    isVisible = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the result of the popup.</summary>  
        public DialogResult Result
        {
            get { return result; }
            set
            {
                if (value != result)
                {
                    result = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Sets the Popup result to false.</summary>  
        public RelayCommand NoCommand
        {
            get
            {
                if (noCommand == null)
                {
                    noCommand = new RelayCommand(
                        p => SetPopupResult(DialogResult.No),
                        p => true);
                }
                return noCommand;
            }
        }

        /// <summary>
        /// Sets the Popup result to true.</summary>  
        public RelayCommand YesCommand
        {
            get
            {
                if (yesCommand == null)
                {
                    yesCommand = new RelayCommand(
                        p => SetPopupResult(DialogResult.Yes),
                        p => true);
                }
                return yesCommand;
            }
        }


        #endregion

        #region Methods

        public YesNoPopupViewModel(string message, string title = "")
        {
            this.title = title;
            this.message = message;
        }

        private void SetPopupResult(DialogResult dialogResult)
        {
            Result = dialogResult;
            window.Close();
        }

        public DialogResult ShowModal()
        {
            window = new YesNoPopup();
            window.DataContext = this;
            window.ShowDialog();
            return Result;
        }



        #endregion

    }
}
