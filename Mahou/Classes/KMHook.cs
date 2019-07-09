using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace Mahou.Classes {
	static class KMHook  { // Keyboard & Mouse Listeners & Event hook
		#region Variables
		public static string __ANY__ = "***ANY***", last_snip;
		public static bool win, alt, ctrl, shift,
			win_r, alt_r, ctrl_r, shift_r,
			shiftRP, ctrlRP, altRP, winRP, //RP = Re-Press
			awas, swas, cwas, wwas, afterEOS, afterEOL, //*was = alt/shift/ctrl was
			keyAfterCTRL, keyAfterALT, keyAfterALTGR, keyAfterSHIFT,
			clickAfterCTRL, clickAfterALT, clickAfterSHIFT,
			hotkeywithmodsfired, csdoing, incapt, waitfornum, 
			IsHotkey, ff_chr_wheeled, preSnip, LMB_down, RMB_down, MMB_down,
			dbl_click, click, selfie, aftsingleAS, JKLERR, JKLERRchecking;
		public static System.Windows.Forms.Timer click_reset = new System.Windows.Forms.Timer();
		public static System.Windows.Forms.Timer JKLERRT = new System.Windows.Forms.Timer();
		public static int skip_mouse_events, skip_spec_keys, cursormove = -1, guess_tries;
		static uint as_lword_layout = 0;
		static uint cs_layout_last = 0;
		static string lastClipText = "", busy_on = "", lastLWClearReason = "";
		static List<Keys> tempNumpads = new List<Keys>();
		static Keys preKey = Keys.None;
		public static List<char> c_snip = new List<char>();
		public static System.Windows.Forms.Timer doublekey = new System.Windows.Forms.Timer();
		public static List<YuKey> c_word_backup = new List<YuKey>();
		public static List<YuKey> c_word_backup_last = new List<YuKey>();
		public static List<IntPtr> PLC_HWNDs = new List<IntPtr>();
		/// <summary> Created for faster check if program is excluded, when checkin too many times(in hooks, timers etc.). </summary>
		public static List<IntPtr> EXCLUDED_HWNDs = new List<IntPtr>(); 
		public static Stopwatch pif = new Stopwatch();
		public static List<IntPtr> NOT_EXCLUDED_HWNDs = new List<IntPtr>(); 
		public static List<IntPtr> ConHost_HWNDs = new List<IntPtr>();
		public static string[] snipps = new []{ "mahou", "eml" };
		public static string[] exps = new [] {
			"Mahou (魔法) - Magical layout switcher.",
			"BladeMight@gmail.com"
		};
		public static string[] as_wrongs;
		public static string[] as_corrects;
		static Dictionary<string, string> DefaultTranslitDict = new Dictionary<string, string>() { 
				{"Щ", "SCH"}, {"щ", "sch"}, {"Ч", "CH"}, {"Ш", "SH"}, {"Ё", "JO"}, {"ВВ", "W"},
				{"Є", "EH"}, {"ю", "yu"}, {"я", "ya"}, {"є", "eh"}, {"Ж", "ZH"},
				{"ч", "ch"}, {"ш", "sh"}, {"Й", "JJ"}, {"ж", "zh"},
				{"Э", "EH"}, {"Ю", "YU"}, {"Я", "YA"}, {"й", "jj"}, {"ё", "jo"}, 
				{"э", "eh"}, {"вв", "w"}, {"кь", "q"}, {"КЬ", "Q"},
				{"ь", "j"}, {"№", "#"}, {"А", "A"}, {"Б", "B"},
				{"В", "V"}, {"Г", "G"}, {"Д", "D"}, {"Е", "E"}, {"З", "Z"}, 
				{"И", "I"}, {"К", "K"}, {"Л", "L"}, {"М", "M"}, {"Н", "N"},
				{"О", "O"}, {"П", "P"}, {"Р", "R"}, {"С", "S"}, {"Т", "T"},
				{"У", "U"}, {"Ф", "F"}, {"Х", "H"}, {"Ц", "C"}, {"Ъ", "'"}, 
				{"а", "a"}, {"б", "b"}, {"в", "v"}, {"г", "g"}, {"д", "d"},
				{"з", "z"}, {"и", "i"}, {"к", "k"}, {"л", "l"}, {"м", "m"},
				{"н", "n"}, {"о", "o"}, {"п", "p"}, {"р", "r"}, {"с", "s"}, 
				{"у", "u"}, {"ф", "f"}, {"х", "h"}, {"ц", "c"}, {"ъ", ":"},
				{"Ы", "Y"}, {"Ь", "J"}, {"е", "e"}, {"т", "t"}, {"ы", "y"}
		};
		static Dictionary<string, string> TranslitDict = new Dictionary<string, string>(DefaultTranslitDict);
		#endregion

		#region Keyboard, Mouse & Event hooks callbacks

		public static void DispatchKeyPress(int vkCode, uint MSG, short Flags = 0) {
			if (MahouUI.CaretLangTooltipEnabled)
				ff_chr_wheeled = false;
			if (vkCode > 254) return;
			var down = ((MSG == WinAPI.WM_SYSKEYDOWN) ? true : false) || ((MSG == WinAPI.WM_KEYDOWN) ? true : false);
			var Key = (Keys)vkCode; // "Key" will further be used instead of "(Keys)vkCode"
			if (MMain.CWords.Count == 0) {
				MMain.CWords.Add(new List<YuKey>());
			}
			if ((Key < Keys.D0 || Key > Keys.D9) && waitfornum && (uint)Key != MMain.Mahou.HKConMorWor.VirtualKeyCode && down)
				MMain.Mahou.FlushConvertMoreWords();
			#region Checks modifiers that are down
			switch (Key) {
				case Keys.LShiftKey:   shift = down; break;
				case Keys.LControlKey: ctrl = down; break;
				case Keys.LMenu:       alt = down; break;
				case Keys.LWin:        win = down; break;
				case Keys.RShiftKey:   shift_r = down; break;
				case Keys.RControlKey: ctrl_r = down; break;
				case Keys.RMenu:       alt_r = down; break;
				case Keys.RWin:        win_r = down; break;
			}
			// Additional fix for scroll tip.
			if (MahouUI.ScrollTip && Key == Keys.Scroll && down) {
				DoSelf(() => {
					KeybdEvent(Keys.Scroll, 0);
					KeybdEvent(Keys.Scroll, 2);
	              });
			}
			uint mods = 0;
			if (alt || alt_r)
				mods += WinAPI.MOD_ALT;
			if (ctrl || ctrl_r)
				mods += WinAPI.MOD_CONTROL;
			if (shift || shift_r)
				mods += WinAPI.MOD_SHIFT;
			if (win || win_r)
				mods += WinAPI.MOD_WIN;
			if (MMain.Mahou.HasHotkey(new Hotkey(false, (uint)Key, mods, 77))) {
				IsHotkey = true;
			} else
				IsHotkey = false;
			Logging.Log("Pressed hotkey?: "+IsHotkey+" => ["+Key+"+"+mods+"] .");
			if ((Key >= Keys.D0 || Key <= Keys.D9) && waitfornum)
				IsHotkey = true;
			if (MahouUI.OnceSpecific && !down) {
				MahouUI.OnceSpecific = false;
			}
			var printable = ((Key >= Keys.D0 && Key <= Keys.Z) || // This is 0-9 & A-Z
			                 Key >= Keys.Oem1 && Key <= Keys.OemBackslash || // Other printable
							(Control.IsKeyLocked(Keys.NumLock) && ( // while numlock is on
						     Key >= Keys.NumPad0 && Key <= Keys.NumPad9)) || // Numpad numbers 
						     Key == Keys.Decimal || Key == Keys.Subtract || Key == Keys.Multiply ||
						     Key == Keys.Divide || Key == Keys.Add); // Numpad symbols
			var printable_mod = !win && !win_r && !alt && !alt_r && !ctrl && !ctrl_r; // E.g. only shift is PrintAble
			//Key log
			Logging.Log("[KEY] > Catched Key=[" + Key + "] with VKCode=[" + vkCode + "] and message=[" + (int)MSG + "], modifiers=[" + 
			            (shift ? "L-Shift" : "") + (shift_r ? "R-Shift" : "") + 
			            (alt ? "L-Alt" : "") + (alt_r ? "R-Alt" : "") + 
			            (ctrl ? "L-Ctrl" : "") + (ctrl_r ? "R-Ctrl" : "") + 
			            (win ? "L-Win" : "") + (win_r ? "R-Win" : "") + "].");
			// Anti C-A-DEL C & A stuck rule
			if (Key == Keys.Delete) {
				if (ctrl && alt)
					ctrl = alt = false;
				if (ctrl && alt_r)
					ctrl = alt_r = false;
				if (ctrl_r && alt_r)
					ctrl_r = alt_r = false;
				if (ctrl_r && alt)
					ctrl_r = alt = false;
			}
			// Anti win-stuck rule
			if (Key == Keys.L) {
				if (win)
					win = false;
				if (win_r)
					win_r = false;
			}
			// Clear currentLayout in MMain.mahou rule
			if (!MahouUI.UseJKL || KMHook.JKLERR)
				if (((win || alt || ctrl || win_r || alt_r || ctrl_r) && Key == Keys.Tab) ||
				    win && (Key != Keys.None && 
				            Key != Keys.LWin && 
				            Key != Keys.RWin)) // On any Win+[AnyKey] hotkey
					MahouUI.currentLayout = 0;
			if (!down && (
			    ((alt || ctrl || alt_r || ctrl_r) && (Key == Keys.Shift || Key == Keys.LShiftKey || Key == Keys.RShiftKey)) ||
			     shift && (Key == Keys.Menu || Key == Keys.LMenu || Key == Keys.RMenu) ||
			     (IsNotWin7() && (win || win_r) && Key == Keys.Space))) {
				if (!MahouUI.UseJKL || KMHook.JKLERR) {
					var time = 200;
					if (IsNotWin7())
						time = 50;
					MahouUI.currentLayout = 0;
					as_lword_layout = 0;
					DoLater(() => { MahouUI.GlobalLayout = MahouUI.currentLayout = Locales.GetCurrentLocale(); }, time);
				}
			}
			#endregion
			#region
			var upper = false;
			if (MahouUI.LangPanelUpperArrow || MahouUI.mouseLTUpperArrow || MahouUI.caretLTUpperArrow)
				upper = IsUpperInput();
			if (MahouUI.LangPanelDisplay)
				if (MahouUI.LangPanelUpperArrow)
					MMain.Mahou._langPanel.DisplayUpper(upper);
			if (MahouUI.MouseLangTooltipEnabled)
				if (MahouUI.mouseLTUpperArrow)
					MMain.Mahou.mouseLangDisplay.DisplayUpper(upper);
			if (MahouUI.CaretLangTooltipEnabled)
				if (MahouUI.caretLTUpperArrow)
					MMain.Mahou.caretLangDisplay.DisplayUpper(upper);
			#endregion
			#region InputHistory
			var sym = '\0';
			if (MahouUI.WriteInputHistory) {
				if ((printable || Key == Keys.Enter || Key == Keys.Space) && printable_mod && down) {
					sym = getSym(vkCode);
					WriteToHistory(sym);
				}
				if (Key == Keys.Back && printable_mod && down) {
					if (MahouUI.InputHistoryBackSpaceWriteType == 0) {
						WriteToHistory("<Back>");
					} else 
						RemLastHistory();
				}
			}
			#endregion
			#region Snippets
			if (MahouUI.SnippetsEnabled && !ExcludedProgram()) {
				if (printable && printable_mod && down) {
					if (sym == '\0')
						sym = getSym(vkCode);
					c_snip.Add(sym);
					Logging.Log("[SNI] > Added ["+ sym + "] to current snippet.");
					Debug.WriteLine("added " + sym);
				}
				var seKey = Keys.Space;
				var asls = false;
				if (MMain.Mahou.SnippetsExpandType == "Tab")
					seKey = Keys.F14;
				if (Key == seKey || seKey == Keys.F14)
					preSnip = true;
				if (MSG == WinAPI.WM_KEYUP) {
					var snip = "";
					foreach (var ch in c_snip) {
						snip += ch;
//						Debug.WriteLine(ch);
					}
					var matched = false;
					Debug.WriteLine("Snip " + snip + ", last: " + last_snip);
					if (Key == seKey) {
		            	matched = CheckSnippet(snip);
		            	if (!matched)
		            		matched = CheckSnippet(last_snip+" "+snip, true);
						if (matched || preSnip)
							c_snip.Clear();
					}
					if (MahouUI.AutoSwitchEnabled && !matched && as_wrongs != null && Key == Keys.Space) {
						var CW = c_word_backup;
						var CLW = c_word_backup_last;
						if (MahouUI.AddOneSpace) {
							CW = MMain.CWord;
							CLW = c_word_backup;
						}
		            	asls = matched = CheckAutoSwitch(snip, CW);
		            	if (!matched) {
		            		var snip2x = last_snip+" "+snip;
		            		Debug.WriteLine("SNIp2x! " + snip2x);
		            		var SPace = new List<YuKey>(){ new YuKey() { Key = Keys.Space, IsAltNumPad = false, IsUpper = false } };
		            		var dash = new List<YuKey>(){ new YuKey() { Key = Keys.OemMinus, IsAltNumPad = false, IsUpper = false } };
		            		var last2words = CLW.Concat(dash).Concat(CW).ToList();
		            		asls = matched = CheckAutoSwitch(snip2x, last2words);
		            		if (!matched) {
			            		last2words = CLW.Concat(MahouUI.AddOneSpace ? new List<YuKey>() : SPace).Concat(CW).ToList();
			            		asls = matched = CheckAutoSwitch(snip2x, last2words);
		            		}
		            	}
    					var snl = WordGuessLayout(snip).Item2;
    					if (!matched) 
    						as_lword_layout = snl;
    					Logging.Log("[AS] > Last AS word layout: " +snl );
						c_snip.Clear();
					}
					if (Key == seKey && !asls) {
						last_snip = snip;
					}
				}
			}
			#endregion
			#region Release Re-Pressed keys
			if (hotkeywithmodsfired && !down &&
			   ((Key == Keys.LShiftKey || Key == Keys.LMenu || Key == Keys.LControlKey || Key == Keys.LWin) ||
			     (Key == Keys.RShiftKey || Key == Keys.RMenu || Key == Keys.RControlKey || Key == Keys.RWin))) {
				hotkeywithmodsfired = false;
				mods = 0;
				if (cwas) {
					cwas = false;
					mods += WinAPI.MOD_CONTROL;
				}
				if (swas) {
					swas = false;
					mods += WinAPI.MOD_SHIFT;
				}
				if (awas) {
					awas = false;
					mods += WinAPI.MOD_ALT;
				}
				if (wwas) {
					wwas = false;
					mods += WinAPI.MOD_WIN;
				}
				SendModsUp((int)mods);
			}
			#endregion
			#region One key layout switch
			if (!down)
				if (Key == Keys.LControlKey || Key == Keys.RControlKey)
					clickAfterCTRL = false;
				if (Key != Keys.LMenu && Key != Keys.RMenu)
					clickAfterALT = false;
				if (Key != Keys.LShiftKey && Key != Keys.RShiftKey)
					clickAfterSHIFT = false;
			if (MahouUI.ChangeLayouByKey) {
					if (((Key == Keys.LControlKey || Key == Keys.RControlKey) && !MahouUI.CtrlInHotkey) ||
					    ((Key == Keys.LShiftKey || Key == Keys.RShiftKey) && !MahouUI.ShiftInHotkey) ||
					    ((Key == Keys.LMenu || Key == Keys.RMenu) && !MahouUI.AltInHotkey) ||
					    ((Key == Keys.LWin || Key == Keys.RWin) && !MahouUI.WinInHotkey) ||
					    Key == Keys.CapsLock || Key == Keys.F18 || vkCode == 240 || Key == Keys.Tab) {
						SpecificKey(Key, MSG, vkCode);
				}
				if ((ctrl || ctrl_r) && (Key != Keys.LControlKey && Key != Keys.RControlKey && Key != Keys.ControlKey || clickAfterCTRL))
					keyAfterCTRL = true;
				else 
					keyAfterCTRL = false;
				if ((alt || alt_r) && (Key != Keys.LMenu && Key != Keys.RMenu && Key != Keys.Menu || clickAfterALT))
					keyAfterALT = true;
				else 
					keyAfterALT = false;
				if (((alt || alt_r) && (ctrl || ctrl_r)) && 
				    (Key != Keys.LMenu && Key != Keys.RMenu && Key != Keys.Menu || clickAfterALT) &&
				    (Key != Keys.LControlKey && Key != Keys.RControlKey && Key != Keys.Control || clickAfterCTRL))
					keyAfterALTGR = true;
				else 
					keyAfterALTGR = false;
				if ((shift || shift_r) && (Key != Keys.LShiftKey && Key != Keys.RShiftKey && Key != Keys.Shift || clickAfterSHIFT))
					keyAfterSHIFT = true;
				else 
					keyAfterSHIFT = false;
			}
			if (MSG == WinAPI.WM_KEYDOWN || MSG == WinAPI.WM_SYSKEYDOWN) {
				if (preKey == Keys.None) {
					preKey = Key;
					Debug.WriteLine("PREKEY: " +preKey);
				}
			}
			if (MSG == WinAPI.WM_KEYUP || MSG == WinAPI.WM_SYSKEYUP) {
				if ((int)Key == (int)preKey) {
					Debug.WriteLine("PREKEY-OFF: " +preKey);
					preKey = Keys.None;
				}
			}
			#endregion
			if ((ctrl||win||alt||ctrl_r||win_r||alt_r) && Key == Keys.Tab) {
				ClearWord(true, true, true, "Any modifier + Tab");
			}
			#region Other, when KeyDown
			if (MSG == WinAPI.WM_KEYDOWN && !waitfornum && !IsHotkey) {
				if (Key == Keys.Back) { //Removes last item from current word when user press Backspace
					if (MMain.CWord.Count != 0) {
						MMain.CWord.RemoveAt(MMain.CWord.Count - 1);
					}
					if (MMain.CWords.Count > 0) {
						if (MMain.CWords[MMain.CWords.Count - 1].Count - 1 > 0) {
							Logging.Log("[WORD] > Removed key [" + MMain.CWords[MMain.CWords.Count - 1][MMain.CWords[MMain.CWords.Count - 1].Count - 1].Key + "] from last word in words.");
						    RemoveLastKeyFromGlobalInputBuffer();
						} else {
							Logging.Log("[WORD] > Removed one empty word from current words.");
							RemoveLastWordFromGlobalInputBuffer();
						}
					}
					if (MahouUI.SnippetsEnabled) {
						if (c_snip.Count != 0) {
							c_snip.RemoveAt(c_snip.Count - 1);
							Logging.Log("[SNI] >Removed one character from current snippet.");
						}
					}
				}
				//Pressing any of these Keys will empty current word, and snippet
				if (Key == Keys.Home || Key == Keys.End ||
				    (Key == Keys.Tab && MMain.Mahou.SnippetsExpandType != "Tab" && snipps.Length > 0) || Key == Keys.PageDown || Key == Keys.PageUp ||
				   Key == Keys.Left || Key == Keys.Right || Key == Keys.Down || Key == Keys.Up ||
				   Key == Keys.BrowserSearch || ((win||win_r) && (Key >= Keys.D1 && Key <= Keys.D9)) ||
				   ((ctrl||win||alt||ctrl_r||win_r||alt_r) && (Key != Keys.Menu  && //Ctrl modifier and key which is not modifier
							Key != Keys.LMenu &&
							Key != Keys.RMenu &&
							Key != Keys.LWin &&
							Key != Keys.ShiftKey &&
							Key != Keys.RShiftKey &&
							Key != Keys.LShiftKey &&
							Key != Keys.RWin &&
							Key != Keys.ControlKey &&
							Key != Keys.LControlKey &&
							Key != Keys.RControlKey ))) { 
					ClearWord(true, true, true, "Pressed combination of key and modifiers(not shift) or key that changes caret position.");
				}
				if (Key == Keys.Space) {
					Logging.Log("[FUN] > Adding one new empty word to words, and adding to it [Space] key.");
					MMain.CWords.Add(new List<YuKey>());
				    AddKeyToGlobalInputBuffer(new YuKey() { Key = Keys.Space });
					if (MahouUI.AddOneSpace && MMain.CWord.Count != 0 &&
					   MMain.CWord[MMain.CWord.Count - 1].Key != Keys.Space) {
						Logging.Log("[FUN] > Eat one space passed, next space will clear last word.");
						MMain.CWord.Add(new YuKey() { Key = Keys.Space });
						afterEOS = true;
					} else {
						ClearWord(true, false, false, "Pressed space");
						afterEOS = false;
					}
				}
				if (Key == Keys.Enter) {
					if (MahouUI.Add1NL && MMain.CWord.Count != 0 && 
					    MMain.CWord[MMain.CWord.Count - 1].Key != Keys.Enter) {
						Logging.Log("[FUN] > Eat one New Line passed, next Enter will clear last word.");
						MMain.CWord.Add(new YuKey() { Key = Keys.Enter });
					    AddKeyToGlobalInputBuffer(new YuKey() { Key = Keys.Enter });
						afterEOL = true;
					} else {
						ClearWord(true, true, true, "Pressed enter");
						afterEOL = false;
					}
					as_lword_layout = 0;
				}
				if (printable && printable_mod) {
					if (afterEOS) { //Clears word after Eat ONE space
						ClearWord(true, false, false, "Clear last word after 1 space");
						afterEOS = false;
					}
					if (afterEOL) { //Clears word after Eat ONE enter
						ClearWord(true, false, false, "Clear last word after 1 enter");
						afterEOL = false;
					}
					var upr = IsUpperInput();
					MMain.CWord.Add(new YuKey() { Key = Key, IsUpper = upr });
				    AddKeyToGlobalInputBuffer(new YuKey() { Key = Key, IsUpper = upr });
					Logging.Log("[WORD] > Added [" + Key + "]^"+upr);
				}
			}
			#endregion
			#region Alt+Numpad (fully workable)
			if (incapt &&
			   (Key == Keys.RMenu || Key == Keys.LMenu || Key == Keys.Menu) && !down) {
				Logging.Log("[NUM] > Capture of numpads ended, captured [" + tempNumpads.Count + "] numpads.");
				if (tempNumpads.Count > 0) { // Prevents zero numpads(alt only) keys
					MMain.CWord.Add(new YuKey() {
						IsAltNumPad = true,
						Numpads = new List<Keys>(tempNumpads)//new List => VERY important here!!!
					});                                      //It prevents pointer to tempNumpads, which is cleared.
				    AddKeyToGlobalInputBuffer(new YuKey() { IsAltNumPad = true, Numpads = new List<Keys>(tempNumpads) });
				}
				tempNumpads.Clear();
				incapt = false;
			}
			if (!incapt && (alt || alt_r) && down) {
				Logging.Log("[NUM] > Alt is down, starting capture of Numpads...");
				incapt = true;
			}
			if ((alt || alt_r) && incapt) {
				if (Key >= Keys.NumPad0 && Key <= Keys.NumPad9 && !down) {
					tempNumpads.Add(Key);
				}
			}
			#endregion
			#region Reset Modifiers in Hotkeys
			MahouUI.ShiftInHotkey = MahouUI.AltInHotkey = MahouUI.WinInHotkey = MahouUI.CtrlInHotkey = false;
			#endregion
			preSnip = false;
			#region Update LD
			MMain.Mahou.UpdateLDs();
			#endregion
		}

	    private static void AddKeyToGlobalInputBuffer(YuKey key)
	    {
	        MMain.CWords[MMain.CWords.Count - 1].Add(key);
        }

	    private static void RemoveLastKeyFromGlobalInputBuffer()
	    {
	        MMain.CWords[MMain.CWords.Count - 1].RemoveAt(MMain.CWords[MMain.CWords.Count - 1].Count - 1);
        }

	    private static void RemoveLastWordFromGlobalInputBuffer()
	    {
	        MMain.CWords.RemoveAt(MMain.CWords.Count - 1);
        }

		public static void ListenMouse(ushort MSG) {
			if ((MSG == (ushort)WinAPI.RawMouseButtons.MouseWheel)) {
				if (MMain.Mahou.caretLangDisplay.Visible && MahouUI.CaretLangTooltipEnabled) {
					var _fw = WinAPI.GetForegroundWindow();
					var _clsNMb = new StringBuilder(40);
					WinAPI.GetClassName(_fw, _clsNMb, _clsNMb.Capacity);
					var clsNM = _clsNMb.ToString();
					if (clsNM == "MozillaWindowClass" || clsNM.Contains("mozilla") || clsNM.Contains("Chrome_WidgetWin"))
						ff_chr_wheeled = true;
				}
			}
			if (MSG == (ushort)WinAPI.RawMouseButtons.LeftDown || MSG == (ushort)WinAPI.RawMouseButtons.RightDown) {
				if (ctrl || ctrl_r)
					clickAfterCTRL = true;
				if (shift || shift_r)
					clickAfterSHIFT = true;
				if (alt || alt_r)
					clickAfterALT = true;
				if (!MahouUI.UseJKL || KMHook.JKLERR)
					MahouUI.currentLayout = 0;
				ClearWord(true, true, true, "Mouse click");
			}
			#region Double click show translate
			if (MahouUI.TrEnabled)
				if (MahouUI.TrOnDoubleClick) {
					if (MSG == (ushort)WinAPI.RawMouseButtons.LeftUp || MSG == (ushort)WinAPI.RawMouseButtons.RightUp) {
						if (dbl_click) {
							Debug.WriteLine("DBL");
							MahouUI.ShowSelectionTranslation(true);
							dbl_click = click = false;
						}
					}
					if (MSG == (ushort)WinAPI.RawMouseButtons.LeftDown || MSG == (ushort)WinAPI.RawMouseButtons.RightDown) {
						if (!click) {
							pif.Start();
							click = true;
							click_reset.Interval = SystemInformation.DoubleClickTime;
							click_reset.Tick += (_, __) => {
								click = false;
								Debug.WriteLine("Slow second click!");
								click_reset.Stop();
								click_reset.Dispose();
								click_reset = new System.Windows.Forms.Timer();
							};
							click_reset.Start();
							Debug.WriteLine("First click, reset after: " + SystemInformation.DoubleClickTime);
						} else {
							var el = pif.ElapsedMilliseconds;
							pif.Reset();
							if (el <= 5) {
								Debug.WriteLine("Too fast ["+el+"ms], probably buggy...");
								click_reset.Stop();
								click_reset.Dispose();
								click_reset = new System.Windows.Forms.Timer();
								click = false;
							} else {
								Debug.WriteLine("Second click, after: [" + el + "ms] + kill reset + waiting to Up button");
								click_reset.Stop();
								click_reset.Dispose();
								click_reset = new System.Windows.Forms.Timer();
								dbl_click = true;
								click = false;
							}
						}
					}
				}
			#endregion
			if (MahouUI.LDUseWindowsMessages) {
				if (MSG == (ushort)WinAPI.RawMouseButtons.LeftDown)
					LMB_down = true;
				else if (MSG == (ushort)WinAPI.RawMouseButtons.LeftUp)
					LMB_down = false;
				if (MSG == (ushort)WinAPI.RawMouseButtons.RightDown)
					RMB_down = true;
				else if (MSG == (ushort)WinAPI.RawMouseButtons.RightUp)
					RMB_down = false; 
				if (MSG == (ushort)WinAPI.RawMouseButtons.MiddleDown)
					MMB_down = true;
				else if (MSG == (ushort)WinAPI.RawMouseButtons.MiddleUp)
					MMB_down = false;
				if (MSG == (ushort)WinAPI.RawMouseButtons.MouseWheel ||
					MSG == (ushort)WinAPI.RawMouseButtons.LeftUp ||
					MSG == (ushort)WinAPI.RawMouseButtons.RightUp ||
					MSG == (ushort)WinAPI.RawMouseButtons.MiddleUp) {
					if (MahouUI.LDForCaret) {
						MMain.Mahou.UpdateCaredLD();
					}
				}
				if (MSG == (ushort)WinAPI.RawMouseButtons.LeftUp ||
					MSG == (ushort)WinAPI.RawMouseButtons.RightUp ||
					MSG == (ushort)WinAPI.RawMouseButtons.MiddleUp)
					if (MahouUI.CaretLangTooltipEnabled)
						ff_chr_wheeled = false;
				if (skip_mouse_events-- == 0 || skip_mouse_events == 0) {
					skip_mouse_events = MahouUI.LD_MouseSkipMessagesCount;
					if (MSG == (ushort)WinAPI.RawMouseFlags.MoveRelative) {
						if (MahouUI.LDForMouse) {
							MMain.Mahou.UpdateMouseLD();
						}
						if ((LMB_down || RMB_down || MMB_down)) {
							if (MahouUI.LDForCaret) {
								MMain.Mahou.UpdateCaredLD();
							}
						}
					}
				}
			}
		}
		public static void LDEventHook(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject,
		                                     int idChild, uint dwEventThread, uint dwmsEventTime) {
			if (MahouUI.LDUseWindowsMessages) {
				if (eventType == WinAPI.EVENT_OBJECT_FOCUS) {
					if (MMain.Mahou != null)
						MMain.Mahou.UpdateLDs();
				}
			}
		}
		public static void EventHookCallback(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject,
		                                       int idChild, uint dwEventThread, uint dwmsEventTime) {
			if (MahouUI.PersistentLayoutOnWindowChange) {
				var proc = Locales.ActiveWindowProcess();
				var cont = PLC_HWNDs.Contains(hwnd);
				if (!cont || !MahouUI.PersistentLayoutOnlyOnce) {
					if (MahouUI.PersistentLayoutForLayout1)
						MMain.Mahou.PersistentLayoutCheck(MMain.Mahou.PersistentLayout1Processes, MahouUI.MAIN_LAYOUT1, proc.ProcessName);
					if (MahouUI.PersistentLayoutForLayout2)
						MMain.Mahou.PersistentLayoutCheck(MMain.Mahou.PersistentLayout2Processes, MahouUI.MAIN_LAYOUT2, proc.ProcessName);
				}
				if (MahouUI.PersistentLayoutOnlyOnce && !cont)
					PLC_HWNDs.Add(hwnd);
			}
			var hwndLayout = Locales.GetCurrentLocale(hwnd);
			as_lword_layout = 0;
			var conhost = false;
			if (MahouUI.UseJKL && !KMHook.JKLERR) {
				if (ConHost_HWNDs.Contains(hwnd)) {
					conhost = true;
					Logging.Log("[JKL] > Known ConHost window: " + hwnd);
					jklXHidServ.CycleAllLayouts(hwnd);
				} else {
					var strb = new StringBuilder(350);
					WinAPI.GetClassName(hwnd, strb, strb.Capacity);
					if (strb.ToString() == "ConsoleWindowClass" || strb.ToString() == "Chrome_WidgetWin_1") {
						conhost = true;
						Logging.Log("[JKL] > ["+hwnd+"] = ConHost window, remembering...");
						ConHost_HWNDs.Add(hwnd);
						jklXHidServ.CycleAllLayouts(hwnd);
					}
				}
			}
			if (!MahouUI.UseJKL || KMHook.JKLERR || !conhost) {
				MahouUI.currentLayout = /*MahouUI.GlobalLayout =*/ hwndLayout;
				Logging.Log("[FOCUS] > Updating currentLayout on window activate to ["+MahouUI.currentLayout+"]...");
			}
			Logging.Log("Hwnd " + hwnd + ", layout: " + hwndLayout + ", Mahou layout: " + MahouUI.GlobalLayout);		
			if (MahouUI.OneLayout)
				if (hwndLayout != MahouUI.GlobalLayout) {
					var title = new StringBuilder(128);
					WinAPI.GetWindowText(hwnd, title, 127);
					DoLater(() => {
						Logging.Log("[ONEL] > Layout in this window ["+title+"] was different, changing layout to Mahou global layout.");
						ChangeToLayout(hwnd, MahouUI.GlobalLayout);
			       	 }, 100);
				}
		}
		#endregion
		#region Functions/Struct
		static bool _hasKey(string[] ar, string key) {
			for (var i = 0; i < ar.Length; i++) {
				if (ar[i] == null) continue;
				if (key.Length == ar[i].Length) {
					if (ar[i].ToLowerInvariant() == key.ToLowerInvariant()) return true;
				}
			}
			return false;
		}
		static bool CheckAutoSwitch(string snip, List<YuKey> word, bool single = true) {
			var matched = false;
			var corr = "";
			var snil = snip.ToLowerInvariant();
			foreach (var element in word) {
				Debug.WriteLine(element.Key);
			}
			for (var i = 0; i < as_wrongs.Length; i++) {
				if (as_corrects.Length > i) {
//					if (snip == as_wrongs[i]) {
//						ExpandSnippet(snip, as_corrects[i], MMain.mahou.AutoSwitchSpaceAfter, MMain.mahou.AutoSwitchSwitchToGuessLayout);
//						break;
//					} else {
	    			if (as_wrongs[i] == null)
	    				break;
						if (snip.Length == as_wrongs[i].Length) {
							if (snil == as_wrongs[i].ToLowerInvariant()) {
	        					if (MahouUI.SoundOnAutoSwitch)
	        						MahouUI.SoundPlay();
	        					if (MahouUI.SoundOnAutoSwitch2)
	        						MahouUI.Sound2Play();
	        					corr = as_corrects[i];
	        					var snl = WordGuessLayout(snil).Item2;
	        					var asl = WordGuessLayout(as_corrects[i]).Item2;
	        					if (_hasKey(as_wrongs, as_corrects[i])) {
	        						Logging.Log("[AS] > Double-layout autoswitch rule: " +as_wrongs[i] +"<=>" +as_corrects[i]);
	        						if (snl == as_lword_layout) {
	        							Logging.Log("[AS] > Leave as it was: "+snil);
	        							break;
	        						}
	        					}
    							as_lword_layout = asl;
	        					var skipLS = (snl == asl);
	        					Debug.WriteLine("snl: " +snil + ", l:" +snl + "\nas_crI: " + as_corrects[i] + ", l: " +asl + "\nSKIP: " +skipLS);
	        					var ofk = false;
	        					if (!skipLS) {
	        						if (MahouUI.UseJKL && MahouUI.SwitchBetweenLayouts && MahouUI.EmulateLS && !KMHook.JKLERR) {
										jklXHidServ.OnLayoutAction = asl;
										var was = Locales.GetCurrentLocale();
	        							jklXHidServ.ActionOnLayout = () =>
								        {
								            if (!MahouUI.AddOneSpace)
								                DeleteFromInput();
											else if (!MahouUI.AutoSwitchSpaceAfter) {
								                DeleteFromInput();
                                                word.RemoveAt(word.Count-1);
											}
				        					StartConvertWord(word.ToArray(), was, true);
											ExpandSnippet(snip, as_corrects[i], !MahouUI.AddOneSpace && MahouUI.AutoSwitchSpaceAfter,
											MahouUI.AutoSwitchSwitchToGuessLayout, true);
										};
	        						} else ofk = true;
        							ChangeToLayout(Locales.ActiveWindow(), asl);
        							Debug.WriteLine("ASL"+asl);
	        					} else ofk = true;
	        					if (ofk) {
									if (!MahouUI.AddOneSpace)
									    DeleteFromInput();
                                else if (!MahouUI.AutoSwitchSpaceAfter) {
									    DeleteFromInput();
                                    word.RemoveAt(word.Count-1);
									}
									StartConvertWord(word.ToArray(), Locales.GetCurrentLocale(), true);
									ExpandSnippet(snip, as_corrects[i], !MahouUI.AddOneSpace && MahouUI.AutoSwitchSpaceAfter,
		        					              MahouUI.AutoSwitchSwitchToGuessLayout, true);
	        					}
								matched = true;
								break;
							}
						}
//					}
				} else {
					Logging.Log("[AS] > word ["+snip+"] has no expansion, snippet is not finished or its expansion commented.", 1);
				}
			}
			if (matched) {
				Logging.Log("[AS] > Changed last snippet to AS-ed, "+corr+", instead of ignorecase: "+ snil);
				aftsingleAS = single;
				last_snip = corr;
			}
			return matched;
		}
		static bool CheckSnippet(string snip, bool xx2 = false) {
			var matched = false;
			var x2 = xx2 && aftsingleAS && !MahouUI.AutoSwitchSpaceAfter;
			Logging.Log("[SNI] > Current snippet is [" + snip + "].");
			for (var i = 0; i < snipps.Length; i++) {
				if (snipps[i] == null) break;
				if (snipps[i].Contains(__ANY__)) {
					var any = "";
					var pins = snipps[i];
					var len = pins.Length;
					var at = pins.IndexOf(__ANY__, StringComparison.InvariantCulture);
					var aft = at+__ANY__.Length;
//					Debug.WriteLine("aftst:"+pins[aft]);
					var laf = len-aft;
					if (snip.Length < laf+at) {
						Logging.Log("[SNI] > Too small snip, to use with "+__ANY__);
						continue;
					}
//					Debug.WriteLine("at:"+at+",aft:"+aft+",laf:"+laf);
					var yay = true;
					if (at <= snip.Length)
						for (var f = 0; f != at; f++) {
							if (snip[f] != pins[f]) yay = false;
						}
					for (var f = 0; f != laf; f++) {
						var t = f + (pins.Length-laf);
						var g = f + (snip.Length-laf);
//						Debug.WriteLine("Calc: " + g + ", " + t +  ", " + at + ", " + laf);
						if (g > snip.Length || g < 0) continue;
//						Debug.WriteLine("Cht: " + snip[g] + ", " + pins[t]);
						if (snip[g] != pins[t]) yay = false;
					}
					if (yay) {
    					if (MahouUI.SoundOnSnippets)
    						MahouUI.SoundPlay();
    					if (MahouUI.SoundOnSnippets2)
    						MahouUI.Sound2Play();
						any = snip.Substring(at, (snip.Length-laf-at));
//						Debug.WriteLine("Yay!" + any);
						Logging.Log("[SNI] > Current snippet [" + snip + "] matched with "+__ANY__+" existing snippet [" + exps[i] + "].");
						var exp = exps[i].Replace(__ANY__, any);
//						Debug.WriteLine("exp: " + exp);
						ExpandSnippet(snip, exp, MahouUI.SnippetSpaceAfter, MahouUI.SnippetsSwitchToGuessLayout, false, x2);
						aftsingleAS = false;
						break;
					}
//		    		Debug.WriteLine("ANY " + yay);
			    }
				if (snip == snipps[i]) {
					if (exps.Length > i) {
    					if (MahouUI.SoundOnSnippets)
    						MahouUI.SoundPlay();
    					if (MahouUI.SoundOnSnippets2)
    						MahouUI.Sound2Play();
						Logging.Log("[SNI] > Current snippet [" + snip + "] matched existing snippet [" + exps[i] + "].");
						ExpandSnippet(snip, exps[i], MahouUI.SnippetSpaceAfter, MahouUI.SnippetsSwitchToGuessLayout, false, x2);
						matched = true;
					} else {
						Logging.Log("[SNI] > Snippet ["+snip+"] has no expansion, snippet is not finished or its expansion commented.", 1);
					}
					aftsingleAS = false;
					break;
				}
			}
			return matched;
		}
		static void RemLastHistory() {
			try {
				var txt = System.IO.File.ReadAllText(System.IO.Path.Combine(MahouUI.nPath, "history.txt"));
				if (txt.Length<1) return;
				txt = txt.Substring(0, txt.Length-1);
				System.IO.File.WriteAllText(System.IO.Path.Combine(MahouUI.nPath, "history.txt"), txt);
			} catch (Exception e) {
				if (!Configs.SwitchToAppData(true, e))
					MahouUI.WriteInputHistory = false;
				Logging.Log("Write history(r) error: "+e.Message, 1);
			}
		}
		static void WriteToHistory(char c) {
			try {
				var sw = System.IO.File.AppendText(System.IO.Path.Combine(MahouUI.nPath, "history.txt"));
				sw.Write(c);
				sw.Close();
			} catch (Exception e) {
				if (!Configs.SwitchToAppData(true, e))
					MahouUI.WriteInputHistory = false;
				Logging.Log("Write history(c) error: "+e.Message, 1);
			}
		}
		static void WriteToHistory(string s) {
			try {
				var sw = System.IO.File.AppendText(System.IO.Path.Combine(MahouUI.nPath, "history.txt"));
				sw.Write(s);
				sw.Close();
			} catch (Exception e) {
				if (!Configs.SwitchToAppData(true, e))
					MahouUI.WriteInputHistory = false;
				Logging.Log("Write history(s) error: "+e.Message, 1);
			}
		}
		static char getSym(int vkCode) {
			var stb = new StringBuilder(10);
			var byt = new byte[256];
			if (IsUpperInput()) {
				byt[(int)Keys.ShiftKey] = 0xFF;
			}
			var layout = Locales.GetCurrentLocale() & 0xffff;
			if (MahouUI.UseJKL && !KMHook.JKLERR) {
				if (layout != (MahouUI.currentLayout & 0xffff)) {
					if (IsConhost())
						layout = MahouUI.currentLayout & 0xffff;
				}
			}
			WinAPI.ToUnicodeEx((uint)vkCode, (uint)vkCode, byt, stb, stb.Capacity, 0, (IntPtr)layout);
			if (stb.Length > 0) {
				var c = stb.ToString()[0];
				return c;	
			}
			return '\0';
		}
		public static void ReloadTSDict() {
			var tsdict = new Dictionary<string, string>();
			var tsdictp = System.IO.Path.Combine(MahouUI.nPath, "TSDict.txt");
			if (System.IO.File.Exists(tsdictp)) {
				var lines = System.IO.File.ReadAllLines(tsdictp);
				for (var i = 0; i != lines.Length; i++) {
					var line = lines[i];
					if (line.Contains("|")) {
				    	var lr = line.Split('|');
				    	tsdict[lr[0]] = lr[1];
//				    	Debug.WriteLine("Added to TSDict: " +lr[0] +" <=> " + lr[1]);
					} else {
						Logging.Log("[TRANSLTRT] > Wrong Transliteration Dict line #"+i+", => " +line);
				    	tsdict = null;
				    	break;
					}
				}
			} else {
				var raw = "";
				foreach (var kv in DefaultTranslitDict) {
					raw += kv.Key+"|"+kv.Value+"\r\n";
				}
				System.IO.File.WriteAllText(tsdictp, raw);
			}
			if (tsdict != null && tsdict.Count != 0) {
				Logging.Log("[TRANSLTRT] > Succesfully initialized Transliteration Dictionary from ["+tsdictp+"].");
				TranslitDict = tsdict;
			} else {
				Logging.Log("[TRANSLTRT] > "+tsdictp+" missing or wrong syntax reset to default transliteration Dictionary.");
				TranslitDict = DefaultTranslitDict;
			}
		}
		static void ExpandSnippet(string snip, string expand, bool spaceAft, bool switchLayout, bool ignoreExpand = false, bool x2 = false) {
			DoSelf(() => {
				try {
		       		Debug.WriteLine("Snippet: " +snip);
					if (switchLayout) {
						var guess = WordGuessLayout(expand);
						Logging.Log("[SNI] > Changing to guess layout [" + guess.Item2 + "] after snippet ["+ guess.Item1 + "].");
						ChangeToLayout(Locales.ActiveWindow(), guess.Item2);
					}
					if (!ignoreExpand) {
		       			var backs = snip.Length+1;
		       			Debug.WriteLine("X2" + x2);
		       			if (x2||MMain.Mahou.SnippetsExpandType == "Tab") backs--;
					    DeleteFromInput(backs);
                        Logging.Log("[SNI] > Expanding snippet [" + snip + "] to [" + expand + "].");
		       			ExpandSnippetWithExpressions(expand);
						ClearWord(true, true, false, "Cleared due to snippet expansion");
//						KInputs.MakeInput(KInputs.AddString(expand));
					}
		       		if (spaceAft && !expand.Contains("__cursorhere"))
						KInputs.MakeInput(KInputs.AddString(" "));
					DoLater(() => MMain.Mahou.Invoke((MethodInvoker)delegate {
						MMain.Mahou.UpdateLDs();
					}), snip.Length*2);
		       	} catch(Exception e) {
					Logging.Log("[SNI] > Some snippets configured wrong, check them, error:\r\n" + e.Message +"\r\n" + e.StackTrace+"\r\n", 1);
					// If not use TASK, form(MessageBox) won't accept the keys(Enter/Escape/Alt+F4).
					var msg = new [] {"", ""};
					msg[0] = MMain.Lang[Languages.Element.MSG_SnippetsError];
					msg[1] = MMain.Lang[Languages.Element.Error];
					var tsk = new System.Threading.Tasks.Task(() => MessageBox.Show(msg[0], msg[1], MessageBoxButtons.OK, MessageBoxIcon.Error));
					tsk.Start();
					KInputs.MakeInput(KInputs.AddString(snip));
				}
              });
		}
		#region in Snippets expressions  
		static readonly string[] expressions = new []{ "__date", "__time", "__version", "__system", "__title", "__keyboard", "__execute", "__cursorhere", "__paste", "__mahouhome", "__delay" };
		static void ExpandSnippetWithExpressions(string expand) {
			string ex = "", args = "", raw = "", err = "";
			bool args_getting = false, is_expr = false, escaped = false;
			var expr_start = -1;
			var contains_expr = false;
			foreach (var expr in expressions) {
				if (expand.Contains(expr)) {
					contains_expr = true;
					break;
				}
			}
			if (!contains_expr) {
				KInputs.MakeInput(KInputs.AddString(expand));
				return;
			}
			for (var i = 0; i!=expand.Length; i++) {
				var args_get = false;
				var e = expand[i]; 
//				Debug.WriteLine("i:"+i+", e:"+e);
				if (!is_expr)
					ex += e;
				else err+=e;
				if (is_expr && e == ')') { // Escape closing
					if (expand[i-1] == '\\') {
						Logging.Log("[EXPR] > Escaped \")\" at position: "+i);
						if (args.Length >2)
							args = args.Substring(0, args.Length-1);
					} else {
						if (args_getting) {
							args_getting = false;
							args_get = true;
	//						Debug.WriteLine("end of args of: " + fun + " -> " +i);
						} else {
							Logging.Log("[EXPR] > Expression \"(\" missing, but \")\" were there, in ["+ex+"], at position: "+expr_start+" in ["+expand+"]");
							KInputs.MakeInput(KInputs.AddString(ex+err));
							is_expr = false;
							args_get = false;
							escaped = false;
							args = ex = raw = "";
						}
					}
				}
				if (args_getting)
					args += e;
				if (is_expr && e == '(' && !args_getting) {
					args_getting = true; 
//					Debug.WriteLine("start of args of: " + fun + " -> " +i);
				}
				var maybe_fun = false;
				if (!args_getting && !string.IsNullOrEmpty(ex) && !is_expr) {
					foreach (var expr in expressions) {
						if (expr.StartsWith(ex, StringComparison.InvariantCulture)) {
							maybe_fun = true;
				    		if (expr == ex) {
								expr_start = i - (ex.Length-1);
								escaped = false;
								if (expr_start-1<0)
									escaped = false;
								else if (expand[expr_start-1] == '\\')
									escaped = true;
								is_expr = !escaped;
//								Debug.WriteLine("expr: " +expr+" equals " + ex + ", expr_start: " + expr_start + " is_expr: " + is_expr);
								err = "";
								break;
				    		}
						} else
							maybe_fun = false;
//						Debug.WriteLine("Try: " +fun+" > " + expr + (maybe_fun ? " OK" : " NO"));
						if (maybe_fun) break;
					}
				}
				if (is_expr && i == expand.Length-1 && !args_get) {
					Logging.Log("[EXPR] > Expression [" + ex +"] missing its end \")\", at positon: " + expr_start +" in: [" + expand + "].", 2);
					KInputs.MakeInput(KInputs.AddString(ex+err+args));
					err = "";
				}
				if (args_get && !escaped) {
					Logging.Log("[EXPR] > Executing expression: " + ex + " with args: [" + args + "]");
					var curlefts = expand.Length - i -1;
					ExecExpression(ex, args, curlefts);
					is_expr = false;
					args_get = false;
					args = ex = "";
				}
				if (!args_getting && !maybe_fun && !is_expr) {
					if (!escaped) {
//						Debug.WriteLine("Not even start of any expression: " + ex);
						raw += ex;
					}
					ex = "";
					maybe_fun = false;
					is_expr = false;
					expr_start = -1;
				}
				if (!string.IsNullOrEmpty(raw)) {
//					Debug.WriteLine("Inputting raw: ["+raw+"]");
					KInputs.MakeInput(KInputs.AddString(raw));
					raw = "";
				}
				if (escaped) {
					Logging.Log("[EXPR] > Ignored espaced expression: " + ex);
				    DeleteFromInput();
                    KInputs.MakeInput(KInputs.AddString(ex));
					is_expr = false;
					args_get = false;
					escaped = false;
					args = ex = raw = "";
				}
			}
			if (cursormove != -1) {
				KInputs.MakeInput(KInputs.AddPress(Keys.Left, cursormove));
			}
			cursormove = -1;
				
		}
		static void ExecExpression(string expr, string args, int curlefts = -1) {
			switch (expr) {
				case "__paste":
					Logging.Log("[EXPR] > Pasting text from snippet.");
					Debug.WriteLine("Paste: " + args);
					GetClipStr();
					RestoreClipBoard(Regex.Replace(args, "\r?\n|\r", Environment.NewLine));
					KInputs.MakeInput(KInputs.AddPress(Keys.V), (int)WinAPI.MOD_CONTROL);
					DoLater(() => RestoreClipBoard(), 300);
					break;
				case "__date":
				case "__time":
					var now = DateTime.Now;
					var format = args;
					if (string.IsNullOrEmpty(args)) {
						if (expr == "__date")
							format = "dd/MM/yyyy";
						else 
							format = "HH:mm:ss";
					}
					KInputs.MakeInput(KInputs.AddString(now.ToString(format)));
					break;
				case "__version":
					KInputs.MakeInput(KInputs.AddString(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()));
					break;
				 case "__title":
					KInputs.MakeInput(KInputs.AddString(MMain.Mahou.Text));
					 break;
				case "__system":
					KInputs.MakeInput(KInputs.AddString(Environment.OSVersion.ToString()));
					break;
				case "__keyboard":
					SimKeyboard(args);
					break;
				case "__execute":
					Execute(args);
					break;
				case "__delay":
					var d = 0;
					if (Int32.TryParse(args, out d))
						Thread.Sleep(d);
					break;
				case "__mahouhome":
					KInputs.MakeInput(KInputs.AddString(MahouUI.nPath));
					break;
				case "__cursorhere":
					Debug.WriteLine("Curlefts: " +curlefts);
					cursormove = curlefts;
					break;
			}
		}
		static void Execute(string args) {
			string fil = "", arg ="";
			var fil_get = false;
			for (var i = 0; i < args.Length; i++) {
				var c = args[i];
				Debug.WriteLine("c: " +c);
				if (!fil_get) {
					if (c == '|') {
						fil_get = true;
					} else fil += c;
				} else {
					arg += c;
				}
			}
			Logging.Log("[EXPR] > Executing: executable: ["+fil+"] with args: ["+arg+"].");
			var p = new ProcessStartInfo();
			p.Arguments = arg;
			p.UseShellExecute = true;
			p.FileName = fil;
			try {
				Process.Start(p);
			} catch(Exception e) {
				Logging.Log("[EXPR] > Execute error: " + e.Message);
			}
		}
		static void SimKeyboard(string args) {
			string[] multi_args;
			var all_keys = new List<List<Keys>>();
			if (args.Contains(" "))
				multi_args = args.Split(' ');
			else
				multi_args = new []{args};
			for (var i = 0; i!= multi_args.Length; i++) {
				var keys = new List<Keys>();
				var _args = multi_args[i];
				string[] multi_keys;
				if (_args.Contains("+"))
					multi_keys = _args.Split('+');
				else 
					multi_keys = new []{_args};
				for (var j = 0; j != multi_keys.Length; j++) {
					var key =  multi_keys[j].ToLower();
					foreach (Keys k in Enum.GetValues(typeof(Keys))) {
						var _n = k.ToString().ToLower()
							.Replace("menu", "alt").Replace("control", "ctrl")
							.Replace("d0", "0").Replace("d1", "1")
							.Replace("d2", "2").Replace("d3", "3")
							.Replace("d4", "4").Replace("d5", "5")
							.Replace("d6", "6").Replace("d7", "7")
							.Replace("d8", "9").Replace("d9", "9")
							.Replace("return", "enter").Replace("numpa", "numpad");
						if (_n == key+"key") { // controlkey, shiftkey
							Logging.Log("Added the " + _n);
							keys.Add(k);
							break;
						}
						if (key.Length>1) {
							if (key[0] == '[' && key[key.Length-1] == ']') {
								var scode = key.Substring(1,key.Length-2).ToLower();
								var code = -1;
								var ok = false;
								if (scode.Contains("x")) {
									scode = scode.Replace("x", "");
									ok = Int32.TryParse(scode, System.Globalization.NumberStyles.HexNumber, null, out code);
								} else {
									ok = Int32.TryParse(scode, out code);
								}
								if (ok)
									if (code == (int)k) { 
										Logging.Log("[EXPR] > Added the key by code: " + code + ", key: " + k);
										keys.Add(k);
										break;
									}
							}
						}
						if (key == "esc") {
							Logging.Log("[EXPR] > Added the short escape: " + key);
							keys.Add(Keys.Escape);
							break;
						}
						if (key == "win") {
							Logging.Log("[EXPR] > Added the lwin as base of: " + _n);
							keys.Add(Keys.LWin);
							break;
						}
						if (_n == key) {
							Logging.Log("[EXPR] > Added the " + _n);
							keys.Add(k);
							break;
						}
					}
				}
				all_keys.Add(keys);
			}
			foreach (var keys in all_keys) {
				var q = new List<WinAPI.INPUT>();
				foreach (var key in keys) {
					Logging.Log("[EXPR] > Pressing: " +key);
					q.Add(KInputs.AddKey(key, true));
				}
				foreach (var key in keys) {
					Logging.Log("[EXPR] > Releasing: " +key);
					q.Add(KInputs.AddKey(key, false));
				}
				KInputs.MakeInput(q.ToArray());
				Thread.Sleep(5);
			}
			Thread.Sleep(30);
		}
		#endregion
		public static bool IsNotWin7() {
//			Logging.Log("OS: " +Environment.OSVersion.Version);
			return Environment.OSVersion.Version.Major == 10 || (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor > 1);
		}
		public static void DoLater(Action act, int timeout) {
			System.Threading.Tasks.Task.Factory.StartNew(() => {
			                                             	Thread.Sleep(timeout);
			                                             	act();
			                                             });
		}
//		public static void SetNextLayout() {
//			var CUR = Locales.GetCurrentLocale();
//			var CUR_IND = MMain.locales.ToList().FindIndex(lid => lid.uId == CUR);
//			CUR_IND++;
//			if (CUR_IND >= MMain.locales.Length)
//				CUR_IND = 0;
//			Debug.WriteLine("NEXT LAYOUT: " + MMain.locales[CUR_IND].Lang + " IND " + CUR_IND  + " LEN " + MMain.locales.Length + " CUR " + CUR) ;
//		}
		static bool IsUpperInput() {
			var caps = Control.IsKeyLocked(Keys.CapsLock);
			if (MahouUI.CapsLockDisablerTimer)
				caps = false;
			if (((shift || shift_r) && !caps) || (!(shift || shift_r) && caps))
				return true;
			if (((shift || shift_r) && caps) || (!(shift || shift_r) && !caps))
				return false;
			return false;
		}
		public static bool ExcludedProgram() {
			if (MMain.Mahou == null) return false;
			var hwnd = WinAPI.GetForegroundWindow();
		if (NOT_EXCLUDED_HWNDs.Contains(hwnd)) {
				Logging.Log("[EXCL] > This program was been checked already, it is not excluded hwnd: " + hwnd);
				return false;
			}
			if (!EXCLUDED_HWNDs.Contains(hwnd)) {
				uint pid;
				WinAPI.GetWindowThreadProcessId(hwnd, out pid);
				Process prc = null;
				try { 
					prc = Process.GetProcessById((int)pid);
					if (prc == null) return false;
					if (MahouUI.ExcludedPrograms.Replace(Environment.NewLine, " ").ToLower().Contains(prc.ProcessName.ToLower().Replace(" ", "_") + ".exe")) {
						Logging.Log(prc.ProcessName + ".exe->excluded");
						EXCLUDED_HWNDs.Add(hwnd);
						return true;
					}
				} catch { Logging.Log("[EXCL] > Process with id ["+pid+"] not exist...", 1); }
			} else {
				Logging.Log("[EXCL] > Excluded program by excluded program saved hwnd: " + hwnd);
				return true;
			}
			NOT_EXCLUDED_HWNDs.Add(hwnd);
			return false;
		}
		static void SpecificKey(Keys Key, uint MSG, int vkCode = 0) {
//			Debug.WriteLine("SPK:" + skip_spec_keys);
			if (skip_spec_keys > 0) {
				skip_spec_keys--;
				if (skip_spec_keys < 0)
					skip_spec_keys = 0;
				return;
			}
//			Debug.WriteLine("Speekky->" + Key);
			for (var i = 1; i!=5; i++) {
				if ((MSG == WinAPI.WM_KEYUP || MSG == WinAPI.WM_SYSKEYUP || vkCode == 240)) {
		       		var specificKey = (int)typeof(MahouUI).GetField("Key"+i).GetValue(MMain.Mahou);
					if (MahouUI.ChangeLayoutInExcluded || !ExcludedProgram()) {
						#region Switch between layouts with one key
						var F18 = Key == Keys.F18;
						var GJIME = false;
						var npre = ((int)preKey == (int)Keys.None || (int)preKey == (int)Key);
						var altgr = (Key == Keys.RMenu && Key == Keys.LControlKey) || 
							(Key == Keys.RMenu && Key == Keys.RControlKey) || 
							(Key == Keys.LMenu && Key == Keys.LControlKey) || 
							((ctrl && Key == Keys.RMenu) || (alt_r && Key == Keys.LControlKey)) ||
							((ctrl && Key == Keys.LMenu) || (alt && Key == Keys.LControlKey)) || 
							((ctrl_r && Key == Keys.RMenu) || (alt_r && Key == Keys.RControlKey)) ||
							((ctrl_r && Key == Keys.LMenu) || (alt && Key == Keys.RControlKey));
						if (specificKey == 8) // Shift+CapsLock
							if (vkCode == 240) { // Google Japanese IME's  Shift+CapsLock repam fix
								skip_spec_keys++; // Skip next CapsLock up event
								GJIME = true;
							}
						if ((Key == Keys.CapsLock && !shift && !shift_r && !alt && !alt_r && !ctrl && !ctrl_r && !win && !win_r && specificKey == 1) ||
						    (Key == Keys.CapsLock && (shift || shift_r) && !alt && !alt_r && !ctrl && !ctrl_r && !win && !win_r && specificKey == 8) )
							if (Control.IsKeyLocked(Keys.CapsLock))
								DoSelf(() => { KeybdEvent(Keys.CapsLock, 0); KeybdEvent(Keys.CapsLock, 2); });
						var speclayout = (string)typeof(MahouUI).GetField("Layout"+i).GetValue(MMain.Mahou);
						if (String.IsNullOrEmpty(speclayout)) {
						    Logging.Log("[SPKEY] > No layout for Layout"+i + " variable.");
						    return;
					    }
						if (speclayout == MMain.Lang[Languages.Element.SwitchBetween]) {
							if (specificKey == 12 && Key == Keys.Tab && !ctrl && !ctrl_r && !shift_r && !shift && !win && !win_r && !alt && !alt_r) {
								Logging.Log("[SPKEY] > Changing layout by Tab key.");
								ChangeLayout();
						    	return;
							}
							if (specificKey == 11 && (
								(Key == Keys.LShiftKey && ctrl) || (Key == Keys.RShiftKey && ctrl_r) ||
								(Key == Keys.LControlKey && shift) || (Key == Keys.RControlKey && shift_r)) && !win && !win_r && !alt && !alt_r) {
								Logging.Log("[SPKEY] > Changing layout by Ctrl+Shift key.");
								ChangeLayout();
						    	return;
							}
							if (specificKey == 10 && (
								(Key == Keys.LShiftKey && alt) || (Key == Keys.RShiftKey && alt_r) ||
								(Key == Keys.LMenu && shift) || (Key == Keys.RMenu && shift_r)) && !win && !win_r && !ctrl && !ctrl_r) {
								Logging.Log("[SPKEY] > Changing layout by Alt+Shift key.");
								ChangeLayout();
						    	return;
							}
							if (specificKey == 8 && (Key == Keys.CapsLock || F18 || GJIME) && (shift || shift_r) && !alt && !alt_r && !ctrl && !ctrl_r) {
								Logging.Log("[SPKEY] > Changing layout by Shift+CapsLock"+(GJIME?"(KeyCode: 240, Google Japanese IME's Shift+CapsLock remap)":"")+(F18?"(F18)":"")+" key.");
								ChangeLayout();
						    	return;
							} else 
							if (!shift && !shift_r && !alt && !alt_r && !ctrl && !ctrl_r && !win && !win_r && specificKey == 1 && 
								    (Key == Keys.CapsLock || F18)) {
								ChangeLayout();
								Logging.Log("[SPKEY] > Changing layout by CapsLock"+(F18?"(F18)":"")+" key.");
						    	return;
							}
							if (specificKey == 2 && Key == Keys.LControlKey && !keyAfterCTRL && npre) {
								Logging.Log("[SPKEY] > Changing layout by L-Ctrl key.");
								ChangeLayout();
						    	return;
							}
							if (specificKey == 3 && Key == Keys.RControlKey && !keyAfterCTRL && npre) {
								Logging.Log("[SPKEY] > Changing layout by R-Ctrl key.");
								ChangeLayout();
						    	return;
							}
							if (specificKey == 4 && Key == Keys.LShiftKey && !keyAfterSHIFT && npre) {
								Logging.Log("[SPKEY] > Changing layout by L-Shift key.");
								ChangeLayout();
						    	return;
							}
							if (specificKey == 5 && Key == Keys.RShiftKey && !keyAfterSHIFT && npre) {
								Logging.Log("[SPKEY] > Changing layout by R-Shift key.");
								ChangeLayout();
						    	return;
							}
							if (specificKey == 6 && Key == Keys.LMenu && !keyAfterALT && npre) {
								Logging.Log("[SPKEY] > Changing layout by L-Alt key.");
								ChangeLayout();
						    	return;
							}
							if (specificKey == 7 && Key == Keys.RMenu && !keyAfterALT && npre) {
								Logging.Log("[SPKEY] > Changing layout by R-Alt key.");
								ChangeLayout();
						    	return;
							}
							if (specificKey == 9 && altgr && !keyAfterALTGR) {
								Logging.Log("[SPKEY] > Changing layout by AltGr key.");
								ChangeLayout();
						    	return;
							}
//							if (catched) {
//			       			    if (Key == Keys.LMenu)
//									DoSelf(()=>{ Thread.Sleep(150); KeybdEvent(Keys.LMenu, 0); KeybdEvent(Keys.LMenu, 2); });
//			       			    if (Key == Keys.RMenu)
//									DoSelf(()=>{ Thread.Sleep(150); KeybdEvent(Keys.RMenu, 0); KeybdEvent(Keys.RMenu, 2); });
//							}
							#endregion
						} else {
							#region By layout switch
							var matched = false;
							if (specificKey == 12 && Key == Keys.Tab && !ctrl && !ctrl_r && !shift_r && !shift && !win && !win_r && !alt && !alt_r) {
								Logging.Log("[SPKEY] > Switching to specific layout by Tab key.");
								ChangeToLayout(Locales.ActiveWindow(), Locales.GetLocaleFromString(speclayout).uId);
								matched = true;
						    	return;
							}
							if (specificKey == 10 && (
								(Key == Keys.LShiftKey && alt) || (Key == Keys.RShiftKey && alt_r) ||
								(Key == Keys.LMenu && shift) || (Key == Keys.RMenu && shift_r)) && !win && !win_r && !ctrl && !ctrl_r) {
								Logging.Log("[SPKEY] > Switching to specific layout by Alt+Shift key.");
								ChangeToLayout(Locales.ActiveWindow(), Locales.GetLocaleFromString(speclayout).uId);
								matched = true;
						    	return;
							}
							if (specificKey == 8 && (Key == Keys.CapsLock || F18 || GJIME) && (shift || shift_r) && !alt && !alt_r && !ctrl && !ctrl_r) {
								Logging.Log("[SPKEY] > Switching to specific layout by Shift+CapsLock"+(GJIME?"(KeyCode: 240, Google Japanese IME's Shift+CapsLock remap)":"")+(F18?"(F18)":"")+" key.");
								ChangeToLayout(Locales.ActiveWindow(), Locales.GetLocaleFromString(speclayout).uId);
								matched = true;
						    	return;
							} else
							if (specificKey == 1 && (Key == Keys.CapsLock || F18)) {
								Logging.Log("[SPKEY] > Switching to specific layout by Caps Lock"+(F18?"(F18)":"")+" key.");
								ChangeToLayout(Locales.ActiveWindow(), Locales.GetLocaleFromString(speclayout).uId);
								matched = true;
						    	return;
							}
							if (specificKey == 2 && Key == Keys.LControlKey && !keyAfterCTRL && npre) {
								Logging.Log("[SPKEY] > Switching to specific layout by  L-Ctrl key.");
								ChangeToLayout(Locales.ActiveWindow(), Locales.GetLocaleFromString(speclayout).uId);
								matched = true;
						    	return;
							}
							if (specificKey == 3 && Key == Keys.RControlKey && !keyAfterCTRL && npre) {
								Logging.Log("[SPKEY] > Switching to specific layout by R-Ctrl key.");
								ChangeToLayout(Locales.ActiveWindow(), Locales.GetLocaleFromString(speclayout).uId);
								matched = true;
						    	return;
							}
							if (specificKey == 4 && Key == Keys.LShiftKey && !keyAfterSHIFT && npre) {
								Logging.Log("[SPKEY] > Switching to specific layout by L-Shift key.");
								ChangeToLayout(Locales.ActiveWindow(), Locales.GetLocaleFromString(speclayout).uId);
								matched = true;
						    	return;
							}
							if (specificKey == 5 && Key == Keys.RShiftKey && !keyAfterSHIFT && npre) {
								Logging.Log("[SPKEY] > Switching to specific layout by R-Shift key.");
								ChangeToLayout(Locales.ActiveWindow(), Locales.GetLocaleFromString(speclayout).uId);
								matched = true;
						    	return;
							}
							if (specificKey == 6 && Key == Keys.LMenu && !keyAfterALT && npre) {
								Logging.Log("[SPKEY] > Switching to specific layout by L-Alt key.");
								ChangeToLayout(Locales.ActiveWindow(), Locales.GetLocaleFromString(speclayout).uId);	
								matched = true;
								DoSelf(()=>{ KeybdEvent(Keys.LMenu, 0); KeybdEvent(Keys.LMenu, 2); });
						    	return;
							}
							if (specificKey == 7 && Key == Keys.RMenu && !keyAfterALT && npre) {
								Logging.Log("[SPKEY] > Switching to specific layout by R-Alt key.");
								ChangeToLayout(Locales.ActiveWindow(), Locales.GetLocaleFromString(speclayout).uId);
								matched = true;
								DoSelf(()=>{ KeybdEvent(Keys.RMenu, 0); KeybdEvent(Keys.RMenu, 2); });
						    	return;
							}
							if (specificKey == 9 && altgr && !keyAfterALTGR) {
								Logging.Log("[SPKEY] > Switching to specific layout by AltGr key.");
								ChangeToLayout(Locales.ActiveWindow(), Locales.GetLocaleFromString(speclayout).uId);
								matched = true;
								DoSelf(()=>{ KeybdEvent(Keys.RMenu, 0); KeybdEvent(Keys.RMenu, 2); });
						    	return;
							}
							try {
								if (matched) {
									Logging.Log("[SPKEY] > Available layout from string ["+speclayout+"] & id ["+i+"].");
									//Fix for alt-show-menu in programs
//				       			    if (Key == Keys.LMenu)
//										DoSelf(()=>{ KeybdEvent(Keys.LMenu, 0); KeybdEvent(Keys.LMenu, 2); });
//				       			    if (Key == Keys.RMenu)
//										DoSelf(()=>{ KeybdEvent(Keys.RMenu, 0); KeybdEvent(Keys.RMenu, 2); });
								}
							} catch { 
								Logging.Log("[SPKEY] > No layout available from string ["+speclayout+"] & id ["+i+"]."); 
							}
						}
						#endregion
				    }
          		}
			}
		}
		public static void ClearModifiers() {
			win = alt = ctrl = shift = win_r = alt_r = ctrl_r = shift_r = false;
			LLHook.ClearModifiers();
			SendModsUp((int)(WinAPI.MOD_ALT + WinAPI.MOD_CONTROL + WinAPI.MOD_SHIFT + WinAPI.MOD_WIN));
		}
		static void ClearWord(bool LastWord = false, bool LastLine = false, bool Snippet = false, string ClearReason = "") {
			var ReasonEnding = ".";
			if (MahouUI.LoggingEnabled && !String.IsNullOrEmpty(ClearReason))
				ReasonEnding = ", reason: [" + ClearReason + "].";
			if (LastWord) {
				c_word_backup_last = new List<YuKey>(c_word_backup);
				c_word_backup = new List<YuKey>(MMain.CWord);
				if (MMain.CWord.Count > 0) {
					MMain.CWord.Clear();
					lastLWClearReason = ReasonEnding;
					Logging.Log("[CLWORD] > Cleared last word" + ReasonEnding);
				}
			}
			if (LastLine) {
				if (MMain.CWords.Count > 0) {
					MMain.CWords.Clear();
					Logging.Log("[CLWORD] > Cleared last line" + ReasonEnding);
				}
			}
			if (Snippet) {
				if (c_snip.Count > 0) {
					if (MahouUI.SnippetsEnabled) {
						c_snip.Clear();
					Logging.Log("[CLWORD] > Cleared current snippet" + ReasonEnding);
				}
				}
			}
			MahouUI.RefreshFLAG();
			MMain.Mahou.RefreshAllIcons();
			MMain.Mahou.UpdateLDs();
		}
		public static string[] SplitWords(string LINE) {
			var LIST = new List<string>();
			var left = LINE;
			var ind = left.IndexOf(' ');
			while ((ind = left.IndexOf(' ')) != -1) {
				ind = left.IndexOf(' ');
				if (ind == 0)
					ind = 1;
				var word = left.Substring(0, ind);
				left = left.Substring(ind, left.Length-ind);
//				Debug.WriteLine(word + "] " + ind + " [" + left);
				LIST.Add(word);
			}
			if (ind == -1 && !String.IsNullOrEmpty(left)) {
				LIST.Add(left);
//				Debug.WriteLine(left);
			}
			return LIST.ToArray();
		}
		public static string ConvertText(string ClipStr, uint l1 = 0, uint l2 = 0) {
			if (l1 == 0) l1 = cs_layout_last;
			if (l2 == 0) l2 = GetNextLayout(l1).uId;
			var result = "";
			var index = 0;
			if (MahouUI.OneLayoutWholeWord) {
				Logging.Log("[CT] > Using one layout whole word convert text mode.");
				var allWords = SplitWords(ClipStr);
				var word_index = 0;
				foreach (var w in allWords) {
					if (w == " ") {
						result += w;
					} else {
						var wx = WordGuessLayout(w, l2).Item1;
						if (!String.IsNullOrEmpty(wx))
							result += wx;
						else result += w;
					}
					word_index +=1;
//					Debug.WriteLine("(" + w + ") ["+ result +"]");
					index++;
				}
			} else {
				Logging.Log("[CT] > Using default convert text mode.");
				for (var I=0; I!=ClipStr.Length; I++) {
					var sm = false;
					var c = ClipStr[I];
					if (c == 'ո' || c == 'Ո') {
						if (c == 'ո') sm = true;
						if (ClipStr.Length > I+1) {
							if (ClipStr[I+1] == 'ւ') {
								var shrt = l2 & 0xffff;
								var _shrt = l1 & 0xffff;
								if (shrt == 1033 || shrt == 1041) {
									result += sm ? "u" : "U";
									I++; continue;
								}
								if (_shrt == 1033 || _shrt == 1041) {
									result += sm ? "u" : "U";
									I++; continue;
								}
								if (shrt == 1049) {
									result += sm ? "г" : "Г";
									I++; continue;
								}
								if (_shrt == 1049) {
									result += sm ? "г" : "Г";
									I++; continue;
								}
							}
						}
					}
					var T = ConvertBetweenLayouts(c, l1 & 0xffff, l2 & 0xffff);
					for (var i = 0; i != MMain.Locales.Length; i++) {
						var l = MMain.Locales[i].uId;
						if (c == '\n')
							T = "\n";
						T = GermanLayoutFix(c);
						T = ConvertBetweenLayouts(c, l & 0xffff, l2 & 0xffff);
						if (T != "") 
							break;
						index++;
					}
					if (T == "")
						T = ClipStr[index].ToString();
					result += T;
				}
			}
			return result;
		}

	    /// <summary>
	    /// Converts selected text.
	    /// </summary>
	    public static void ConvertSelection()
	    {
	        Debug.WriteLine("Start CS");
	        try
	        {
	            //Used to catch errors
	            DoSelf(() =>
	            {
	                Logging.Log("[CS] > Starting Convert selection.");
	                var ClipStr = GetClipStr();
	                ClipStr = Regex.Replace(ClipStr, @"(\d+)[,.?бю/]", "$1.");
	                if (!String.IsNullOrEmpty(ClipStr))
	                {
	                    csdoing = true;
	                    Logging.Log("[CS] > Starting conversion of [" + ClipStr + "].");
	                    DeleteFromInput();
	                    var items = 0;
	                    if (MahouUI.ConvertSelectionLS && !MahouUI.OneLayoutWholeWord)
	                    {
	                        Logging.Log("[CS] > Using CS-Switch mode.");
	                        var wasLocale = Locales.GetCurrentLocale() & 0xffff;
	                        var wawasLocale = wasLocale & 0xffff;
	                        ChangeLayout(true);
	                        uint nowLocale = 0;
	                        if (MahouUI.SwitchBetweenLayouts)
	                        {
	                            nowLocale = wasLocale == (MahouUI.MAIN_LAYOUT1 & 0xffff) ? MahouUI.MAIN_LAYOUT2 & 0xffff : MahouUI.MAIN_LAYOUT1 & 0xffff;
	                            if (nowLocale == wasLocale &&
	                                (MahouUI.currentLayout == MahouUI.MAIN_LAYOUT1 ||
	                                 MahouUI.currentLayout == MahouUI.MAIN_LAYOUT2))
	                            {
	                                if (wasLocale != MahouUI.currentLayout)
	                                    nowLocale = MahouUI.currentLayout;
	                            }
	                        }
	                        else
	                        {
	                            Thread.Sleep(10);
	                            nowLocale = GetNextLayout().uId & 0xffff;
	                        }

	                        var index = 0;
	                        var q = new List<WinAPI.INPUT>();
	                        foreach (var c in ClipStr)
	                        {
	                            items++;
	                            wasLocale = wawasLocale;
	                            var s = new StringBuilder(10);
	                            var sb = new StringBuilder(10);
	                            var yk = new YuKey();
	                            var scan = WinAPI.VkKeyScanEx(c, wasLocale);
	                            var state = (scan >> 8) & 0xff;
	                            var bytes = new byte[255];
	                            if (state == 1)
	                                bytes[(int) Keys.ShiftKey] = 0xFF;
	                            var scan2 = WinAPI.VkKeyScanEx(c, nowLocale);
	                            var state2 = (scan2 >> 8) & 0xff;
	                            var bytes2 = new byte[255];
	                            if (state2 == 1)
	                                bytes2[(int) Keys.ShiftKey] = 0xFF;
	                            if (MahouUI.ConvertSelectionLSPlus)
	                            {
	                                Logging.Log("[CS] > Using Experimental CS-Switch mode.");
	                                WinAPI.ToUnicodeEx((uint) scan, (uint) scan, bytes, s, s.Capacity, 0, (IntPtr) wasLocale);
	                                Logging.Log("[CS] > Char 1 is [" + s + "] in locale +[" + wasLocale + "].");
	                                if (ClipStr[index].ToString() == s.ToString())
	                                {
	                                    if (!NeedToIgnoreSymbol((Keys) (scan & 0xff), state == 1, wasLocale))
	                                    {
	                                        Logging.Log("Making input of [" + scan + "] in locale +[" + nowLocale + "].");
	                                        q.Add(KInputs.AddString(ConvertBetweenLayouts(c, wasLocale, nowLocale))[0]);
	                                    }

	                                    index++;
	                                    continue;
	                                }

	                                WinAPI.ToUnicodeEx((uint) scan2, (uint) scan2, bytes2, sb, sb.Capacity, 0, (IntPtr) nowLocale);
	                                Logging.Log("[CS] > Char 2 is [" + sb + "] in locale +[" + nowLocale + "].");
	                                if (ClipStr[index].ToString() == sb.ToString())
	                                {
	                                    Logging.Log("[CS] > Char 1, 2 and original are equivalent.");
	                                    ChangeToLayout(Locales.ActiveWindow(), wasLocale);
	                                    wasLocale = nowLocale;
	                                    scan = scan2;
	                                    state = state2;
	                                }
	                            }

	                            if (c == '\n')
	                            {
	                                yk.Key = Keys.Enter;
	                                yk.IsUpper = false;
	                            }
	                            else
	                            {
	                                if (scan != -1)
	                                {
	                                    var key = (Keys) (scan & 0xff);
	                                    var upper = false || state == 1;
	                                    yk = new YuKey() {Key = key, IsUpper = upper};
	                                    Logging.Log("[CS] > Key of char [" + c + "] = {" + key + "}, upper = +[" + state + "].");
	                                }
	                                else
	                                {
	                                    yk = new YuKey() {Key = Keys.None};
	                                }
	                            }

	                            if (yk.Key == Keys.None)
	                            {
	                                // retype unrecognized as unicode
	                                var unrecognized = ClipStr[items - 1].ToString();
	                                var unr = KInputs.AddString(unrecognized)[0];
	                                Logging.Log("[CS] > Key of char [" + c + "] = not exist, using input as string.");
	                                q.Add(unr);
	                            }
	                            else
	                            {
	                                if (!NeedToIgnoreSymbol(yk.Key, yk.IsUpper, wasLocale))
	                                {
	                                    Logging.Log("[CS] > Making input of [" + yk.Key + "] key with upper = [" + yk.IsUpper + "].");
	                                    if (yk.IsUpper)
	                                        q.Add(KInputs.AddKey(Keys.LShiftKey, true));
	                                    q.AddRange(KInputs.AddPress(yk.Key));
	                                    if (yk.IsUpper)
	                                        q.Add(KInputs.AddKey(Keys.LShiftKey, false));
	                                }
	                            }

	                            index++;
	                        }

	                        KInputs.MakeInput(q.ToArray());
	                    }
	                    else
	                    {
	                        var l1 = cs_layout_last;
	                        if (MahouUI.ConvertSelectionLS)
	                        {
	                            Logging.Log("[CS] > Using Layout Switch in Convert Selection.");
	                            l1 = Locales.GetCurrentLocale();
	                            if (MahouUI.UseJKL && !KMHook.JKLERR)
	                                l1 = MahouUI.currentLayout;
	                            ChangeLayout();
	                        }

	                        var l2 = GetNextLayout(l1).uId;
	                        Debug.WriteLine("next: " + l2);
	                        var result = ConvertText(ClipStr, l1, l2);
	                        cs_layout_last = l2;
	                        Logging.Log("[CS] > Conversion of string [" + ClipStr + "] from locale [" + l1 + "] into locale [" + l2 + "] became [" + result + "].");
	                        //Inputs converted text
	                        Logging.Log("[CS] > Making input of [" + result + "] as string");
	                        KInputs.MakeInput(KInputs.AddString(result));
	                        items = result.Length;
	                    }

	                    ReSelect(items);
	                }

	                NativeClipboard.Clear();
	                RestoreClipBoard();
	            });
	        }
	        catch (Exception e)
	        {
	            Logging.Log("[CS] > Convert Selection encountered error, details:\r\n" + e.Message + "\r\n" + e.StackTrace, 1);
	        }

	        Memory.Flush();
	    }

	    public enum ConvT {
			Transliteration,
			Random,
			Title,
			Swap,
			Upper,
			Lower
		}
		public static void SelectionConversion(ConvT t = 0) {
			var tn = Enum.GetName(typeof(ConvT), t);
			try { //Used to catch errors
				Locales.ShowErrorIfNotEnoughLayouts();
				DoSelf(() => {
					Logging.Log("["+tn+"] > Starting "+tn+" selection.");
					var ClipStr = GetClipStr();
					if (!String.IsNullOrEmpty(ClipStr)) {
						var output = "";
						switch (t) {
							case ConvT.Transliteration:
								output = TransliterateText(ClipStr); break;
							case ConvT.Random:
								output = ToSTULRSelection(ClipStr,false,false,false,true); break;
							case ConvT.Title:
								output = ToSTULRSelection(ClipStr,false,true); break;
							case ConvT.Swap:
								output = ToSTULRSelection(ClipStr,true); break;
							case ConvT.Upper:
								output = ToSTULRSelection(ClipStr); break;
							case ConvT.Lower:
								output = ToSTULRSelection(ClipStr,false,false,true); break;
						}
						Logging.Log("Inputting ["+output+"] as "+tn);
						KInputs.MakeInput(KInputs.AddString(output));
						ReSelect(output.Length);
					}
					NativeClipboard.Clear();
					RestoreClipBoard();
	            });
				} catch(Exception e) {
					Logging.Log("["+tn+"] > Selection encountered error, details:\r\n" +e.Message+"\r\n"+e.StackTrace, 1);
				}
			Memory.Flush();
		}
		public static string TransliterateText(string ClipStr) {
			Logging.Log("[TRANSLTRT] > Starting Transliterate text.");
			var output = ClipStr;
			foreach (var key in TranslitDict) {
				if (output.Contains(key.Key))
                	output.Replace(key.Key, key.Value);
            }
			if (ClipStr == output) {
				foreach (var key in TranslitDict) {
					if (ClipStr.Contains(key.Value))
	                	ClipStr = ClipStr.Replace(key.Value, key.Key);
            	}
				if (ClipStr == output)
				foreach (var key in TranslitDict) {
					if (ClipStr.Contains(key.Key))
	                	ClipStr = ClipStr.Replace(key.Key, key.Value);
            	}
				return ClipStr;
			}
			return output;
		}
		public static string ToSTULRSelection(string ClipStr, bool swap = false, bool title = false, bool lower = false, bool random = false) {
			var ClipStrLines = ClipStr.Split('\n');
			var lines = 0;
			var output = "";
			Random rand = null;
			if (random) rand = new Random();
			foreach (var line in ClipStrLines) {
				lines++;
				var ClipStrWords = SplitWords(line);
				var words = 0;
				foreach (var word in ClipStrWords) {
					words++;
					var STULR = "";
					if (title) {
						if (word.Length > 0)
							STULR += word[0].ToString().ToUpper();
						if (word.Length > 1)
							foreach(var ch in word.Substring(1, word.Length - 1)) {
								STULR += char.ToLower(ch);
							}
					} else {
						foreach(var ch in word) {
							if (random) {
								if (rand.NextDouble() >= 0.5) {
									STULR += char.ToLower(ch);
								} else {
									STULR += char.ToUpper(ch);
								}
							} else if (swap) {
								if (char.IsUpper(ch))
									STULR += char.ToLower(ch);
								else if (char.IsLower(ch))
									STULR += char.ToUpper(ch);
								else
									STULR += ch;
							} else {
								if (lower)
									STULR += char.ToLower(ch);
								else
									STULR += char.ToUpper(ch);
							}
						}
					}
					output +=STULR;
				}
				if (lines != ClipStrLines.Length)
					output +="\n";
			}
			return output;
		}
		static void ReSelect(int count) {
			if (MahouUI.ReSelect) {
				//reselects text
				Logging.Log("Reselecting text.");
				KInputs.MakeInput(KInputs.AddPress(Keys.Left, count), (int)WinAPI.MOD_SHIFT);
			}
		}
		static string ArmenianSignleCharFix(string word, uint next_layout, uint this_layout = 0) {
			var shrt = next_layout & 0xffff;
			var _shrt = this_layout & 0xffff;
//			if (shrt == 1033 || shrt == 1041) // English/Japanese
			var repl = word;
//			Debug.WriteLine("Next: " + next_layout + ", word: " +word);
			if (shrt == 1067) // Armenian
				repl = word.Replace("u", "w6").Replace("U", "W6").Replace("г","ц6").Replace("Г","Ц6");
			if (_shrt == 1067) {
				if (shrt == 1033 || shrt == 1041) // English/Japanese
					repl = word.Replace("ու", "u").Replace("Ու", "U");
				if (shrt == 1049) //  Russian
					repl = word.Replace("ու", "Г").Replace("Ու", "Г");
			}
//			else if (shrt == 1049) // Russian
//				word.Replace("Ու", "Г").Replace("ու", "г");
			Debug.WriteLine("RELT: " + repl);
			return repl;
		}
		static string GermanLayoutFix(char c) {
			if (!MahouUI.QWERTZ_fix)
				return "";
			var T = "";
			switch (c) {
				case 'ä':
					T = "э"; break; 
				case 'э':
					T = "ä"; break;
				case 'ö':
					T = "ж"; break;
				case 'ж':
					T = "ö"; break;
				case 'ü':
					T = "х"; break;
				case 'х':
					T = "ü"; break;
				case 'Ä':
					T = "Э"; break;
				case 'Э':
					T = "Ä"; break;
				case 'Ö':
					T = "Ж"; break;
				case 'Ж':
					T = "Ö"; break;
				case 'Ü':
					T = "Х"; break;
				case 'Х':
					T = "Ü"; break;
				case 'Я':
					T = "Y"; break;
				case 'Y':
					T = "Я"; break;
				case 'Н':
					T = "Z"; break;
				case 'Z':
					T = "Н"; break;
				case 'я':
					T = "y"; break;
				case 'y':
					T = "я"; break;
				case 'н':
					T = "z"; break;
				case 'z':
					T = "н"; break;
				case '-':
					T = "ß"; break;
				default:
					T = ""; break;
			}
			Logging.Log("German fix T:" + T +  "/ c: " + c);
			return T;
		}
		static bool WaitForClip2BeFree() {
			Debug.WriteLine(">> WFC2F");
			var CB_Blocker = IntPtr.Zero;
			var tries = 0;
			do { 
				CB_Blocker = WinAPI.GetOpenClipboardWindow();
				if (CB_Blocker == IntPtr.Zero) break;
				Logging.Log("Clipboard blocked by process id ["+WinAPI.GetWindowThreadProcessId(CB_Blocker, IntPtr.Zero) +"].", 2);
				tries ++;
				if (tries > 3000) {
					Logging.Log("3000 Tries to wait for clipboard blocker ended, blocker didn't free'd clipboard |_|.", 2); return false;
				}
			} while (CB_Blocker != IntPtr.Zero);
			Debug.WriteLine(">> WFC2F t: " + tries);
			return true;
		}
		public static bool RestoreClipBoard(string special = "") {
			Debug.WriteLine(">> RC");
			var restore = special;
			var spc = true;
			if (String.IsNullOrEmpty(restore)) {
				restore = lastClipText;
				spc = false;
			}
			Logging.Log((spc?"Special ":"")+"Restoring clipboard text: ["+restore+"].");
			if (WaitForClip2BeFree()) {
				try { Clipboard.SetDataObject(restore, true, 5, 120); return true; } 
				catch { Logging.Log("Error during clipboard "+(spc?"Special ":"")+"text restore after 5 tries.", 2); return false; }
			}
			return false;
		}
		public static string GetClipboard(int tries = 1, int timeout = 5) {
			var txt = NativeClipboard.GetText();
			for (var i = 1; i<tries; i++) {
				if (!String.IsNullOrEmpty(txt)) break;
				txt = NativeClipboard.GetText();
				Thread.Sleep(timeout);
			}
			return txt;
		}
		/// <summary>
		/// Sends RCtrl + Insert to selected get text, and returns that text by using WinAPI.GetText().
		/// </summary>
		/// <returns>string</returns>
		static string MakeCopy()  {
			Debug.WriteLine(">> MC");
			KInputs.MakeInput(KInputs.AddPress(Keys.Insert), (int)WinAPI.MOD_CONTROL);
			Thread.Sleep(30);
			var txt = NativeClipboard.GetText();
			if (string.IsNullOrEmpty(txt)) {
				KInputs.MakeInput(KInputs.AddPress(Keys.C), (int)WinAPI.MOD_CONTROL);
				Thread.Sleep(30);
				txt = NativeClipboard.GetText();
			}
			return txt;
		}

	    public static string GetClipStr()
	    {
	        Debug.WriteLine(">> GCS");
	        Locales.ShowErrorIfNotEnoughLayouts();
	        var clipStr = "";
	        // Backup & Restore feature, now only text supported...
	        if (MMain.MahouActive() && MMain.Mahou.ActiveControl is TextBox)
	            return (MMain.Mahou.ActiveControl as TextBox).SelectedText;
	        Logging.Log("Taking backup of clipboard text if possible.");
	        lastClipText = NativeClipboard.GetText();
//			Thread.Sleep(50);
	        if (!String.IsNullOrEmpty(lastClipText))
	            lastClipText = Clipboard.GetText();
//			This prevents from converting text that already exist in Clipboard
//			by pressing "Convert Selection hotkey" without selected text.
	        NativeClipboard.Clear();
	        Logging.Log("Getting selected text.");
	        if (MahouUI.SelectedTextGetMoreTries)
	            for (var i = 0; i != MMain.Mahou.SelectedTextGetMoreTriesCount; i++)
	            {
	                if (WaitForClip2BeFree())
	                {
	                    clipStr = MakeCopy();
	                    if (!String.IsNullOrEmpty(clipStr))
	                        break;
	                }
	            }
	        else
	        {
	            if (WaitForClip2BeFree())
	            {
	                clipStr = MakeCopy();
	                if (String.IsNullOrEmpty(clipStr))
	                    clipStr = MakeCopy();
	            }
	        }

	        if (String.IsNullOrEmpty(clipStr))
	            return "";
	        return Regex.Replace(clipStr, "\r?\n|\r", "\n");
	    }

	    /// <summary>
		/// Re-presses modifiers you hold when hotkey fired(due to SendModsUp()).
		/// </summary>
		public static void RePress()  {
			DoSelf(() => {
				//Repress's modifiers by RePress variables
				if (shiftRP) {
					KeybdEvent(Keys.LShiftKey, 0);
					swas = true;
					shiftRP = false;
				}
				if (altRP) {
					KeybdEvent(Keys.LMenu, 0);
					awas = true;
					altRP = false;
				}
				if (ctrlRP) {
					KeybdEvent(Keys.LControlKey, 0);
					cwas = true;
					ctrlRP = false;
				}
				if (winRP) {
					KeybdEvent(Keys.LWin, 0);
					wwas = true;
					winRP = false;
				}
			       });
		}
		/// <summary>
		/// Do action without RawInput listeners(e.g. not catch).
		/// Useful with SendInput or keybd_event functions.
		/// </summary>
		/// <param name="self_action">Action that will be done without RawInput listeners, Hotkeys and low-level hook.</param>
		public static void DoSelf(Action self_action) {
			if (selfie) {
				Logging.Log("Inside "+busy_on+"called: "+self_action.Method.Name);
				self_action();
			} else {
				Debug.WriteLine(">> DS" + self_action.Method.Name);
				MMain.Mahou.Invoke((MethodInvoker)delegate {
                   	if (MahouUI.RemapCapslockAsF18) { LLHook.UnSet(); } MMain.Mahou.UnregisterHotkeys(); });
				MMain.Rif.Invoke((MethodInvoker)delegate{MMain.Rif.RegisterRawInputDevices(IntPtr.Zero, WinAPI.RawInputDeviceFlags.Remove);});
				selfie = true;
				busy_on = self_action.Method.Name;
				self_action();
				MMain.Mahou.Invoke((MethodInvoker)delegate {
                   	if (MahouUI.RemapCapslockAsF18) { LLHook.Set(); } MMain.Mahou.RegisterHotkeys(); });
				MMain.Rif.Invoke((MethodInvoker)delegate{MMain.Rif.RegisterRawInputDevices(MMain.Rif.Handle);});
				selfie = false;
				Debug.WriteLine(">> ES" + self_action.Method.Name);
			}
		}

	    public static void StartConvertWord(YuKey[] yuKeys, uint wasLocale, bool skipSnipppets = false)
	    {
	        if (yuKeys.Length == 0)
	        {
	            Logging.Log("Convert Last failed: EMPTY WORD.");
	            return;
	        }

	        Logging.Log("Start Convert Word len: [" + yuKeys.Length + "], wl:" + wasLocale + ", ss:" + skipSnipppets);
	        DoSelf(() =>
	        {
	            Debug.WriteLine(">> ST CLW");
	            var backs = yuKeys.Length;
	            // Fix for cmd exe pause hotkey leaving one char. 
	            var clsNM = new StringBuilder(256);
	            if (IsNotWin7() && clsNM.ToString() == "ConsoleWindowClass" && MMain.Mahou.HKCLast.VirtualKeyCode == (int) Keys.Pause)
	                backs++;
	            Debug.WriteLine(">> LC Aft. " + (MMain.Locales.Length * 20));
	            Logging.Log("Deleting old word, with lenght of [" + yuKeys.Length + "].");
	            DeleteFromInput(backs);
	            if (MahouUI.UseDelayAfterBackspaces)
	                Thread.Sleep(MMain.Mahou.DelayAfterBackspaces);
	            if (!skipSnipppets)
	                c_snip.Clear();
	            var result = new List<WinAPI.INPUT>();
	            foreach (var key in yuKeys)
	            {
	                if (key.IsAltNumPad)
	                {
	                    Logging.Log("An YuKey with [" + key.Numpads.Count + "] numpad(s) passed.");
	                    result.Add(KInputs.AddKey(Keys.LMenu, true));
	                    foreach (var numpad in key.Numpads)
	                    {
	                        Logging.Log(numpad + " is being inputted.");
	                        result.AddRange(KInputs.AddPress(numpad));
	                    }

	                    result.Add(KInputs.AddKey(Keys.LMenu, false));
	                }
	                else
	                {
	                    Logging.Log("An YuKey with state passed, key = {" + key.Key + "}, upper = [" + key.IsUpper + "].");
	                    var isUpper = key.IsUpper && !Control.IsKeyLocked(Keys.CapsLock);
	                    if (isUpper)
	                        result.Add(KInputs.AddKey(Keys.LShiftKey, true));

	                    var virtKeyboardState = new byte[256];
	                    if (key.IsUpper)
	                    {
	                        virtKeyboardState[(int)Keys.ShiftKey] = 0xFF; // press shift  Если старший БИТ байта установлен, клавиша - внизу (нажата).  0xFF ?????
	                    }

                        if (!NeedToIgnoreSymbol(key.Key, key.IsUpper, wasLocale))
	                    {
                            // here need to convert to the next layout
                            var nextLocale = GetNextLayout().uId & 0xffff;

	                        var resultBuffer = new StringBuilder();
	                        WinAPI.ToUnicodeEx((uint)key.Key, (uint)WinAPI.MapVirtualKey((uint)key.Key, 0), virtKeyboardState, resultBuffer, 5, 0, (IntPtr)nextLocale);

	                        var aaa = resultBuffer[0]; // 0x09A4U;
                            result.AddRange(KInputs.AddPress((Keys)aaa, isUnicode: true));
	                    }
	                    if (isUpper)
	                        result.Add(KInputs.AddKey(Keys.LShiftKey, false));

	                    if (!skipSnipppets)
	                    {
	                        var loc = (Locales.GetCurrentLocale() & 0xffff);
	                        if (MahouUI.UseJKL && !KMHook.JKLERR)
	                            loc = MahouUI.currentLayout & 0xffff;
	                        var resultBuffer = new StringBuilder();
	                        WinAPI.ToUnicodeEx((uint)key.Key, (uint) WinAPI.MapVirtualKey((uint)key.Key, 0), virtKeyboardState, resultBuffer, 5, 0, (IntPtr) loc);
	                        c_snip.Add(resultBuffer[0]);
	                    }
	                }
	            }

	            KInputs.MakeInput(result.ToArray());
	            Debug.WriteLine("XX CLW_END");
	        });
	    }

	    private static void DeleteFromInput(int symbols = 1)
	    {
	        KInputs.MakeInput(KInputs.AddPress(Keys.Back, symbols));
        }

	    /// <summary>
	    /// Converts last word/line/words.
	    /// </summary>
	    /// <param name="keysToConvert">List of YuKeys to be converted.</param>
	    public static void ConvertLast(List<YuKey> keysToConvert)
	    {
	        try
	        {
	            //Used to catch errors, since it called as Task
	            Debug.WriteLine("Start CL");
	            Debug.WriteLine(keysToConvert.Count + " LL");
	            Logging.Log("[CLAST] > Starting to convert word, count:" + keysToConvert.Count + ", LW: " + MMain.CWord.Count + " Last CR:" + lastLWClearReason);
	            if (keysToConvert.Count <= 0)
	                return;
	            Locales.ShowErrorIfNotEnoughLayouts();
	            var YuKeys = keysToConvert.ToArray();
	            if (MahouUI.SoundOnConvLast)
	                MahouUI.SoundPlay();
	            if (MahouUI.SoundOnConvLast2)
	                MahouUI.Sound2Play();
	            var wasLocale = Locales.GetCurrentLocale() & 0xFFFF;
	            if (MahouUI.UseJKL && !KMHook.JKLERR)
	                wasLocale = MahouUI.currentLayout;
	            var desl = GetNextLayout(wasLocale).uId;
	            if (MahouUI.UseJKL && MahouUI.SwitchBetweenLayouts && MahouUI.EmulateLS && !JKLERR)
	            {
	                Debug.WriteLine("JKL-ed CLW");
	                Logging.Log("[CLAST] > On JKL layout: " + desl);
	                if (!JKLERRchecking)
	                {
	                    Debug.WriteLine("JKL-ed CLW JKLERRNCH");
	                    jklXHidServ.actionOnLayoutExecuted = false;
	                    jklXHidServ.ActionOnLayout = () => StartConvertWord(YuKeys, wasLocale);
	                    jklXHidServ.OnLayoutAction = desl;
	                    ChangeLayout(true);
	                    JKLERRchecking = true;
	                    var t = 0;
	                    JKLERRT.Interval = 50;
	                    JKLERRT.Tick += (_, __) =>
	                    {
	                        if (!jklXHidServ.actionOnLayoutExecuted)
	                        {
	                            Logging.Log("JKL convert word failed, JKL didn't monitor the layout or didn't send it, fallback to default...", 1);
	                            Logging.Log("JKL seems BAD.");
	                            StartConvertWord(YuKeys, wasLocale);
	                            JKLERR = true;
	                            JKLERRchecking = false;
	                            JKLERRT.Stop();
	                            JKLERRT.Dispose();
	                            JKLERRT = new System.Windows.Forms.Timer();
	                        }
	                        else
	                        {
	                            Logging.Log("JKL seems OK.");
	                            Debug.WriteLine("JKL seems OK...");
	                            JKLERRchecking = false;
	                            JKLERRT.Stop();
	                            JKLERRT.Dispose();
	                            JKLERRT = new System.Windows.Forms.Timer();
	                        }

	                        Debug.WriteLine("JKL CHECK...");
	                        if (t > 50)
	                            JKLERRchecking = false;
	                        t++;
	                    };
	                    JKLERRT.Start();
	                }
	            }
	            else
	            {
	                StartConvertWord(YuKeys, wasLocale);
	            }
	        }
	        catch (Exception e)
	        {
	            Logging.Log("Convert Last encountered error, details:\r\n" + e.Message + "\r\n" + e.StackTrace, 1);
	        }

	        Debug.WriteLine("===============> Fin");
	        MMain.Mahou.UpdateLDs();
	        Memory.Flush();
	    }

	    /// <summary>
	    /// </summary>
	    /// <param name="key">Key to be checked.</param>
	    /// <param name="upper">State of key to be checked.</param>
	    /// <param name="wasLocale">Last layout id.</param>
	    /// <returns></returns>
	    static bool NeedToIgnoreSymbol(Keys key, bool upper, uint wasLocale)
	    {
	        Logging.Log("Passing Key = [" + key + "]+[" + (upper ? "UPPER" : "lower") + "] with WasLayoutID = [" + wasLocale + "] through symbol ignore rules.");
	        if (MMain.Mahou.HKSymIgn.Enabled &&
	            MahouUI.SymIgnEnabled &&
	            (wasLocale == 1033 || wasLocale == 1041) &&
	            ((Locales.AllList().Length < 3 && !MahouUI.SwitchBetweenLayouts) ||
	             MahouUI.SwitchBetweenLayouts) && (
	                key == Keys.Oem5 ||
	                key == Keys.OemOpenBrackets ||
	                key == Keys.Oem6 ||
	                key == Keys.Oem1 ||
	                key == Keys.Oem7 ||
	                key == Keys.Oemcomma ||
	                key == Keys.OemPeriod ||
	                key == Keys.OemQuestion))
	        {
	            ReplaceIgnoredSymbols(key, upper);
	            return true;
	        }

	        return false;
	    }

	    /// <summary>
	    /// Rules to ignore symbols in ConvertLast() function.
	    /// </summary>
	    /// <param name="key">Key to be checked.</param>
	    /// <param name="upper">State of key to be checked.</param>
	    /// <param name="wasLocale">Last layout id.</param>
	    /// <returns></returns>
	    static void ReplaceIgnoredSymbols(Keys key, bool upper)
	    {
	        var isCaptured = true;
	        switch (key)
	        {
	            case Keys.Oem5:
	                KInputs.MakeInput(upper ? KInputs.AddString("|") : KInputs.AddString("\\"));
	                break;
	            case Keys.OemOpenBrackets:
	                KInputs.MakeInput(upper ? KInputs.AddString("{") : KInputs.AddString("["));
	                break;
	            case Keys.Oem6:
	                KInputs.MakeInput(upper ? KInputs.AddString("}") : KInputs.AddString("]"));
	                break;
	            case Keys.Oem1:
	                KInputs.MakeInput(upper ? KInputs.AddString(":") : KInputs.AddString(";"));
	                break;
	            case Keys.Oem7:
	                KInputs.MakeInput(upper ? KInputs.AddString("\"") : KInputs.AddString("'"));
	                break;
	            case Keys.Oemcomma:
	                KInputs.MakeInput(upper ? KInputs.AddString("<") : KInputs.AddString(","));
	                break;
	            case Keys.OemPeriod:
	                KInputs.MakeInput(upper ? KInputs.AddString(">") : KInputs.AddString("."));
	                break;
	            case Keys.OemQuestion:
	                KInputs.MakeInput(upper ? KInputs.AddString("?") : KInputs.AddString("/"));
	                break;

	            default:
	                isCaptured = false;
	                break;
	        }

	        if (isCaptured)
	            Memory.Flush();
	    }

	    public static bool IsConhost() {
			var strb = new StringBuilder(256);
			WinAPI.GetClassName(WinAPI.GetForegroundWindow(), strb, strb.Capacity);
			return strb.ToString().Contains("ConsoleWindowClass");
		}
        /// <summary>
        /// Changes current layout.
        /// </summary>
        public static uint ChangeLayout(bool quiet = false)
        {
            uint desired = 0;
            as_lword_layout = 0;
            Debug.WriteLine(">> LC + SELF");
            DoSelf(() =>
            {
                if (!quiet)
                {
                    if (MahouUI.SoundOnLayoutSwitch)
                        MahouUI.SoundPlay();
                    if (MahouUI.SoundOnLayoutSwitch2)
                        MahouUI.Sound2Play();
                }
                if (Locales.ActiveWindowProcess().ProcessName.ToLower() == "HD-Frontend".ToLower())
                {
                    KInputs.MakeInput(KInputs.AddPress(Keys.Space), (int)WinAPI.MOD_CONTROL);
                    Thread.Sleep(13);
                }
                else
                {
                    if (MahouUI.SwitchBetweenLayouts)
                    {
                        uint last = 0;
                        var conhost = false;
                        if (MahouUI.UseJKL && !KMHook.JKLERR)
                        {
                            conhost = IsConhost();
                        }
                        for (var i = MMain.Locales.Length; i != 0; i--)
                        {
                            var nowLocale = Locales.GetCurrentLocale();
                            if (MahouUI.UseJKL)
                            {
                                if (nowLocale == 0 || conhost)
                                    nowLocale = MahouUI.currentLayout;
                                if (last == nowLocale && nowLocale != 0)
                                {
                                    nowLocale = MahouUI.currentLayout;
                                    desired = 0;
                                }
                            }
                            if (nowLocale == desired)
                                break;
                            var notnowLocale = nowLocale == MahouUI.MAIN_LAYOUT1 ? MahouUI.MAIN_LAYOUT2 : MahouUI.MAIN_LAYOUT1;
                            last = nowLocale;
                            ChangeToLayout(Locales.ActiveWindow(), notnowLocale, conhost);
                            desired = notnowLocale;
                            if (MahouUI.EmulateLS)
                                break;
                        }
                    }
                    else
                    {
                        if (MahouUI.EmulateLS)
                        {
                            CycleEmulateLayoutSwitch();
                        }
                        else
                        {
                            CycleLayoutSwitch();
                        }
                    }
                }
            });
            return desired;
        }
        /// <summary>
        /// Calls functions to change layout based on EmulateLS variable.
        /// </summary>
        /// <param name="hwnd">Target window to change its layout.</param>
        /// <param name="LayoutId">Desired layout to switch to.</param>
        public static void ChangeToLayout(IntPtr hwnd, uint LayoutId, bool conhost = false) {
			Debug.WriteLine(">> CTL");
			if (MahouUI.EmulateLS) 
				EmulateChangeToLayout(LayoutId, conhost);
			 else
			 	NormalChangeToLayout(hwnd, LayoutId, conhost);
		}
		/// <summary>
		/// Changing layout to LayoutId in hwnd with PostMessage and WM_INPUTLANGCHANGEREQUEST.
		/// </summary>
		/// <param name="hwnd">Target window to change its layout.</param>
		/// <param name="LayoutId">Desired layout to switch to.</param>
		static void NormalChangeToLayout(IntPtr hwnd, uint LayoutId, bool conhost = false) {
			Debug.WriteLine(">> N-CTL");
			Logging.Log("Changing layout using normal mode, WinAPI.SendMessage [WinAPI.WM_INPUTLANGCHANGEREQUEST] with LParam ["+LayoutId+"].");
			var tries = 0;
			uint last = 0;
			var loc = Locales.GetCurrentLocale();
			//Cycles while layout not changed
			do {
				if (MahouUI.UseJKL && !KMHook.JKLERR)
					if ((loc == last && loc != 0) || conhost)
						loc = MahouUI.currentLayout;
				if (LayoutId == 0) {
					Logging.Log("Layout change skipped, 0 is not layout.", 1);
				} else 
					WinAPI.SendMessage(hwnd, (int)WinAPI.WM_INPUTLANGCHANGEREQUEST, 0, LayoutId);
				Thread.Sleep(10);//Give some time to switch layout
				tries++;
				if (tries == MMain.Locales.Length) {
					Logging.Log("Tries break, probably failed layout changing...",1);
					break;
				}
				last = loc;
			} while (loc != LayoutId);
//			if (!MahouUI.UseJKL) // Wow, gives no sense!!
				MahouUI.currentLayout = MahouUI.GlobalLayout = LayoutId;
		}
		static bool failed = true;
		/// <summary>
		/// Changing layout to LayoutId by emulating windows layout switch hotkey. 
		/// </summary>
		/// <param name="LayoutId">Desired layout to switch to.</param>
		static void EmulateChangeToLayout(uint LayoutId, bool conhost = false) {
			Debug.WriteLine(">> E-CTL");
			var last = MahouUI.currentLayout;
			if (last == LayoutId) {
				if (!conhost && last == Locales.GetCurrentLocale()) {
					Debug.WriteLine("Layout already " + LayoutId);
					return;
				}
				Debug.WriteLine("False, layout isn't actually #"+last);
			}
			Logging.Log("Changing to specific layout ["+LayoutId+"] by emulating layout switch.");
			for (var i = MMain.Locales.Length; i != 0; i--) {
				var loc = Locales.GetCurrentLocale();
//				Debug.WriteLine(loc + " " + last);
				if (MahouUI.UseJKL && !KMHook.JKLERR && ((loc == 0 || loc == last) || conhost)) {
					jklXHidServ.start_cyclEmuSwitch = true;
					jklXHidServ.cycleEmuDesiredLayout = LayoutId;
					Debug.WriteLine("LI: " + LayoutId);
					CycleEmulateLayoutSwitch();
					break;
				} else {
//					Debug.WriteLine(i+".LayoutID: " + LayoutId + ", loc: " +loc);
					if (loc == LayoutId) {
						failed = false;
						break;
					}
					CycleEmulateLayoutSwitch();
					Thread.Sleep(30);
				}
				last = loc;
				if (!failed)
					break;
			}
			if (!MahouUI.UseJKL || KMHook.JKLERR)
				if (!failed) {
					MahouUI.currentLayout = MahouUI.GlobalLayout = LayoutId;
				} else
					Logging.Log("Changing to layout [" + LayoutId + "] using emulation failed after # of layouts tries,\r\nmaybe you have more that 16 layouts, disabled change layout hotkey in windows, or working in console window(use getconkbl.dll)?", 1);
			failed = true;
		}
		/// <summary>
		/// Changing layout by emulating windows layout switch hotkey
		/// </summary>
		public static void CycleEmulateLayoutSwitch() {
			Debug.WriteLine(">> CELS");
			if (MahouUI.EmulateLSType == "Alt+Shift") {
				Logging.Log("Changing layout using cycle mode by simulating key press [Alt+Shift].");
				//Emulate Alt+Shift
				KInputs.MakeInput(KInputs.AddPress(Keys.LShiftKey), (int)WinAPI.MOD_ALT);
			} else if (MahouUI.EmulateLSType == "Ctrl+Shift") {
				Logging.Log("Changing layout using cycle mode by simulating key press [Ctrl+Shift].");
				//Emulate Ctrl+Shift
				KInputs.MakeInput(KInputs.AddPress(Keys.LShiftKey), (int)WinAPI.MOD_CONTROL);
			} else {
				Logging.Log("Changing layout using cycle mode by simulating key press [Win+Space].");
				//Emulate Win+Space
				KInputs.MakeInput(KInputs.AddPress(Keys.Space), (int)WinAPI.MOD_WIN);
				Thread.Sleep(20); //Important!
			}
			if (!MahouUI.UseJKL || KMHook.JKLERR)
				DoLater(() => { MahouUI.currentLayout = MahouUI.GlobalLayout = Locales.GetCurrentLocale(); }, 10);
		}
		public static Locales.Locale GetNextLayout(uint before = 0) {
			Debug.WriteLine(">> GNL");
			var loc = new Locales.Locale();
			uint last = 0;
			var cur = Locales.GetCurrentLocale(); 
			if (MahouUI.UseJKL && !KMHook.JKLERR)
				if (cur == 0 || cur == last)
					cur = MahouUI.currentLayout;
			if (before != 0)
				cur = before;
			Debug.WriteLine("Current: " +cur);
			for (var i=MMain.Locales.Length; i!=0; i--) {
				if (last != 0 && cur != last)
					break;
				var br = false;
				if (MahouUI.SwitchBetweenLayouts) {
					if (cur == MahouUI.MAIN_LAYOUT1) 
						loc.uId = MahouUI.MAIN_LAYOUT2;
					else if (cur == MahouUI.MAIN_LAYOUT2) {
						loc.uId = MahouUI.MAIN_LAYOUT1;
					} else 
						loc.uId = MahouUI.MAIN_LAYOUT1;
					break;
				}
				Thread.Sleep(5);
				var curind = MMain.Locales.ToList().FindIndex(lid => lid.uId == cur);
				for (var g=0; g != MMain.Locales.Length; g++) {
					var l = MMain.Locales[g];
					Debug.WriteLine("Checking: " + l.Lang + ", with "+cur);
					if (curind == MMain.Locales.Length - 1) {
						Logging.Log("Locales BREAK!");
						loc = MMain.Locales[0];
						br = true;
						break;
					}
					Logging.Log("LIDC = "+g +" curid = "+curind + " Lidle = " +(MMain.Locales.Length - 1));
					if (l.Lang.Contains("Microsoft Office IME")) // fake layout
						continue;
					if (g > curind)
						if (l.uId != cur) {
							Logging.Log("Locales +1 Next BREAK on " + l.uId);
							loc = l;
							if (last !=0) // ensure its checked at least twice
								br = true;
							break;
					}
				}
				last = cur;
				if (br)
					break;
			}
			Debug.WriteLine("Next layout: " + loc.uId);
			return loc;
		}
		/// <summary>
		/// Changing layout to next with PostMessage and WM_INPUTLANGCHANGEREQUEST and LParam HKL_NEXT.
		/// </summary>
		public static void CycleLayoutSwitch() {
			Debug.WriteLine(">> CLS");
			Logging.Log("Changing layout using cycle mode by sending Message [WinAPI.WM_INPUTLANGCHANGEREQUEST] with LParam [HKL_NEXT] using WinAPI.PostMessage to ActiveWindow");
			//Use WinAPI.PostMessage to switch to next layout
			ChangeToLayout(Locales.ActiveWindow(), GetNextLayout().uId);
		}
		/// <summary>
		/// Converts character(c) from layout(uID1) to another layout(uID2) by using WinAPI.ToUnicodeEx().
		/// </summary>
		/// <param name="symbol">Character to be converted.</param>
		/// <param name="uID1">Layout id 1(from).</param>
		/// <param name="uID2">Layout id 2(to)</param>
		/// <returns></returns>
		static string ConvertBetweenLayouts(char symbol, uint uID1, uint uID2)  { //Remakes c from uID1  to uID2
			var cc = symbol;
			var chsc = WinAPI.VkKeyScanEx(cc, uID1);
			var state = (chsc >> 8) & 0xff;
			var byt = new byte[256];
			//it needs just 1 but,anyway let it be 10, i think that's better
			var s = new StringBuilder(10);
			//Checks if 'chsc' have upper state
			if (state == 1) {
				byt[(int)Keys.ShiftKey] = 0xFF;
			}
			//"Convert magic✩" is the string below
			var ant = WinAPI.ToUnicodeEx((uint)chsc, (uint)chsc, byt, s, s.Capacity, 0, (IntPtr)uID2);
			return chsc != -1 ? s.ToString() : "";
		}
		/// <summary>
		/// Simplified WinAPI.keybd_event() with extended recognize feature.
		/// </summary>
		/// <param name="key">Key to be inputted.</param>
		/// <param name="flags">Flags(state) of key.</param>
		public static void KeybdEvent(Keys key, int flags)  { // 
			//Do not remove this line, it needed for "Left Control Switch Layout" to work properly
//			Thread.Sleep(15);
			WinAPI.keybd_event((byte)key, 0, flags | (KInputs.IsExtended(key) ? 1 : 0), 0);
		}
		public static void RePressAfter(int mods) {
			ctrlRP = Hotkey.ContainsModifier(mods, (int)WinAPI.MOD_CONTROL);
			shiftRP = Hotkey.ContainsModifier(mods, (int)WinAPI.MOD_SHIFT);
			altRP = Hotkey.ContainsModifier(mods, (int)WinAPI.MOD_ALT);
			winRP = Hotkey.ContainsModifier(mods, (int)WinAPI.MOD_WIN);
		}
		/// <summary>
		/// Sends modifiers up by modstoup array. 
		/// </summary>
		/// <param name="modstoup">Array of modifiers which will be send up. 0 = ctrl, 1 = shift, 2 = alt.</param>
		public static void SendModsUp(int modstoup)  { //
			//These three below are needed to release all modifiers, so even if you will still hold any of it
			//it will skip them and do as it must.
			if (modstoup <= 0) return;
			Debug.WriteLine(">> SMU: " + Hotkey.GetMods(modstoup));
			DoSelf(() => {
				if (Hotkey.ContainsModifier(modstoup, (int)WinAPI.MOD_WIN)) {
					KMHook.KeybdEvent(Keys.LWin, 2); // Right Win Up
					KMHook.KeybdEvent(Keys.RWin, 2); // Left Win Up
					win = win_r = false;
					LLHook.SetModifier(WinAPI.MOD_WIN, false);
				}
				if (Hotkey.ContainsModifier(modstoup, (int)WinAPI.MOD_SHIFT)) {
					KMHook.KeybdEvent(Keys.RShiftKey, 2); // Right Shift Up
					KMHook.KeybdEvent(Keys.LShiftKey, 2); // Left Shift Up
					shift = shift_r = false;
					LLHook.SetModifier(WinAPI.MOD_SHIFT, false);
				}
				if (Hotkey.ContainsModifier(modstoup, (int)WinAPI.MOD_CONTROL)) {
					KMHook.KeybdEvent(Keys.RControlKey, 2); // Right Control Up
					KMHook.KeybdEvent(Keys.LControlKey, 2); // Left Control Up
					ctrl = ctrl_r = false;
					LLHook.SetModifier(WinAPI.MOD_CONTROL, false);
				}
				if (Hotkey.ContainsModifier(modstoup, (int)WinAPI.MOD_ALT)) {
					KMHook.KeybdEvent(Keys.RMenu, 2); // Right Alt Up
					KMHook.KeybdEvent(Keys.LMenu, 2); // Left Alt Up
					alt = alt_r = false;
					LLHook.SetModifier(WinAPI.MOD_ALT, false);
				}
				Logging.Log("Modifiers ["+modstoup+ "] sent up.");
              });
		}
		/// <summary>
		/// Checks if key is modifier, and calls SendModsUp() if it is.
		/// </summary>
		/// <param name="key">Key to be checked.</param>
		public static void IfKeyIsMod(Keys key) {
			uint mods = 0;
			switch (key) {
				case Keys.LControlKey:
				case Keys.RControlKey:
					mods += WinAPI.MOD_CONTROL;
					break;
				case Keys.LShiftKey:
				case Keys.RShiftKey:
					mods += WinAPI.MOD_SHIFT;
					break;
				case Keys.LMenu:
				case Keys.RMenu:
				case Keys.Alt:
					mods += WinAPI.MOD_ALT;
					break;
				case Keys.LWin:
				case Keys.RWin:
					mods += WinAPI.MOD_WIN;
					break;
			}
			if (mods > 0)
				SendModsUp((int)mods);
		}
		public static Tuple<string, uint> WordGuessLayout(string word, uint _target = 0) {
			uint layout = 0;
			var guess = "";
			uint target = 0;
			if (_target == 0) {
				if (MahouUI.SwitchBetweenLayouts) {
					var cur = Locales.GetCurrentLocale();
					if (MahouUI.UseJKL && !KMHook.JKLERR)
						cur = MahouUI.currentLayout;
					target = cur == MahouUI.MAIN_LAYOUT1 ? MahouUI.MAIN_LAYOUT2 : MahouUI.MAIN_LAYOUT1;
				} else 
					target = GetNextLayout().uId;
			} else target = _target;
			for (var i = 0; i != MMain.Locales.Length; i++ ) {
				if (MMain.Locales[i].Lang.Contains("Microsoft Office IME")) // fake layout
					continue;
				var l = MMain.Locales[i].uId;
				var l2 = target;
				if (l == target) continue;
				var wordLMinuses = 0;
				var wordL2Minuses = 0;
				var minmin = 0;
				var thismin = 0;
				uint lay = 0;
				var wordL = "";
				var wordL2 = "";
				var result = "";
				Debug.WriteLine("Testing " +word+" against: " +l+" and "+l2);
				for (var I = 0; I!=word.Length; I++) {
					var c = word[I];
					var sm = false;
					if (c == 'ո' || c == 'Ո') {
						if (c == 'ո') sm = true;
						if (word.Length > I+1) {
							if (word[I+1] == 'ւ') {
								var shrt = l2 & 0xffff;
								var _shrt = l & 0xffff;
								if (shrt == 1033 || shrt == 1041) {
									wordL += sm ? "u" : "U";
									I++; continue;
								}
								if (_shrt == 1033 || _shrt == 1041) {
									wordL2 += sm ? "u" : "U";
									I++; continue;
								}
								if (shrt == 1049) {
									wordL += sm ? "г" : "Г";
									I++; continue;
								}
								if (_shrt == 1049) {
									wordL2 += sm ? "г" : "Г";
									I++; continue;
								}
							}
						}
					}
					var T3 = GermanLayoutFix(c);
					if (T3 != "") {
						wordL += T3;
						wordL2 += T3;
						continue;
					}
					if (c == '\n') {
						wordL += "\n";
						wordL2 += "\n";
						continue;
					}
					var T1 = ConvertBetweenLayouts(c, l & 0xffff, l2 & 0xffff);
					wordL += T1;
					if (T1 == "") wordLMinuses++;
					var T2 = ConvertBetweenLayouts(c, l2 & 0xffff, l & 0xffff);
					wordL2 += T2;
					if (T2 == "") wordL2Minuses++;
//					Debug.WriteLine("T1: "+ T1 + ", T2: "+ T2 + ", C: " +c);
					if (T2 == "" && T1 == "") {
//							Debug.WriteLine("Char ["+c+"] is not in any of two layouts ["+l+"], ["+l2+"] just rewriting.");
						wordL += word[I].ToString();
						wordL2 += word[I].ToString();
					}
				}
				if (wordLMinuses > wordL2Minuses) {
					thismin = wordL2Minuses;
					lay = l2;
					result = wordL2;
				}
				else {
					thismin = wordLMinuses;
					lay = l;
					result = wordL;
				}
//				Debug.WriteLine("End, " + lay + "|" +wordL + ", " + wordL2 + "|" +wordLMinuses + ", " +wordL2Minuses);
				if (wordLMinuses == wordL2Minuses) {
					thismin = wordLMinuses;
					lay = 0;
					result = word;
				}
				if (result.Length > guess.Length || (lay != 0 && thismin <= minmin)) {
					guess = result;
					layout = lay;
				}
				if (thismin < minmin)
					minmin = thismin;
				if (lay == target) break;
			}
			if (target == layout) 
				guess = word;
			if (layout == target) {
				guess_tries++;
				Debug.WriteLine("WARNING! Guess Try [#"+guess_tries+"], target layout and word layout are same!, taking next layout as target!");
				if (guess_tries < MMain.Locales.Length+1) {
					target = GetNextLayout(target).uId;
					Debug.WriteLine("Retry with: layout: " +layout +", target: " + target);
					return WordGuessLayout(word, target);
				} else {
					guess_tries = 0;
				}
			} else {
				guess_tries = 0;
			}
			Debug.WriteLine("Word " + word + " layout is " + layout + " targeting: " + target +" guess: " + guess);
			return Tuple.Create(guess, layout);
		}
		public static Tuple<bool, int> SnippetsLineCommented(string snippets, int k) {
			if (k == 0 || (k-1 >= 0 && snippets[k-1].Equals('\n'))) { // only at every new line
				var end = snippets.IndexOf('\n', k);
				if (end==-1)
					end=snippets.Length;
				var l = end-k-1;
				if (end==-1)
					l = end-k;
				if (end == k)
					l = 0;
				var line = snippets.Substring(k, l);
				if (line.Length > 0) // Ingore empty lines
					if (line[0] == '#' || (line[0] == '/' && (line.Length > 1 && line[1] == '/'))) {
						Logging.Log("Ignored commented line in snippets:[" + line + "].");
						return new Tuple<bool, int>(true, line.Length-1);
					}
			}
			return new Tuple<bool, int>(false, 0);
		}
		public static void GetSnippetsData(string snippets, bool isSnip = true) {
			var leng = 0;
			if (isSnip)
				leng = MahouUI.SnippetsCount;
			else
				leng = MahouUI.AutoSwitchCount;
			var smalls = new string[leng+1024];
			var  bigs = new string[leng+1024];
			if (String.IsNullOrEmpty(snippets)) return;
			snippets = snippets.Replace("\r", "");
			int last_exp_len = 0, ids = 0, idb = 0, add_alias = 0;
			for (var k = 0; k < snippets.Length-6; k++) {
				var com = SnippetsLineCommented(snippets, k);
				if (com.Item1) {
					k+=com.Item2; // skip commented line, speedup!
					continue;
				}
				if ((last_exp_len <= 0 || last_exp_len-- == 0) && snippets[k].Equals('-') && snippets[k+1].Equals('>')) {
					var len = -1;
					var endl = snippets.IndexOf('\n', k+2);
					if (endl==-1)
						endl=snippets.Length;
//					Debug.WriteLine((k+2) + " X " +endl);
					var cool = snippets.Substring(k+2, endl - (k+2));
					if (cool.Length > 4)
						for (var i = 0; i != cool.Length-5; i ++) {
							if (cool[i].Equals('=') && cool[i+1].Equals('=') && cool[i+2].Equals('=') && cool[i+3].Equals('=') && cool[i+4].Equals('>')) {
								len = i;
							}
						}
					else 
						len = cool.Length;
					if (len == -1)
						len = endl-(k+2);
					var sm = snippets.Substring(k+2, len).Replace("\r", "");
					if (sm.Contains("|")) {
						var esm = sm.Replace("||", pipe_esc);
						foreach (var n in esm.Split('|')) {
							smalls[ids] = n.Replace(pipe_esc , "|");
//							Debug.WriteLine("ADded sm alias: " +ids + ", ++ " + smalls[ids]);
							ids++;
							add_alias++;
						}
					} else {
						smalls[ids] = sm;
						ids++;
					}
				}
				if (snippets[k].Equals('=') && snippets[k+1].Equals('=') && snippets[k+2].Equals('=') && snippets[k+3].Equals('=') && snippets[k+4].Equals('>')) {
					var endl = snippets.IndexOf('\n', k+2);
					if (endl==-1)
						endl=snippets.Length;
					var pool = snippets.Substring(k+5, endl - (k+5));
					if(isSnip)
						pool = snippets.Substring(k+5);
					var pyust = new StringBuilder(); // Should be faster than string +=
					for (var g = 0; g != pool.Length-5; g++) {
						if (pool[g].Equals('<') && pool[g+1].Equals('=') && pool[g+2].Equals('=') && pool[g+3].Equals('=') && pool[g+4].Equals('='))
							break;
						pyust.Append(pool[g]);
					}
					last_exp_len = pyust.Length;
					if (add_alias != 0) {
						while (add_alias != 0) {
//							Debug.WriteLine("ADded exp alias: " +idb + ", ++ " + pyust);
							bigs[idb] = (pyust.ToString());
							idb++;
							add_alias--;
						}
					} else {
						bigs[idb] = (pyust.ToString());
						idb++;
					}
					k+=5;
				}
			}
			if (isSnip) {
//				snipps = exps = null;
//				Memory.Flush();
				snipps = smalls;
				exps = bigs;
			} else {
//				as_wrongs = as_corrects = null;
//				Memory.Flush();
				as_wrongs = smalls;
				as_corrects = bigs;
				
			}
		}
		/// <summary>
		/// Re-Initializes snippets.
		/// </summary>
		public static void ReInitSnippets() {
			if (System.IO.File.Exists(MahouUI.snipfile)) {
				var snippets = System.IO.File.ReadAllText(MahouUI.snipfile);
				Stopwatch watch = null;
				if (MahouUI.LoggingEnabled) {
					watch = new Stopwatch();
					watch.Start();
				}
				GetSnippetsData(snippets);
				if (MahouUI.LoggingEnabled) {
					watch.Stop();
					Logging.Log("Snippets init finished, elapsed ["+watch.Elapsed.TotalMilliseconds+"] ms.");
					watch.Reset();
					watch.Start();
				}
				if (MahouUI.AutoSwitchEnabled)
					GetSnippetsData(MahouUI.AutoSwitchDictionaryRaw, false);
				else {
					as_wrongs = as_corrects = null;
					Memory.Flush();
				}
				if (MahouUI.LoggingEnabled && MahouUI.AutoSwitchEnabled) {
					watch.Stop();
					Logging.Log("AutoSwitch dictionary init finished, elapsed ["+watch.Elapsed.TotalMilliseconds+"] ms.");
				}
			}
			Memory.Flush();
		}
		#region Snippets Aliases
		static readonly string pipe_esc = "__pipeEscape::";
		#endregion
		/// <summary>
		///  Contains key(Keys key), it state(bool upper), if it is Alt+[NumPad](bool altnum) and array of numpads(list of numpad keys).
		/// </summary>
		public struct YuKey {
			public Keys Key;
			public bool IsUpper;
			public bool IsAltNumPad;
			public List<Keys> Numpads;
		}
		#endregion
	}
}