namespace RemuxOpt
{
	public class MsgBox
	{
		private static Win32.WindowsHookProc _hookProcDelegate;
		private static int _hHook;
		private static string _title;
		private static string _msg;
        private static Font _customTextFont;

		public static DialogResult Show(string msg, string title, MessageBoxButtons btns, MessageBoxIcon icon)
		{
			// Create a callback delegate
			_hookProcDelegate = HookCallback;

			// Remember the title & message that we'll look for.
			// The hook sees *all* windows, so we need to make sure we operate on the right one.
			_msg = msg;
			_title = title;

			// Set the hook.
			// Suppress "GetCurrentThreadId() is deprecated" warning.
			// It's documented that Thread.ManagedThreadId doesn't work with SetWindowsHookEx()
#pragma warning disable 0618
			_hHook = Win32.SetWindowsHookEx(Win32.WH_CBT, _hookProcDelegate, IntPtr.Zero, AppDomain.GetCurrentThreadId());
#pragma warning restore 0618

			// Pop a standard MessageBox. The hook will center it.
			var rslt = MessageBox.Show(msg, title, btns, icon);

			// Release hook, clean up (may have already occurred)
			Unhook();

			return rslt;
		}

		public static DialogResult Show(string msg, string title, MessageBoxButtons btns, MessageBoxIcon icon,
		    MessageBoxDefaultButton defBtn)
		{
			// Create a callback delegate
			_hookProcDelegate = HookCallback;

			// Remember the title & message that we'll look for.
			// The hook sees *all* windows, so we need to make sure we operate on the right one.
			_msg = msg;
			_title = title;

			// Set the hook.
			// Suppress "GetCurrentThreadId() is deprecated" warning.
			// It's documented that Thread.ManagedThreadId doesn't work with SetWindowsHookEx()
#pragma warning disable 0618
			_hHook = Win32.SetWindowsHookEx(Win32.WH_CBT, _hookProcDelegate, IntPtr.Zero, AppDomain.GetCurrentThreadId());
#pragma warning restore 0618

			// Pop a standard MessageBox. The hook will center it.
			var rslt = MessageBox.Show(msg, title, btns, icon, defBtn);

			// Release hook, clean up (may have already occurred)
			Unhook();

			return rslt;
		}

		public static DialogResult Show(string msg, string title, MessageBoxButtons btns, MessageBoxIcon icon,
		    MessageBoxDefaultButton defBtn, Font customTextFont)
		{
			// Create a callback delegate
			_hookProcDelegate = HookCallback;

			// Remember the title & message that we'll look for.
			// The hook sees *all* windows, so we need to make sure we operate on the right one.
			_msg = msg;
			_title = title;
            _customTextFont = customTextFont;

			// Set the hook.
			// Suppress "GetCurrentThreadId() is deprecated" warning.
			// It's documented that Thread.ManagedThreadId doesn't work with SetWindowsHookEx()
#pragma warning disable 0618
			_hHook = Win32.SetWindowsHookEx(Win32.WH_CBT, _hookProcDelegate, IntPtr.Zero, AppDomain.GetCurrentThreadId());
#pragma warning restore 0618

			// Pop a standard MessageBox. The hook will center it.
			var rslt = MessageBox.Show(msg, title, btns, icon, defBtn);

			// Release hook, clean up (may have already occurred)
			Unhook();

			return rslt;
		}

		private static void Unhook()
		{
			Win32.UnhookWindowsHookEx(_hHook);

			_hHook = 0;
			_hookProcDelegate = null;
			_msg = null;
			_title = null;
		}

		private static int HookCallback(int code, IntPtr wParam, IntPtr lParam)
		{
			var hHook = _hHook;	// Local copy for CallNextHookEx() JIC we release _hHook

			// Look for HCBT_ACTIVATE, *not* HCBT_CREATEWND:
			//   child controls haven't yet been created upon HCBT_CREATEWND.
			if (code == Win32.HCBT_ACTIVATE)
			{
				var cls = Win32.GetClassName(wParam);
				if (cls == "#32770")	// MessageBoxes are Dialog boxes
				{
					var title = Win32.GetWindowText(wParam);
					var msg = Win32.GetDlgItemText(wParam, 0xFFFF);	// -1 aka IDC_STATIC
					if (title == _title && msg == _msg)
					{
						CenterWindowOnParent(wParam);
						Unhook();	// Release hook - we've done what we needed
					}
				}

                if (_customTextFont != null)
                {
                    //https://stackoverflow.com/questions/2259027/bold-text-in-messagebox/2259213#2259213
                    //https://stackoverflow.com/questions/19204656/messagebox-with-custom-font
                    //http://forums.codeguru.com/showthread.php?304012-How-to-change-Font-of-MessageBox
                    IntPtr hText = Win32.GetDlgItem(wParam, 0xffff);
                    Win32.SendMessage(hText, Win32.WM_SETFONT, _customTextFont.ToHfont(), (IntPtr)1);
                }
			}

			return Win32.CallNextHookEx(hHook, code, wParam, lParam);
		}

		// Boilerplate window-centering code.
		// Split out of HookCallback() for clarity.
		private static void CenterWindowOnParent(IntPtr hChildWnd)
		{
			// Get child (MessageBox) size
			var rcChild = new Win32.RECT();
			Win32.GetWindowRect(hChildWnd, ref rcChild);
			var cxChild = rcChild.right - rcChild.left;
			var cyChild = rcChild.bottom - rcChild.top;

			// Get parent (Form) size & location
			var hParent = Win32.GetParent(hChildWnd);
			if (hParent == IntPtr.Zero)
			{
				//fallback to desktop? issue when drag drop
				hParent = Win32.GetAncestor(hChildWnd, Win32.GetAncestorFlags.GetParent);
			}
			var rcParent = new Win32.RECT();
			Win32.GetWindowRect(hParent, ref rcParent);
			var cxParent = rcParent.right - rcParent.left;
			var cyParent = rcParent.bottom - rcParent.top;

			// Center the MessageBox on the Form
			var x = rcParent.left + (cxParent - cxChild) / 2;
			var y = rcParent.top + (cyParent - cyChild) / 2;
			uint uFlags = 0x15;	// SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE;

			Win32.SetWindowPos(hChildWnd, IntPtr.Zero, x, y, 0, 0, uFlags);
		}
	}
}
