namespace RemuxOpt
{
    public class HorizontalScrollDataGridView : DataGridView
    {
        private CheckBox _headerCheckBox;
        private int _checkboxColumnIndex = -1;
        private bool _internalChange = false;

        public bool HeaderCheckState
        {
            get => _headerCheckBox?.Checked ?? false;
            set
            {
                if (_headerCheckBox != null)
                    _headerCheckBox.Checked = value;
            }
        }

        /*
        Horizontal Scrollbar Visible        Ctrl Key Pressed        Action
        ✅ Yes                              ❌ No                   👉 Horizontal scroll (default)
        ✅ Yes                              ✅ Yes                   ⬆️ Vertical scroll (override)
        ❌ No                               Doesn't matter           ⬆️ Vertical scroll (fallback)
         */
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            bool ctrlPressed = (ModifierKeys & Keys.Control) == Keys.Control;

            // Check if horizontal scroll is possible
            bool horizontalScrollVisible = Columns.GetColumnsWidth(DataGridViewElementStates.Visible) > ClientSize.Width;

            if (!horizontalScrollVisible || ctrlPressed)
            {
                // Force vertical scroll manually
                try
                {
                    int linesToScroll = SystemInformation.MouseWheelScrollLines;
                    int direction = e.Delta > 0 ? -1 : 1; // Mouse up: scroll up (row index decreases)

                    int newIndex = FirstDisplayedScrollingRowIndex + direction * linesToScroll;
                    newIndex = Math.Max(0, Math.Min(RowCount - 1, newIndex));

                    FirstDisplayedScrollingRowIndex = newIndex;
                }
                catch
                {
                    // Ignore if no rows or index out of range
                }
            }
            else
            {
                // Horizontal scroll
                int scrollAmount = e.Delta > 0 ? -1 : 1;
                int newOffset = HorizontalScrollingOffset + scrollAmount * 30;
                HorizontalScrollingOffset = Math.Max(0, newOffset);
            }
        }

        /*
        There is a known quirk in DataGridView: when you press Home, it selects the first column but doesn’t scroll horizontally — unlike End,
        which does scroll right. This is default behavior and happens even in a plain DataGridView.
        */
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Home)
            {
                // Scroll all the way to the left
                HorizontalScrollingOffset = 0;

                // Optional: Move selection to first visible cell in the current row
                if (CurrentCell != null)
                {
                    int rowIndex = CurrentCell.RowIndex;
                    foreach (DataGridViewColumn col in Columns)
                    {
                        if (col.Visible)
                        {
                            CurrentCell = this[col.Index, rowIndex];
                            break;
                        }
                    }
                }

                return true; // Mark as handled
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        public HorizontalScrollDataGridView()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

            CellValueChanged += HorizontalScrollDataGridView_CellValueChanged;
            CurrentCellDirtyStateChanged += HorizontalScrollDataGridView_CurrentCellDirtyStateChanged;
            ColumnWidthChanged += (s, e) => RepositionHeaderCheckBox();
            Scroll += (s, e) => RepositionHeaderCheckBox();
        }

        public void AddHeaderCheckBoxToColumn(int columnIndex)
        {
            if (columnIndex < 0 || columnIndex >= Columns.Count)
                throw new ArgumentOutOfRangeException(nameof(columnIndex));

            _checkboxColumnIndex = columnIndex;

            if (_headerCheckBox != null)
            {
                Controls.Remove(_headerCheckBox);
                _headerCheckBox.Dispose();
            }

            _headerCheckBox = new CheckBox
            {
                Size = new Size(15, 15),
                BackColor = Color.Transparent,
                Padding = new Padding(0),
                Margin = new Padding(),
                Text = ""
            };

            _headerCheckBox.CheckedChanged += HeaderCheckBox_CheckedChanged;

            Controls.Add(_headerCheckBox);
            RepositionHeaderCheckBox();
        }

        private void RepositionHeaderCheckBox()
        {
            if (_headerCheckBox == null || _checkboxColumnIndex < 0 || _checkboxColumnIndex >= Columns.Count)
            { 
                return;
            }

            Rectangle headerRect = GetCellDisplayRectangle(_checkboxColumnIndex, -1, true);
            _headerCheckBox.Location = new Point(
                headerRect.X + (headerRect.Width - _headerCheckBox.Width) / 2,
                headerRect.Y + (headerRect.Height - _headerCheckBox.Height) / 2
            );
        }

        private void HeaderCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (_internalChange)
            { 
                return;
            }

            CurrentCell = null;

            _internalChange = true;

            foreach (DataGridViewRow row in Rows)
            {
                if (!row.IsNewRow)
                { 
                    row.Cells[_checkboxColumnIndex].Value = _headerCheckBox.Checked;
                }
            }

            _internalChange = false;
        }

        private void HorizontalScrollDataGridView_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (CurrentCell is DataGridViewCheckBoxCell && IsCurrentCellDirty)
            {
                CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void HorizontalScrollDataGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_internalChange)
            { 
                return;
            }

            if (e.ColumnIndex == _checkboxColumnIndex && e.RowIndex >= 0)
            {
                UpdateHeaderCheckboxState();
            }
        }

        private void UpdateHeaderCheckboxState()
        {
            if (_headerCheckBox == null || _checkboxColumnIndex < 0)
                return;

            var checkedCount = 0;
            var rowCount = 0;

            foreach (DataGridViewRow row in Rows)
            {
                if (!row.IsNewRow)
                {
                    rowCount++;
                    bool isChecked = Convert.ToBoolean(row.Cells[_checkboxColumnIndex].Value);
                    if (isChecked)
                        checkedCount++;
                }
            }

            _internalChange = true;
            if (checkedCount == 0)
            { 
                _headerCheckBox.CheckState = CheckState.Unchecked;
            }
            else if (checkedCount == rowCount)
            { 
                _headerCheckBox.CheckState = CheckState.Checked;
            }
            else
            { 
                _headerCheckBox.CheckState = CheckState.Indeterminate;
            }

            _internalChange = false;
        }

        public void ClearWithHeaderCheckboxCleanup()
        {
            SuspendLayout();

            // Remove the header checkbox if it exists
            if (_headerCheckBox != null)
            {
                Controls.Remove(_headerCheckBox);
                _headerCheckBox.Dispose();
                _headerCheckBox = null;
                _checkboxColumnIndex = -1;
            }

            Rows.Clear();
            Columns.Clear();
            ResumeLayout();
        }
    }
}