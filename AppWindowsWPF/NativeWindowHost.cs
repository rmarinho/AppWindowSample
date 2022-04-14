using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace AppWindowsWPF
{
	internal class NativeWindowHost : Control
	{
		private IntPtr m_originalParentHandle;
		private NativeWindow m_remoteWindow;

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);

			if (m_remoteWindow != null)
			{
				EmbedRemoteWindow(m_remoteWindow);
			}
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			if (m_remoteWindow != null)
			{
				UpdateRemoteWindowSize(m_remoteWindow, repaint: false);
			}
		}

		private void UpdateRemoteWindowSize(NativeWindow remoteWindow, bool repaint)
		{
			if (RectangleToClient(remoteWindow.Bounds) != Bounds)
			{
				remoteWindow.Move(0, 0, Width, Height, repaint);
			}
		}

		private void EmbedRemoteWindow(NativeWindow remoteWindow)
		{
			Trace.WriteLine(string.Format("EmbedRemoteWindow -> <{0:X}> ClassName = '{1}' : Title = '{2}'",
				remoteWindow.Handle.ToInt64(), remoteWindow.ClassName, remoteWindow.Title));
			m_originalParentHandle = remoteWindow.SetParent(Handle);
			remoteWindow.Style = NativeWindow.WS_VISIBLE;

			UpdateRemoteWindowSize(remoteWindow, repaint: true);
		}

		public void AttachWindow(NativeWindow remoteWindow)
		{
			if (remoteWindow == null)
			{
				throw new ArgumentException("The remote window is not valid!", "remoteWindow");
			}
			if (m_remoteWindow != null)
			{
				throw new InvalidOperationException("A remote window window is already attached!");
			}

			m_remoteWindow = remoteWindow;
			if (IsHandleCreated)
			{
				if (InvokeRequired)
				{
					Invoke(new Action<NativeWindow>(EmbedRemoteWindow), m_remoteWindow);
				}
				else
				{
					EmbedRemoteWindow(m_remoteWindow);
				}
			}
		}

		public void Detach()
		{
			if (m_remoteWindow == null)
			{
				throw new InvalidOperationException("No remote window attached!");
			}

			m_remoteWindow.SetParent(IntPtr.Zero);
			m_remoteWindow = null;
		}
	}

	internal class NativeWindow
	{
		public NativeWindow(IntPtr hwnd)
		{
			Handle = hwnd;
		}

		public IntPtr Handle { get; private set; }

		public IntPtr SetParent(IntPtr parentHwnd)
		{
			return SetParent(Handle, parentHwnd);
		}

		public string Title
		{
			get
			{
				var titleBuilder = new StringBuilder(256);
				GetWindowText(Handle, titleBuilder, titleBuilder.Capacity);

				return titleBuilder.ToString();
			}
		}

		public string ClassName
		{
			get
			{
				var classNameBuilder = new StringBuilder(256);
				GetClassName(Handle, classNameBuilder, classNameBuilder.Capacity);

				return classNameBuilder.ToString();
			}
		}

		public int Style
		{
			get
			{
				return GetWindowLong(Handle, GWL_STYLE).ToInt32();
			}
			set
			{
				SetWindowLong32(Handle, GWL_STYLE, value);
			}
		}

		public bool IsVisible
		{
			get
			{
				return (Style & WS_VISIBLE) > 0;
			}
		}

		public bool Exists
		{
			get
			{
				return IsWindow(Handle);
			}
		}

		public Rectangle Bounds
		{
			get
			{
				RECT rect;
				GetWindowRect(Handle, out rect);

				return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
			}
		}

		public bool Activate()
		{
			return SetWindowPos(Handle, HWND_TOP, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
		}

		public bool Move(int x, int y, int width, int height, bool repaint)
		{
			return MoveWindow(Handle, x, y, width, height, repaint);
		}

		public IntPtr Close()
		{
			return SendMessage(Handle, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
		}

		#region user32 imports

		private const uint WM_CLOSE = 0x0010;

		private const int GWL_STYLE = -16;

		public const int WS_VISIBLE = 0x10000000;

		private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
		private static readonly IntPtr HWND_TOP = new IntPtr(0);
		private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

		private const uint SWP_NOSIZE = 0x0001;
		private const uint SWP_NOMOVE = 0x0002;
		private const uint SWP_NOZORDER = 0x0004;

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		private static extern IntPtr SendMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern IntPtr SetParent(IntPtr hwnd, IntPtr parentHwnd);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern IntPtr GetWindowLong(IntPtr hwnd, int index);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpWindowText, int nMaxCount);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

		// This helper static method is required because the 32-bit version of user32.dll does not contain this API
		// (on any versions of Windows), so linking the method will fail at run-time. The bridge dispatches the request
		// to the correct function (SetWindowLong in 32-bit mode and SetWindowLongPtr in 64-bit mode)
		private static IntPtr SetWindowLong(IntPtr hwnd, int index, IntPtr dwNewLong)
		{
			if (IntPtr.Size == 8)
			{
				return SetWindowLongPtr64(hwnd, index, dwNewLong);
			}
			else
			{
				return new IntPtr(SetWindowLong32(hwnd, index, dwNewLong.ToInt32()));
			}
		}

		[DllImport("user32.dll", EntryPoint = "SetWindowLong")]
		private static extern int SetWindowLong32(IntPtr hwnd, int index, int dwNewLong);

		[DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
		private static extern IntPtr SetWindowLongPtr64(IntPtr hwnd, int index, IntPtr dwNewLong);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool MoveWindow(IntPtr hwnd, int x, int y, int width, int height, bool repaint);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool IsWindow(IntPtr hWnd);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter, int x, int y, int width, int height, uint flags);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);

		[StructLayout(LayoutKind.Sequential)]
		private struct RECT
		{
			public int Left;
			public int Top;
			public int Right;
			public int Bottom;
		}

		#endregion
	}
}
