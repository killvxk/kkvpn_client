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
    /// Interaction logic for PageDisconnect.xaml
    /// </summary>
    public partial class PageDisconnect : Page
    {
        private ConnectionManager Connection;
        private MainWindow ParentWindow;

        public PageDisconnect(MainWindow ParentWindow)
        {
            this.ParentWindow = ParentWindow;
            Connection = ((App)Application.Current).Connection;
            InitializeComponent();
        }

        private void btnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            Connection.Disconnect();

            ParentWindow.NavigateTo("welcome");
            ParentWindow.ChangeConnectMenuItemTarget("connection");
            ParentWindow.SetConnected(false);
        }
    }
}
