﻿using System;
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
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            // take a copy to prevent thread issues
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public ViewModelBase()
        {
            MessageService.RoutedMessage += new EventHandler<MessageServiceEventArgs>(OnMessageServiceMessage);
        }

        /// <summary>
        /// Event handler for the global MessageService.</summary>
        protected virtual void OnMessageServiceMessage(object sender, MessageServiceEventArgs e) { }
    }
}
