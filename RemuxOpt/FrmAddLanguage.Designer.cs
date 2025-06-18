namespace RemuxOpt
{
    partial class FrmAddLanguage
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
            label1 = new Label();
            cbLanguages = new ComboBox();
            btnOk = new Button();
            btnCancel = new Button();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 25);
            label1.Name = "label1";
            label1.Size = new Size(367, 15);
            label1.TabIndex = 0;
            label1.Text = "Select a language from the list below to add it to your configuration:";
            // 
            // cbLanguages
            // 
            cbLanguages.FormattingEnabled = true;
            cbLanguages.Location = new Point(12, 58);
            cbLanguages.Name = "cbLanguages";
            cbLanguages.Size = new Size(367, 23);
            cbLanguages.TabIndex = 1;
            // 
            // btnOk
            // 
            btnOk.DialogResult = DialogResult.OK;
            btnOk.Enabled = false;
            btnOk.Location = new Point(215, 109);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(79, 25);
            btnOk.TabIndex = 2;
            btnOk.Text = "Ok";
            btnOk.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(300, 109);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(79, 25);
            btnCancel.TabIndex = 3;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // FrmAddLanguage
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(400, 147);
            Controls.Add(btnCancel);
            Controls.Add(btnOk);
            Controls.Add(cbLanguages);
            Controls.Add(label1);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Name = "FrmAddLanguage";
            Text = "Available languages";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private ComboBox cbLanguages;
        private Button btnOk;
        private Button btnCancel;
    }
}