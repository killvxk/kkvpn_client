using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace kkvpn_client.Misc
{
    internal class Logger
    {
        public static Logger Instance; 
        public event EventHandler OnNewLogMessage;
        private string LogFileName;

        public Logger(string LogFileName)
        {
            this.LogFileName = LogFileName;

            //Application.Current.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(Application_ThreadException); // non-UI
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException); // UI

            Instance = this;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogException((e.ExceptionObject as Exception));
        }

        private void Application_ThreadException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogException(e.Exception);
        }

        public void LogMsg(string msg)
        {
            Log(DateTime.Now + ": " + msg);
        }

        public void LogError(string msg)
        {
            Log("Błąd: " + DateTime.Now + ": " + msg);
        }

        public void LogException(Exception ex)
        {
            StreamWriter logfile;

            if (!File.Exists(LogFileName))
            {
                logfile = new StreamWriter(LogFileName);
            }
            else
            {
                logfile = File.AppendText(LogFileName);
            }

            using (logfile)
            {
                Exception e = ex;
                int level = 0;
                while (e != null)
                {
                    string logLine = "Wyjątek: " + DateTime.Now + ": " + ex.Message + Environment.NewLine + "StackTrace: " + ex.StackTrace + "\n";
                    if (level > 0)
                    {
                        string indent = "";
                        for (int i = 0; i < level; ++i) indent += "--";

                        logLine = logLine.Replace("\n", indent + ">\n");
                    }

                    logfile.WriteLine(logLine);
                    e = e.InnerException;
                    level++;
                }
                logfile.Close();
            }

            if (OnNewLogMessage != null)
            {
                OnNewLogMessage(this, new NewLogMessageEventArgs(DateTime.Now + ": " + ex.Message));
            }
        }

        private void Log(string msg)
        {
            StreamWriter logfile;

            if (!File.Exists(LogFileName))
            {
                logfile = new StreamWriter(LogFileName);
            }
            else
            {
                logfile = File.AppendText(LogFileName);
            }

            logfile.WriteLine(msg);
            logfile.Close();

            if (OnNewLogMessage != null)
            {
                OnNewLogMessage(this, new NewLogMessageEventArgs(msg));
            }
        }
    }

    internal class NewLogMessageEventArgs : EventArgs
    {
        public string LogMessage;

        public NewLogMessageEventArgs(string LogMessage)
        {
            this.LogMessage = LogMessage;
        }
    }
}
