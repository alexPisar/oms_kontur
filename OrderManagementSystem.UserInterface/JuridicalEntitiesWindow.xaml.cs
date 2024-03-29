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
using DevExpress.Xpf.Ribbon;
using OrderManagementSystem.UserInterface.ViewModels;

namespace OrderManagementSystem.UserInterface
{
    /// <summary>
    /// Логика взаимодействия для JuridicalEntitiesWindow.xaml
    /// </summary>
    public partial class JuridicalEntitiesWindow :  DXRibbonWindow
    {
        public JuridicalEntitiesWindow()
        {
            InitializeComponent();
        }

        private void ChangedBuyer(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            ((JuridicalEntitiesModel)DataContext).ChangedBuyer();
        }
    }
}
