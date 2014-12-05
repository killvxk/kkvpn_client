using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
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
    /// Interaction logic for PageStatus.xaml
    /// </summary>
    public partial class PageStatus : Page
    {
        private const int RefreshTime = 1000;

        private ConnectionManager Connection;
        private MainWindow ParentWindow;

        private Timer StatsRefreshTimer;

        public PageStatus(MainWindow ParentWindow)
        {
            this.ParentWindow = ParentWindow;
            Connection = ((App)Application.Current).Connection;
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Dictionary<string, string> info = Connection.GetSubnetworkInfo();
            if (info != null)
            {
                lblNetworkName.Text = info["NetworkName"];
                lblSubnet.Text = info["Subnetwork"];
                lblMask.Text = info["Mask"];
                lblIP.Text = info["IP"];
            }

            if (StatsRefreshTimer != null)
            {
                StatsRefreshTimer.Stop();
            }
            StatsRefreshTimer = new Timer(RefreshTime);
            StatsRefreshTimer.Elapsed += StatsRefreshTimer_Elapsed;
            StatsRefreshTimer.Start();
        }

        void StatsRefreshTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!Connection.Connected)
            {
                StatsRefreshTimer.Stop();
            }
            else
            {
                this.Dispatcher.Invoke(() => {
                    Statistics stats = Connection.GetOverallStatistics();

                    lblPeers.Text = Connection.GetPeers().Length.ToString();

                    lblDLSpeed.Text = stats.DLSpeed.ToString("0.00") + " KB/s";
                    lblULSpeed.Text = stats.ULSpeed.ToString("0.00") + " KB/s";

                    lblDLBytes.Text = ((double)stats.DLBytes / 1024.0).ToString("0.00") + " KB";
                    lblDLPackets.Text = stats.DLPackets.ToString();
                    lblULBytes.Text = ((double)stats.ULBytes / 1024.0).ToString("0.00") + " KB";
                    lblULPackets.Text = stats.ULPackets.ToString();
                });
            }
        }
    }
}
