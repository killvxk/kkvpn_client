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
    /// Interaction logic for PageAddPeer.xaml
    /// </summary>
    public partial class PageAddPeer : Page
    {
        private MainWindow ParentWindow;
        private ConnectionManager Connection;

        public PageAddPeer(MainWindow ParentWindow)
        {
            this.ParentWindow = ParentWindow;
            Connection = ((App)Application.Current).Connection;
            InitializeComponent();
        }

        private void btnAddPeer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Connection.AddPeer(tbConnectionString.Text, tbAddress.Text.IPToInt());
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show("Wystąpił błąd podczas dodawania użytkownika!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                App.LogException(ex);
            }
            catch (WrongConnectionStringException ex)
            {
                MessageBox.Show("Błędny łańcuch znaków! Sprawdź czy ciąg znaków jest na pewno poprawny.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                App.LogException(ex);
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            tbAddress.Text = Connection.GetLowestFreeIP();
        }
    }
}
