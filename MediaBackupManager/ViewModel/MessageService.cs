using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBackupManager.ViewModel
{
    public static class MessageService
    {
        //TODO:Q-Fine to use a universal message service with magic strings instead of individual events?
        public static event EventHandler<MessageServiceEventArgs> RoutedMessage;

        public static void SendMessage(object sender, string propertyName, string message, object argument)
        {
            RoutedMessage(sender, new MessageServiceEventArgs(propertyName, message, argument));
        }
    }

    public class MessageServiceEventArgs
    {
        public string Property { get; }
        public string Message { get; }
        public object Argument { get; }

        public MessageServiceEventArgs(string propertyName, string message, object argument)
        {
            this.Property = propertyName;
            this.Message = message;
            this.Argument = argument;
        }
    }

}
