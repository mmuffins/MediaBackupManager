using MediaBackupManager.Model;
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

        private void OnCurrentDirectoryChanged(object sender, PropertyChangedEventArgs e)
        {
            //TODO:Q-referencing MediaBackupManager.Model breaks mvvm, any better way to implement this?
            if (e.PropertyName.Equals("CurrentDirectory"))
            {
                // Get the current directory
                var currentDir = "";
                if (this.treeDirectory.SelectedItem is FileDirectory)
                    currentDir = ((FileDirectory)this.treeDirectory.SelectedItem).FullName;
                else if (this.treeDirectory.SelectedItem is BackupSet)
                    currentDir = ((BackupSet)this.treeDirectory.SelectedItem).RootDirectory;

                // Get the new directory
                var newDir = "";
                if (((DirectoryBrowserViewModel)sender).CurrentDirectory != null)
                    newDir = ((DirectoryBrowserViewModel)sender).CurrentDirectory.FullName;

                if (currentDir != newDir)
                {
                    // Change the directory
                }

            }

        }

        private void treeDirectory_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // SelectedItem on treeView is readonly for some reason, 
            // so we need to raise an event instead of directly binding
            if (this.DataContext != null)
                ((DirectoryBrowserViewModel)this.DataContext).SelectedDirectoryTreeItem = ((RoutedPropertyChangedEventArgs<object>)e).NewValue;
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            this.CommandBindings.Clear();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext != null)
                ((DirectoryBrowserViewModel)this.DataContext).PropertyChanged += new PropertyChangedEventHandler(OnCurrentDirectoryChanged);
        }
    }
}
