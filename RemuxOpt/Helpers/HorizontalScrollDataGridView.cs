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

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            // Check if the Shift key is pressed: horizontal scrolling
            if ((ModifierKeys & Keys.Shift) == Keys.Shift)
            {
                // Default vertical scrolling
                base.OnMouseWheel(e);
            }
            else
            {
                // Scroll horizontally
                int scrollAmount = e.Delta > 0 ? -1 : 1;
                int newOffset = HorizontalScrollingOffset + scrollAmount * 30; // Adjust multiplier if needed
                newOffset = Math.Max(0, newOffset);
                HorizontalScrollingOffset = newOffset;
            }
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
            if (CurrentCell is DataGridViewCheckBoxCell && this.IsCurrentCellDirty)
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
