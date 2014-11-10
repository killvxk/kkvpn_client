using kkvpn_client.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
        private AppSettings Settings;

        public PageNewSubnetwork(MainWindow ParentWindow)
        {
            this.ParentWindow = ParentWindow;
            Connection = ((App)Application.Current).Connection;
            Settings = ((App)Application.Current).Settings;
            InitializeComponent();
        }

        private void tbAddress_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.SubnetworkAddress = tbAddress.Text;
            UpdateSubnetworkInfo();
        }

        private void iudCIDR_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Settings.SubnetworkCIDR = iudCIDR.Value?? 24;
            UpdateSubnetworkInfo();
        }

        private void UpdateSubnetworkInfo()
        {
            if (iudCIDR == null || tbAddress == null || lblCount == null
                 || lblMask == null || lblRange == null)
            {
                return;
            }

            uint count = (uint)((1 << (32 - (int)(iudCIDR.Value?? 24))) - 2);
            uint enteredIP = tbAddress.Text.IPToInt().InvertBytes();
            uint mask = (((uint)(iudCIDR.Value?? 24)).GetMaskFromCIDR()).InvertBytes();

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
                ParentWindow.ChangeAddPeerMenuItemTarget("addpeer");
                ParentWindow.ShowMenu(false);

                await Connection.CreateNewNetwork(
                    tbNetworkName.Text,
                    tbPeerName.Text,
                    tbAddress.Text.IPToInt(),
                    (uint)iudCIDR.Value);
            }
            catch (OperationCanceledException)
            {
                Connection.Disconnect();
            }
            catch (SocketException ex)
            {
                MessageBox.Show(
                    "Błąd połączenia: " + ex.Message,
                    "Błąd",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                    );
                Connection.Disconnect();
            }
            catch (Exception ex)
            {
                Connection.Disconnect();
                MessageBox.Show("Otwarcie nowej sieci niemożliwe: " + ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                Logger.Instance.LogException(ex);

                ParentWindow.ShowMenu(true);
                ParentWindow.NavigateTo("connection");
                ParentWindow.SetConnected(false);
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            ParentWindow.NavigateTo("connection");
            ParentWindow.SetVisibilityWelcomeAndSettings(true);
            ParentWindow.ChangeConnectMenuItemTarget("connection");
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            tbNetworkName.Text = Settings.SubnetworkName;
            tbPeerName.Text = Settings.UserName;
            tbAddress.Text = Settings.SubnetworkAddress;
            iudCIDR.Value = Settings.SubnetworkCIDR;
        }

        private void tbNetworkName_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.SubnetworkName = tbNetworkName.Text;
        }

        private void tbPeerName_TextChanged(object sender, TextChangedEventArgs e)
        {
            Settings.UserName = tbPeerName.Text;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            Settings.SaveToFile();
        }
    }
}
