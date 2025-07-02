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
            chkApplyNamingConventions = new CheckBox();
            label1 = new Label();
            chkRemoveUnlistedLanguageTracks = new CheckBox();
            label2 = new Label();
            bAddCustomRule = new Button();
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
            bCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            bCancel.DialogResult = DialogResult.Cancel;
            bCancel.Location = new Point(667, 322);
            bCancel.Name = "bCancel";
            bCancel.Size = new Size(80, 25);
            bCancel.TabIndex = 4;
            bCancel.Text = "Cancel";
            bCancel.UseVisualStyleBackColor = true;
            // 
            // bSave
            // 
            bSave.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            bSave.Location = new Point(581, 322);
            bSave.Name = "bSave";
            bSave.Size = new Size(80, 25);
            bSave.TabIndex = 5;
            bSave.Text = "Save";
            bSave.UseVisualStyleBackColor = true;
            // 
            // chkApplyNamingConventions
            // 
            chkApplyNamingConventions.AutoSize = true;
            chkApplyNamingConventions.Location = new Point(12, 201);
            chkApplyNamingConventions.Name = "chkApplyNamingConventions";
            chkApplyNamingConventions.Size = new Size(169, 19);
            chkApplyNamingConventions.TabIndex = 6;
            chkApplyNamingConventions.Text = "Apply naming conventions";
            chkApplyNamingConventions.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            label1.ForeColor = Color.Gray;
            label1.Location = new Point(29, 223);
            label1.Name = "label1";
            label1.Size = new Size(718, 19);
            label1.TabIndex = 7;
            label1.Text = "Replace \" !\" with \"!\"";
            // 
            // chkRemoveUnlistedLanguageTracks
            // 
            chkRemoveUnlistedLanguageTracks.AutoSize = true;
            chkRemoveUnlistedLanguageTracks.Location = new Point(12, 139);
            chkRemoveUnlistedLanguageTracks.Name = "chkRemoveUnlistedLanguageTracks";
            chkRemoveUnlistedLanguageTracks.Size = new Size(323, 19);
            chkRemoveUnlistedLanguageTracks.TabIndex = 9;
            chkRemoveUnlistedLanguageTracks.Text = "Remove the tracks in languages that are NOT configured";
            chkRemoveUnlistedLanguageTracks.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            label2.ForeColor = Color.Gray;
            label2.Location = new Point(29, 161);
            label2.Name = "label2";
            label2.Size = new Size(718, 31);
            label2.TabIndex = 10;
            label2.Text = resources.GetString("label2.Text");
            // 
            // bAddCustomRule
            // 
            bAddCustomRule.Enabled = false;
            bAddCustomRule.Location = new Point(29, 245);
            bAddCustomRule.Name = "bAddCustomRule";
            bAddCustomRule.Size = new Size(75, 23);
            bAddCustomRule.TabIndex = 11;
            bAddCustomRule.Text = "Add rule";
            bAddCustomRule.UseVisualStyleBackColor = true;
            // 
            // FrmOptions
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(759, 359);
            Controls.Add(bAddCustomRule);
            Controls.Add(label2);
            Controls.Add(chkRemoveUnlistedLanguageTracks);
            Controls.Add(label1);
            Controls.Add(chkApplyNamingConventions);
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
        private CheckBox chkApplyNamingConventions;
        private Label label1;
        private CheckBox chkRemoveUnlistedLanguageTracks;
        private Label label2;
        private Button bAddCustomRule;
    }
}