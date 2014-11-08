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

namespace kkvpn_client.Screens
{
    /// <summary>
    /// Interaction logic for pageWelcome.xaml
    /// </summary>
    public partial class PageWelcome : Page
    {
        private MainWindow ParentWindow;

        public PageWelcome(MainWindow ParentWindow)
        {
            this.ParentWindow = ParentWindow;
            InitializeComponent();
        }

        private void btnConnection_Click(object sender, RoutedEventArgs e)
        {
            ParentWindow.NavigateTo("connection");
            ParentWindow.btnConnect.IsChecked = true;
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            ParentWindow.NavigateTo("settings");
            ParentWindow.btnSettings.IsChecked = true;
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            ParentWindow.NavigateTo("help");
            ParentWindow.btnHelp.IsChecked = true;
        }
    }
}
