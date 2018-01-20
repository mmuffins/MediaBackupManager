using MediaBackupManager.Model;
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
        FileIndex fileIndex;

        public TestPage()
        {
            InitializeComponent();
            this.fileIndex = App.Current.Properties["FileIndex"] as FileIndex;

            fileIndex.AddDirectory(new DirectoryInfo(@"D:\indexdir"));
            fileIndex.AddDirectory(new DirectoryInfo(@"D:\indexdir\dd"));

            fileIndex.AddDirectory(new DirectoryInfo(@"F:\indexdir\main\images"));
            fileIndex.AddDirectory(new DirectoryInfo(@"F:\indexdir\main\images2"));

        }
    }
}
