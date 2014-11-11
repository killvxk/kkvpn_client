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
using Hardcodet.Wpf.TaskbarNotification;
using kkvpn_client.Screens;

namespace kkvpn_client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ConnectionManager Connection;
        private Dictionary<string, Page> pages;
        private bool Connected;

        public MainWindow()
        {
            pages = new Dictionary<string, Page>() 
            { 
                {"welcome", new PageWelcome(this)},
                {"connection", new PageConnection(this)},
                {"settings", new PageSettings(this)},
                {"newsubnetwork", new PageNewSubnetwork(this)},
                {"connecttoexisting", new PageConnectToExisting(this)},
                {"status", new PageStatus(this)},
                {"addpeer", new PageAddPeer(this)},
                {"peers", new PagePeers(this)},
                {"disconnect", new PageDisconnect(this)},
                {"wait", new PageWait(this)},
                {"help", new PageHelp1(this)},
                {"help2", new PageHelp2(this)},
                {"help3", new PageHelp3(this)},
                {"help4", new PageHelp4(this)},
                {"addingpeerfailed", new PageAddingPeerFailed(this)},
                {"checksum", new PageChecksum(this)},
                {"log", new PageLog(this)}
            };
            Connected = false;

            Connection = ((App)Application.Current).Connection;

            InitializeComponent();
        }

        public void NavigateTo(string PageName)
        {
            this.frMain.Content = pages[PageName];
        }

        public void SetConnected(bool Connected)
        {
            this.Connected = Connected;
            SwitchMenus(Connected);
        }

        public void ChangeConnectMenuItemTarget(string NavigateTo)
        {
            btnConnect.Tag = NavigateTo;
        }

        public void ChangeAddPeerMenuItemTarget(string NavigateTo)
        {
            btnAddPeer.Tag = NavigateTo;
        }

        public void SetVisibilityWelcomeAndSettings(bool Visible)
        {
            if (Visible)
            {
                btnWelcome.Visibility = Visibility.Visible;
                btnSettings.Visibility = Visibility.Visible;
                btnHelp.Visibility = Visibility.Visible;
            }
            else
            {
                btnWelcome.Visibility = Visibility.Collapsed;
                btnSettings.Visibility = Visibility.Collapsed;
                btnHelp.Visibility = Visibility.Collapsed;
            }
        }

        public void ShowMenu(bool Show)
        {
            Visibility visibility = (Show) ? Visibility.Visible : Visibility.Collapsed;

            btnWelcome.Visibility = visibility;
            btnConnect.Visibility = visibility;
            btnSettings.Visibility = visibility;
            btnHelp.Visibility = visibility;

            btnStatus.Visibility = visibility;
            btnPeers.Visibility = visibility;
            btnAddPeer.Visibility = visibility;
            btnLog.Visibility = visibility;
            btnDisconnect.Visibility = visibility;
        }

        public void ShowChecksum(byte[] data, string returnToPage)
        {
            ((PageChecksum)pages["checksum"]).ShowChecksum(data, returnToPage);
            NavigateTo("checksum");
        }

        private void SwitchMenus(bool Connected)
        {
            Visibility VisibilityMain = (Connected)? Visibility.Collapsed: Visibility.Visible;
            Visibility VisibilityConnection = (!Connected)? Visibility.Collapsed: Visibility.Visible;

            btnWelcome.Visibility = VisibilityMain;
            btnConnect.Visibility = VisibilityMain;
            btnSettings.Visibility = VisibilityMain;
            btnHelp.Visibility = VisibilityMain;

            btnStatus.Visibility = VisibilityConnection;
            btnPeers.Visibility = VisibilityConnection;
            btnAddPeer.Visibility = VisibilityConnection;
            btnLog.Visibility = VisibilityConnection;
            btnDisconnect.Visibility = VisibilityConnection;

            if (Connected)
            {
                btnStatus.IsChecked = true;
            }
            else
            {
                btnWelcome.IsChecked = true;
            }
        }

        private void AdjustWindowSize()
        {
            if (this.WindowState == WindowState.Maximized)
            {
                SystemCommands.RestoreWindow(this);
            }
            else
            {
                SystemCommands.MaximizeWindow(this);
            }
        }

        private void spTitleBarRight_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount == 2)
                {
                    AdjustWindowSize();
                }
                else
                {
                    this.DragMove();
                }
            }
        }

        private void btnMenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (frMain != null)
            {
                this.NavigateTo((sender as Control).Tag as string);
            }
        }

        private void wMain_Loaded(object sender, RoutedEventArgs e)
        {
            this.SetInitialPosition(); 
            ((App)Application.Current).Connection.OnConnected += Connection_OnConnected;
            ((App)Application.Current).Connection.OnDisconnected += Connection_OnDisconnected;
            ((App)Application.Current).OnReceivedURIData += MainWindow_OnReceivedURIData;

            this.NavigateTo("welcome");
        }

        private void Connection_OnConnected(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (Connection.Connected)
                {
                    ShowMenu(true);
                    NavigateTo("status");
                    SetConnected(true);
                }
            });
        }

        private void Connection_OnDisconnected(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                ShowMenu(true);
                NavigateTo("welcome");
                SetConnected(false);
                ChangeConnectMenuItemTarget("connection");
            });
        }

        private void MainWindow_OnReceivedURIData(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.Activate();
                if (Connection.Connected)
                {
                    ((PageAddPeer)pages["addpeer"]).SetConnectionString(
                        ((ReceivedURIDataEventArgs)e).Args[1]
                        );
                    NavigateTo("addpeer");
                    btnAddPeer.IsChecked = true;
                }
                else
                {
                    MessageBox.Show("Aby dodać użytkownika musisz być połączony do istniejącej podsieci!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            });
        }

        private void Connection_ExternalConnected(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() => 
            { 
                ShowMenu(true);
                NavigateTo("status");
                SetConnected(true);
            });
        }

        private void SetInitialPosition()
        {
            var workingArea = System.Windows.SystemParameters.WorkArea;
            this.Left = workingArea.Right - this.Width - 4;
            this.Top = workingArea.Bottom - this.Height - 4;
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void wMain_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.Connected
                || frMain.Content == pages["connecttoexisting"]
                || frMain.Content == pages["checksum"])
            {
                e.Cancel = true;
                SystemCommands.MinimizeWindow(this);
            }
        }
    }
}
