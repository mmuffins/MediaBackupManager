using MediaBackupManager.Model;
using MediaBackupManager.ViewModel;
using System;
using System.Collections.Generic;
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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainWindowViewModel mvvm = new MainWindowViewModel();
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = mvvm;
            //App.Current.Properties["FileIndex"] = new FileIndex();

            var testPage = new TestPage();
            testPage.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            testPage.Show();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            this.CommandBindings.Clear();
        }
    }
}
