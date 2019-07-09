using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Mahou.Classes {
	/// <summary>
	/// Low level hook.
	/// </summary>
	public static class LLHook {
		public static IntPtr _LLHook_ID = IntPtr.Zero;
		public static WinAPI.LowLevelProc _LLHook_proc = LLHook.Callback;
		static bool alt, shift, ctrl, win;
		public static void Set() {
			if (!MahouUI.ENABLED) return;
			if (_LLHook_ID != IntPtr.Zero)
				UnSet();
			using (var currProcess = Process.GetCurrentProcess())
				using (var currModule = currProcess.MainModule)
					_LLHook_ID = WinAPI.SetWindowsHookEx(WinAPI.WH_KEYBOARD_LL, _LLHook_proc, 
					                                     WinAPI.GetModuleHandle(currModule.ModuleName), 0);
			if (_LLHook_ID == IntPtr.Zero)
				Logging.Log("Registering LLHook failed: " + Marshal.GetLastWin32Error(), 1);
		}
		public static void UnSet() {
			var r = WinAPI.UnhookWindowsHookEx(_LLHook_ID);
			if (r)
				_LLHook_ID = IntPtr.Zero;
			else 
				Logging.Log("BAD! LLHook unregister failed: " + System.Runtime.InteropServices.Marshal.GetLastWin32Error(), 1);
		}
		public static IntPtr Callback(int nCode, IntPtr wParam, IntPtr lParam) {
			if (MMain.Mahou == null || nCode < 0) return WinAPI.CallNextHookEx(_LLHook_ID, nCode, wParam, lParam);
			if (KMHook.ExcludedProgram() && !MahouUI.ChangeLayoutInExcluded) 
				return WinAPI.CallNextHookEx(_LLHook_ID, nCode, wParam, lParam);
			var vk = Marshal.ReadInt32(lParam);
			var Key = (Keys)vk;
			SetModifs(Key, wParam);
			if (MahouUI.SnippetsEnabled)
				if (KMHook.c_snip.Count > 0)
					if (MMain.Mahou.SnippetsExpandType == "Tab" && Key == Keys.Tab && !shift && !alt && !win && !ctrl) {
						WinAPI.keybd_event((byte)Keys.F14, (byte)Keys.F14, (int)WinAPI.KEYEVENTF_KEYUP, 0);
						return (IntPtr)1; // Disable event
					}
			if (MahouUI.RemapCapslockAsF18) {
				bool _shift = !shift, _alt = !alt, _ctrl = !ctrl, _win = !win;
				if (Key == Keys.CapsLock) {
					for (var i = 1; i!=5; i++) {
						var KeyIndex = (int)typeof(MahouUI).GetField("Key"+i).GetValue(MMain.Mahou);
						if (KeyIndex == 8) // Shift+CapsLock 
							_shift = shift;
					}
				}
				uint mods = 0;
				if (alt) mods += WinAPI.MOD_ALT;
				if (ctrl) mods += WinAPI.MOD_CONTROL;
				if (shift) mods += WinAPI.MOD_SHIFT;
				if (win) mods += WinAPI.MOD_WIN;
				var has = MMain.Mahou.HasHotkey(new Hotkey(false, (uint)Keys.F18, mods, 77));
				if (has) {
					if (Hotkey.ContainsModifier((int)mods, (int)WinAPI.MOD_SHIFT))
						_shift = shift;
					if (Hotkey.ContainsModifier((int)mods, (int)WinAPI.MOD_ALT))
						_alt = alt;
					if (Hotkey.ContainsModifier((int)mods, (int)WinAPI.MOD_CONTROL))
						_ctrl = ctrl;
					if (Hotkey.ContainsModifier((int)mods, (int)WinAPI.MOD_WIN))
						_win = win;
				}
				var GJIME = vk >= 240 && vk <= 242;
			    //			Debug.WriteLine(Key + " " +has + "// " + _shift + " " + _alt + " " + _ctrl + " " + _win + " " + mods + " >> " + (Key == Keys.CapsLock && _shift && _alt && _ctrl && _win));
				if ((Key == Keys.CapsLock || GJIME) && _shift && _alt && _ctrl && _win) {
					var flags = (int)(KInputs.IsExtended(Key) ? WinAPI.KEYEVENTF_EXTENDEDKEY : 0);
					if (wParam == (IntPtr)WinAPI.WM_KEYUP)
						flags |= (int)WinAPI.KEYEVENTF_KEYUP;
					WinAPI.keybd_event((byte)Keys.F18, (byte)Keys.F18, flags , 0);
					return (IntPtr)1; // Disable event
				}
	//			Debug.WriteLine(Marshal.GetLastWin32Error());
			}
			return WinAPI.CallNextHookEx(_LLHook_ID, nCode, wParam, lParam);
		}
		static void SetModifs(Keys Key, IntPtr msg) {
			switch (Key) {
				case Keys.LShiftKey:
				case Keys.RShiftKey:
				case Keys.ShiftKey:
					shift = ((msg == (IntPtr)WinAPI.WM_SYSKEYDOWN) ? true : false) || ((msg == (IntPtr)WinAPI.WM_KEYDOWN) ? true : false);
					break;
				case Keys.RControlKey:
				case Keys.LControlKey:
				case Keys.ControlKey:
					ctrl = ((msg == (IntPtr)WinAPI.WM_SYSKEYDOWN) ? true : false) || ((msg == (IntPtr)WinAPI.WM_KEYDOWN) ? true : false);
					break;
				case Keys.RMenu:
				case Keys.LMenu:
				case Keys.Menu:
					alt = ((msg == (IntPtr)WinAPI.WM_SYSKEYDOWN) ? true : false) || ((msg == (IntPtr)WinAPI.WM_KEYDOWN) ? true : false);
					break;
				case Keys.RWin:
				case Keys.LWin:
					win = ((msg == (IntPtr)WinAPI.WM_SYSKEYDOWN) ? true : false) || ((msg == (IntPtr)WinAPI.WM_KEYDOWN) ? true : false);
					break;
			}
		}
		public static void ClearModifiers() {
			alt = shift = ctrl = win = false;
		}
		public static void SetModifier(uint Mod, bool down) {
			if (Mod == WinAPI.MOD_WIN)
				win = down;
			if (Mod == WinAPI.MOD_SHIFT)
				shift = down;
			if (Mod == WinAPI.MOD_ALT)
				alt = down;
			if (Mod == WinAPI.MOD_CONTROL)
				ctrl = down;
		}
	}
}
