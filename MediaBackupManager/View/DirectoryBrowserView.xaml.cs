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
        
        private void gridFiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListViewItem)
            {
                if (!((ListViewItem)sender).IsSelected)
                {
                    return;
                }
            }

            // An item on the item grid was double clicked, inform the viewmodel
            if (DataContext != null)
                ((DirectoryBrowserViewModel)DataContext).GridFiles_MouseDoubleClick(((ListViewItem)sender).Content);
        }
    }
}
