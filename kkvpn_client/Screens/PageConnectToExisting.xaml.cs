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
    /// Interaction logic for PageConnectToExisting.xaml
    /// </summary>
    public partial class PageConnectToExisting : Page
    {
        private ConnectionManager Connection;
        private MainWindow ParentWindow;

        public PageConnectToExisting(MainWindow ParentWindow)
        {
            this.ParentWindow = ParentWindow;
            Connection = ((App)Application.Current).Connection;
            InitializeComponent();
        }

        private void btnCopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(tbConnectionString.Text);
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Connection.Disconnect();
            ReturnToConnectionMainPage();
        }

        private void tbName_TextChanged(object sender, TextChangedEventArgs e)
        {
            tbConnectionString.Text = Connection.GetConnectionString(tbName.Text);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Connection.OpenForConnection();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Otwarcie portu niemożliwe: " + ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                ReturnToConnectionMainPage();
            }
        }

        private void ReturnToConnectionMainPage()
        {
            ParentWindow.NavigateTo("connection");
            ParentWindow.SetVisibilityWelcomeAndSettings(true);
            ParentWindow.ChangeConnectMenuItemTarget("connection");
        }
    }
}
