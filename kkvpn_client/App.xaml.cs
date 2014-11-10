using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using kkvpn_client.Engine;
using System.Threading;
using System.Windows.Threading;
using Microsoft.Shell;
using kkvpn_client.Misc;

namespace kkvpn_client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    partial class App : Application, ISingleInstanceApp
    {
        private const string LogFileName = "C:\\Users\\WDKRemoteUser\\Desktop\\log.txt";
        private const string Unique = "kkVPN_Unique";

        internal ConnectionManager Connection;
        internal Logger Log;
        internal AppSettings Settings;
        internal UPnPPortMapper UPnP;

        internal event EventHandler OnReceivedURIData; 

        private bool InitFail = false;

        [STAThread]
        public static void Main()
        {
            if (SingleInstance<App>.InitializeAsFirstInstance(Unique))
            {
                var application = new App();
                application.InitializeComponent();
                application.Run();

                SingleInstance<App>.Cleanup();
            }
        }

        public bool SignalExternalCommandLineArgs(IList<string> args)
        {
            if (OnReceivedURIData != null)
            {
                OnReceivedURIData(this, new ReceivedURIDataEventArgs(args));
            }

            return true;
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Log = new Logger(LogFileName);
            Connection = new ConnectionManager();
            try
            {
                XmlSerializer xml = new XmlSerializer(typeof(AppSettings));
                try
                {
                    if (File.Exists(AppSettings.ConfigFile))
                    {
                        using (StreamReader sr = new StreamReader(AppSettings.ConfigFile))
                        {
                            Settings = xml.Deserialize(sr) as AppSettings;
                        }
                    }
                }
                catch (XmlException ex)
                {
                    MessageBox.Show(
                        "Nie udało się odczytać pliku ustawień: " + ex.Message,
                        "Błąd!",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                        );
                }

                if (Settings == null)
                {
                    Settings = new AppSettings(true);
                    Settings.SaveToFile();
                }

                MiscFunctions.SetIESoundsEnabled(false);
                UPnP = new UPnPPortMapper();
                Connection.InitializeManager();
            }
            catch (Exception ex)
            {
                if (ex is Win32ErrorException)
                {
                    MessageBox.Show(
                        "Nie udało się połączyć ze sterownikiem VPN! Proszę sprawdzić, czy został on prawidłowo zainstalowany!", 
                        "Błąd!",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                        );
                    InitFail = true;
                    this.Shutdown(1);
                }
                else
                {
                    throw;
                }
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            if (!InitFail)
            {
                UPnP.Dispose();
                Connection.Dispose();
            }
        }
    }

    public class ReceivedURIDataEventArgs : EventArgs
    {
        public IList<string> Args;

        public ReceivedURIDataEventArgs(IList<string> Args)
        {
            this.Args = Args;
        }
    }
}
