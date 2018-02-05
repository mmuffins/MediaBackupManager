using MediaBackupManager.SupportingClasses;
using MediaBackupManager.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MediaBackupManager.View
{
    /// <summary>
    /// Interaction logic for DirectoryBrowserView.xaml
    /// </summary>
    public partial class DirectoryBrowserView : UserControl
    {
        public DirectoryBrowserView()
        {
            InitializeComponent();
        }

        private void treeDirectory_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // SelectedItem on treeView is readonly for some reason, 
            // so we need to raise an event instead of directly binding
            if (DataContext != null)
                ((DirectoryBrowserViewModel)DataContext).SelectedDirectoryTreeItem = e.NewValue;
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            this.CommandBindings.Clear();
        }
        
        private void GridFiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListViewItem)
            {
                if (!((ListViewItem)sender).IsSelected)
                {
                    return;
                }
            }

            // An item on the item grid was double clicked, inform the viewmodel
            MessageService.SendMessage(this, "GridFiles_MouseDoubleClick", ((ListViewItem)sender).Content);

            //if (DataContext != null)
            //    ((DirectoryBrowserViewModel)DataContext).GridFiles_MouseDoubleClick(((ListViewItem)sender).Content);

        }

        private void BreadcrumbBar_MouseUp(object sender, MouseButtonEventArgs e)
        {
            MessageService.SendMessage(this, "BreadcrumbBar_MouseUp", ((StackPanel)sender).DataContext);

        }

        private void SearchBar_KeyDown(object sender, KeyEventArgs e)
        {
            if (!(sender is TextBox))
                return;

            if (e.Key != Key.Enter)
                return;

            MessageService.SendMessage(this, "PerformFileSearch", ((TextBox)sender).Text);
        }

        private void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Back and delete keypresses events are consumed by the textbox to 
            // delete a character, so we need to handle them seperately from the keydown event

            if (!(sender is TextBox))
                return;

            if(string.IsNullOrWhiteSpace(((TextBox)sender).Text))
                MessageService.SendMessage(this, "ResetFileSearch", ((TextBox)sender).Text);
        }
    }
}
