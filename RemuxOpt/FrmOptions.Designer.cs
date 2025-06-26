namespace RemuxOpt
{
    partial class FrmOptions
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmOptions));
            chkReadFilesRecursively = new CheckBox();
            lbInfo1 = new Label();
            chkDeleteOriginal = new CheckBox();
            bCancel = new Button();
            bSave = new Button();
            SuspendLayout();
            // 
            // chkReadFilesRecursively
            // 
            chkReadFilesRecursively.AutoSize = true;
            chkReadFilesRecursively.Location = new Point(12, 12);
            chkReadFilesRecursively.Name = "chkReadFilesRecursively";
            chkReadFilesRecursively.Size = new Size(237, 19);
            chkReadFilesRecursively.TabIndex = 0;
            chkReadFilesRecursively.Text = "Read files from all subfolders recursively";
            chkReadFilesRecursively.UseVisualStyleBackColor = true;
            // 
            // lbInfo1
            // 
            lbInfo1.ForeColor = Color.Gray;
            lbInfo1.Location = new Point(29, 33);
            lbInfo1.Name = "lbInfo1";
            lbInfo1.Size = new Size(718, 81);
            lbInfo1.TabIndex = 1;
            lbInfo1.Text = resources.GetString("lbInfo1.Text");
            // 
            // chkDeleteOriginal
            // 
            chkDeleteOriginal.AutoSize = true;
            chkDeleteOriginal.Location = new Point(12, 117);
            chkDeleteOriginal.Name = "chkDeleteOriginal";
            chkDeleteOriginal.Size = new Size(290, 19);
            chkDeleteOriginal.TabIndex = 2;
            chkDeleteOriginal.Text = "Delete the original file if no errors are encountered";
            chkDeleteOriginal.UseVisualStyleBackColor = true;
            // 
            // bCancel
            // 
            bCancel.DialogResult = DialogResult.Cancel;
            bCancel.Location = new Point(672, 151);
            bCancel.Name = "bCancel";
            bCancel.Size = new Size(80, 25);
            bCancel.TabIndex = 4;
            bCancel.Text = "Cancel";
            bCancel.UseVisualStyleBackColor = true;
            // 
            // bSave
            // 
            bSave.Location = new Point(586, 151);
            bSave.Name = "bSave";
            bSave.Size = new Size(80, 25);
            bSave.TabIndex = 5;
            bSave.Text = "Save";
            bSave.UseVisualStyleBackColor = true;
            // 
            // FrmOptions
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(759, 188);
            Controls.Add(bSave);
            Controls.Add(bCancel);
            Controls.Add(chkDeleteOriginal);
            Controls.Add(lbInfo1);
            Controls.Add(chkReadFilesRecursively);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Name = "FrmOptions";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Options";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private CheckBox chkReadFilesRecursively;
        private Label lbInfo1;
        private CheckBox chkDeleteOriginal;
        private Button bCancel;
        private Button bSave;
    }
}