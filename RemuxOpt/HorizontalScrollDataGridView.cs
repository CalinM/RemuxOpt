using System;
using System.Drawing;
using System.Windows.Forms;

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

        public HorizontalScrollDataGridView()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

            this.CellValueChanged += HorizontalScrollDataGridView_CellValueChanged;
            this.CurrentCellDirtyStateChanged += HorizontalScrollDataGridView_CurrentCellDirtyStateChanged;
            this.ColumnWidthChanged += (s, e) => RepositionHeaderCheckBox();
            this.Scroll += (s, e) => RepositionHeaderCheckBox();
        }

        public void AddHeaderCheckBoxToColumn(int columnIndex)
        {
            if (columnIndex < 0 || columnIndex >= this.Columns.Count)
                throw new ArgumentOutOfRangeException(nameof(columnIndex));

            _checkboxColumnIndex = columnIndex;

            if (_headerCheckBox != null)
            {
                this.Controls.Remove(_headerCheckBox);
                _headerCheckBox.Dispose();
            }

            _headerCheckBox = new CheckBox
            {
                Size = new Size(15, 15),
                BackColor = Color.Transparent,
                Padding = new Padding(0),
                Margin = new Padding(0),
                Text = ""
            };

            _headerCheckBox.CheckedChanged += HeaderCheckBox_CheckedChanged;

            this.Controls.Add(_headerCheckBox);
            RepositionHeaderCheckBox();
        }

        private void RepositionHeaderCheckBox()
        {
            if (_headerCheckBox == null || _checkboxColumnIndex < 0 || _checkboxColumnIndex >= this.Columns.Count)
                return;

            Rectangle headerRect = this.GetCellDisplayRectangle(_checkboxColumnIndex, -1, true);
            _headerCheckBox.Location = new Point(
                headerRect.X + (headerRect.Width - _headerCheckBox.Width) / 2,
                headerRect.Y + (headerRect.Height - _headerCheckBox.Height) / 2
            );
        }

        private void HeaderCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (_internalChange)
                return;

            this.CurrentCell = null;

            _internalChange = true;

            foreach (DataGridViewRow row in this.Rows)
            {
                if (!row.IsNewRow)
                    row.Cells[_checkboxColumnIndex].Value = _headerCheckBox.Checked;
            }

            _internalChange = false;
        }

        private void HorizontalScrollDataGridView_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (this.CurrentCell is DataGridViewCheckBoxCell && this.IsCurrentCellDirty)
            {
                this.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void HorizontalScrollDataGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_internalChange)
                return;

            if (e.ColumnIndex == _checkboxColumnIndex && e.RowIndex >= 0)
            {
                UpdateHeaderCheckboxState();
            }
        }

        private void UpdateHeaderCheckboxState()
        {
            if (_headerCheckBox == null || _checkboxColumnIndex < 0)
                return;

            int checkedCount = 0;
            int rowCount = 0;

            foreach (DataGridViewRow row in this.Rows)
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
                _headerCheckBox.CheckState = CheckState.Unchecked;
            else if (checkedCount == rowCount)
                _headerCheckBox.CheckState = CheckState.Checked;
            else
                _headerCheckBox.CheckState = CheckState.Indeterminate;
            _internalChange = false;
        }
    }
}
