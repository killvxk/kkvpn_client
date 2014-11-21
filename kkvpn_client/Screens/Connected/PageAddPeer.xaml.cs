using kkvpn_client.Communication;
using kkvpn_client.Misc;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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
    /// Interaction logic for PageAddPeer.xaml
    /// </summary>
    public partial class PageAddPeer : Page
    {
        private MainWindow ParentWindow;
        private ConnectionManager Connection;

        public PageAddPeer(MainWindow ParentWindow)
        {
            this.ParentWindow = ParentWindow;
            Connection = ((App)Application.Current).Connection;
            InitializeComponent();

            Connection.OnAddedPeer += Connection_OnAddedPeer;
        }

        public void SetConnectionString(string ConnectionString)
        {
            tbConnectionString.Text = ConnectionString;
        }

        private void btnAddPeer_Click(object sender, RoutedEventArgs e)
        {
            SetToWait();
            Connection.AddPeer(tbConnectionString.Text, tbAddress.Text.IPToInt());
        }

        private void Connection_OnAddedPeer(object sender, EventArgs e)
        {
            SetToIdle(e as AddedPeerEventArgs);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            tbAddress.Text = Connection.GetLowestFreeIP();
        }

        private void SetToWait()
        {
            ParentWindow.NavigateTo("wait");
            ParentWindow.ChangeAddPeerMenuItemTarget("wait");
        }

        private void SetToIdle(AddedPeerEventArgs eventArgs)
        {
            if (!Connection.Connected)
            {
                return;
            }
            tbConnectionString.Text = "";

            if (eventArgs.Success)
            {
                ParentWindow.NavigateTo("addpeer");
                ParentWindow.ChangeAddPeerMenuItemTarget("addpeer");
            }
            else
            {
                MessageBox.Show(eventArgs.Exc.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                if (!(eventArgs.Exc is NewClientNotReachedException))
                {
                    Logger.Instance.LogException(eventArgs.Exc);
                }

                ParentWindow.NavigateTo("addingpeerfailed");
                ParentWindow.ChangeAddPeerMenuItemTarget("addingpeerfailed");
            }
        }

        private void btnChecksum_Click(object sender, RoutedEventArgs e)
        {
            string ConnectionString = tbConnectionString.Text;

            if (ConnectionString.IndexOf(':') > -1)
            {
                ConnectionString = ConnectionString.Remove(0, ConnectionString.IndexOf(':') + 1);
            }

            byte[] key = Base64PeerData.ExtractPublicKey(ConnectionString);

            if (key != null)
            {
                ParentWindow.ShowChecksum(key, "addpeer");
            }
            else
            {
                MessageBox.Show("Niepoprawny ciąg znaków! Odkodowanie klucza publicznego nie powiodło się!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
