﻿using System;
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

namespace MediaBackupManager.View.Popups
{
    /// <summary>
    /// Interaction logic for PopupBase.xaml
    /// </summary>
    public partial class PopupBase : Window
    {
        public PopupBase()
        {
            InitializeComponent();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            this.CommandBindings.Clear();
        }
    }
}
