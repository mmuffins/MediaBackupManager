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
    /// <summary>
    /// Base class for popup viewmodels.</summary>  
    public class PopupBaseViewModel : ViewModelBase
    {
        #region Fields

        protected PopupBase window;
        ViewModelBase popupViewModel;
        string title;
        DialogResult result;
        bool showOkButton;
        bool showCancelButton;
        bool showIgnoreBuggon;
        string okButtonCaption;
        string cancelButtonCaption;
        string ignoreButtonCaption;

        RelayCommand okCommand;
        RelayCommand cancelCommand;
        RelayCommand ignoreCommand;

        #endregion

        #region Properties

        /// <summary>
        /// Sets or gets the content viemodel of the popup.</summary>  
        public ViewModelBase PopupViewModel
        {
            get { return popupViewModel; }
            set
            {
                if (value != popupViewModel)
                {
                    popupViewModel = value;
                    NotifyPropertyChanged();
                }
            }
        }

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
        /// Gets or sets the value controlling whether the OK button should be displayed.</summary>  
        public bool ShowOkButton
        {
            get { return showOkButton; }
            set
            {
                if (value != showOkButton)
                {
                    showOkButton = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the value controlling whether the Cancel button should be displayed.</summary>  
        public bool ShowCancelButton
        {
            get { return showCancelButton; }
            set
            {
                if (value != showCancelButton)
                {
                    showCancelButton = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the value controlling whether the Ignore button should be displayed.</summary>  
        public bool ShowIgnoreButton
        {
            get { return showIgnoreBuggon; }
            set
            {
                if (value != showIgnoreBuggon)
                {
                    showIgnoreBuggon = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the caption on the Ok button.</summary>  
        public string OkButtonCaption
        {
            get { return okButtonCaption; }
            set
            {
                if (value != okButtonCaption)
                {
                    okButtonCaption = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the caption on the Cancel button.</summary>  
        public string CancelButtonCaption
        {
            get { return cancelButtonCaption; }
            set
            {
                if (value != cancelButtonCaption)
                {
                    cancelButtonCaption = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the caption on the Ignore button.</summary>  
        public string IgnoreButtonCaption
        {
            get { return ignoreButtonCaption; }
            set
            {
                if (value != ignoreButtonCaption)
                {
                    ignoreButtonCaption = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Sets the Popup result to Ok.</summary>  
        public RelayCommand OkCommand
        {
            get
            {
                if (okCommand == null)
                {
                    okCommand = new RelayCommand(
                        p => SetPopupResult(DialogResult.OK),
                        p => true);
                }
                return okCommand;
            }
        }

        /// <summary>
        /// Sets the Popup result to Cancel.</summary>  
        public RelayCommand CancelCommand
        {
            get
            {
                if (cancelCommand == null)
                {
                    cancelCommand = new RelayCommand(
                        p => SetPopupResult(DialogResult.Cancel),
                        p => true);
                }
                return cancelCommand;
            }
        }

        /// <summary>
        /// Sets the Popup result to Ignore.</summary>  
        public RelayCommand IgnoreCommand
        {
            get
            {
                if (ignoreCommand == null)
                {
                    ignoreCommand = new RelayCommand(
                        p => SetPopupResult(DialogResult.Ignore),
                        p => true);
                }
                return ignoreCommand;
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

        #endregion

        /// <summary>
        /// Initializes a new instance of the PopupBaseViewModel class.</summary>  
        public PopupBaseViewModel()
        {
            this.okButtonCaption = "Ok";
            this.cancelButtonCaption = "Cancel";
            this.ignoreButtonCaption = "Ignore";
            this.showOkButton = true;
            this.showCancelButton = false;
            this.showIgnoreBuggon = false;

            this.popupViewModel = this;
            window = new PopupBase();
            window.Owner = App.Current.MainWindow;
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            window.DataContext = this;
        }

        /// <summary>
        /// Sets the DialogResult property of the popup.</summary>  
        private void SetPopupResult(DialogResult dialogResult)
        {
            Result = dialogResult;
            window.Close();
        }
    }
}
