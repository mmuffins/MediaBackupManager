using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MediaBackupManager.SupportingClasses
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        #region Fields

        public event PropertyChangedEventHandler PropertyChanged;
        string title;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the title of the current view.</summary>  
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

        #endregion

        #region Methods

        public ViewModelBase()
        {
            MessageService.RoutedMessage += new EventHandler<MessageServiceEventArgs>(OnMessageServiceMessage);
        }

        /// <summary>
        /// Event handler for the global MessageService.</summary>
        protected virtual void OnMessageServiceMessage(object sender, MessageServiceEventArgs e) { }

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            // take a copy to prevent thread issues
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }


        #endregion
    }
}
