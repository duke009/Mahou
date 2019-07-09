using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Mahou.Classes {
	static class KInputs {
	    /// <summary>
	    /// Creates INPUT from key and state.
	    /// </summary>
	    /// <param name="key">Key to be converted to INPUT.</param>
	    /// <param name="isPressed">State of key(down=true, up=false)</param>
	    /// <returns>WinAPI.INPUT</returns>
	    public static WinAPI.INPUT AddKey(Keys key, bool isPressed, bool isUnicode = false)
	    {
	        var vk = isUnicode ? (ushort)0x0u : (ushort) key;
	        var scan = isUnicode ? (ushort)key : (ushort) WinAPI.MapVirtualKey(vk, 0);
	        System.Diagnostics.Debug.WriteLine("ADDED VK: " + vk + " KEY: " + key + " scan: " + scan);

	        var flags = isPressed ? 0 : WinAPI.KEYEVENTF_KEYUP;
	        if (IsExtended(key))
	            flags |= WinAPI.KEYEVENTF_EXTENDEDKEY;
	        if (isUnicode)
	            flags |= WinAPI.KEYEVENTF_UNICODE;

	        var input = new WinAPI.INPUT
	        {
	            Type = WinAPI.INPUT_KEYBOARD,
	            Data =
	            {
	                Keyboard = new WinAPI.KEYBDINPUT
	                {
	                    Vk = vk,
	                    Flags = flags,
	                    Scan = scan,
	                    ExtraInfo = IntPtr.Zero,
	                    Time = 0
	                }
	            }
	        };
	        return input;
	    }

	    public static WinAPI.INPUT[] AddPress(Keys key, int times = 1, bool isUnicode = false) {
			var q = new List<WinAPI.INPUT>();
			for (var j = 0; j <= times-1; j++) {
				System.Diagnostics.Debug.WriteLine("Sending "+j+", key:"+key);
				q.Add(AddKey(key, true, isUnicode));
				q.Add(AddKey(key, false, isUnicode));
			}
			return q.ToArray();;
	    }
		/// <summary>
	    /// Returns true if key is extended, else false.
	    /// </summary>
	    /// <param name="key">Key to be checked.</param>
	    /// <returns>bool</returns>
	    public static bool IsExtended(Keys key) { //Check for extended keys
			return key == Keys.RMenu ||
				key == Keys.RControlKey ||
				key == Keys.Insert ||
				key == Keys.Delete ||
				key == Keys.Home ||
				key == Keys.End || 
				key == Keys.Prior ||
				key == Keys.Next || 
				key == Keys.Right ||
				key == Keys.Up || 
				key == Keys.Left ||
				key == Keys.Down || 
				key == Keys.NumLock || 
				key == Keys.Cancel ||
				key == Keys.Snapshot || 
				key == Keys.Return || 
				key == Keys.Divide;
	    }
	    public static string GetWordByIndex(string LINE, int index) {
	    	var WORDS = KMHook.SplitWords(LINE);
	    	var word = "";
	    	var len = 0;
	    	for (var i = 0; i != WORDS.Length; i++) {
	    		if (index >= len && index <= (len += WORDS[i].Length)) {
	    			word = WORDS[i];
	//    			System.Diagnostics.Debug.WriteLine("WORD AT INDEX "+index + " IS => ["+word+"]");
	    		}
	    	}
			return word;
	    }
	    /// <summary>
	    /// Creates array of INPUT from string.
	    /// </summary>
	    /// <param name="str">String to be converted into INPUT[].</param>
	    /// <returns>INPUT[]</returns>
	    public static WinAPI.INPUT[] AddString(string str) {
	        var result = new List<WinAPI.INPUT>();
	        var index = 0;
	        foreach (var s in str) {
	        	bool uselt1_vk, uselt2_vk;
	        	ushort resultvk = 0;
	        	var lt1_vk = WinAPI.VkKeyScanEx(s, Mahou.MahouUI.MAIN_LAYOUT1);
	        	uselt1_vk = lt1_vk != -1;
	        	var lt2_vk = WinAPI.VkKeyScanEx(s, Mahou.MahouUI.MAIN_LAYOUT2);
	        	uselt2_vk = lt2_vk != -1;
	        	if (uselt1_vk && uselt2_vk) {
	        		var guess = KMHook.WordGuessLayout(GetWordByIndex(str, index));
	//        		System.Diagnostics.Debug.WriteLine("ST:"+guess.Item2);
	        		var lt_guess = guess.Item2;
	        		resultvk = (ushort)WinAPI.VkKeyScanEx(s, lt_guess);
	        	} else if (uselt1_vk)
	        		resultvk = (ushort)lt1_vk;
	        	else if (uselt2_vk) 
	        		resultvk = (ushort)lt2_vk;
	        	var resultvk_state = ((resultvk >> 8) & 0xff) == 1;
	        	if (resultvk_state)
	        		result.Add(KInputs.AddKey(Keys.RShiftKey, true));
	            var down = new WinAPI.INPUT {
	                Type = WinAPI.INPUT_KEYBOARD,
	                Data = {
	                    Keyboard = new WinAPI.KEYBDINPUT {
	                    	Vk = MahouUI.GuessKeyCodeFix ? resultvk : (ushort)0,
	                        Flags = (UInt32)(WinAPI.KEYEVENTF_UNICODE),
	                        Scan = (UInt16)s,
	                        ExtraInfo = IntPtr.Zero,
	                        Time = 0
	                    }
	                }
	            };
	            var up = new WinAPI.INPUT {
	                Type = WinAPI.INPUT_KEYBOARD,
	                Data = {
	                    Keyboard = new WinAPI.KEYBDINPUT {
	                    	Vk = MahouUI.GuessKeyCodeFix ? resultvk : (ushort)0,
	                        Flags = (UInt32)(WinAPI.KEYEVENTF_UNICODE | WinAPI.KEYEVENTF_KEYUP),
	                        Scan = (UInt16)s,
	                        ExtraInfo = IntPtr.Zero,
	                        Time = 0
	                    }
	                }
	            };
	            if (s == '\n') {
	                down = AddKey(Keys.Return, true);
	                up = AddKey(Keys.Return, false);
	            }
	            result.Add(down);
	            result.Add(up);
	        	if (resultvk_state)
	        		result.Add(KInputs.AddKey(Keys.RShiftKey, false));
	            index++;
	        }
	        return result.ToArray();
	    }
	    /// <summary>
	    /// Makes input INPUT's in "inputs" variable.
	    /// </summary>
	    /// <param name="inputs">Array of INPUT to be inputted.</param>
	    public static void MakeInput(WinAPI.INPUT[] inputs, int mods = 0) { //Simply, sends input
	    	var rinputs = new List<WinAPI.INPUT>();
	    	if (Hotkey.ContainsModifier(mods, (int)WinAPI.MOD_SHIFT)) rinputs.Add(AddKey(Keys.LShiftKey, true));
	    	if (Hotkey.ContainsModifier(mods, (int)WinAPI.MOD_ALT)) rinputs.Add(AddKey(Keys.LMenu, true));
	    	if (Hotkey.ContainsModifier(mods, (int)WinAPI.MOD_CONTROL)) rinputs.Add(AddKey(Keys.LControlKey, true));
	    	if (Hotkey.ContainsModifier(mods, (int)WinAPI.MOD_WIN)) rinputs.Add(AddKey(Keys.LWin, true));
	    	rinputs.AddRange(inputs);
	    	if (Hotkey.ContainsModifier(mods, (int)WinAPI.MOD_SHIFT)) rinputs.Add(AddKey(Keys.LShiftKey, false));
	    	if (Hotkey.ContainsModifier(mods, (int)WinAPI.MOD_ALT)) rinputs.Add(AddKey(Keys.LMenu, false));
	    	if (Hotkey.ContainsModifier(mods, (int)WinAPI.MOD_CONTROL)) rinputs.Add(AddKey(Keys.LControlKey, false));
	    	if (Hotkey.ContainsModifier(mods, (int)WinAPI.MOD_WIN)) rinputs.Add(AddKey(Keys.LWin, false));
	    	var sinputs = rinputs.ToArray();
	    	var done = WinAPI.SendInput((UInt32)sinputs.Length, sinputs, Marshal.SizeOf(typeof(WinAPI.INPUT)));
	    	System.Diagnostics.Debug.WriteLine("VK SENDED: " + sinputs[0].Data.Keyboard.Vk);
	    	if (done != sinputs.Length)
	    		Logging.Log("ERROR during send input, lenght: " +done+ ", Win32ERR: " + Marshal.GetLastWin32Error());
	    }
	}
}