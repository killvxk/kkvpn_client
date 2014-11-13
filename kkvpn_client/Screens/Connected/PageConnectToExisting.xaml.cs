using kkvpn_client.Misc;
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
        private AppSettings Settings;
        private bool ReturningFromChecksum;

        public PageConnectToExisting(MainWindow ParentWindow)
        {
            this.ParentWindow = ParentWindow;
            Connection = ((App)Application.Current).Connection;
            Settings = ((App)Application.Current).Settings;
            ReturningFromChecksum = false;

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

        private void tbPeerName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (tbPeerName.Text != "")
            {
                Settings.PeerName = tbPeerName.Text;
                tbConnectionString.Text = Connection.GetConnectionString(tbPeerName.Text);
            }
            else
            {
                tbConnectionString.Text = "(wpisz nazwę użytkownika)";
            }

            btnCopyToClipboard.IsEnabled = (tbPeerName.Text != "");
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (ReturningFromChecksum)
            {
                return;
            }

            try
            {
                tbPeerName.Text = Settings.PeerName;
                tbConnectionString.Text = Connection.GetConnectionString(tbPeerName.Text);
                lblPort.Text = "Otwieranie portu, proszę czekać.";
                await Connection.OpenForConnection();                
                lblPort.Text = "Port otwarty, oczekiwanie na połączenie.";
            }
            catch (OperationCanceledException) 
            {
                Connection.Disconnect();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Otwarcie portu niemożliwe: " + ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                Logger.Instance.LogException(ex);
                ReturnToConnectionMainPage();
            }
        }

        private void ReturnToConnectionMainPage()
        {
            ReturningFromChecksum = false;
            ParentWindow.NavigateTo("connection");
            ParentWindow.SetVisibilityWelcomeAndSettings(true);
            ParentWindow.ChangeConnectMenuItemTarget("connection");
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            Settings.SaveToFile();
        }

        private void btnChecksum_Click(object sender, RoutedEventArgs e)
        {
            ReturningFromChecksum = true;
            ParentWindow.ShowChecksum(Connection.PublicKey, "connecttoexisting");
        }
    }
}
