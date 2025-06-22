using System.Runtime.InteropServices;
using System.Text;

    using System;
using System.Drawing;
using System.Windows.Forms;


namespace RemuxOpt
{
    public static class MsgBox
    {
        /*
            Usages:

                MsgBox.Show(
                    this,
                    "Something went wrong.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
         
            
                var font = new Font("Segoe UI", 10F, FontStyle.Bold);
                MsgBox.Show(
                    this,
                    "Styled message",
                    "Font Test",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information,
                    MessageBoxDefaultButton.Button1,
                    font
                );


                MsgBox.ShowAutoClose(
                    this,
                    "This will close automatically.",
                    "Auto-Close",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information,
                    MessageBoxDefaultButton.Button1,
                    timeoutSeconds: 5
                );
        */

        private static HookProc _hookProc;
        private static IntPtr _hHook = IntPtr.Zero;
        private static string _expectedTitle;
        private static Font? _customFont;
        private static System.Threading.Timer _countdownTimer;
        private static IntPtr _dialogHandle = IntPtr.Zero;
        private static string _originalMessage;
        private static int _remainingSeconds;

        // Public API
        public static DialogResult Show(IWin32Window owner, string text, string title,
            MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.None,
            MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button1, Font customFont = null)
        {
            _expectedTitle = title;
            _customFont = customFont;
            _hookProc = (nCode, wParam, lParam) => HookCallbackWithTimeout(nCode, wParam, lParam);

            _hHook = SetWindowsHookEx(WH_CBT, _hookProc, IntPtr.Zero, GetCurrentThreadId());

            var result = MessageBox.Show(owner, text, title, buttons, icon, defaultButton);

            SafeUnhook();
            _customFont = null;

            GC.KeepAlive(_hookProc); // prevents premature GC

            return result;
        }

        public static DialogResult ShowAutoClose(IWin32Window owner, string text, string title,
            MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.None,
            MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button1, int timeoutSeconds = 10,
            Font customFont = null)
        {
            _expectedTitle = title;
            _customFont = customFont;
            _originalMessage = text;
            _remainingSeconds = timeoutSeconds;
            _hookProc = (nCode, wParam, lParam) => HookCallbackWithTimeout(nCode, wParam, lParam, timeoutSeconds);

            _hHook = SetWindowsHookEx(WH_CBT, _hookProc, IntPtr.Zero, GetCurrentThreadId());

            // Add countdown to initial message
            string messageWithCountdown = $"{text}\n\nThis dialog will close in {timeoutSeconds} seconds.";
            var result = MessageBox.Show(owner, messageWithCountdown, title, buttons, icon, defaultButton);

            SafeUnhook();
            SafeDisposeTimer();
            _customFont = null;
            _dialogHandle = IntPtr.Zero;

            GC.KeepAlive(_hookProc); // prevents premature GC

            return result;
        }

        private static void SafeUnhook()
        {
            if (_hHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hHook);
                _hHook = IntPtr.Zero;
            }
        }

        private static void SafeDisposeTimer()
        {
            _countdownTimer?.Dispose();
            _countdownTimer = null;
        }

