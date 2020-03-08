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

namespace Bank_ClientApp
{

    public partial class CIWindow : Window
    {
        public Window myHandler;

        public CIWindow()
        {
            InitializeComponent();
        }

        private void Grid_Unloaded(object sender, RoutedEventArgs e)
        {
            myHandler.ShowInTaskbar = true;
            myHandler.Visibility = Visibility.Visible;
        }
    }
}
