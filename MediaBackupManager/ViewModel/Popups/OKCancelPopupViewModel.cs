using MediaBackupManager.SupportingClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MediaBackupManager.ViewModel.Popups
{
    public class OKCancelPopupViewModel : PopupBaseViewModel
    {
        #region Fields

        string message;

        #endregion

        #region Properties

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

        #endregion

        #region Methods
        /// <summary>
        /// Initializes a new instance of the OKCancelPopup class with a custom title and button captions.</summary>  
        /// <param name="okButtonCaption">The caption to be displayed for the OK button.</param>
        /// <param name="cancelButtonCaption">The caption to be displayed for the Cancel button.</param>
        public OKCancelPopupViewModel(string message, string title, string okButtonCaption, string cancelButtonCaption)
        {
            this.Title = title;
            this.Message = message;

            this.OkButtonCaption = okButtonCaption;
            this.CancelButtonCaption = cancelButtonCaption;

            this.ShowOkButton = true;
            this.ShowCancelButton = true;
            this.ShowIgnoreButton = false;
        }

        /// <summary>
        /// Initializes a new instance of the YesNoPopup class.</summary>  
        public OKCancelPopupViewModel(string message, string title)
            : this(message, title, "Ok", "Cancel") { }

        /// <summary>
        /// Initializes a new instance of the YesNoPopup class with a custom title.</summary>  
        public OKCancelPopupViewModel(string message)
            : this(message, "") { }

        /// <summary>
        /// Opens a window and returns only when the newly opened window is closed.</summary>  
        public DialogResult ShowDialog()
        {
            window.ShowDialog();
            window = null;
            return Result;
        }


        #endregion

    }
}
