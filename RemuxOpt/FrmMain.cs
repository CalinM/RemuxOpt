﻿using Microsoft.WindowsAPICodePack.Dialogs;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
        private readonly ContextMenuStrip _headerContextMenu = new();
        private string? _clickedColumnNameForContext = null;
        private Label _dragTooltipLabel;

        private AppOptions _appOptions = new();

        public FrmMain()
        {
            InitializeComponent();

            var version = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            Text += $" - v{version.FileVersion}";

            Load += FrmMain_Load;
            FormClosing += FrmMain_FormClosing;

            InitBackgroundWorker();
            InitGrid();
            InitTooltips();
            InitListViews();
            InitOptions();

            chkEmptyVideoTitle.CheckedChanged += CbEmptyVideoTitle_CheckedChanged;
            chkEmptyVideoTitle.Checked = true; // Default to empty video title

            lbAddAudioLanguage.LinkClicked += LbAddLanguage_LinkClicked;
            btnAddSubtitleLanguage.LinkClicked += LbAddSubtitleLanguage_LinkClicked;


            pFiles.DragEnter += HandleDragEffect;
            pFiles.DragOver += HandleDragEffect;
            pFiles.DragLeave += (s, e) => _dragTooltipLabel.Visible = false;

            pFiles.DragDrop += async (s, e) =>
            {
                _dragTooltipLabel.Visible = false;
                lbDragFolderHere.Visible = false;

                // Check if Ctrl key was pressed during drop
                bool ctrlPressed = (e.KeyState & 8) == 8;

                if (!ctrlPressed)
                {
                    ClearPreviousFilesDetails();
                }

                tcGrid.SelectTab(tpGrid);

                var paths = (string[])e.Data.GetData(DataFormats.FileDrop);

                var droppedFiles = paths.SelectMany(path =>
                {
                    if (Directory.Exists(path))
                    {
                        var files = Directory.GetFiles(path, "*.*", _appOptions.ReadFilesRecursively ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                            .Where(f => Path.GetExtension(f).Equals(".mkv", StringComparison.OrdinalIgnoreCase) ||
                                        Path.GetExtension(f).Equals(".mp4", StringComparison.OrdinalIgnoreCase));

                        return files.Select(f => new MkvFileInfo { FileName = f });
                    }
                    else if (File.Exists(path) &&
                            (Path.GetExtension(path).Equals(".mkv", StringComparison.OrdinalIgnoreCase) ||
                             Path.GetExtension(path).Equals(".mp4", StringComparison.OrdinalIgnoreCase)))
                    {
                        return [new MkvFileInfo { FileName = path }];
                    }
                    else
                    {
                        return Enumerable.Empty<MkvFileInfo>();
                    }
                }).ToList();

                if (!_backgroundWorker.IsBusy)
                {
                    pProgress.Visible = true;

                    var context = new BackgroundWorkerContext
                    {
                        TaskType = BackgroundTaskType.LoadDroppedFiles,
                        Payload = new WorkerPayload
                        {
                            Files = droppedFiles
                        }
                    };

                    _backgroundWorker.RunWorkerAsync(context);
                }
                else
                {
                    MessageBox.Show("Processing is already running.");
                }
            };

            btbOutputPath.ButtonClick += BtbOutputPath_ButtonClick;

            bRemux.Click += (s, e) =>
            {
                if (lvAudioTracks.Items.Count == 0 && MsgBox.Show(this, "There are no audio tracks languages configured, and this will remove all audio tracks from the remuxed files(s). Are you sure you want to continue?", "Warning",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.No)
                {
                    return;
                }

                if (!EnsureValidAndCreatePath(btbOutputPath.Text, out var error))
                {
                    MsgBox.Show(this, error, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var selectedFiles = GetCheckedFilenames();

                if (selectedFiles.Count == 0)
                {
                    MsgBox.Show(this, "No files selected for remuxing.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
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
                    OutputFolder = btbOutputPath.Text,
                    RemoveFileTitle = chkRemoveFileTitle.Checked,
                    RemoveUnlistedLanguageTracks = _appOptions.RemoveUnlistedLanguageTracks,
                    DefaultAudioTrackLanguageCode = GetDefaultLanguageCode(lvAudioTracks),
                    DefaultSubtitleTrackLanguageCode = GetDefaultLanguageCode(lvSubtitleTracks),
                    AudioLanguageOrder = GetAudioLanguageCodes(lvAudioTracks),
                    SubtitleLanguageOrder = GetAudioLanguageCodes(lvSubtitleTracks)
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

        private void InitializeDragTooltip()
        {
            _dragTooltipLabel = new Label
            {
                Text = "Append files ...",
                BackColor = SystemColors.Info,
                ForeColor = SystemColors.InfoText,
                BorderStyle = BorderStyle.FixedSingle,
                AutoSize = true,
                Visible = false,
                Font = new Font("Segoe UI", 8f)
            };
            this.Controls.Add(_dragTooltipLabel);
            _dragTooltipLabel.BringToFront();
        }

        void HandleDragEffect(object s, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                bool ctrlPressed = (e.KeyState & 8) == 8; // 8 is the flag for Ctrl key
                e.Effect = ctrlPressed ? DragDropEffects.Link : DragDropEffects.Copy;

                // Show tooltip when Ctrl is pressed
                if (ctrlPressed && _dataGridViewResults.Rows.Count > 0)
                {
                    var screenPos = new Point(e.X, e.Y);
                    var clientPos = this.PointToClient(screenPos);

                    _dragTooltipLabel.Location = new Point(clientPos.X + 10, clientPos.Y - 25);
                    _dragTooltipLabel.Visible = true;
                }
                else
                {
                    _dragTooltipLabel.Visible = false;
                }
            }
            else
            {
                e.Effect = DragDropEffects.None;
                _dragTooltipLabel.Visible = false;
            }
        }


        private List<string> GetAudioLanguageCodes(ListView listview)
        {
            return listview.Items.Cast<ListViewItem>()
                .Select(item => item.SubItems[1].Text)
                .ToList();
        }

        private void ClearPreviousFilesDetails()
        {
            _dataGridViewResults.ClearWithHeaderCheckboxCleanup();
            txtFilesDetails.Clear();
            tbOutput.Clear();
        }

        public static bool EnsureValidAndCreatePath(string path, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(path))
            {
                errorMessage = "Path is empty or whitespace.";
                return false;
            }

            // Check for invalid characters
            if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                errorMessage = "Path contains invalid characters.";
                return false;
            }

            // Check for absolute path
            if (!Path.IsPathRooted(path))
            {
                errorMessage = "Path must be absolute.";
                return false;
            }

            try
            {
                string directoryPath = path;

                // If it's a file path, extract the directory
                if (Path.HasExtension(path))
                {
                    directoryPath = Path.GetDirectoryName(path)!;
                }

                if (string.IsNullOrEmpty(directoryPath))
                {
                    errorMessage = "Could not determine the directory part of the path.";
                    return false;
                }

                // Create the directory if it doesn't exist
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
            }
            catch (UnauthorizedAccessException)
            {
                errorMessage = "Access denied: Unable to create directory.";
                return false;
            }
            catch (Exception ex)
            {
                errorMessage = $"Unexpected error: {ex.Message}";
                return false;
            }

            return true;
        }

        private void BtbOutputPath_ButtonClick(object? sender, EventArgs e)
        {
            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = true;
                dialog.Title = "Please pick the output folder ...";
                dialog.InitialDirectory = btbOutputPath.Text;

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    btbOutputPath.Text = dialog.FileName;
                }
            }
        }

        private string GetDefaultLanguageCode(ListView listview)
        {
            XDocument doc = XDocument.Load("config.xml");

            string? defaultAudioLangCode = doc
                .Root?
                .Element(listview.Name)?
                .Elements("Language")
                .FirstOrDefault(x => string.Equals(x.Attribute("IsDefault")?.Value, "true", StringComparison.OrdinalIgnoreCase))
                ?.Attribute("Code")?.Value;

            return !string.IsNullOrEmpty(defaultAudioLangCode) ? defaultAudioLangCode : string.Empty;
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
            InitializeGridContextMenu();
            InitializeDragTooltip();

            _dataGridViewResults.CellFormatting += _dataGridViewResults_CellFormatting;
            _dataGridViewResults.CellValueChanged += _dataGridViewResults_CellValueChanged;
            _dataGridViewResults.ColumnHeaderMouseClick += _dataGridViewResults_ColumnHeaderMouseClick;
        }

        private void _dataGridViewResults_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var row = _dataGridViewResults.Rows[e.RowIndex];
            if (row.Tag is not MkvFileInfo file)
            {
                return;
            }

            var columnName = _dataGridViewResults.Columns[e.ColumnIndex].Name;
            if (
                !(columnName.StartsWith("Audio") && columnName.EndsWith("_Lang")) &&
                !(columnName.StartsWith("Sub") && columnName.EndsWith("_Lang")) &&
                columnName != "ExternalAudioLanguage"
            )
            {
                return;
            }

            string? newValue = row.Cells[e.ColumnIndex].Value?.ToString();

            if (columnName == "ExternalAudioLanguage")
            {
                var extAudio = file.ExternalAudioTracks.FirstOrDefault();
                if (extAudio != null)
                {
                    extAudio.Language = newValue;
                }
            }
            else
            {
                var audioMatch = Regex.Match(columnName, @"Audio(\d+)_Lang");
                if (audioMatch.Success)
                {
                    int trackIndex = int.Parse(audioMatch.Groups[1].Value);
                    if (trackIndex < file.AudioTracks.Count)
                    {
                        file.AudioTracks[trackIndex].Language = newValue;
                    }
                    return;
                }

                var subMatch = Regex.Match(columnName, @"Sub(\d+)_Lang");
                if (subMatch.Success)
                {
                    int trackIndex = int.Parse(subMatch.Groups[1].Value);
                    if (trackIndex < file.SubtitleTracks.Count)
                    {
                        file.SubtitleTracks[trackIndex].Language = newValue;
                    }
                    return;
                }
            }

            row.Cells[e.ColumnIndex].Tag = "edited";

            row.Cells[e.ColumnIndex].Style.Font = new Font(_dataGridViewResults.Font, FontStyle.Bold);
        }

        private void _dataGridViewResults_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;

            var column = _dataGridViewResults.Columns[e.ColumnIndex];

            // Only show for specific columns
            if (
                !(column.Name.StartsWith("Audio") && column.Name.EndsWith("_Lang")) &&
                !(column.Name.StartsWith("Sub") && column.Name.EndsWith("_Lang")) &&
                column.Name != "ExternalAudioLanguage")
            {
                return;
            }

            _clickedColumnNameForContext = column.Name;

            _headerContextMenu.Show(_dataGridViewResults, _dataGridViewResults.PointToClient(Cursor.Position));
        }

        private void InitializeGridContextMenu()
        {
            _headerContextMenu.Items.Add("Assign/update a language code ...").Click += (s, e) => UpdateLanguageCode();
        }

        private void UpdateLanguageCode()
        {
            var selectedFiles = GetCheckedFilenames();

            using var addLanguageForm = new FrmAddLanguage(new List<string>(), $"Update the language code on field \"{_clickedColumnNameForContext}\" for the loaded data ({selectedFiles.Count} files selected)");
            if (addLanguageForm.ShowDialog(this) == DialogResult.OK)
            {
                if (MsgBox.Show(this, $"Are you sure you want to set \"{addLanguageForm.SelectedLanguage.Name}\" on all selected files ({selectedFiles.Count})", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                {
                    return;
                }

                UpdateFieldByColumnName(selectedFiles, addLanguageForm.SelectedLanguage.Abr3a);
            }
        }

        private void _dataGridViewResults_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            var dgv = sender as DataGridView;
            if (dgv == null || e.RowIndex < 0)
            {
                return;
            }

            var columnName = dgv.Columns[e.ColumnIndex].Name;

            if (columnName != "FileName" && !dgv.Rows[e.RowIndex].Selected && e.ColumnIndex > 0)
            {
                if (_rowColorMap.TryGetValue(e.RowIndex, out Color rowColor))
                {
                    e.CellStyle.BackColor = rowColor;
                }
            }

            if (columnName == "Filename")
            {
                e.CellStyle.BackColor = Color.WhiteSmoke;
            }

            var audioCodes = GetAudioLanguageCodes(lvAudioTracks);

            if (columnName == "ExternalAudioType")
            {
                ExternalAudioFileCellStyle();
            }

            if (columnName == "ExternalAudioLanguage")
            {
                var langCell = dgv.Rows[e.RowIndex].Cells[e.ColumnIndex];
                var langValue = Convert.ToString(langCell.Value)?.Trim();

                if (!string.IsNullOrEmpty(langValue) && !audioCodes.Contains(langValue))
                {
                    UnknownLanguageOrLanguageNotConfiguredCellStyle();
                }
                else
                {
                    ExternalAudioFileCellStyle();
                }
            }

            // Check if the column is an Audio Lang column like "Audio0_Lang", "Audio1_Lang", ... ExternalAudioLanguage
            if (columnName.StartsWith("Audio") && columnName.EndsWith("_Lang") || columnName == "ExternalAudioLanguage")
            {
                // Extract the audio index from column name
                string indexStr = Regex.Replace(columnName, @"\D", ""); // Remove non-digits
                if (int.TryParse(indexStr, out int audioIndex))
                {
                    var langCell = dgv.Rows[e.RowIndex].Cells[e.ColumnIndex];
                    var langValue = Convert.ToString(langCell.Value)?.Trim();

                    var languageNotConfigured = !string.IsNullOrEmpty(langValue) && !audioCodes.Contains(langValue);

                    // Get the forced flag from hidden column
                    var forcedCell = dgv.Rows[e.RowIndex].Cells[$"IsForced{audioIndex}"];
                    var isForced = false;

                    if (forcedCell?.Value != null)
                    {
                        if (forcedCell.Value is bool b)
                        {
                            isForced = b;
                        }
                        else
                        {
                            bool.TryParse(forcedCell.Value.ToString(), out isForced);
                        }
                    }

                    if (languageNotConfigured)
                    {
                        UnknownLanguageOrLanguageNotConfiguredCellStyle();
                    }
                    else if (isForced)
                    {
                        ForcedTrackCellStyle();
                    }
                }
            }

            // Check if the column is an Subtile Lang column like "Sub0_Lang", "Sub1_Lang", ...
            if (columnName.StartsWith("Sub") && columnName.EndsWith("_Lang"))
            {
                var subtitlesCodes = GetAudioLanguageCodes(lvSubtitleTracks);

                // Extract the index number from column name
                // Example: "Sub0_Lang" -> 0
                string indexStr = Regex.Replace(columnName, @"\D", ""); //  // \D = non-digit
                if (int.TryParse(indexStr, out int audioIndex))
                {
                    var langCell = dgv.Rows[e.RowIndex].Cells[e.ColumnIndex];
                    var langValue = Convert.ToString(langCell.Value)?.Trim();

                    var languageNotConfigured = !string.IsNullOrEmpty(langValue) && !subtitlesCodes.Contains(langValue);

                    // Get the forced flag value from hidden column
                    var forcedCell = dgv.Rows[e.RowIndex].Cells[$"SubIsForced{audioIndex}"];
                    bool isForced = false;

                    if (forcedCell.Value != null)
                    {
                        if (forcedCell.Value is bool b)
                            isForced = b;
                        else
                            bool.TryParse(forcedCell.Value.ToString(), out isForced);
                    }

                    if (languageNotConfigured)
                    {
                        UnknownLanguageOrLanguageNotConfiguredCellStyle();
                    }
                    else if (isForced)
                    {
                        ForcedTrackCellStyle();
                    }
                }
            }

            var cell = _dataGridViewResults.Rows[e.RowIndex].Cells[e.ColumnIndex];

            if (cell.Tag?.ToString() == "edited")
            {
                e.CellStyle.Font = new Font(_dataGridViewResults.Font, FontStyle.Bold);
            }


            void UnknownLanguageOrLanguageNotConfiguredCellStyle()
            {
                e.CellStyle.BackColor = Color.Red;
                e.CellStyle.ForeColor = Color.White;
            }

            void ExternalAudioFileCellStyle()
            {
                e.CellStyle.BackColor = Color.AntiqueWhite;
            }

            void ForcedTrackCellStyle()
            {
                e.CellStyle.BackColor = Color.LightCoral;
                e.CellStyle.ForeColor = Color.Black;
            }
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
                    e.Result = RunRemuxProcess(context, _backgroundWorker, e);
                    break;
            }
        }

        private async Task<WorkerResult> LoadMkvFileInfosAsync(List<MkvFileInfo> files, BackgroundWorker worker, DoWorkEventArgs e)
        {
            var workerResult = new WorkerResult
            {
                TaskType = BackgroundTaskType.LoadDroppedFiles
            };

            var mkvMetadataExtractor = new MkvMetadataExtractor();

            for (int i = 0; i < files.Count; i++)
            {
                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                    return workerResult;
                }

                string file = files[i].FileName;

                try
                {
                    // Report starting progress for current file (more granular)
                    int startProgress = Math.Max(1, (int)((double)i / files.Count * 100));
                    var startProgressMessage = new ProgressMessage(
                        $"Processing file {i + 1} of {files.Count}: {Path.GetFileName(file)}",
                        string.Empty
                    );
                    worker.ReportProgress(startProgress, startProgressMessage);

                    // Add a small delay to make progress visible for fast operations
                    if (files.Count <= 3)
                    {
                        await Task.Delay(100);
                    }

                    // Report mid-progress for current file
                    int midProgress = Math.Max(startProgress + 1, (int)((double)(i + 0.5) / files.Count * 100));
                    var midProgressMessage = new ProgressMessage(
                        $"Analyzing file {i + 1} of {files.Count}: {Path.GetFileName(file)}",
                        string.Empty
                    );
                    worker.ReportProgress(midProgress, midProgressMessage);

                    var info = await mkvMetadataExtractor.ExtractInfoAsync(file);
                    workerResult.Files.Add(info);

                    // Report completion progress for current file
                    int endProgress = Math.Min(100, (int)((double)(i + 1) / files.Count * 100));
                    var endProgressMessage = new ProgressMessage(
                        $"Completed file {i + 1} of {files.Count}: {Path.GetFileName(file)}",
                        string.Empty
                    );
                    worker.ReportProgress(endProgress, endProgressMessage);

                    // Add a small delay to make completion visible
                    if (files.Count <= 3)
                    {
                        await Task.Delay(50);
                    }
                }
                catch (Exception ex)
                {
                    // Report error but continue with progress
                    int errorProgress = Math.Min(100, (int)((double)(i + 1) / files.Count * 100));
                    var errorProgressMessage = new ProgressMessage(
                        $"Error processing file {i + 1} of {files.Count}: {Path.GetFileName(file)}",
                        string.Empty
                    );
                    worker.ReportProgress(errorProgress, errorProgressMessage);

                    MsgBox.Show(this, $"Failed to read {file}: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            // Ensure we reach 100% at the end
            if (workerResult.Files.Count > 0)
            {
                var finalProgressMessage = new ProgressMessage(
                    $"Completed processing {workerResult.Files.Count} file(s)",
                    string.Empty
                );
                worker.ReportProgress(100, finalProgressMessage);
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
                                PopulateGrid(workerResult.Files.OrderBy(o => o.FilePath).ThenBy(o => o.FileName).ToList());
                                BuildTextView(workerResult.Files);

                                if (workerResult.Files.Select(x => x.FilePath).Distinct().Count() == 1)
                                {
                                    btbOutputPath.Text = Path.Combine(workerResult.Files.FirstOrDefault().FilePath, "Output");
                                    btbOutputPath.Enabled = true;
                                }
                                else
                                {
                                    btbOutputPath.Text = "\\Output";
                                    btbOutputPath.Enabled = false;
                                }
                            }

                            break;
                        case BackgroundTaskType.RemuxSelectedFiles:
                            var opRes = MkvmergeErrorChecker.CheckForErrors(tbOutput.Text);

                            if (opRes.HasErrors)
                            {
                                MsgBox.Show(this, $"mkvmerge had errors!\n\n{MkvmergeErrorChecker.GetSummary(opRes)}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                break;
                            }

                            if (opRes.HasWarnings)
                            {
                                MsgBox.Show(this, $"mkvmerge completed with warnings:\n\n{MkvmergeErrorChecker.GetSummary(opRes)}", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                break;
                            }

                            MsgBoxEnhanced.ShowAutoClose(this, "Operation completed successfully!", "Success",
                                MessageBoxButtons.OK, MessageBoxIcon.Information,
                                MessageBoxDefaultButton.Button1, timeoutSeconds: 5);

                            tcGrid.SelectTab(tpOutput);
                            tbOutput.SelectionStart = tbOutput.Text.Length;
                            tbOutput.SelectionLength = 0;
                            tbOutput.ScrollToCaret();
                            break;
                    }
                }
            }

            progressBar.Value = 0;
            pProgress.Visible = false;
        }

        private void PopulateGrid(List<MkvFileInfo> files)
        {
            _rowColorMap.Clear();

            _dataGridViewResults.Columns.Clear();
            _dataGridViewResults.Rows.Clear();

            int maxAudio = files.Max(f => f.AudioTracks.Count);
            int maxSubs = files.Max(f => f.SubtitleTracks.Count);

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
            _dataGridViewResults.Columns.Add(
                new DataGridViewTextBoxColumn
                {
                    Name = "Filename",
                    HeaderText = "File name",
                    Frozen = true
                });

            var showFilePathColumn = files.Select(x => x.FilePath).Distinct().Count() > 1;
            if (showFilePathColumn)
            {
                _dataGridViewResults.Columns.Add(
                    new DataGridViewTextBoxColumn
                    {
                        Name = "FilePath",
                        HeaderText = "File path"
                    });
            }

            var hasExternalAudio = files.FirstOrDefault(x => x.ExternalAudioTracks.Any()) != null;

            // External audio column
            if (hasExternalAudio)
            {
                _dataGridViewResults.Columns.Add($"ExternalAudioType", "External Audio Type");
                _dataGridViewResults.Columns.Add($"ExternalAudioLanguage", "External Audio Language");
            }


            // Audio columns
            for (int i = 0; i < maxAudio; i++)
            {
                _dataGridViewResults.Columns.Add($"Audio{i}_Lang", $"Audio{i + 1} Language");
                _dataGridViewResults.Columns.Add($"Audio{i}_Title", $"Audio{i + 1} Title");
                _dataGridViewResults.Columns.Add($"Audio{i}_CodecId", $"Audio{i + 1} Codec");
                _dataGridViewResults.Columns.Add($"Audio{i}_Ch", $"Audio{i + 1} Ch");
                _dataGridViewResults.Columns.Add($"Audio{i}_Bitrate", $"Audio{i + 1} Bitrate");

                var forcedCol = new DataGridViewTextBoxColumn
                {
                    Name = $"IsForced{i}",
                    Visible = false // hide the column
                };

                _dataGridViewResults.Columns.Add(forcedCol);
            }

            // Subtitle columns
            for (int i = 0; i < maxSubs; i++)
            {
                _dataGridViewResults.Columns.Add($"Sub{i}_Lang", $"Sub{i + 1} Lang");
                _dataGridViewResults.Columns.Add($"Sub{i}_Title", $"Sub{i + 1} Title");

                var forcedCol = new DataGridViewTextBoxColumn
                {
                    Name = $"SubIsForced{i}",
                    Visible = false
                };
                _dataGridViewResults.Columns.Add(forcedCol);
            }

            _dataGridViewResults.Columns.Add("Attachments", "Attachments");
            _dataGridViewResults.AddHeaderCheckBoxToColumn(0);



            // Rows
            foreach (var file in files)
            {
                var row = new List<object>
                {
                    true,
                    Path.GetFileName(file.FileName)
                };

                if (showFilePathColumn)
                {
                    row.Add(file.FilePath);
                }

                if (file.ExternalAudioTracks.Any())
                {
                    var eaf = file.ExternalAudioTracks.FirstOrDefault(); //only one is supported for display and process (todo more)

                    row.Add(eaf.Extension);
                    row.Add(eaf.Language);
                }
                else if (hasExternalAudio)
                {
                    row.Add(string.Empty);
                    row.Add(string.Empty);
                }


                foreach (var a in file.AudioTracks)
                {
                    row.Add(a.Language);
                    row.Add(a.Title);
                    row.Add(a.CodecId);
                    row.Add(a.Channels);
                    row.Add(a.BitRate.HasValue ? (a.BitRate.Value / 1000).ToString() : "");
                    row.Add(a.IsForced);
                }

                for (int i = file.AudioTracks.Count; i < maxAudio; i++)
                {
                    row.Add("");        //language
                    row.Add("");        //title
                    row.Add("");        //codec
                    row.Add("");        //channels
                    row.Add("");        //bitRate
                    row.Add("false");   //forced                    
                }

                foreach (var s in file.SubtitleTracks)
                {
                    row.Add(s.Language);
                    row.Add(s.Title);
                    row.Add(s.IsForced);
                }

                for (var i = file.SubtitleTracks.Count; i < maxSubs; i++)
                {
                    row.Add("");        //language
                    row.Add("");        //title
                    row.Add("false");   //forced
                }


                row.Add(file.Attachments.Count);

                var dgRow = _dataGridViewResults.Rows[_dataGridViewResults.Rows.Add(row.ToArray())];
                dgRow.Tag = file;
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

            foreach (DataGridViewColumn gridCol in _dataGridViewResults.Columns)
            {
                if (gridCol is DataGridViewCheckBoxColumn)
                {
                    continue; // Skip checkbox column
                }

                gridCol.ReadOnly = true;
            }

            var invalidRowCells = CountInvalidLanguageCodes();
            if (invalidRowCells.Count > 0)
            {
                MsgBox.Show(this, $"{invalidRowCells.Count} files have audio tracks with unrecognized language codes!\n\nThe cells can be manually edited or a mass update option can be triggered by right clicking the columns headers.", "Validation Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                //making the cells having issues editable
                foreach (var kvp in invalidRowCells)
                {
                    var row = _dataGridViewResults.Rows[kvp.Key];

                    foreach (int cellIndex in kvp.Value)
                    {
                        var cell = row.Cells[cellIndex];
                        cell.ReadOnly = false;
                    }
                }
            }

            _dataGridViewResults.ResumeLayout();

            if (showFilePathColumn)
            {
                RebuildRowColorMap();
            }
        }

        private Dictionary<int, Color> _rowColorMap = [];

        private void RebuildRowColorMap()
        {
            string lastPath = null;
            bool toggle = false; // false: yellow, true: light green

            for (int i = 0; i < _dataGridViewResults.Rows.Count; i++)
            {
                var row = _dataGridViewResults.Rows[i];
                var currentPath = Convert.ToString(row.Cells["FilePath"].Value)?.Trim();

                if (!string.Equals(currentPath, lastPath, StringComparison.OrdinalIgnoreCase))
                {
                    toggle = !toggle;
                    lastPath = currentPath;
                }

                //_rowColorMap[i] = toggle ? Color.LightGreen : Color.LightYellow;
                _rowColorMap[i] = toggle ? Color.FromArgb(244, 243, 238) : Color.FromArgb(217, 226, 225);
            }
        }

        private Dictionary<int, List<int>> CountInvalidLanguageCodes()
        {
            var result = new Dictionary<int, List<int>>();
            var validCodes = GetAudioLanguageCodes(lvAudioTracks);

            foreach (DataGridViewRow row in _dataGridViewResults.Rows)
            {
                foreach (DataGridViewColumn col in _dataGridViewResults.Columns)
                {
                    if ((col.Name.StartsWith("Audio") && col.Name.EndsWith("_Lang")) || col.Name == "ExternalAudioLanguage")
                    {
                        var langValue = Convert.ToString(row.Cells[col.Index].Value)?.Trim();

                        if (!string.IsNullOrEmpty(langValue) && !validCodes.Contains(langValue))
                        {
                            if (!result.ContainsKey(row.Index))
                            {
                                result[row.Index] = new List<int>();
                            }

                            result[row.Index].Add(col.Index);
                        }
                    }
                }
            }

            return result;
        }

        private void BuildTextView(List<MkvFileInfo> files)
        {
            var sb = new StringBuilder();

            foreach (var file in files)
            {
                sb.AppendLine($"File: {Path.GetFileName(file.FileName)}");
                if (_appOptions.ReadFilesRecursively)
                {
                    sb.AppendLine($"Path: {Path.GetDirectoryName(file.FileName)}");
                }

                foreach (var audio in file.AudioTracks)
                {
                    var channels = audio.Channels switch
                    {
                        1 => "1.0",
                        2 => "2.0",
                        6 => "5.1",
                        8 => "7.1",
                        _ => $"{audio.Channels}.0"
                    };

                    var bitrate = audio.BitRate.HasValue ? $"{audio.BitRate.Value / 1000} kbps" : "unknown bitrate";
                    sb.AppendLine($"  Audio [{audio.Language}]: {channels} @ {bitrate}" +
                        (string.IsNullOrEmpty(audio.Title) ? "" : $" - {audio.Title}"));
                }

                foreach (var sub in file.SubtitleTracks)
                {
                    sb.AppendLine($"  Subtitle [{sub.Language}]: {sub.Title}");
                }

                foreach (var att in file.Attachments)
                {
                    sb.AppendLine($"  Attachment: {att.FileName} ({att.MimeType})");
                }

                sb.AppendLine();
            }

            txtFilesDetails.Text = sb.ToString();
        }


        private void FrmMain_FormClosing(object? sender, FormClosingEventArgs e)
        {
            _appOptions.SaveFormSettings(this);
        }

        private void FrmMain_Load(object? sender, EventArgs e)
        {
            _appOptions.LoadFormSettings(this);
            bRemux.Enabled = CheckToolsInPath();
            btbOutputPath.Enabled = !_appOptions.ReadFilesRecursively;
        }

        private bool CheckToolsInPath()
        {
            var result = true;

            var exePath = GetExecutablePath("mkvmerge.exe");

            if (!string.IsNullOrEmpty(exePath))
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(exePath);
                lbMkvVersion.Text = $"mkvmerge found! Version: {versionInfo.FileVersion}";
            }
            else
            {
                lbMkvVersion.Text = $"mkvmerge not found!";
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

                    lbFFprobeVersion.Text = $"ffprobe found! Version: {version}";
                }
            }
            else
            {
                lbFFprobeVersion.Text = $"ffprobe not found!";
                lbFFprobeVersion.ForeColor = Color.Red;
                result = false;
            }

            exePath = GetExecutablePath("MediaInfo.exe");

            if (!string.IsNullOrEmpty(exePath))
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "MediaInfo",
                    Arguments = "--Version", // ffprobe prints version info here
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    // First line looks like: "ffprobe version 6.1.1 ..."
                    string versionLine = output.Split('\n').FirstOrDefault(line => line.StartsWith("MediaInfoLib"));
                    string version = versionLine?.Split(' ')[2] ?? "Unknown";

                    lbMediaInfoCliVersion.Text = $"MediaInfo CLI found! Version: {version}";
                }
            }
            else
            {
                lbMediaInfoCliVersion.Text = $"MediaInfo CLI not found!";
                lbMediaInfoCliVersion.ForeColor = Color.Red;
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
            toolTip.SetToolTip(chkReadFilesRecursively, "When a folder is dragged onto the main form, and the above option is checked, all files from that folder and its subfolders are added to the processing list.\r\nA new column is added to the grid to display the full file path.\r\nThe output folder selection is disabled in this mode. Instead, remuxed files are saved in an \"Output\" subfolder within each original file's folder.\r\n");
            toolTip.SetToolTip(chkRemoveUnlistedLanguageTracks, "Then checked, this option will remove from the original file all tracks that have a language code not present among those configured.\r\nIf unchecked, those traks will be moved at the end of the track list, unsorted.\r\n");
            toolTip.SetToolTip(chkApplyNamingConventions, "Replace \" !\" with \"!\"");
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

            const int verticalScrollbarWidth = 17;
            listView.Columns[0].Width = listView.ClientSize.Width - codeWidth - delWidth - verticalScrollbarWidth;

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

            InitializeListViewContextMenu(listView);
            LoadListFromXml(listView);
        }

        private void InitializeListViewContextMenu(ListView listView)
        {
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Set default").Click += (s, e) => SetDefaultAudioLanguage(listView);

            if (listView.Name == "lvSubtitleTracks")
            {
                contextMenu.Items.Add("-");
                contextMenu.Items.Add("No default").Click += (s, e) => SetNoDefault(listView);
            }

            listView.MouseDown += (sender, e) => ListView_MouseDown(listView, e, contextMenu);
        }

        private void ListView_MouseDown(object sender, MouseEventArgs e, ContextMenuStrip contextMenu)
        {
            var listView = sender as ListView;

            if (e.Button == MouseButtons.Right && listView != null)
            {
                var info = listView.HitTest(e.Location);
                listView.FocusedItem = info.Item;
                listView.ContextMenuStrip = info.Item != null ? contextMenu : null;
            }
        }

        private void SetDefaultAudioLanguage(ListView listView)
        {
            if (listView?.FocusedItem != null)
            {
                foreach (ListViewItem item in listView.Items)
                {
                    item.Font = listView.Font;
                }

                listView.FocusedItem.Font = new Font(listView.Font, FontStyle.Bold);
                SaveListToXml();
            }
        }

        private void SetNoDefault(ListView listView)
        {
            foreach (ListViewItem item in listView.Items)
            {
                item.Font = listView.Font;
            }

            SaveListToXml();
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
            {
                return;
            }

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
                serverLv.Items.Remove(hit.Item);
                SaveListToXml();
                _dataGridViewResults.Invalidate();
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

        private void AddLanguageToListView(ListView targetListView, LanguageObject lang, bool isDefault)
        {
            var item = new ListViewItem(lang.Name);
            item.SubItems.Add(lang.Abr3a);

            var delItem = new ListViewSubItem(item, "✕")
            {
            };

            if (isDefault)
            {
                item.Font = new Font(targetListView.Font, FontStyle.Bold);
            }

            item.UseItemStyleForSubItems = false;
            item.SubItems.Add(delItem);
            item.Tag = lang.Abr3a;
            targetListView.Items.Add(item);
        }

        private void AddLanguageToListView(ListView targetListView, LanguageObject[] langs)
        {
            foreach (var lang in langs)
            {
                AddLanguageToListView(targetListView, lang, isDefault: false);
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


            using var addLanguageForm = new FrmAddLanguage(selectedCodes, "Select a language from the list below to add it to your configuration:");
            PositionModal(addLanguageForm, (LinkLabel)sender, this, 10);

            if (addLanguageForm.ShowDialog() == DialogResult.OK)
            {
                AddLanguageToListView(targetListView, addLanguageForm.SelectedLanguage, isDefault: false);
                SaveListToXml();
                _dataGridViewResults.Invalidate();
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
                        string name = langElement.Attribute("Name")?.Value ?? string.Empty;
                        string code = langElement.Attribute("Code")?.Value ?? string.Empty;
                        bool isDefault = false;

                        if (bool.TryParse(langElement.Attribute("IsDefault")?.Value, out bool parsed))
                            isDefault = parsed;

                        AddLanguageToListView(targetListView, new LanguageObject(name, code), isDefault);
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
            lvAudioTracks.Items[0].Font = new Font(lvAudioTracks.Font, FontStyle.Bold);

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
                                new XAttribute("Code", item.Tag?.ToString() ?? string.Empty),
                                new XAttribute("IsDefault", item.Font.Bold ? "true" : "false")
                            )
                        )
                    ),
                    new XElement("lvSubtitleTracks",
                        lvSubtitleTracks.Items.Cast<ListViewItem>().Select(item =>
                            new XElement("Language",
                                new XAttribute("Name", item.Text),
                                new XAttribute("Code", item.Tag?.ToString() ?? string.Empty),
                                new XAttribute("IsDefault", item.Font.Bold ? "true" : "false")
                            )
                        )
                    )
                )
            );

            doc.Save("config.xml"); // Save near the EXE
        }

        private void InitOptions()
        {
            chkReadFilesRecursively.Checked = _appOptions.ReadFilesRecursively;
            chkDeleteOriginal.Checked = _appOptions.DeleteOriginalsAfterSuccessfulRemux;
            chkRemoveUnlistedLanguageTracks.Checked = _appOptions.RemoveUnlistedLanguageTracks;
            chkApplyNamingConventions.Checked = _appOptions.ApplyNamingConventions;

            chkReadFilesRecursively.CheckedChanged += (s, e) =>
            {
                _appOptions.ReadFilesRecursively = chkReadFilesRecursively.Checked;
                _appOptions.SaveOptions();
            };

            chkDeleteOriginal.CheckedChanged += (s, e) =>
            {
                _appOptions.DeleteOriginalsAfterSuccessfulRemux = chkDeleteOriginal.Checked;
                _appOptions.SaveOptions();
            };

            chkRemoveUnlistedLanguageTracks.CheckedChanged += (s, e) =>
            {
                _appOptions.RemoveUnlistedLanguageTracks = chkRemoveUnlistedLanguageTracks.Checked;
                _appOptions.SaveOptions();
            };

            chkApplyNamingConventions.CheckedChanged += (s, e) =>
            {
                _appOptions.ApplyNamingConventions = chkApplyNamingConventions.Checked;
                _appOptions.SaveOptions();
            };
        }

        private List<MkvFileInfo> GetCheckedFilenames()
        {
            return _dataGridViewResults.Rows
                .Cast<DataGridViewRow>()
                .Where(row => Convert.ToBoolean(row.Cells["Selected"].Value))
                .Select(row => (MkvFileInfo)row.Tag)
                .ToList();
        }

        private void UpdateFieldByColumnName(List<MkvFileInfo> files, string newValue)
        {
            foreach (DataGridViewRow row in _dataGridViewResults.Rows)
            {
                if (row.Tag is not MkvFileInfo file || !files.Contains(file))
                    continue;

                // === Update object ===
                if (_clickedColumnNameForContext == "ExternalAudioLanguage")
                {
                    var extAudio = file.ExternalAudioTracks.FirstOrDefault();
                    if (extAudio != null)
                    {
                        SetPropertyValue(extAudio, "Language", newValue);
                    }
                }
                else
                {
                    var match = Regex.Match(_clickedColumnNameForContext, @"^(Audio|Sub)(\d+)_(\w+)$");
                    if (!match.Success)
                        continue;

                    string listType = match.Groups[1].Value;
                    int index = int.Parse(match.Groups[2].Value);
                    string propertyName = match.Groups[3].Value;

                    object? targetItem = null;

                    if (listType == "Audio" && index < file.AudioTracks.Count)
                    {
                        targetItem = file.AudioTracks[index];
                    }
                    else if (listType == "Sub" && index < file.SubtitleTracks.Count)
                    {
                        targetItem = file.SubtitleTracks[index];
                    }

                    if (targetItem != null)
                    {
                        SetPropertyValue(targetItem, propertyName, newValue);
                    }
                }

                // === Update grid cell ===
                if (_dataGridViewResults.Columns.Contains(_clickedColumnNameForContext))
                {
                    var cell = row.Cells[_clickedColumnNameForContext];
                    cell.Value = newValue;

                    // Optional: visually mark it as updated (bold)
                    cell.Style.Font = new Font(_dataGridViewResults.Font, FontStyle.Bold);
                    cell.Tag = "edited";
                }
            }
        }

        private void SetPropertyValue(object target, string propertyName, string? value)
        {
            var prop = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop == null || !prop.CanWrite) return;

            object? typedValue = value;

            try
            {
                if (prop.PropertyType == typeof(int) && int.TryParse(value, out int intVal))
                    typedValue = intVal;
                else if (prop.PropertyType == typeof(bool) && bool.TryParse(value, out bool boolVal))
                    typedValue = boolVal;
                else if (prop.PropertyType == typeof(int?) && int.TryParse(value, out int nullableInt))
                    typedValue = nullableInt;
                else if (prop.PropertyType != typeof(string) && string.IsNullOrWhiteSpace(value))
                    typedValue = null;

                prop.SetValue(target, typedValue);
            }
            catch
            {
                // Log or ignore invalid conversion
            }
        }

        private WorkerResult RunRemuxProcess(BackgroundWorkerContext context, BackgroundWorker worker, DoWorkEventArgs e)
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
            Process? currentProcess = null;

            for (int i = 0; i < selectedFiles.Count; i++)
            {
                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                    try { currentProcess.Kill(); } catch { }
                    return workerResult;
                }

                var currentFile = selectedFiles[i];

                sb.AppendLine("Input file:");
                sb.AppendLine(currentFile.FileName);
                //sb.AppendLine("Output file:");
                //sb.AppendLine(currentFile.OutputFileName);

                // Initial progress report for starting this file
                var initialProgressMessage = new ProgressMessage(
                    $"Processing file {i + 1} of {selectedFiles.Count}: {Path.GetFileName(currentFile.FileName)}",
                    sb.ToString()
                );

                int initialProgress = CalculateOverallProgress(i, selectedFiles.Count, 0);
                _backgroundWorker.ReportProgress(initialProgress, initialProgressMessage);

                try
                {
                    var mkvMA = remuxHelper.BuildMkvMergeArgs(currentFile);
                    sb.AppendLine("Arguments:");
                    sb.AppendLine(mkvMA.arguments);
                    sb.AppendLine();

                    var stdoutBuilder = new StringBuilder();
                    var stderrBuilder = new StringBuilder();

                    var psi = new ProcessStartInfo
                    {
                        FileName = Common.MkvMergePath,
                        Arguments = mkvMA.arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8
                    };

                    currentProcess = new Process { StartInfo = psi };

                    currentProcess.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data != null)
                        {
                            // Append or process output line
                            stdoutBuilder.AppendLine(e.Data);

                            // Check for cancellation
                            if (worker.CancellationPending)
                            {
                                try
                                {
                                    stdoutBuilder.AppendLine("Process cancelled by user!");
                                    currentProcess.Kill(entireProcessTree: true);
                                }
                                catch { }
                            }

                            // Check for progress updates
                            if (e.Data.StartsWith("Progress: ") && e.Data.EndsWith("%"))
                            {
                                string progressText = e.Data.Substring(10, e.Data.Length - 11);
                                if (int.TryParse(progressText, out int fileProgressValue))
                                {
                                    // Calculate overall progress
                                    int overallProgress = CalculateOverallProgress(i, selectedFiles.Count, fileProgressValue);

                                    // Create progress message with current file progress
                                    var progressMessage = new ProgressMessage(
                                        $"Processing file {i + 1} of {selectedFiles.Count}: {Path.GetFileName(currentFile.FileName)} ({fileProgressValue}%)",
                                        sb.ToString()
                                    );

                                    _backgroundWorker.ReportProgress(overallProgress, progressMessage);
                                }
                            }
                        }
                    };

                    currentProcess.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data != null) stderrBuilder.AppendLine(e.Data);
                    };

                    currentProcess.Start();
                    currentProcess.BeginOutputReadLine();
                    currentProcess.BeginErrorReadLine();
                    currentProcess.WaitForExit();

                    string stdout = stdoutBuilder.ToString();
                    string stderr = stderrBuilder.ToString();

                    if (currentProcess.ExitCode != 0)
                    {
                        sb.AppendLine($"[ERROR] mkvmerge exited with code {currentProcess.ExitCode}");
                        sb.AppendLine();

                        if (!string.IsNullOrWhiteSpace(stderr))
                        {
                            sb.AppendLine(stderr);
                        }

                        if (!string.IsNullOrWhiteSpace(stdout))
                        {
                            sb.AppendLine(stdout);
                        }
                    }
                    else
                    {
                        sb.AppendLine("Remux completed successfully.");

                        if (!string.IsNullOrWhiteSpace(stdout))
                        {
                            sb.AppendLine(stdout);
                        }

                        if (_appOptions.DeleteOriginalsAfterSuccessfulRemux)
                        {
                            File.Delete(currentFile.FileName);
                            sb.AppendLine($"The original file \"{currentFile.FileName}\" has been deleted!");
                        }

                        if (_appOptions.ApplyNamingConventions)
                        {
                            ApplyNamingConventions(mkvMA.outputFilePath, sb);
                            MoveSourcesTxtFileToOutputFolder(currentFile.FilePath, mkvMA.outputFilePath, sb);
                        }
                    }
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"[EXCEPTION] {ex.Message}");

                }

                sb.AppendLine(new string('-', 60));

                // Final progress report for completed file
                var finalProgressMessage = new ProgressMessage(
                    $"Completed file {i + 1} of {selectedFiles.Count}: {Path.GetFileName(currentFile.FileName)}",
                    sb.ToString()
                );

                int finalProgress = CalculateOverallProgress(i, selectedFiles.Count, 100);
                _backgroundWorker.ReportProgress(finalProgress, finalProgressMessage);
            }

            return workerResult;
        }

        private void ApplyNamingConventions(string outputFilePath, StringBuilder sb)
        {
            try
            {
                // Get directory, filename, and extension
                var directory = Path.GetDirectoryName(outputFilePath);
                var fileName = Path.GetFileName(outputFilePath);
                var newFileName = fileName.Replace(" !", "!");

                // Only rename if there's an actual change
                if (fileName != newFileName)
                {
                    string newFilePath = Path.Combine(directory, newFileName);
                    File.Move(outputFilePath, newFilePath);
                }

                sb.AppendLine($"The naming conventions were applied to \"{outputFilePath}\"!");
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Error: Failed to apply naming conventions to: \"{outputFilePath}\"\n\n{ex.Message}");
            }
        }

        private void MoveSourcesTxtFileToOutputFolder(string inputFilePath, string outputFilePath, StringBuilder sb)
        {
            try
            {
                //move the "Video source.txt" file to "Output\source.txt"
                var videSourceFullFilePath = Path.Combine(inputFilePath, "Video sources.txt");
                if (File.Exists(videSourceFullFilePath))
                {
                    var sourceFullFilePath = Path.Combine(Path.GetDirectoryName(outputFilePath), "sources.txt");
                    File.Move(videSourceFullFilePath, sourceFullFilePath);

                    sb.AppendLine("Moved the \"Video source.txt\" file to output folder!");
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Error: Failed to move \"Video source.txt\" file to output folder!\n\n{ex.Message}");
            }
        }


        private int CalculateOverallProgress(int currentFileIndex, int totalFiles, int currentFileProgress)
        {
            // Each file represents (100 / totalFiles)% of the total work
            double progressPerFile = 100.0 / totalFiles;

            // Progress from completed files
            double completedFilesProgress = currentFileIndex * progressPerFile;

            // Progress from current file
            double currentFileContribution = (currentFileProgress / 100.0) * progressPerFile;

            // Total progress
            int totalProgress = (int)Math.Round(completedFilesProgress + currentFileContribution);

            return Math.Min(totalProgress, 100); // Ensure we don't exceed 100%
        }
    }
}
