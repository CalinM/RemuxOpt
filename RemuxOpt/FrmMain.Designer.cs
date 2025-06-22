namespace RemuxOpt
{
    partial class FrmMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            pParameters = new Panel();
            btbOutputPath = new ButtonTextBox();
            lbOutputFolder = new Label();
            lbMediaInfoCliVersion = new Label();
            chkRemoveFileTitle = new CheckBox();
            bRemux = new Button();
            lbFFprobeVersion = new Label();
            lbMkvVersion = new Label();
            chkRemoveAttachments = new CheckBox();
            chkRemoveForced = new CheckBox();
            chkAutoTitleForAudioTrack = new CheckBox();
            chkPreserveSubtitlesTrackTitles = new CheckBox();
            btnAddSubtitleLanguage = new LinkLabel();
            lvSubtitleTracks = new ListView();
            lbSubtitleTracks = new Label();
            lbAddAudioLanguage = new LinkLabel();
            lvAudioTracks = new ListView();
            lbAudioTracks = new Label();
            chkEmptyVideoTitle = new CheckBox();
            tbVideoTitle = new TextBox();
            lbVideoTitle = new Label();
            pFiles = new Panel();
            pProgress = new Panel();
            btnCancelWorker = new Button();
            progressLabel = new Label();
            progressBar = new ProgressBar();
            tcGrid = new TabControl();
            tpGrid = new TabPage();
            lbDragFolderHere = new Label();
            tpTextView = new TabPage();
            txtFilesDetails = new TextBox();
            tpOutput = new TabPage();
            tbOutput = new TextBox();
            pParameters.SuspendLayout();
            pFiles.SuspendLayout();
            pProgress.SuspendLayout();
            tcGrid.SuspendLayout();
            tpGrid.SuspendLayout();
            tpTextView.SuspendLayout();
            tpOutput.SuspendLayout();
            SuspendLayout();
            // 
            // pParameters
            // 
            pParameters.Controls.Add(btbOutputPath);
            pParameters.Controls.Add(lbOutputFolder);
            pParameters.Controls.Add(lbMediaInfoCliVersion);
            pParameters.Controls.Add(chkRemoveFileTitle);
            pParameters.Controls.Add(bRemux);
            pParameters.Controls.Add(lbFFprobeVersion);
            pParameters.Controls.Add(lbMkvVersion);
            pParameters.Controls.Add(chkRemoveAttachments);
            pParameters.Controls.Add(chkRemoveForced);
            pParameters.Controls.Add(chkAutoTitleForAudioTrack);
            pParameters.Controls.Add(chkPreserveSubtitlesTrackTitles);
            pParameters.Controls.Add(btnAddSubtitleLanguage);
            pParameters.Controls.Add(lvSubtitleTracks);
            pParameters.Controls.Add(lbSubtitleTracks);
            pParameters.Controls.Add(lbAddAudioLanguage);
            pParameters.Controls.Add(lvAudioTracks);
            pParameters.Controls.Add(lbAudioTracks);
            pParameters.Controls.Add(chkEmptyVideoTitle);
            pParameters.Controls.Add(tbVideoTitle);
            pParameters.Controls.Add(lbVideoTitle);
            pParameters.Dock = DockStyle.Left;
            pParameters.Location = new Point(0, 0);
            pParameters.Name = "pParameters";
            pParameters.Size = new Size(440, 761);
            pParameters.TabIndex = 0;
            // 
            // btbOutputPath
            // 
            btbOutputPath.Location = new Point(12, 575);
            btbOutputPath.Name = "btbOutputPath";
            btbOutputPath.Size = new Size(414, 23);
            btbOutputPath.TabIndex = 51;
            // 
            // lbOutputFolder
            // 
            lbOutputFolder.AutoSize = true;
            lbOutputFolder.Location = new Point(12, 557);
            lbOutputFolder.Name = "lbOutputFolder";
            lbOutputFolder.Size = new Size(82, 15);
            lbOutputFolder.TabIndex = 49;
            lbOutputFolder.Text = "Output folder:";
            // 
            // lbMediaInfoCliVersion
            // 
            lbMediaInfoCliVersion.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lbMediaInfoCliVersion.AutoEllipsis = true;
            lbMediaInfoCliVersion.Location = new Point(12, 742);
            lbMediaInfoCliVersion.Name = "lbMediaInfoCliVersion";
            lbMediaInfoCliVersion.Size = new Size(414, 15);
            lbMediaInfoCliVersion.TabIndex = 48;
            lbMediaInfoCliVersion.Text = "check media info cli";
            // 
            // chkRemoveFileTitle
            // 
            chkRemoveFileTitle.AutoSize = true;
            chkRemoveFileTitle.Checked = true;
            chkRemoveFileTitle.CheckState = CheckState.Checked;
            chkRemoveFileTitle.Location = new Point(16, 657);
            chkRemoveFileTitle.Name = "chkRemoveFileTitle";
            chkRemoveFileTitle.Size = new Size(172, 19);
            chkRemoveFileTitle.TabIndex = 47;
            chkRemoveFileTitle.Text = "Remove container (file) title";
            chkRemoveFileTitle.UseVisualStyleBackColor = true;
            // 
            // bRemux
            // 
            bRemux.Location = new Point(332, 669);
            bRemux.Name = "bRemux";
            bRemux.Size = new Size(95, 23);
            bRemux.TabIndex = 46;
            bRemux.Text = "Remux";
            bRemux.UseVisualStyleBackColor = true;
            // 
            // lbFFprobeVersion
            // 
            lbFFprobeVersion.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lbFFprobeVersion.AutoEllipsis = true;
            lbFFprobeVersion.Location = new Point(12, 726);
            lbFFprobeVersion.Name = "lbFFprobeVersion";
            lbFFprobeVersion.Size = new Size(414, 15);
            lbFFprobeVersion.TabIndex = 45;
            lbFFprobeVersion.Text = "check ffprobe";
            // 
            // lbMkvVersion
            // 
            lbMkvVersion.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lbMkvVersion.Location = new Point(12, 710);
            lbMkvVersion.Name = "lbMkvVersion";
            lbMkvVersion.Size = new Size(414, 15);
            lbMkvVersion.TabIndex = 44;
            lbMkvVersion.Text = "check mkv";
            // 
            // chkRemoveAttachments
            // 
            chkRemoveAttachments.AutoSize = true;
            chkRemoveAttachments.Checked = true;
            chkRemoveAttachments.CheckState = CheckState.Checked;
            chkRemoveAttachments.Location = new Point(16, 636);
            chkRemoveAttachments.Name = "chkRemoveAttachments";
            chkRemoveAttachments.Size = new Size(138, 19);
            chkRemoveAttachments.TabIndex = 43;
            chkRemoveAttachments.Text = "Remove attachments";
            chkRemoveAttachments.UseVisualStyleBackColor = true;
            // 
            // chkRemoveForced
            // 
            chkRemoveForced.AutoSize = true;
            chkRemoveForced.Checked = true;
            chkRemoveForced.CheckState = CheckState.Checked;
            chkRemoveForced.Location = new Point(16, 615);
            chkRemoveForced.Name = "chkRemoveForced";
            chkRemoveForced.Size = new Size(232, 19);
            chkRemoveForced.TabIndex = 42;
            chkRemoveForced.Text = "Remove forced attribute from all tracks";
            chkRemoveForced.UseVisualStyleBackColor = true;
            // 
            // chkAutoTitleForAudioTrack
            // 
            chkAutoTitleForAudioTrack.AutoSize = true;
            chkAutoTitleForAudioTrack.Location = new Point(349, 278);
            chkAutoTitleForAudioTrack.Name = "chkAutoTitleForAudioTrack";
            chkAutoTitleForAudioTrack.Size = new Size(77, 19);
            chkAutoTitleForAudioTrack.TabIndex = 41;
            chkAutoTitleForAudioTrack.Text = "Auto-title";
            chkAutoTitleForAudioTrack.UseVisualStyleBackColor = true;
            // 
            // chkPreserveSubtitlesTrackTitles
            // 
            chkPreserveSubtitlesTrackTitles.AutoSize = true;
            chkPreserveSubtitlesTrackTitles.Checked = true;
            chkPreserveSubtitlesTrackTitles.CheckState = CheckState.Checked;
            chkPreserveSubtitlesTrackTitles.Location = new Point(328, 514);
            chkPreserveSubtitlesTrackTitles.Name = "chkPreserveSubtitlesTrackTitles";
            chkPreserveSubtitlesTrackTitles.Size = new Size(98, 19);
            chkPreserveSubtitlesTrackTitles.TabIndex = 40;
            chkPreserveSubtitlesTrackTitles.Text = "Preserve titles";
            chkPreserveSubtitlesTrackTitles.UseVisualStyleBackColor = true;
            // 
            // btnAddSubtitleLanguage
            // 
            btnAddSubtitleLanguage.AutoSize = true;
            btnAddSubtitleLanguage.Location = new Point(12, 514);
            btnAddSubtitleLanguage.Name = "btnAddSubtitleLanguage";
            btnAddSubtitleLanguage.Size = new Size(93, 15);
            btnAddSubtitleLanguage.TabIndex = 39;
            btnAddSubtitleLanguage.TabStop = true;
            btnAddSubtitleLanguage.Text = "Add language ...";
            // 
            // lvSubtitleTracks
            // 
            lvSubtitleTracks.Location = new Point(12, 334);
            lvSubtitleTracks.Name = "lvSubtitleTracks";
            lvSubtitleTracks.Size = new Size(415, 175);
            lvSubtitleTracks.TabIndex = 38;
            lvSubtitleTracks.UseCompatibleStateImageBehavior = false;
            lvSubtitleTracks.View = View.Details;
            // 
            // lbSubtitleTracks
            // 
            lbSubtitleTracks.AutoSize = true;
            lbSubtitleTracks.Location = new Point(12, 316);
            lbSubtitleTracks.Name = "lbSubtitleTracks";
            lbSubtitleTracks.Size = new Size(243, 15);
            lbSubtitleTracks.TabIndex = 37;
            lbSubtitleTracks.Text = "Keep subtitle tracks (drag && drop to reorder):";
            // 
            // lbAddAudioLanguage
            // 
            lbAddAudioLanguage.AutoSize = true;
            lbAddAudioLanguage.Location = new Point(12, 279);
            lbAddAudioLanguage.Name = "lbAddAudioLanguage";
            lbAddAudioLanguage.Size = new Size(93, 15);
            lbAddAudioLanguage.TabIndex = 36;
            lbAddAudioLanguage.TabStop = true;
            lbAddAudioLanguage.Text = "Add language ...";
            // 
            // lvAudioTracks
            // 
            lvAudioTracks.Location = new Point(12, 99);
            lvAudioTracks.Name = "lvAudioTracks";
            lvAudioTracks.Size = new Size(415, 175);
            lvAudioTracks.TabIndex = 35;
            lvAudioTracks.UseCompatibleStateImageBehavior = false;
            lvAudioTracks.View = View.Details;
            // 
            // lbAudioTracks
            // 
            lbAudioTracks.AutoSize = true;
            lbAudioTracks.Location = new Point(12, 81);
            lbAudioTracks.Name = "lbAudioTracks";
            lbAudioTracks.Size = new Size(234, 15);
            lbAudioTracks.TabIndex = 34;
            lbAudioTracks.Text = "Keep audio tracks (drag && drop to reorder):";
            // 
            // chkEmptyVideoTitle
            // 
            chkEmptyVideoTitle.AutoSize = true;
            chkEmptyVideoTitle.Location = new Point(352, 15);
            chkEmptyVideoTitle.Name = "chkEmptyVideoTitle";
            chkEmptyVideoTitle.Size = new Size(74, 19);
            chkEmptyVideoTitle.TabIndex = 33;
            chkEmptyVideoTitle.Text = "Set blank";
            chkEmptyVideoTitle.UseVisualStyleBackColor = true;
            // 
            // tbVideoTitle
            // 
            tbVideoTitle.Location = new Point(12, 36);
            tbVideoTitle.Name = "tbVideoTitle";
            tbVideoTitle.Size = new Size(415, 23);
            tbVideoTitle.TabIndex = 32;
            // 
            // lbVideoTitle
            // 
            lbVideoTitle.AutoSize = true;
            lbVideoTitle.Location = new Point(12, 16);
            lbVideoTitle.Name = "lbVideoTitle";
            lbVideoTitle.Size = new Size(92, 15);
            lbVideoTitle.TabIndex = 31;
            lbVideoTitle.Text = "Video track title:";
            // 
            // pFiles
            // 
            pFiles.AllowDrop = true;
            pFiles.Controls.Add(pProgress);
            pFiles.Controls.Add(tcGrid);
            pFiles.Dock = DockStyle.Fill;
            pFiles.Location = new Point(440, 0);
            pFiles.Name = "pFiles";
            pFiles.Size = new Size(844, 761);
            pFiles.TabIndex = 1;
            // 
            // pProgress
            // 
            pProgress.Controls.Add(btnCancelWorker);
            pProgress.Controls.Add(progressLabel);
            pProgress.Controls.Add(progressBar);
            pProgress.Dock = DockStyle.Bottom;
            pProgress.Location = new Point(0, 671);
            pProgress.Name = "pProgress";
            pProgress.Size = new Size(844, 90);
            pProgress.TabIndex = 5;
            pProgress.Visible = false;
            // 
            // btnCancelWorker
            // 
            btnCancelWorker.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCancelWorker.Location = new Point(737, 57);
            btnCancelWorker.Name = "btnCancelWorker";
            btnCancelWorker.Size = new Size(95, 23);
            btnCancelWorker.TabIndex = 6;
            btnCancelWorker.Text = "Cancel";
            btnCancelWorker.UseVisualStyleBackColor = true;
            // 
            // progressLabel
            // 
            progressLabel.AutoSize = true;
            progressLabel.Location = new Point(10, 12);
            progressLabel.Name = "progressLabel";
            progressLabel.Size = new Size(12, 15);
            progressLabel.TabIndex = 5;
            progressLabel.Text = "-";
            // 
            // progressBar
            // 
            progressBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            progressBar.Location = new Point(10, 30);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(822, 17);
            progressBar.TabIndex = 4;
            // 
            // tcGrid
            // 
            tcGrid.Controls.Add(tpGrid);
            tcGrid.Controls.Add(tpTextView);
            tcGrid.Controls.Add(tpOutput);
            tcGrid.Dock = DockStyle.Fill;
            tcGrid.Location = new Point(0, 0);
            tcGrid.Name = "tcGrid";
            tcGrid.SelectedIndex = 0;
            tcGrid.Size = new Size(844, 761);
            tcGrid.TabIndex = 4;
            // 
            // tpGrid
            // 
            tpGrid.Controls.Add(lbDragFolderHere);
            tpGrid.Location = new Point(4, 24);
            tpGrid.Name = "tpGrid";
            tpGrid.Padding = new Padding(3);
            tpGrid.Size = new Size(836, 733);
            tpGrid.TabIndex = 0;
            tpGrid.Text = "Table-view";
            tpGrid.UseVisualStyleBackColor = true;
            // 
            // lbDragFolderHere
            // 
            lbDragFolderHere.BackColor = SystemColors.Window;
            lbDragFolderHere.Dock = DockStyle.Fill;
            lbDragFolderHere.Location = new Point(3, 3);
            lbDragFolderHere.Name = "lbDragFolderHere";
            lbDragFolderHere.Size = new Size(830, 727);
            lbDragFolderHere.TabIndex = 3;
            lbDragFolderHere.Text = "Drag a folder (or files) here ...";
            lbDragFolderHere.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // tpTextView
            // 
            tpTextView.Controls.Add(txtFilesDetails);
            tpTextView.Location = new Point(4, 24);
            tpTextView.Name = "tpTextView";
            tpTextView.Padding = new Padding(3);
            tpTextView.Size = new Size(838, 731);
            tpTextView.TabIndex = 1;
            tpTextView.Text = "Text-view";
            tpTextView.UseVisualStyleBackColor = true;
            // 
            // txtFilesDetails
            // 
            txtFilesDetails.BackColor = SystemColors.Window;
            txtFilesDetails.Dock = DockStyle.Fill;
            txtFilesDetails.Location = new Point(3, 3);
            txtFilesDetails.Multiline = true;
            txtFilesDetails.Name = "txtFilesDetails";
            txtFilesDetails.ReadOnly = true;
            txtFilesDetails.ScrollBars = ScrollBars.Vertical;
            txtFilesDetails.Size = new Size(832, 725);
            txtFilesDetails.TabIndex = 1;
            // 
            // tpOutput
            // 
            tpOutput.Controls.Add(tbOutput);
            tpOutput.Location = new Point(4, 24);
            tpOutput.Name = "tpOutput";
            tpOutput.Padding = new Padding(3);
            tpOutput.Size = new Size(838, 731);
            tpOutput.TabIndex = 2;
            tpOutput.Text = "Output";
            tpOutput.UseVisualStyleBackColor = true;
            // 
            // tbOutput
            // 
            tbOutput.BackColor = SystemColors.Window;
            tbOutput.Dock = DockStyle.Fill;
            tbOutput.Font = new Font("Consolas", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            tbOutput.Location = new Point(3, 3);
            tbOutput.Multiline = true;
            tbOutput.Name = "tbOutput";
            tbOutput.ReadOnly = true;
            tbOutput.ScrollBars = ScrollBars.Vertical;
            tbOutput.Size = new Size(832, 725);
            tbOutput.TabIndex = 2;
            // 
            // FrmMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1284, 761);
            Controls.Add(pFiles);
            Controls.Add(pParameters);
            MinimumSize = new Size(1300, 800);
            Name = "FrmMain";
            Text = "RemuxOpt";
            FormClosing += FrmMain_FormClosing;
            pParameters.ResumeLayout(false);
            pParameters.PerformLayout();
            pFiles.ResumeLayout(false);
            pProgress.ResumeLayout(false);
            pProgress.PerformLayout();
            tcGrid.ResumeLayout(false);
            tpGrid.ResumeLayout(false);
            tpTextView.ResumeLayout(false);
            tpTextView.PerformLayout();
            tpOutput.ResumeLayout(false);
            tpOutput.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Panel pParameters;
        private CheckBox chkRemoveAttachments;
        private CheckBox chkRemoveForced;
        private CheckBox chkAutoTitleForAudioTrack;
        private CheckBox chkPreserveSubtitlesTrackTitles;
        private LinkLabel btnAddSubtitleLanguage;
        private ListView lvSubtitleTracks;
        private Label lbSubtitleTracks;
        private LinkLabel lbAddAudioLanguage;
        private ListView lvAudioTracks;
        private Label lbAudioTracks;
        private CheckBox chkEmptyVideoTitle;
        private TextBox tbVideoTitle;
        private Label lbVideoTitle;
        private Panel pFiles;
        private Label lbMkvVersion;
        private Label lbFFprobeVersion;
        private Panel pProgress;
        private Button btnCancelWorker;
        private Label progressLabel;
        private ProgressBar progressBar;
        private TabControl tcGrid;
        internal TabPage tpGrid;
        private TabPage tpTextView;
        private TextBox txtFilesDetails;
        private Button bRemux;
        private Label lbDragFolderHere;
        private TabPage tpOutput;
        private TextBox tbOutput;
        private CheckBox chkRemoveFileTitle;
        private Label lbMediaInfoCliVersion;
        private Label lbOutputFolder;
        private ButtonTextBox btbOutputPath;
    }
}
