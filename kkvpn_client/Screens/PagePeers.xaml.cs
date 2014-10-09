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
    /// Interaction logic for PagePeers.xaml
    /// </summary>
    public partial class PagePeers : Page
    {
        private MainWindow ParentWindow;

        public PagePeers(MainWindow ParentWindow)
        {
            this.ParentWindow = ParentWindow;
            InitializeComponent();
        }
    }
}
