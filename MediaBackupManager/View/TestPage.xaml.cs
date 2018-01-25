using MediaBackupManager.Model;
using MediaBackupManager.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Shapes;

namespace MediaBackupManager.View
{
    /// <summary>
    /// Interaction logic for TestPage.xaml
    /// </summary>
    public partial class TestPage : Window
    {
        FileIndex index;
        TestPageViewModel mvvm;

        public TestPage(FileIndex index)
        {
            InitializeComponent();
            this.index = index;
            mvvm = new TestPageViewModel(index);
            this.DataContext = mvvm;
            //this.index = App.Current.Properties["FileIndex"] as FileIndex;
        }

    }
}
