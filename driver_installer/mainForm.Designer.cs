namespace driver_installer
{
    partial class mainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnYes = new System.Windows.Forms.Button();
            this.btnNo = new System.Windows.Forms.Button();
            this.rtbDesc = new System.Windows.Forms.RichTextBox();
            this.lbList = new System.Windows.Forms.ListBox();
            this.pbar = new System.Windows.Forms.ProgressBar();
            this.SuspendLayout();
            // 
            // btnYes
            // 
            this.btnYes.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnYes.Location = new System.Drawing.Point(158, 232);
            this.btnYes.Name = "btnYes";
            this.btnYes.Size = new System.Drawing.Size(75, 23);
            this.btnYes.TabIndex = 7;
            this.btnYes.Text = "Tak";
            this.btnYes.UseVisualStyleBackColor = true;
            this.btnYes.Click += new System.EventHandler(this.btnYes_Click);
            // 
            // btnNo
            // 
            this.btnNo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnNo.Location = new System.Drawing.Point(239, 232);
            this.btnNo.Name = "btnNo";
            this.btnNo.Size = new System.Drawing.Size(75, 23);
            this.btnNo.TabIndex = 6;
            this.btnNo.Text = "Nie";
            this.btnNo.UseVisualStyleBackColor = true;
            this.btnNo.Click += new System.EventHandler(this.btnNo_Click);
            // 
            // rtbDesc
            // 
            this.rtbDesc.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.rtbDesc.Location = new System.Drawing.Point(12, 12);
            this.rtbDesc.Name = "rtbDesc";
            this.rtbDesc.ReadOnly = true;
            this.rtbDesc.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
            this.rtbDesc.Size = new System.Drawing.Size(308, 65);
            this.rtbDesc.TabIndex = 5;
            this.rtbDesc.Text = "Instalacja kkVPN wymaga uruchomienia trybu \"testsigning\", \nktóry pozwala na insta" +
    "lację sterowników autocertyfikowanych.\nWymagane będzie ponowne uruchomienie komp" +
    "utera.\nCzy chcesz kontynuować?";
            // 
            // lbList
            // 
            this.lbList.FormattingEnabled = true;
            this.lbList.Items.AddRange(new object[] {
            "Instalacja certyfikatu",
            "Kopiowanie sterownika",
            "Tworzenie usługi sterownika",
            "Uruchamianie trybu TESTSIGNING"});
            this.lbList.Location = new System.Drawing.Point(12, 75);
            this.lbList.Name = "lbList";
            this.lbList.Size = new System.Drawing.Size(302, 121);
            this.lbList.TabIndex = 8;
            // 
            // pbar
            // 
            this.pbar.Location = new System.Drawing.Point(13, 202);
            this.pbar.Name = "pbar";
            this.pbar.Size = new System.Drawing.Size(301, 19);
            this.pbar.TabIndex = 9;
            // 
            // mainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(326, 267);
            this.Controls.Add(this.pbar);
            this.Controls.Add(this.lbList);
            this.Controls.Add(this.btnYes);
            this.Controls.Add(this.btnNo);
            this.Controls.Add(this.rtbDesc);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "mainForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "Instalacja sterownika kkVPN";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnYes;
        private System.Windows.Forms.Button btnNo;
        private System.Windows.Forms.RichTextBox rtbDesc;
        private System.Windows.Forms.ListBox lbList;
        private System.Windows.Forms.ProgressBar pbar;
    }
}

