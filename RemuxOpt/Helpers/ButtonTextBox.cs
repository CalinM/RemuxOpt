using System.Runtime.InteropServices;

namespace RemuxOpt
{
    public class ButtonTextBox : TextBox
    {
        private Button _button;
        
        public ButtonTextBox()
        {
            _button = new Button
            {
                Dock = DockStyle.Right,
                BackColor = Color.WhiteSmoke,
                Width = 24,
                Cursor = Cursors.Arrow,
                FlatStyle = FlatStyle.Flat,
                Padding = new Padding(0),
                Margin = new Padding(0),
                Text = "...",
            };

            _button.FlatAppearance.BorderSize = 0; // Optional: hide default border
            _button.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics,
                    _button.ClientRectangle,
                    Color.Silver, ButtonBorderStyle.Solid); // Use desired color
            };
            
            Controls.Add(_button);
        }

        public event EventHandler ButtonClick
        {
            add { _button.Click += value; }
            remove { _button.Click -= value; }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == 0x30) // WM_SETFONT
            {
                var padding = _button.Width + 3;
                SendMessage(this.Handle, EM_SETMARGINS, (IntPtr)2, (IntPtr)(padding << 16));
            }
        }

        private const int EM_SETMARGINS = 0xD3;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    }
}