using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SylphyHorn.Services
{
	

		public static class OverrideSystemMenu
	{

			static OverrideSystemMenu()
			{
				Application.ApplicationExit += delegate {
					try
					{
						foreach (DictionaryEntry de in htThreads)
						{
							Hook h = (Hook)de.Value;
							RemoveMenu(h.hMenu, h.uniqueId, 0);
							//DeleteMenu(h.hMenu, h.uniqueId, 0);
							UnhookWinEvent(h.hWinEventHook);
						}
					}
					catch
					{
					}
				};
			}

			private const int EVENT_OBJECT_INVOKED = 0x8013;
			private const int OBJID_SYSMENU = -1;
			private const int WINEVENT_OUTOFCONTEXT = 0;
			private const int MF_STRING = 0x00000000;
			private const int HWND_TOPMOST = -1;
			private const int HWND_NOTOPMOST = -2;
			private const int SWP_NOMOVE = 0x0002;
			private const int SWP_NOSIZE = 0x0001;
			private const uint MF_UNCHECKED = 0x00000000;
			private const uint MF_CHECKED = 0x00000008;

			[DllImport("user32.dll")]
			private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

			[DllImport("user32.dll")]
			private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, uint uIDNewItem, String lpNewItem);

			[DllImport("user32.dll", SetLastError = true)]
			static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

			[DllImport("user32.dll", SetLastError = true)]
			private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventProc lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

			[DllImport("user32.dll", SetLastError = true)]
			internal static extern int UnhookWinEvent(IntPtr hWinEventHook);

			[DllImport("user32.dll", SetLastError = true)]
			private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

			[DllImport("user32.dll")]
			private static extern bool CheckMenuItem(IntPtr hMenu, uint uIDCheckItem, uint uCheck);

			[DllImport("user32.dll")]
			private static extern bool RemoveMenu(IntPtr hMenu, uint uPosition, uint uFlags);

			//[DllImport("user32.dll")]
			//private static extern bool DeleteMenu(IntPtr hMenu, uint uPosition, uint uFlags);

			private static Hashtable htThreads = new Hashtable();
			private static WinEventProc CallWinEventProc = new WinEventProc(EventCallback);
			private delegate void WinEventProc(IntPtr hWinEventHook, int iEvent, IntPtr hWnd, int idObject, int idChild, int dwEventThread, int dwmsEventTime);
			private static void EventCallback(IntPtr hWinEventHook, int iEvent, IntPtr hWnd, int idObject, int idChild, int dwEventThread, int dwmsEventTime)
			{
				//callback function, called when message is intercepted
				if (iEvent == EVENT_OBJECT_INVOKED)
				{
					if (idObject == OBJID_SYSMENU)
					{
						Hook h = (Hook)htThreads[(uint)dwEventThread];
						if (h != null && h.uniqueId == idChild)
						{
							bool b = !h.Checked;
							if (b)
								SetWindowPos(h.hMainWindowHandle, (IntPtr)HWND_TOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE);
							else
								SetWindowPos(h.hMainWindowHandle, (IntPtr)HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE);

							CheckMenuItem(h.hMenu, h.uniqueId, (b ? MF_CHECKED : MF_UNCHECKED));
							h.Checked = b;
						}
					}
				}
			}

			private class Hook
			{
				public uint uniqueId = 1001;
				public IntPtr hWinEventHook;
				public IntPtr hMenu;
				public IntPtr hMainWindowHandle;
				public bool Checked;
			}

			public static void AddMenuItems()
			{
				Process[] arr = Process.GetProcesses();
				foreach (Process p in arr)
				{
					if (p.MainWindowHandle == IntPtr.Zero)
						continue;

					if (p.ProcessName != "mspaint") // <-- remove or change this line
						continue;

					IntPtr hMenu = GetSystemMenu(p.MainWindowHandle, false);
					if (hMenu == IntPtr.Zero)
						continue;

					bool b = AppendMenu(hMenu, MF_STRING, 1001, "Always On Top");
					uint pid = 0;
					uint tid = GetWindowThreadProcessId(p.MainWindowHandle, out pid);

					Hook h = (Hook)htThreads[tid];
					if (h == null)
					{
						h = new Hook();
						h.hMenu = hMenu;
						h.hWinEventHook = SetWinEventHook(EVENT_OBJECT_INVOKED, EVENT_OBJECT_INVOKED, IntPtr.Zero, CallWinEventProc, pid, tid, WINEVENT_OUTOFCONTEXT);
						h.hMainWindowHandle = p.MainWindowHandle;
						htThreads[tid] = h;
					}
				}
			}
		}


	}
}
