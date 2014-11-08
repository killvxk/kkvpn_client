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
                {"welcome", new Screens.PageWelcome(this)},
                {"connection", new Screens.PageConnection(this)},
                {"settings", new Screens.PageSettings(this)},
                {"newsubnetwork", new Screens.PageNewSubnetwork(this)},
                {"connecttoexisting", new Screens.PageConnectToExisting(this)},
                {"status", new Screens.PageStatus(this)},
                {"addpeer", new Screens.PageAddPeer(this)},
                {"peers", new Screens.PagePeers(this)},
                {"disconnect", new Screens.PageDisconnect(this)},
                {"wait", new Screens.PageWait(this)},
                {"help", new Screens.PageHelp1(this)},
                {"help2", new Screens.PageHelp2(this)},
                {"help3", new Screens.PageHelp3(this)},
                {"help4", new Screens.PageHelp4(this)},
                {"addingpeerfailed", new Screens.PageAddingPeerFailed(this)}
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
            btnDisconnect.Visibility = visibility;
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

            this.NavigateTo("welcome");
        }

        void Connection_OnConnected(object sender, EventArgs e)
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

        private void SetInitialPosition()
        {
            var workingArea = System.Windows.SystemParameters.WorkArea;
            this.Left = workingArea.Right - this.Width - 4;
            this.Top = workingArea.Bottom - this.Height - 4;
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

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            if (this.Connected)
            {
                SystemCommands.MinimizeWindow(this);
            }
            else
            {
                this.Close();
            }
        }
    }
}
