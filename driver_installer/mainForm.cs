using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace driver_installer
{
    public partial class mainForm : Form
    {
        public mainForm()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                if (args[1] == "-off")
                {
                    Operations.ChangeBCD(false);
                    Operations.DeleteService();
                }
            }
            else
            {
                InitializeComponent();
            }

            pbar.Maximum = 4;
        }

        private async void btnYes_Click(object sender, EventArgs e)
        {
            try
            {
                btnYes.Enabled = false;
                btnNo.Enabled = false;

                await Operations.InstallCert();
                MarkDone(0);

                if (!await Operations.CopyDriverFiles())
                    Error(1);
                else
                    MarkDone(1);

                if (!await Operations.InstallDriver())
                    Error(2);
                else
                    MarkDone(2);

                if (!await Operations.ChangeBCD(true))
                    Error(3);
                else
                    MarkDone(3);

                if (MessageBox.Show("Instalacja sterownika zakończona sukcesem. Czy chcesz zrestartować komputer?", "Sukces", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo("shutdown.exe", "-r -t 0");
                    startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;

                    Process.Start(startInfo);
                }

                Close();
            }
            catch (Exception ex)
            {
                Operations.Log("Wyjątek podczas instalacji: " + ex.Message + " : " + ex.StackTrace);
                MessageBox.Show("Nieoczekiwany błąd podczas instalacji sterownika! Proszę sprawdzić plik DrvierInstallLog.txt", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        }

        private void MarkDone(int step)
        {
            lbList.Items[step] += " - Wykonano";
            pbar.PerformStep();
        }

        private void Error(int step)
        {
            MessageBox.Show("Nieoczekiwany błąd podczas instalacji sterownika! Proszę sprawdzić plik DrvierInstallLog.txt", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            lbList.Items[step] += " - Błąd";

            btnNo.Text = "Wyjście";
            btnNo.Enabled = true;
        }

        private void btnNo_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
