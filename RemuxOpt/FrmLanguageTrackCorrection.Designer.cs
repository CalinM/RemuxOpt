namespace RemuxOpt
{
    partial class FrmLanguageTrackCorrection
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
            lbInfo = new Label();
            SuspendLayout();
            // 
            // lbInfo
            // 
            lbInfo.AutoSize = true;
            lbInfo.Location = new Point(12, 22);
            lbInfo.Name = "lbInfo";
            lbInfo.Size = new Size(389, 15);
            lbInfo.TabIndex = 0;
            lbInfo.Text = "Update the language code on field X for the loaded data (y files selected)";
            // 
            // FrmLanguageTrackCorrection
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(lbInfo);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Name = "FrmLanguageTrackCorrection";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Language track correction";
            Load += FrmLanguageTrackCorrection_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lbInfo;
    }
}