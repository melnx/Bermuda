using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Bermuda.Catalog;

namespace Bermuda.BermudaConfig
{
    /// <summary>
    /// Interaction logic for NewOpenConfig.xaml
    /// </summary>
    public partial class NewOpenConfig : Window
    {
        public NewOpenConfig()
        {
            InitializeComponent();
        }

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            ComputeNode compute_node = new ComputeNode();
            ComputeNodeConfig config = new ComputeNodeConfig(compute_node);
            config.Show();
            Close();
        }

        private void btnOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenConfig dlg = new OpenConfig();
            dlg.Show();
            Close();
        }
    }
}
