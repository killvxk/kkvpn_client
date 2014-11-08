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
    /// Interaction logic for PageConnection.xaml
    /// </summary>
    public partial class PageConnection : Page
    {
        private ConnectionManager Connection;
        private MainWindow ParentWindow;

        public PageConnection(MainWindow ParentWindow)
        {
            this.ParentWindow = ParentWindow;
            Connection = ((App)Application.Current).Connection;
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ParentWindow.NavigateTo((sender as Control).Tag as string);
            ParentWindow.SetVisibilityWelcomeAndSettings(false);
            ParentWindow.ChangeConnectMenuItemTarget((sender as Control).Tag as string);
        }
    }
}
