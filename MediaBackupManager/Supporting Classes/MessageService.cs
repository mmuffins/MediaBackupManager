using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBackupManager.SupportingClasses
{
    /// <summary>
    /// A helper class that allows the exchange of messages between objects, decreasing the coupling between MVVM layers.</summary>  
    public static class MessageService
    {
        //TODO:Q-Fine to use a universal message service with magic strings instead of individual events?
        public static event EventHandler<MessageServiceEventArgs> RoutedMessage;

        /// <summary>
        /// Sends a message by generating a RoutedMessage event.</summary>
        /// <param name="sender">The sender of the message.</param>
        /// <param name="propertyName">The property name of the message.</param>
        /// <param name="parameter">An optional object related to the message.</param>
        public static void SendMessage(object sender, string propertyName, object parameter)
        {
            RoutedMessage(sender, new MessageServiceEventArgs(propertyName, parameter));
        }
    }

    /// <summary>
    /// Provides data for RoutedMessage evengs.</summary>  
    public class MessageServiceEventArgs
    {
        /// <summary>
        /// Gets the property name of the event.</summary>
        public string Property { get; }

        /// <summary>
        /// Gets an optional object related to the event.</summary>
        public object Parameter { get; }

        public MessageServiceEventArgs(string propertyName, object parameter)
        {
            this.Property = propertyName;
            this.Parameter = parameter;
        }
    }

}
