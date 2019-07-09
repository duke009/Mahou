using System;

namespace Mahou.Classes {
	public class Hotkey {
		public readonly int ID;
		public readonly uint VirtualKeyCode;
		public readonly uint Modifiers;
		public readonly bool Enabled, Double;
		/// <summary>
		/// Initializes an hotkey, modifs are ctrl(2), shift(4), alt(1), win(8).
		/// </summary>
		public Hotkey(bool enabled, uint VKCode, uint modifiers, int id, bool dble = false) {
			this.ID = id;
			this.Enabled = enabled;
			this.VirtualKeyCode = VKCode;
			this.Modifiers = modifiers;
			this.Double = dble;
		}
		/// <summary>
		/// All Mahou hotkey names and IDs.
		/// </summary>
		public enum HKID {
			ConvertLastWord,
			ConvertSelection,
			ConvertLastLine,
			ConvertMultipleWords,
			ToTitleSelection,
			ToSwapSelection,
			ToUpperSelection,
			ToLowerSelection,
			ToRandomSelection,
			TransliterateSelection,
			ToggleSymbolIgnoreMode,
			ToggleVisibility,
			Exit,
			Restart,
			ToggleLangPanel,
			ShowSelectionTranslation,
			ToggleMahou
		}
		/// <summary>
		/// Gets all modifiers in hotkey.
		/// </summary>
		/// <param name="hkmods">Hotkey modifiers string.</param>
		public static uint GetMods(string hkmods) {
			uint MOD = 0;
			if (hkmods.Contains("Alt"))
				MOD += WinAPI.MOD_ALT;
			if (hkmods.Contains("Control"))
				MOD += WinAPI.MOD_CONTROL;
			if (hkmods.Contains("Shift"))
				MOD += WinAPI.MOD_SHIFT;
			if( hkmods.Contains("Win"))
				MOD += WinAPI.MOD_WIN;
			return MOD;
		}
		/// <summary>
		/// Get all modifiers in hotkey as string.
		/// </summary>
		/// <param name="hkmods">Hotkey modifiers hex unit.</param>
		/// <returns></returns>
		public static string GetMods(int hkmods) {
			var MOD = "";
			if (ContainsModifier(hkmods, (int)WinAPI.MOD_ALT))
				MOD += "Alt";
			if (ContainsModifier(hkmods, (int)WinAPI.MOD_CONTROL))
				MOD += "Control";
			if (ContainsModifier(hkmods, (int)WinAPI.MOD_SHIFT))
				MOD += "Shift";
			if (ContainsModifier(hkmods, (int)WinAPI.MOD_WIN))
				MOD += "Win";
			return MOD;
		}
		/// <summary>
		/// Checks if modifiers "mods" contains modifier "mod".
		/// </summary>
		/// <returns>True if "mods" contains "mod".</returns>
		public static bool ContainsModifier(int mods, int mod) {
			if (mod == WinAPI.MOD_WIN && mods >= WinAPI.MOD_WIN) {
				return true;
			}
			if (mod == WinAPI.MOD_SHIFT && (mods == WinAPI.MOD_SHIFT || 
			    mods == WinAPI.MOD_SHIFT + WinAPI.MOD_ALT ||
			    mods == WinAPI.MOD_SHIFT + WinAPI.MOD_CONTROL ||
			    mods == WinAPI.MOD_SHIFT + WinAPI.MOD_WIN || 
			    mods == WinAPI.MOD_SHIFT + WinAPI.MOD_ALT + WinAPI.MOD_CONTROL ||
			    mods == WinAPI.MOD_SHIFT + WinAPI.MOD_WIN + WinAPI.MOD_CONTROL ||
			   	mods == WinAPI.MOD_SHIFT + WinAPI.MOD_WIN + WinAPI.MOD_ALT ||
			   	mods == WinAPI.MOD_SHIFT + WinAPI.MOD_WIN + WinAPI.MOD_CONTROL + WinAPI.MOD_ALT)) {
				return true;
			}
			if (mod == WinAPI.MOD_CONTROL && (mods == WinAPI.MOD_CONTROL || 
			    mods == WinAPI.MOD_CONTROL + WinAPI.MOD_SHIFT ||
			    mods == WinAPI.MOD_CONTROL + WinAPI.MOD_ALT ||
			    mods == WinAPI.MOD_CONTROL + WinAPI.MOD_WIN || 
			    mods == WinAPI.MOD_CONTROL + WinAPI.MOD_ALT + WinAPI.MOD_SHIFT ||
			    mods == WinAPI.MOD_CONTROL + WinAPI.MOD_WIN + WinAPI.MOD_SHIFT ||
			   	mods == WinAPI.MOD_CONTROL + WinAPI.MOD_WIN + WinAPI.MOD_ALT ||
			   	mods == WinAPI.MOD_CONTROL + WinAPI.MOD_WIN + WinAPI.MOD_SHIFT + WinAPI.MOD_ALT)) {
				return true;
			}
			if (mod == WinAPI.MOD_ALT && mods % 2 != 0) {
				return true;
			}
			return false;
		}
		#region GetHash code, Equals etc.
		public override int GetHashCode() {
			return (VirtualKeyCode.GetHashCode() * Modifiers.GetHashCode()) ^ 7;
		}
		public override bool Equals(object obj) {
			var other = obj as Hotkey;
			if (other == null)
				return false;
			if (((this.VirtualKeyCode == 16 || this.VirtualKeyCode == 160 || this.VirtualKeyCode == 161) && this.Modifiers == 0 && other.Modifiers == WinAPI.MOD_SHIFT && other.VirtualKeyCode == 0) 
			 || ((other.VirtualKeyCode == 16 || other.VirtualKeyCode == 160 || other.VirtualKeyCode == 161) && other.Modifiers == 0 && this.Modifiers == WinAPI.MOD_SHIFT && this.VirtualKeyCode == 0))
				return true;
			if (((this.VirtualKeyCode == 18 || this.VirtualKeyCode == 164 || this.VirtualKeyCode == 165) && this.Modifiers == 0 && other.Modifiers == WinAPI.MOD_ALT && other.VirtualKeyCode == 0) 
			 || ((other.VirtualKeyCode == 18 || other.VirtualKeyCode == 164 || other.VirtualKeyCode == 165) && other.Modifiers == 0 && this.Modifiers == WinAPI.MOD_ALT && this.VirtualKeyCode == 0))
				return true;
			if (((this.VirtualKeyCode == 17 || this.VirtualKeyCode == 162 || this.VirtualKeyCode == 163) && this.Modifiers == 0 && other.Modifiers == WinAPI.MOD_CONTROL && other.VirtualKeyCode == 0) 
			 || ((other.VirtualKeyCode == 17 || other.VirtualKeyCode == 162 || other.VirtualKeyCode == 163) && other.Modifiers == 0 && this.Modifiers == WinAPI.MOD_CONTROL && this.VirtualKeyCode == 0))
				return true;
			return this.VirtualKeyCode == other.VirtualKeyCode && 
				   this.Modifiers == other.Modifiers;
		}
		public static bool operator ==(Hotkey lhs, Hotkey rhs) {
			if (ReferenceEquals(lhs, rhs))
				return true;
			if (ReferenceEquals(lhs, null) || ReferenceEquals(rhs, null))
				return false;
			return lhs.Equals(rhs);
		}
		public static bool operator !=(Hotkey lhs, Hotkey rhs) {
			return !(lhs == rhs);
		}
		#endregion
		public static void CallHotkey(Hotkey hotkey, HKID hkID, ref bool hkOK, Action hkAction) {
			var once = !hotkey.Double;
		    if (hotkey.ID == (int)hkID && hotkey.Enabled) {
				if (hkOK || once) {
					Logging.Log("Hotkey [" + Enum.GetName(typeof(HKID), hkID) + "] fired.");
					if (MahouUI.BlockHKWithCtrl && ContainsModifier((int)hotkey.Modifiers, (int)WinAPI.MOD_CONTROL)) { } else {
						if (hotkey.Modifiers > 0 && MahouUI.RePress) {
							KMHook.hotkeywithmodsfired = true;
							KMHook.RePressAfter(Convert.ToInt32(hotkey.Modifiers));
						}
					}
					if (hotkey.ID <= (int)Hotkey.HKID.TransliterateSelection || hotkey.ID == (int)Hotkey.HKID.ShowSelectionTranslation
					   ) {
						KMHook.SendModsUp(Convert.ToInt32(hotkey.Modifiers));
					}
					KMHook.IfKeyIsMod((System.Windows.Forms.Keys)hotkey.VirtualKeyCode);
					hkAction();
					if (MahouUI.RePress)
						KMHook.RePress();
				} else if (hotkey.Double) {
					Logging.Log("Waiting for second hotkey ["+Enum.GetName(typeof(HKID), hkID) +"] press.");
					hkOK = true;
					KMHook.doublekey.Interval = MMain.Mahou.DoubleHKInterval;
					KMHook.doublekey.Start();
				}
			}
		}
		
	}
}
