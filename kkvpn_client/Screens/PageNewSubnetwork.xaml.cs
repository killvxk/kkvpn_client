using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
    /// Interaction logic for PageNewSubnetwork.xaml
    /// </summary>
    public partial class PageNewSubnetwork : Page
    {
        private ConnectionManager Connection;
        private MainWindow ParentWindow;

        public PageNewSubnetwork(MainWindow ParentWindow)
        {
            this.ParentWindow = ParentWindow;
            Connection = ((App)Application.Current).Connection;
            InitializeComponent();
        }

        private void tbAddress_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateSubnetworkInfo();
        }

        private void iudCIDR_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            UpdateSubnetworkInfo();
        }

        private void UpdateSubnetworkInfo()
        {
            if (iudCIDR == null || tbAddress == null || lblCount == null
                 || lblMask == null || lblRange == null)
            {
                return;
            }

            uint count = (uint)((1 << (32 - (int)iudCIDR.Value)) - 2);
            uint enteredIP = tbAddress.Text.IPToInt().InvertBytes();
            uint mask = (((uint)iudCIDR.Value).GetMaskFromCIDR()).InvertBytes();

            btnCreate.IsEnabled = enteredIP != 0;
            if (enteredIP != 0)
            {
                lblCount.Text = count.ToString();
                lblMask.Text = (new IPAddress(mask)).ToString();
                lblRange.Text = (new IPAddress((mask & enteredIP) + ((uint)1).InvertBytes())).ToString()
                                + " - " +
                                (new IPAddress((mask & enteredIP) + count.InvertBytes())).ToString();
            }
            else
            {
                lblCount.Text = "-";
                lblRange.Text = "-";
                lblMask.Text = "-";
            }
        }

        private async void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ParentWindow.NavigateTo("wait");
                ParentWindow.ShowMenu(false);

                await Connection.CreateNewNetwork(
                    tbNetworkName.Text,
                    tbPeerName.Text,
                    tbAddress.Text.IPToInt(),
                    (uint)iudCIDR.Value);

                ParentWindow.ShowMenu(true);
                ParentWindow.NavigateTo("status");
                ParentWindow.SetConnected(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Otwarcie nowej sieci niemożliwe: " + ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            ParentWindow.NavigateTo("connection");
            ParentWindow.SetVisibilityWelcomeAndSettings(true);
            ParentWindow.ChangeConnectMenuItemTarget("connection");
        }
    }
}
