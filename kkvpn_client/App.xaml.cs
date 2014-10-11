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

namespace kkvpn_client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    partial class App : Application
    {
        private const string logfilename = "C:\\Users\\WDKRemoteUser\\Desktop\\log.txt";

        internal ConnectionManager Connection;
        internal AppSettings Settings;
        internal UPnPPortMapper UPnP;

        private bool InitFail = false;
        
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Application.Current.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(Application_ThreadException); // non-UI
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException); // UI

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

                UPnP = new UPnPPortMapper();
                Connection.InitializeManager();
            }
            catch (Exception ex)
            {
                if (ex is InvalidOperationException || ex is Win32ErrorException)
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
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogException((e.ExceptionObject as Exception));
        }

        private void Application_ThreadException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogException(e.Exception);
        }

        static public void LogMsg(string msg)
        {
            StreamWriter logfile;

            if (!File.Exists(logfilename))
                logfile = new StreamWriter(logfilename);
            else
                logfile = File.AppendText(logfilename);

            logfile.WriteLine(DateTime.Now + ": " + msg + "\n");
            logfile.Close();
        }

        static public void LogException(Exception ex)
        {
            StreamWriter logfile;

            if (!File.Exists(logfilename))
                logfile = new StreamWriter(logfilename);
            else
                logfile = File.AppendText(logfilename);

            logfile.WriteLine(DateTime.Now + ": " + ex.Message + Environment.NewLine + "StackTrace: " + ex.StackTrace);
            Exception e = ex.InnerException;
            while (e != null)
            {
                logfile.WriteLine("  InnerException: " + ex.Message + Environment.NewLine + "StackTrace: " + ex.StackTrace);
                e = e.InnerException;
            }
            logfile.Close();
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
}
