using MediaBackupManager.SupportingClasses;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MediaBackupManager.View
{
    /// <summary>
    /// Interaction logic for ArchiveOverview.xaml
    /// </summary>
    public partial class ArchiveOverview : UserControl
    {
        public ArchiveOverview()
        {
            InitializeComponent();
        }

        private void Archive_MouseDown(object sender, MouseButtonEventArgs e)
        {
            
            MessageService.SendMessage(this, "ShowDirectoryBrowserView", ((Grid)sender).DataContext);
        }

        private void OnArchiveLabelKeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                BindingExpression binding = ((TextBox)sender).GetBindingExpression(TextBox.TextProperty);
                binding.UpdateSource();
            }
        }
    }
}
