﻿namespace RemuxOpt
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
            lbInfo = new Label();
            btnOk = new Button();
            btnCancel = new Button();
            SuspendLayout();
            // 
            // lbInfo
            // 
            lbInfo.Location = new Point(12, 25);
            lbInfo.Name = "lbInfo";
            lbInfo.Size = new Size(367, 32);
            lbInfo.TabIndex = 0;
            lbInfo.Text = "info placeholder";
            // 
            // btnOk
            // 
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
            Controls.Add(lbInfo);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Name = "FrmAddLanguage";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Available languages";
            ResumeLayout(false);
        }

        #endregion

        private Label lbInfo;
        private Button btnOk;
        private Button btnCancel;
    }
}