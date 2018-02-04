using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBackupManager.SupportingClasses
{
    public static class MessageService
    {
        //TODO:Q-Fine to use a universal message service with magic strings instead of individual events?
        public static event EventHandler<MessageServiceEventArgs> RoutedMessage;

        public static void SendMessage(object sender, string propertyName, object parameter)
        {
            RoutedMessage(sender, new MessageServiceEventArgs(propertyName, parameter));
        }
    }

    public class MessageServiceEventArgs
    {
        public string Property { get; }
        public object Parameter { get; }

        public MessageServiceEventArgs(string propertyName, object parameter)
        {
            this.Property = propertyName;
            this.Parameter = parameter;
        }
    }

}
