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
    /// Interaction logic for PageLog.xaml
    /// </summary>
    public partial class PageLog : Page
    {
        private MainWindow ParentWindow;

        public PageLog(MainWindow ParentWindow)
        {
            this.ParentWindow = ParentWindow;
            Logger.Instance.OnNewLogMessage += Instance_OnNewLogMessage;

            InitializeComponent();
        }

        void Instance_OnNewLogMessage(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() => {
                lbLog.Items.Add(((NewLogMessageEventArgs)e).LogMessage);
                });
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            lbLog.Items.Clear();
        }
    }
}
