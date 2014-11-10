using kkvpn_client.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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
    /// Interaction logic for PageChecksum.xaml
    /// </summary>
    public partial class PageChecksum : Page
    {
        private const int inRow = 4;

        private MainWindow ParentWindow;
        private string ReturnToPage;

        public PageChecksum(MainWindow ParentWindow)
        {
            this.ParentWindow = ParentWindow;
            InitializeComponent();
        }

        public void ShowChecksum(byte[] data, string returnToPage)
        {
            ReturnToPage = returnToPage;

            tbMD5.Text = ByteArrayToString(MD5.Create().ComputeHash(data));
            tbSHA1.Text = ByteArrayToString(SHA1.Create().ComputeHash(data));
            byte[] sha256 = SHA256.Create().ComputeHash(data);
            tbSHA256.Text = ByteArrayToString(sha256);

            string[] words = PGPWordListGenerator.GenerateWordList(sha256);
            for (int i = 0; i < words.Length / inRow; ++i)
            {
                TextBlock tb = ((TextBlock)this.FindName("tbWords" + i.ToString()));
                if (tb != null)
                {
                    tb.Text = "";
                    for (int j = i * inRow; j < (i * inRow) + inRow; ++j)
                    {
                        tb.Text += string.Format("{0,-13} ", words[j]);
                    }
                }
            }
        }

        private string ByteArrayToString(byte[] data)
        {
            StringBuilder result = new StringBuilder("");

            for (int i = 0; i < data.Length; ++i)
            {
                if (i % 4 == 0 && i != 0)
                {
                    result.Append(" ");
                }
                result.Append(data[i].ToString("X2"));
            }

            return result.ToString();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            ParentWindow.NavigateTo(ReturnToPage);
        }
    }
}
