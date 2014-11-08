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
            //tbKeyFilePath.Text = Settings.KeyFile;
            //CheckKeyFile();
        }

        //private void CheckKeyFile()
        //{
        //    if (File.Exists(Settings.KeyFile))
        //    {
        //        imPresent.Visibility = Visibility.Visible;
        //        imNotPresent.Visibility = Visibility.Collapsed;
        //        lblKeyPresent.Text = "Klucz jest obecny.";
        //    }
        //    else
        //    {
        //        imPresent.Visibility = Visibility.Collapsed;
        //        imNotPresent.Visibility = Visibility.Visible;
        //        lblKeyPresent.Text = "Klucz nie jest obecny. Proszę wybrać plik lub wygenerować nowy klucz.";
        //    }
        //}

        private void tbKeyFilePath_TextChanged(object sender, TextChangedEventArgs e)
        {
            //Settings.KeyFile = tbKeyFilePath.Text;
            //CheckKeyFile();
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.DefaultExt = ".xml";

            if (dialog.ShowDialog()?? false)
            {
                tbKeyFilePath.Text = dialog.FileName;
                //Settings.KeyFile = tbKeyFilePath.Text;
                //CheckKeyFile();
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            Settings.SaveToFile();
        }
    }
}
