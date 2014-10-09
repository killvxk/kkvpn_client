using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;

namespace kkvpn_client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public ConnectionManager Connection;
        
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Connection = new ConnectionManager();
            try
            {
                Connection.InitializeManager();
            }
            catch
            {
                MessageBox.Show(
                    "Nie udało się połączyć ze sterownikiem VPN! Proszę sprawdzić, czy został on prawidłowo zainstalowany!", 
                    "Błąd!",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                    );
                //this.Shutdown(1);
            }
            //tb = (TaskbarIcon)FindResource("niNotifyIcon");
        }
    }
}
