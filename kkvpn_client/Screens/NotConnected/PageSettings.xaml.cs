using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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
    /// Interaction logic for PageSettings.xaml
    /// </summary>
    public partial class PageSettings : Page
    {
        private MainWindow ParentWindow;
        private AppSettings Settings;

        public PageSettings(MainWindow ParentWindow)
        {
            this.ParentWindow = ParentWindow;
            this.Settings = ((App)Application.Current).Settings;
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            iudSupport.Value = Settings.UdpSupport;
            iudTransmission.Value = Settings.UdpTransmission;
            cbRandom.IsChecked = Settings.UdpUseRandomPorts;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            Settings.UdpSupport = iudSupport.Value ?? 57384;
            Settings.UdpTransmission = iudTransmission.Value ?? 57394;
            Settings.UdpUseRandomPorts = cbRandom.IsChecked ?? false;

            ((App)Application.Current).Connection.SetPortNumbers(Settings.UdpUseRandomPorts, Settings.UdpSupport, Settings.UdpTransmission);
            Settings.SaveToFile();
        }

        private void cbRandom_Checked(object sender, RoutedEventArgs e)
        {
            iudSupport.IsEnabled = !(cbRandom.IsChecked ?? false);
            iudTransmission.IsEnabled = !(cbRandom.IsChecked ?? false);
        }
    }
}
