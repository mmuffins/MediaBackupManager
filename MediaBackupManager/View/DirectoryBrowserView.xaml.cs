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
        GridViewColumnHeader _lastHeaderClicked = null;
        ListSortDirection _lastDirection = ListSortDirection.Ascending;

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

        private void Sort(string sortBy, ListSortDirection direction)
        {
            ICollectionView dataView =
              CollectionViewSource.GetDefaultView(gridFiles.ItemsSource);


            dataView.SortDescriptions.Clear();
            SortDescription sd = new SortDescription(sortBy, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();
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
        }

        private void BreadcrumbBar_MouseUp(object sender, MouseButtonEventArgs e)
        {
            MessageService.SendMessage(this, "BreadcrumbBar_MouseUp", ((StackPanel)sender).DataContext);

        }

        void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {
            var headerClicked = e.OriginalSource as GridViewColumnHeader;
            ListSortDirection direction;

            if (headerClicked != null)
            {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    if (headerClicked != _lastHeaderClicked)
                    {
                        direction = ListSortDirection.Ascending;
                    }
                    else
                    {
                        if (_lastDirection == ListSortDirection.Ascending)
                            direction = ListSortDirection.Descending;
                        else
                            direction = ListSortDirection.Ascending;
                    }

                    var columnBinding = headerClicked.Column.DisplayMemberBinding as Binding;
                    var sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;

                    Sort(sortBy, direction);

                    if (direction == ListSortDirection.Ascending)
                    {
                        headerClicked.Column.HeaderTemplate =
                          Resources["HeaderTemplateArrowUp"] as DataTemplate;
                    }
                    else
                    {
                        headerClicked.Column.HeaderTemplate =
                          Resources["HeaderTemplateArrowDown"] as DataTemplate;
                    }

                    // Remove arrow from previously sorted header  
                    if (_lastHeaderClicked != null && _lastHeaderClicked != headerClicked)
                    {
                        _lastHeaderClicked.Column.HeaderTemplate = null;
                    }

                    _lastHeaderClicked = headerClicked;
                    _lastDirection = direction;
                }
            }
        }
    }
}
