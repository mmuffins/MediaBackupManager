﻿using MediaBackupManager.Model;
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
        public MainWindow()
        {
            InitializeComponent();
            var index = new FileIndex();
            this.DataContext = new MainWindowViewModel(index); ;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            this.CommandBindings.Clear();
        }
    }
}
