using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Xml.Linq;
using static System.Windows.Forms.ListViewItem;
using ListView = System.Windows.Forms.ListView;
using ToolTip = System.Windows.Forms.ToolTip;

namespace RemuxOpt
{
    public partial class FrmMain : Form
    {
        private BackgroundWorker _backgroundWorker;
        private HorizontalScrollDataGridView _dataGridViewResults;

        public FrmMain()
        {
            InitializeComponent();
            Load += FrmMain_Load;
            FormClosing += FrmMain_FormClosing;

            InitBackgroundWorker();
            InitGrid();
            InitTooltips();
            InitListViews();

            chkEmptyVideoTitle.CheckedChanged += CbEmptyVideoTitle_CheckedChanged;
            chkEmptyVideoTitle.Checked = true; // Default to empty video title

            lbAddAudioLanguage.LinkClicked += LbAddLanguage_LinkClicked;
            btnAddSubtitleLanguage.LinkClicked += LbAddSubtitleLanguage_LinkClicked;


            pFiles.DragEnter += (s, e) => e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            pFiles.DragDrop += async (s, e) =>
            {
                lbDragFolderHere.Visible = false;

                var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
                var dropppedFiles = paths.SelectMany(path =>
                    Directory.Exists(path) ?
                        Directory.GetFiles(path, "*.mkv", SearchOption.AllDirectories)
                        .Concat(Directory.GetFiles(path, "*.mp4", SearchOption.AllDirectories)) :
                        [path]).ToList();

                if (!_backgroundWorker.IsBusy)
                {
                    pProgress.Visible = true;

                    var context = new BackgroundWorkerContext
                    {
                        TaskType = BackgroundTaskType.LoadDroppedFiles,
                        Payload = new WorkerPayload
                        {
                            Files = dropppedFiles
                        }
                    };

                    _backgroundWorker.RunWorkerAsync(context);
                }
                else
                {
                    MessageBox.Show("Processing is already running.");
                }
            };

            bRemux.Click += (s, e) =>
            {
                if (lvAudioTracks.Items.Count == 0 && MsgBox.Show(this, "There are no audio tracks languages configured, and this will remove all audio tracks from the remuxed files(s). Are you sure you want to continue?", "Warning",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.No)
                {
                    return;
                }

                var selectedFiles = GetCheckedFilenames();

                if (selectedFiles.Count == 0)
                {
                    MsgBox.Show(this, "No files selected for remuxing.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;;
                }

                if (_backgroundWorker.IsBusy)
                {
                    MsgBox.Show(this, "Another operation is currently running. Please wait.", "Busy", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                pProgress.Visible = true;

                var remuxHelper = new MkvRemuxHelper
                {
                    UseAutoTitle = chkAutoTitleForAudioTrack.Checked,
                    RemoveAttachments = chkRemoveAttachments.Checked,
                    RemoveForcedFlags = chkRemoveAttachments.Checked,
                    RermoveFileTitle = chkRemoveFileTitle.Checked,
                    AudioLanguageOrder = lvAudioTracks.Items.Cast<ListViewItem>()
                        .Select(item => item.SubItems[1].Text)
                        .ToList(),
                    SubtitleLanguageOrder = lvSubtitleTracks.Items.Cast<ListViewItem>()
                        .Select(item => item.SubItems[1].Text)
                        .ToList()
                };

                tbOutput.Clear();
                progressBar.Value = 0;

                var context = new BackgroundWorkerContext
                {
                    TaskType = BackgroundTaskType.RemuxSelectedFiles,
                    Payload = new WorkerPayload
                    {
                        Files = selectedFiles,
                        RemuxHelper = remuxHelper
                    }
                };

                _backgroundWorker.RunWorkerAsync(context);
            };
        }

        private void InitGrid()
        {
            _dataGridViewResults = new HorizontalScrollDataGridView
            {                
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackgroundColor = SystemColors.Window,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
                Width = tpGrid.ClientSize.Width,
                Height = tpGrid.ClientSize.Height,
                BorderStyle = BorderStyle.None,
                RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing,
                RowHeadersWidth = 25,
                EditMode = DataGridViewEditMode.EditOnEnter
            };

            tpGrid.Controls.Add(_dataGridViewResults);
        }

        private void InitBackgroundWorker()
        {
            _backgroundWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };

            _backgroundWorker.DoWork += BackgroundWorker_DoWork;
            _backgroundWorker.ProgressChanged += BackgroundWorker_ProgressChanged;
            _backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;

            btnCancelWorker.Click += BtnCancelWorker_Click;
        }

        private void BtnCancelWorker_Click(object? sender, EventArgs e)
        {
            if (_backgroundWorker.IsBusy && _backgroundWorker.WorkerSupportsCancellation)
            {
                _backgroundWorker.CancelAsync();
            }
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var context = (BackgroundWorkerContext)e.Argument;

            switch (context.TaskType)
            {
                case BackgroundTaskType.LoadDroppedFiles:
                    e.Result = LoadMkvFileInfosAsync(context.Payload.Files, _backgroundWorker, e).GetAwaiter().GetResult();
                    break;

                case BackgroundTaskType.RemuxSelectedFiles:
                    e.Result = RunRemuxProcess(context);
                    break;
            }
        }

        private async Task<WorkerResult> LoadMkvFileInfosAsync(List<string> files, BackgroundWorker worker, DoWorkEventArgs e)
        {
            var workerResult = new WorkerResult
            {
                TaskType = BackgroundTaskType.LoadDroppedFiles
            };

            for (int i = 0; i < files.Count; i++)
            {
                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                    return workerResult;
                }

                string file = files[i];

                try
                {
                    var info = await MkvMetadataExtractor.ExtractInfoAsync(file);
                    workerResult.Files.Add(info);

                    int percent = (int)((i + 1) / (double)files.Count * 100);

                    var progressMessage = new ProgressMessage(
                        $"Processing: {Path.GetFileName(Path.GetFileName(file))}",
                        string.Empty
                    );
                    worker.ReportProgress(percent, progressMessage);
                }
                catch (Exception ex)
                {
                    MsgBox.Show(this, $"Failed to read {file}: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            return workerResult;
        }

        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;

            if (e.UserState is ProgressMessage msg)
            {
                progressLabel.Text = msg.StatusText;
                tbOutput.Text = msg.RemuxLog;
            }
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressLabel.Text = "Done!";

            if (e.Cancelled)
            {
                MsgBox.Show(this, "Operation was cancelled.", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (e.Error != null)
            {
                MsgBox.Show(this, "Error: " + e.Error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                if (e.Result is WorkerResult workerResult)
                {
                    switch (workerResult.TaskType)
                    {
                        case BackgroundTaskType.LoadDroppedFiles:
                            if (workerResult.Files.Count == 0)
                            {
                                MsgBox.Show(this, "No files found or processed.", "No Results", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                            else
                            {
                                PopulateGrid(workerResult.Files);
                                BuildTextView(workerResult.Files);
                            }

                            break;
                        case BackgroundTaskType.RemuxSelectedFiles:
                            MsgBox.ShowAutoClose(this, "Done!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, timeoutSeconds: 5);
                            break;
                    }
                }
            }

            progressBar.Value = 0;
            pProgress.Visible = false;
        }


        private void PopulateGrid(List<MkvFileInfo> files)
        {
            _dataGridViewResults.Columns.Clear();
            _dataGridViewResults.Rows.Clear();

            int maxAudio = files.Max(f => f.AudioTracks.Count);
            int maxSubs = files.Max(f => f.Subtitles.Count);

            //System columns
            var checkCol = new DataGridViewCheckBoxColumn
            {
                Name = "Selected",
                HeaderText = " ",      // You can write "✓" or "Select" if you like
                Width = 32,
                Frozen = true,
                ReadOnly = false,
                Resizable = DataGridViewTriState.False
            };

            _dataGridViewResults.Columns.Insert(0, checkCol);

            // Base columns
            var col = new DataGridViewTextBoxColumn
            {
                Name = "Filename",
                HeaderText = "File name",
                Frozen = true
            };
            _dataGridViewResults.Columns.Add(col);

            // Audio columns
            for (int i = 0; i < maxAudio; i++)
            {
                _dataGridViewResults.Columns.Add($"Audio{i}_Lang", $"Audio{i + 1} Lang");
                _dataGridViewResults.Columns.Add($"Audio{i}_Title", $"Audio{i + 1} Title");
                _dataGridViewResults.Columns.Add($"Audio{i}_Ch", $"Audio{i + 1} Ch");
                _dataGridViewResults.Columns.Add($"Audio{i}_Bitrate", $"Audio{i + 1} kbps");
            }

            // Subtitle columns
            for (int i = 0; i < maxSubs; i++)
            {
                _dataGridViewResults.Columns.Add($"Sub{i}_Lang", $"Sub{i + 1} Lang");
                _dataGridViewResults.Columns.Add($"Sub{i}_Title", $"Sub{i + 1} Title");
            }

            _dataGridViewResults.Columns.Add("Attachments", "Attachments");

            foreach (DataGridViewColumn gridCol in _dataGridViewResults.Columns)
            {
                if (gridCol is DataGridViewCheckBoxColumn)
                {
                    continue; // Skip checkbox column
                }

                gridCol.ReadOnly = true;
            }



            _dataGridViewResults.AddHeaderCheckBoxToColumn(0);

            // Rows
            foreach (var file in files)
            {
                var row = new List<object>
                {
                    true,
                    Path.GetFileName(file.FileName)
                };

                foreach (var a in file.AudioTracks)
                {
                    row.Add(a.Language);
                    row.Add(a.Title);
                    row.Add(a.Channels);
                    row.Add(a.Bitrate.HasValue ? (a.Bitrate.Value / 1000).ToString() : "");
                }

                for (int i = file.AudioTracks.Count; i < maxAudio; i++)
                    row.AddRange(["", "", "", ""]);

                foreach (var s in file.Subtitles)
                {
                    row.Add(s.Language);
                    row.Add(s.Title);
                }

                for (int i = file.Subtitles.Count; i < maxSubs; i++)
                    row.AddRange(["", ""]);

                row.Add(file.Attachments.Count);

                var dgRow = _dataGridViewResults.Rows[_dataGridViewResults.Rows.Add(row.ToArray())];
                dgRow.Tag = file.FileName;
            }

            _dataGridViewResults.SuspendLayout();
            _dataGridViewResults.AutoResizeColumns();

            foreach (DataGridViewColumn gridCol in _dataGridViewResults.Columns)
            {
                int w = gridCol.Width; // store calculated width
                gridCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                gridCol.Width = w;
            }

            _dataGridViewResults.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            _dataGridViewResults.HeaderCheckState = true;
            _dataGridViewResults.ResumeLayout();
        }

        private void BuildTextView(List<MkvFileInfo> files)
        {
            var sb = new StringBuilder();

            foreach (var info in files)
            {
                sb.AppendLine($"File: {Path.GetFileName(info.FileName)}");

                foreach (var audio in info.AudioTracks)
                {
                    var channels = audio.Channels switch
                    {
                        1 => "1.0",
                        2 => "2.0",
                        6 => "5.1",
                        8 => "7.1",
                        _ => $"{audio.Channels}.0"
                    };

                    var bitrate = audio.Bitrate.HasValue ? $"{audio.Bitrate.Value / 1000} kbps" : "unknown bitrate";
                    sb.AppendLine($"  Audio [{audio.Language}]: {channels} @ {bitrate} - {audio.Title}");
                }

                foreach (var sub in info.Subtitles)
                {
                    sb.AppendLine($"  Subtitle [{sub.Language}]: {sub.Title}");
                }

                foreach (var att in info.Attachments)
                {
                    sb.AppendLine($"  Attachment: {att.FileName} ({att.MimeType})");
                }

                sb.AppendLine();
            }

            txtFilesDetails.Text = sb.ToString();
        }


        private void FrmMain_FormClosing(object? sender, FormClosingEventArgs e)
        {
            SaveFormSettings(this); // Save window size, state, and position            
        }

        private void FrmMain_Load(object? sender, EventArgs e)
        {
            LoadFormSettings(this);
            bRemux.Enabled = CheckToolsInPath();
        }

        private static void SaveFormSettings(Form mainForm)
        {
            var doc = File.Exists("config.xml")
                ? XDocument.Load("config.xml")
                : new XDocument(new XElement("Configuration"));

            var formSettings = new XElement("FormSettings",
                new XAttribute("Width", mainForm.Width),
                new XAttribute("Height", mainForm.Height),
                new XAttribute("Left", mainForm.Left),
                new XAttribute("Top", mainForm.Top),
                new XAttribute("WindowState", mainForm.WindowState.ToString())
            );

            doc.Root?.Element("FormSettings")?.Remove(); // Remove old settings
            doc.Root?.Add(formSettings);
            doc.Save("config.xml");
        }

        private static void LoadFormSettings(Form mainForm)
        {
            if (File.Exists("config.xml"))
            {
                var doc = XDocument.Load("config.xml");
                var formSettings = doc.Root?.Element("FormSettings");

                if (formSettings != null)
                {
                    mainForm.Width = int.Parse(formSettings.Attribute("Width")?.Value ?? "800");
                    mainForm.Height = int.Parse(formSettings.Attribute("Height")?.Value ?? "600");
                    mainForm.Left = int.Parse(formSettings.Attribute("Left")?.Value ?? "100");
                    mainForm.Top = int.Parse(formSettings.Attribute("Top")?.Value ?? "100");

                    if (Enum.TryParse(formSettings.Attribute("WindowState")?.Value, out FormWindowState state))
                    {
                        mainForm.WindowState = state;
                    }
                }
            }
        }

        private bool CheckToolsInPath()
        {
            var result = true;

            var exePath = GetExecutablePath("mkvmerge.exe");

            if (!string.IsNullOrEmpty(exePath))
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(exePath);
                lbMkvVersion.Text = $"mkvmerge found in PATH! Version: {versionInfo.FileVersion}";
            }
            else
            {
                lbMkvVersion.Text = $"mkvmerge not found in PATH!";
                lbMkvVersion.ForeColor = Color.Red;
                result = false;
            }

            exePath = GetExecutablePath("ffprobe.exe");

            if (!string.IsNullOrEmpty(exePath))
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "ffprobe",
                    Arguments = "-version", // ffprobe prints version info here
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    // First line looks like: "ffprobe version 6.1.1 ..."
                    string versionLine = output.Split('\n').FirstOrDefault(line => line.StartsWith("ffprobe version"));
                    string version = versionLine?.Split(' ')[2] ?? "Unknown";

                    lbFFprobeVersion.Text = $"ffprobe found in PATH! Version: {version}";
                }
            }
            else
            {
                lbFFprobeVersion.Text = $"ffprobe not found in PATH!";
                lbFFprobeVersion.ForeColor = Color.Red;
                result = false;
            }

            return result;
        }

        private static string GetExecutablePath(string executableName)
        {
            string pathVariable = Environment.GetEnvironmentVariable("PATH");

            return pathVariable.Split(';')
                .Select(path => Path.Combine(path, executableName))
                .FirstOrDefault(File.Exists);
        }

        private void InitTooltips()
        {
            var toolTip = new ToolTip
            {
                IsBalloon = true, 
                AutoPopDelay = 5000,
                InitialDelay = 1000,
                ReshowDelay = 500,
                ShowAlways = true
            };

            // Assign tooltips to controls
            toolTip.SetToolTip(chkAutoTitleForAudioTrack, $"If checked, the title track is built from the track technical specs!{Environment.NewLine}{Environment.NewLine}<Language-name> <channels> @ <bitrate>");
            toolTip.SetToolTip(chkPreserveSubtitlesTrackTitles, "If checked, the 'Forced', 'SDH', 'CC' values will be preserved!");
            toolTip.SetToolTip(_dataGridViewResults, "Use SHIFT key modifier to scroll vertically using the mouse wheel!");
        }

        #region ListViews Initialization and Event Handlers

        private void InitListViews()
        {
            InitListView(lvAudioTracks);
            InitListView(lvSubtitleTracks);
        }

        private void InitListView(ListView listView)
        {
            listView.Columns.Add("Language", 250);

            const int codeWidth = 50;
            listView.Columns.Add("Code", codeWidth);

            const int delWidth = 50;
            listView.Columns.Add(string.Empty, delWidth, HorizontalAlignment.Center);

            listView.Columns[0].Width = listView.ClientSize.Width - codeWidth - delWidth;


            listView.ItemDrag += LvGeneric_ItemDrag;
            listView.DragEnter += LvGeneric_DragEnter;
            listView.DragDrop += LvGeneric_DragDrop;
            listView.DragOver += LvGeneric_DragOver;
            listView.MouseClick += LvGeneric_MouseClick;

            listView.AllowDrop = true;
            listView.HeaderStyle = ColumnHeaderStyle.Nonclickable;

            listView.InsertionMark.Color = Color.Red; // Set the indicator color
            listView.InsertionMark.AppearsAfterItem = false; // Show before the item
            listView.FullRowSelect = true;

            LoadListFromXml(listView);
        }

        private void LvGeneric_ItemDrag(object sender, ItemDragEventArgs e)
        {
            ((ListView)sender).DoDragDrop(e.Item, DragDropEffects.Move);
        }

        private void LvGeneric_DragEnter(object sender, DragEventArgs e)
        {
            // Check if data contains file drop (external files dragged in)
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Files from outside detected — disallow drop
                e.Effect = DragDropEffects.None;
            }
            else
            {
                // Otherwise allow your internal drag-drop logic
                e.Effect = DragDropEffects.Move;  // or whatever effect you want
            }
        }

        private void LvGeneric_DragDrop(object sender, DragEventArgs e)
        {
            var listView = (ListView)sender;

            if (!e.Data.GetDataPresent(typeof(ListViewItem)))
                return;

            ListViewItem draggedItem = (ListViewItem)e.Data.GetData(typeof(ListViewItem));
            int sourceIndex = listView.Items.IndexOf(draggedItem);
            int targetIndex = listView.InsertionMark.Index;
            bool appearsAfter = listView.InsertionMark.AppearsAfterItem;

            // Clear insertion mark
            listView.InsertionMark.Index = -1;

            if (targetIndex == -1 || sourceIndex == -1)
                return;

            // Calculate actual insertion index considering AppearsAfterItem
            if (appearsAfter)
                targetIndex++;

            // Cancel no-op moves (dropping back at original position or right after)
            if (targetIndex == sourceIndex || targetIndex == sourceIndex + 1)
                return;

            // Remove dragged item
            listView.Items.RemoveAt(sourceIndex);

            // Adjust target index if dragged item was before insertion point
            if (targetIndex > sourceIndex)
                targetIndex--;

            // Clamp targetIndex to valid range
            targetIndex = Math.Max(0, Math.Min(targetIndex, listView.Items.Count));

            // Insert dragged item at the corrected index
            listView.Items.Insert(targetIndex, draggedItem);

            SaveListToXml();
        }

        private void LvGeneric_DragOver(object sender, DragEventArgs e)
        {
            var listView = (ListView)sender;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.None;
                listView.InsertionMark.Index = -1;
                return;
            }

            Point pt = listView.PointToClient(new Point(e.X, e.Y));
            int nearestIndex = listView.InsertionMark.NearestIndex(pt);

            if (nearestIndex == -1)
            {
                listView.InsertionMark.Index = -1;
                return;
            }

            Rectangle itemBounds = listView.GetItemRect(nearestIndex);
            bool appearsAfter = pt.Y > itemBounds.Top + itemBounds.Height / 2;

            listView.InsertionMark.Index = nearestIndex;
            listView.InsertionMark.AppearsAfterItem = appearsAfter;

            e.Effect = DragDropEffects.Move;
        }


        private void LvGeneric_MouseClick(object sender, MouseEventArgs e)
        {
            var serverLv = (ListView)sender;

            ListViewHitTestInfo hit = serverLv.HitTest(e.Location);
            if (hit.Item != null && hit.SubItem == hit.Item.SubItems[2]) // Last column
            {
                serverLv.Items.Remove(hit.Item); // Remove item
                SaveListToXml(); // Persist changes
            }
        }

        #endregion

        private void CbEmptyVideoTitle_CheckedChanged(object? sender, EventArgs e)
        {
            var isChecked = chkEmptyVideoTitle.Checked;

            tbVideoTitle.Enabled = !isChecked;
            tbVideoTitle.BackColor = isChecked ? Color.WhiteSmoke : SystemColors.Window;

            if (isChecked)
            {
                tbVideoTitle.Text = string.Empty; // Clear the text box when checked
            }
        }

        private void AddLanguageToListView(ListView targetListView, LanguageObject lang)
        {
            var item = new ListViewItem(lang.Name);
            item.SubItems.Add(lang.Abr3a);

            var delItem = new ListViewSubItem(item, "✕")
            {
                Font = new Font(lvAudioTracks.Font, FontStyle.Bold)
            };

            item.UseItemStyleForSubItems = false;
            item.SubItems.Add(delItem);
            item.Tag = lang.Abr3a;
            targetListView.Items.Add(item);
        }

        private void AddLanguageToListView(ListView targetListView, LanguageObject[] langs)
        {
            foreach (var lang in langs)
            {
                AddLanguageToListView(targetListView, lang);
            }
        }

        private static void PositionModal(Form modalForm, LinkLabel linkLabel, Form parentForm, int verticalOffset = 0)
        {
            Point targetLocation = linkLabel.PointToScreen(Point.Empty);
            int modalHeight = modalForm.Height;
            int screenHeight = Screen.PrimaryScreen.WorkingArea.Height;

            // Check if there's enough space below the LinkLabel
            if (targetLocation.Y + linkLabel.Height + modalHeight < screenHeight)
            {
                modalForm.StartPosition = FormStartPosition.Manual;
                modalForm.Location = new Point(targetLocation.X, targetLocation.Y + linkLabel.Height + verticalOffset);
            }
            else
            {
                // Center in the parent form if not enough space
                modalForm.StartPosition = FormStartPosition.CenterParent;
            }
        }


        private void LbAddLanguage_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
        {
            AddLanguage(sender, lvAudioTracks);
        }

        private void LbAddSubtitleLanguage_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
        {
            AddLanguage(sender, lvSubtitleTracks);
        }

        private void AddLanguage(object? sender, ListView targetListView)
        {
            var selectedCodes = targetListView.Items.Cast<ListViewItem>()
                           .Select(item => item.Tag?.ToString() ?? string.Empty)
                           .Where(tag => !string.IsNullOrEmpty(tag))
                           .ToList();


            var addLanguageForm = new FrmAddLanguage(selectedCodes);
            PositionModal(addLanguageForm, (LinkLabel)sender, this, 10);

            if (addLanguageForm.ShowDialog() == DialogResult.OK)
            {
                AddLanguageToListView(targetListView, addLanguageForm.SelectedLanguage);
                SaveListToXml();
            }
        }

        private void LoadListFromXml(ListView targetListView)
        {
            if (File.Exists("config.xml"))
            {
                XDocument doc = XDocument.Load("config.xml");
                XElement listElement = doc.Root?.Element(targetListView.Name); // Get the correct ListView section

                if (listElement != null)
                {
                    targetListView.Items.Clear(); // Clear existing items

                    foreach (XElement langElement in listElement.Elements("Language"))
                    {
                        AddLanguageToListView(targetListView,
                            new LanguageObject(langElement.Attribute("Name")?.Value ?? string.Empty, langElement.Attribute("Code")?.Value ?? string.Empty)
                        );
                    }
                }
            }
            else
            {
                AddDefaultLanguagesToListViews();
            }
        }

        private void AddDefaultLanguagesToListViews()
        {
            AddDefaultLanguagesToListView(lvAudioTracks);
            AddDefaultLanguagesToListView(lvSubtitleTracks);

            SaveListToXml();

            void AddDefaultLanguagesToListView(ListView targetListView)
            {
                AddLanguageToListView(targetListView,
                [
                    new LanguageObject("Romanian; Moldavian; Moldovan", "rum"),
                    new LanguageObject("Filipino; Pilipino", "fil"),
                    new LanguageObject("Tagalog", "tgl"),
                    new LanguageObject("Dutch", "dut"),
                    new LanguageObject("English", "eng")
                ]);
            }           
        }

        private void SaveListToXml()
        {
            var doc = new XDocument(
                new XElement("Configuration",
                    new XElement("lvAudioTracks",
                        lvAudioTracks.Items.Cast<ListViewItem>().Select(item =>
                            new XElement("Language",
                                new XAttribute("Name", item.Text),
                                new XAttribute("Code", item.Tag?.ToString() ?? "")
                            )
                        )
                    ),
                    new XElement("lvSubtitleTracks",
                        lvSubtitleTracks.Items.Cast<ListViewItem>().Select(item =>
                            new XElement("Language",
                                new XAttribute("Name", item.Text),
                                new XAttribute("Code", item.Tag?.ToString() ?? "")
                            )
                        )
                    )
                )
            );

            doc.Save("config.xml"); // Save near the EXE
        }

        private List<string> GetCheckedFilenames()
        {
            return _dataGridViewResults.Rows
                .Cast<DataGridViewRow>()
                .Where(row => Convert.ToBoolean(row.Cells["Selected"].Value))
                .Select(row => row.Tag?.ToString()!)
                .ToList();
        }

        private WorkerResult RunRemuxProcess(BackgroundWorkerContext context)
        {
            var workerResult = new WorkerResult();

            if (context.Payload is not WorkerPayload wp)
            {
                workerResult.TaskType = BackgroundTaskType.Unknown;
                return workerResult;
            }

            workerResult.TaskType = BackgroundTaskType.RemuxSelectedFiles;

            var selectedFiles = wp.Files;
            var remuxHelper = wp.RemuxHelper;

            var sb = new StringBuilder();

            for (int i = 0; i < selectedFiles.Count; i++)
            {
                string inputFile = selectedFiles[i];
                string outputFile = Path.Combine(
                    Path.GetDirectoryName(inputFile)!,
                    Path.GetFileNameWithoutExtension(inputFile) + ".remuxed.mkv"
                );

                sb.AppendLine("Input file:");
                sb.AppendLine(inputFile);
                sb.AppendLine("Output file:");
                sb.AppendLine(outputFile);

                var progressMessage = new ProgressMessage(
                    $"Processing: {Path.GetFileName(inputFile)}",
                    sb.ToString()
                );

                _backgroundWorker.ReportProgress(
                     Math.Min(100, (int)(((i + 1) / (float)selectedFiles.Count) * 100)),
                    progressMessage
                );

                try
                {
                    string args = remuxHelper.BuildMkvMergeArgs(inputFile, outputFile);

                    sb.AppendLine("Arguments:");
                    sb.AppendLine(args);
                    sb.AppendLine();

                    var psi = new ProcessStartInfo
                    {
                        FileName = remuxHelper.MkvMergePath,
                        Arguments = args,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(psi);
                    string stdout = process.StandardOutput.ReadToEnd();
                    string stderr = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        sb.AppendLine($"[ERROR] mkvmerge exited with code {process.ExitCode}");
                        sb.AppendLine(stderr);
                    }
                    else
                    {
                        sb.AppendLine("Remux completed successfully.");
                        if (!string.IsNullOrWhiteSpace(stdout))
                            sb.AppendLine(stdout);
                    }
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"[EXCEPTION] {ex.Message}");
                }

                sb.AppendLine(new string('-', 60));

                progressMessage.RemuxLog = sb.ToString();

                _backgroundWorker.ReportProgress(
                     Math.Min(100, (int)(((i + 1) / (float)selectedFiles.Count) * 100)),
                    progressMessage
                );
            }

            return workerResult;
        }
    }
}
