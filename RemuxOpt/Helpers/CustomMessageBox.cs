using System.Runtime.InteropServices;
using System.Text;

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
                    "This will close in 5 seconds.",
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
            _hookProc = (nCode, wParam, lParam) => HookCallbackWithTimeout(nCode, wParam, lParam, timeoutSeconds);

            _hHook = SetWindowsHookEx(WH_CBT, _hookProc, IntPtr.Zero, GetCurrentThreadId());

            var result = MessageBox.Show(owner, text, title, buttons, icon, defaultButton);

            SafeUnhook();
            _customFont = null;

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

        // Win32 API imports
        private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        private const int WH_CBT = 5;
        private const int HCBT_ACTIVATE = 5;
        private const int WM_SETFONT = 0x30;
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
                    CenterOnParent(wParam);

                    if (_customFont != null)
                    {
                        IntPtr hText = GetDlgItem(wParam, IDC_STATIC);
                        SendMessage(hText, WM_SETFONT, _customFont.ToHfont(), (IntPtr)1);
                    }

                    // Schedule auto-close
                    if (timeoutSeconds > 0)
                    { 
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
}