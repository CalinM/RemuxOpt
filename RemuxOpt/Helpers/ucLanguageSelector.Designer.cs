namespace RemuxOpt
{
    partial class ucLanguageSelector
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            cbLanguages = new ComboBox();
            SuspendLayout();
            // 
            // cbLanguages
            // 
            cbLanguages.FormattingEnabled = true;
            cbLanguages.Location = new Point(0, 0);
            cbLanguages.Name = "cbLanguages";
            cbLanguages.Size = new Size(367, 23);
            cbLanguages.TabIndex = 2;
            // 
            // ucLanguageSelector
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(cbLanguages);
            Name = "ucLanguageSelector";
            Size = new Size(367, 23);
            ResumeLayout(false);
        }

        #endregion

        private ComboBox cbLanguages;
    }
}
