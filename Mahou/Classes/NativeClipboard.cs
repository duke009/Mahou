using System;
using System.Runtime.InteropServices;
//using System.Collections.Generic;

namespace Mahou.Classes
{
    public static class NativeClipboard {
    	/// <summary>
    	/// Clears clipboard.
    	/// </summary>
        public static void Clear() {
            WinAPI.OpenClipboard(IntPtr.Zero);
            WinAPI.EmptyClipboard();
            WinAPI.CloseClipboard();
        }
        /// <summary>
        /// Gets clipboard text if clipboard data contains text(CF_UNICODETEXT).
        /// </summary>
        /// <returns>string</returns>
        public static string GetText()  { // Gets text data from clipboard

            if (!WinAPI.IsClipboardFormatAvailable(WinAPI.CF_UNICODETEXT))
                return null;
            var Tries = 0;
            var opened = false;
            string data = null;
            while (true) {
                ++Tries;
                opened = WinAPI.OpenClipboard(IntPtr.Zero);
                var hGlobal = WinAPI.GetClipboardData(WinAPI.CF_UNICODETEXT);
                var lpwcstr = WinAPI.GlobalLock(hGlobal);
                data = Marshal.PtrToStringUni(lpwcstr);
                if (opened) {
                    WinAPI.GlobalUnlock(hGlobal);
                    break;
                }
                System.Threading.Thread.Sleep(1);
            }
            WinAPI.CloseClipboard();
            Logging.Log("Clipboard text was get.");
            return data;
        }
        /// <summary>
        /// Gets all clipboard data's, but only text-based data supported... for now...
        /// </summary>
        /// <returns></returns>
//        public static ClipboardData GetClipboardDatas()
//        {
//            var cd = new ClipboardData()
//            {
//                data = new List<byte[]>(),
//                format = new List<uint>()
//            };
//            WinAPI.OpenClipboard(IntPtr.Zero);
//            foreach (var fmt in new uint[]{1,2,4,5,6,7,8,9,10,11,12,13,14})
//            {
//                IntPtr pos = WinAPI.GetClipboardData(fmt);
//                if (pos == IntPtr.Zero)
//                    continue;
//                UIntPtr lenght = WinAPI.GlobalSize(pos);
//                IntPtr gLock = WinAPI.GlobalLock(pos);
//                //Console.WriteLine(fmt + " is available in clipboard!!");
//                byte[] data;
//                if ((uint)lenght > 0)
//                {
//                    //Init a buffer which will contain the clipboard data
//                    data = new byte[(uint)lenght];
//                    int l = Convert.ToInt32(lenght.ToString());
//                    //Copy data from clipboard to our byte[] buffer
//                    Marshal.Copy(gLock, data, 0, l);
//                }
//                else
//                {
//                    data = new byte[0];
//                }
//                cd.data.Add(data);
//                cd.format.Add(fmt);
//            }
//            WinAPI.CloseClipboard();
//            return cd;
//        }
        /// <summary>
        /// Stores all data's to clipboard, but only text-based data supported... for now...
        /// </summary>
        /// <param name="datas">Data's to be stored into clipboard.</param>
//        public static void RestoreData(ClipboardData datas)
//        {
//            WinAPI.OpenClipboard(IntPtr.Zero);
//            WinAPI.EmptyClipboard();
//            for (int i = 0; i != datas.data.Count; i++)
//            {
//                var data = datas.data[i];
//                IntPtr alloc = WinAPI.GlobalAlloc(WinAPI.GMEM_MOVEABLE | WinAPI.GMEM_DDESHARE, new UIntPtr(Convert.ToUInt32(data.GetLength(0))));
//                var glock = WinAPI.GlobalLock(alloc);
//                if (glock == IntPtr.Zero) {
//                	WinAPI.CloseClipboard();
//                	WinAPI.OpenClipboard(IntPtr.Zero);
//                	WinAPI.EmptyClipboard();
//                	continue;
//                }
//                var fmt = datas.format[i];
//                Marshal.Copy(data, 0, glock, data.GetLength(0));
//                WinAPI.SetClipboardData(fmt, alloc);
//                WinAPI.GlobalUnlock(alloc);
//            }
//            WinAPI.CloseClipboard();
//            Logging.Log("Clipboard text was restored");
//        }
        /// <summary>
        /// Contains data(list of byte array) and data format(uint).
        /// </summary>
//        public struct ClipboardData
//        {
//            public List<byte[]> data;
//            public List<uint> format;
//        }
    }
}