        // Win32 API imports
        private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        private const int WH_CBT = 5;
        private const int HCBT_ACTIVATE = 5;
        private const int WM_SETFONT = 0x30;
        private const int WM_SETTEXT = 0x000C;
        private const int IDC_STATIC = -1;
        private const int WM_CLOSE = 0x0010;

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, int dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDlgItem(IntPtr hDlg, int nIDDlgItem);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("kernel32.dll")]
        private static extern int GetCurrentThreadId();

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, string lParam);

        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        // Hook callback
        private static IntPtr HookCallbackWithTimeout(int nCode, IntPtr wParam, IntPtr lParam, int timeoutSeconds = 0)
        {
            if (nCode == HCBT_ACTIVATE)
            {
                StringBuilder sb = new(256);
                GetWindowText(wParam, sb, sb.Capacity);
                string windowTitle = sb.ToString();

                if (windowTitle == _expectedTitle)
                {
                    _dialogHandle = wParam;
                    CenterOnParent(wParam);

                    if (_customFont != null)
                    {
                        IntPtr hText = GetDlgItem(wParam, IDC_STATIC);
                        SendMessage(hText, WM_SETFONT, _customFont.ToHfont(), (IntPtr)1);
                    }

                    // Start countdown timer for auto-close
                    if (timeoutSeconds > 0)
                    {
                        _countdownTimer = new System.Threading.Timer(UpdateCountdown, null, 1000, 1000);
                        
                        // Also schedule the final close
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(timeoutSeconds * 1000);
                            if (IsWindow(wParam))
                            {
                                SendMessage(wParam, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                            }
                        });
                    }

                    // Unhook now
                    UnhookWindowsHookEx(_hHook);
                    _hHook = IntPtr.Zero;
                }
            }

            return CallNextHookEx(_hHook, nCode, wParam, lParam);
        }

        private static void UpdateCountdown(object state)
        {
            _remainingSeconds--;
            
            if (_remainingSeconds <= 0 || _dialogHandle == IntPtr.Zero || !IsWindow(_dialogHandle))
            {
                SafeDisposeTimer();
                return;
            }

            // Update the message text with countdown
            string updatedMessage = $"{_originalMessage}\n\nThis dialog will close in {_remainingSeconds} seconds ...";
            
            // Get the static text control and update it
            IntPtr hText = GetDlgItem(_dialogHandle, IDC_STATIC);
            if (hText != IntPtr.Zero)
            {
                SendMessage(hText, WM_SETTEXT, IntPtr.Zero, updatedMessage);
            }
        }

        private static void CenterOnParent(IntPtr hWnd)
        {
            // Try to get parent window, fallback to screen center
            IntPtr hParent = GetParent(hWnd);

            GetWindowRect(hWnd, out var rcChild);
            int childWidth = rcChild.Right - rcChild.Left;
            int childHeight = rcChild.Bottom - rcChild.Top;

            RECT rcParent;
            if (hParent != IntPtr.Zero && GetWindowRect(hParent, out rcParent))
            {
                int parentWidth = rcParent.Right - rcParent.Left;
                int parentHeight = rcParent.Bottom - rcParent.Top;

                int x = rcParent.Left + (parentWidth - childWidth) / 2;
                int y = rcParent.Top + (parentHeight - childHeight) / 2;

                SetWindowPos(hWnd, IntPtr.Zero, x, y, 0, 0, SWP_NOZORDER | SWP_NOSIZE | SWP_NOACTIVATE);
            }
            else
            {
                // Center on screen (work area) as fallback
                Rectangle screenBounds = Screen.PrimaryScreen.WorkingArea;
                int x = screenBounds.Left + (screenBounds.Width - childWidth) / 2;
                int y = screenBounds.Top + (screenBounds.Height - childHeight) / 2;

                SetWindowPos(hWnd, IntPtr.Zero, x, y, 0, 0, SWP_NOZORDER | SWP_NOSIZE | SWP_NOACTIVATE);
            }
        }
    }

   public partial class CustomMessageBox : Form
    {
        private Label lblMessage;
        private Button btnOK;
        private Button btnCancel;
        private Button btnYes;
        private Button btnNo;
        private PictureBox picIcon;
        private System.Windows.Forms.Timer countdownTimer;
        
        private string originalMessage;
        private int remainingSeconds;
        private bool autoClose;

        public DialogResult Result { get; private set; } = DialogResult.None;

        public CustomMessageBox()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // Form properties
            this.Text = "Message";
            this.Size = new Size(400, 180); // Increased initial height
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = SystemColors.Control;
            
            // Icon PictureBox
            this.picIcon = new PictureBox();
            this.picIcon.Size = new Size(32, 32);
            this.picIcon.Location = new Point(12, 12);
            this.picIcon.SizeMode = PictureBoxSizeMode.StretchImage;
            this.Controls.Add(this.picIcon);
            
            // Message Label
            this.lblMessage = new Label();
            this.lblMessage.Location = new Point(54, 12);
            this.lblMessage.Size = new Size(320, 80); // Increased initial height
            this.lblMessage.Text = "";
            this.lblMessage.Font = new Font("Segoe UI", 9F);
            this.lblMessage.UseMnemonic = false;
            this.lblMessage.AutoSize = false; // Prevent auto-sizing conflicts
            this.Controls.Add(this.lblMessage);
            
            // Buttons (initially hidden)
            this.btnOK = CreateButton("OK", DialogResult.OK);
            this.btnCancel = CreateButton("Cancel", DialogResult.Cancel);
            this.btnYes = CreateButton("Yes", DialogResult.Yes);
            this.btnNo = CreateButton("No", DialogResult.No);
            
            // Timer for countdown
            this.countdownTimer = new System.Windows.Forms.Timer();
            this.countdownTimer.Interval = 1000; // 1 second
            this.countdownTimer.Tick += CountdownTimer_Tick;
            
            this.ResumeLayout(false);
        }

        private Button CreateButton(string text, DialogResult result)
        {
            var button = new Button();
            button.Text = text;
            button.Size = new Size(75, 23);
            button.UseVisualStyleBackColor = true;
            button.Click += (s, e) => {
                this.Result = result;
                this.Close();
            };
            button.Visible = false;
            this.Controls.Add(button);
            return button;
        }

        public static DialogResult Show(IWin32Window owner, string text, string caption = "",
            MessageBoxButtons buttons = MessageBoxButtons.OK, 
            MessageBoxIcon icon = MessageBoxIcon.None,
            int autoCloseSeconds = 0,
            Font customFont = null)
        {
            using (var msgBox = new CustomMessageBox())
            {
                msgBox.Text = caption;
                msgBox.originalMessage = text;
                msgBox.remainingSeconds = autoCloseSeconds;
                msgBox.autoClose = autoCloseSeconds > 0;
                
                if (customFont != null)
                    msgBox.lblMessage.Font = customFont;
                
                msgBox.SetupIcon(icon);
                msgBox.SetupButtons(buttons);
                msgBox.UpdateMessage();
                
                // Handle proper sizing after form is shown
                msgBox.Load += (s, e) => {
                    msgBox.ResizeForm();
                    if (msgBox.autoClose)
                    {
                        msgBox.countdownTimer.Start();
                    }
                };
                
                msgBox.ShowDialog(owner);
                return msgBox.Result;
            }
        }

        private void SetupIcon(MessageBoxIcon icon)
        {
            switch (icon)
            {
                case MessageBoxIcon.Error:
                    picIcon.Image = SystemIcons.Error.ToBitmap();
                    break;
                case MessageBoxIcon.Warning:
                    picIcon.Image = SystemIcons.Warning.ToBitmap();
                    break;
                case MessageBoxIcon.Information:
                    picIcon.Image = SystemIcons.Information.ToBitmap();
                    break;
                case MessageBoxIcon.Question:
                    picIcon.Image = SystemIcons.Question.ToBitmap();
                    break;
                default:
                    picIcon.Visible = false;
                    lblMessage.Location = new Point(12, 12);
                    lblMessage.Size = new Size(360, 60);
                    break;
            }
        }

        private void SetupButtons(MessageBoxButtons buttons)
        {
            var buttonList = new List<Button>();
            
            switch (buttons)
            {
                case MessageBoxButtons.OK:
                    buttonList.Add(btnOK);
                    this.AcceptButton = btnOK;
                    break;
                case MessageBoxButtons.OKCancel:
                    buttonList.Add(btnOK);
                    buttonList.Add(btnCancel);
                    this.AcceptButton = btnOK;
                    this.CancelButton = btnCancel;
                    break;
                case MessageBoxButtons.YesNo:
                    buttonList.Add(btnYes);
                    buttonList.Add(btnNo);
                    this.AcceptButton = btnYes;
                    break;
                case MessageBoxButtons.YesNoCancel:
                    buttonList.Add(btnYes);
                    buttonList.Add(btnNo);
                    buttonList.Add(btnCancel);
                    this.AcceptButton = btnYes;
                    this.CancelButton = btnCancel;
                    break;
            }
            
            // Show the buttons first
            foreach (var button in buttonList)
            {
                button.Visible = true;
            }
            
            // Position buttons after form is properly sized
            PositionButtons(buttonList);
        }
        
        private void PositionButtons(List<Button> buttonList)
        {
            int buttonWidth = 75;
            int buttonHeight = 23;
            int buttonSpacing = 6;
            int totalWidth = (buttonWidth * buttonList.Count) + (buttonSpacing * (buttonList.Count - 1));
            int startX = (this.ClientSize.Width - totalWidth) / 2;
            int buttonY = this.ClientSize.Height - buttonHeight - 12; // 12px margin from bottom
            
            for (int i = 0; i < buttonList.Count; i++)
            {
                buttonList[i].Size = new Size(buttonWidth, buttonHeight);
                buttonList[i].Location = new Point(startX + (i * (buttonWidth + buttonSpacing)), buttonY);
            }
        }

        private void UpdateMessage()
        {
            string message = originalMessage;
            if (autoClose && remainingSeconds > 0)
            {
                message += $"\n\nThis dialog will close in {remainingSeconds} seconds.";
            }
            lblMessage.Text = message;
        }

        private void ResizeForm()
        {
            // Ensure the form is visible before measuring text
            if (!this.Visible)
                return;
                
            // Calculate required height based on message
            using (Graphics g = this.CreateGraphics()) // Use form graphics instead of label
            {
                SizeF textSize = g.MeasureString(lblMessage.Text, lblMessage.Font, lblMessage.Width);
                int requiredMessageHeight = (int)Math.Ceiling(textSize.Height) + 20; // padding
                
                lblMessage.Height = Math.Max(requiredMessageHeight, 40); // minimum height
                
                // Calculate total form height: message + button area + margins
                int buttonAreaHeight = 23 + 24; // button height + margins (12 top + 12 bottom)
                int totalClientHeight = lblMessage.Bottom + buttonAreaHeight;
                
                // Ensure minimum form size
                totalClientHeight = Math.Max(totalClientHeight, 120);
                
                this.ClientSize = new Size(this.ClientSize.Width, totalClientHeight);
            }
            
            // Reposition buttons after resizing
            var visibleButtons = new List<Button>();
            if (btnOK.Visible) visibleButtons.Add(btnOK);
            if (btnCancel.Visible) visibleButtons.Add(btnCancel);
            if (btnYes.Visible) visibleButtons.Add(btnYes);
            if (btnNo.Visible) visibleButtons.Add(btnNo);
            
            if (visibleButtons.Count > 0)
            {
                PositionButtons(visibleButtons);
            }
        }

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            remainingSeconds--;
            
            if (remainingSeconds <= 0)
            {
                countdownTimer.Stop();
                this.Result = DialogResult.OK; // Default result for auto-close
                this.Close();
                return;
            }
            
            UpdateMessage();
            ResizeForm(); // Resize in case the message text changes size
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            countdownTimer?.Stop();
            countdownTimer?.Dispose();
            base.OnFormClosed(e);
        }
    }

    // Enhanced MsgBox that can use either approach
    public static class MsgBoxEnhanced
    {
        public static DialogResult Show(IWin32Window owner, string text, string title,
            MessageBoxButtons buttons = MessageBoxButtons.OK, 
            MessageBoxIcon icon = MessageBoxIcon.None,
            MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button1, 
            Font customFont = null)
        {
            return CustomMessageBox.Show(owner, text, title, buttons, icon, 0, customFont);
        }

        public static DialogResult ShowAutoClose(IWin32Window owner, string text, string title,
            MessageBoxButtons buttons = MessageBoxButtons.OK, 
            MessageBoxIcon icon = MessageBoxIcon.None,
            MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button1, 
            int timeoutSeconds = 10,
            Font customFont = null)
        {
            return CustomMessageBox.Show(owner, text, title, buttons, icon, timeoutSeconds, customFont);
        }
    }
}
