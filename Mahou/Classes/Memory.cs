using System;
using System.Runtime.InteropServices;

namespace Mahou.Classes
{
    public static class Memory {
        [DllImport("kernel32.dll", EntryPoint = "SetProcessWorkingSetSize",
            ExactSpelling = true, CharSet = CharSet.Ansi, SetLastError = true)] 
        static extern int SetProcessWorkingSetSize(IntPtr process, int minimumWorkingSetSize, int
            maximumWorkingSetSize); 
        public static void Flush() {
            //GC.Collect();
            //GC.WaitForPendingFinalizers();
            if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
                SetProcessWorkingSetSize(System.Diagnostics.Process.GetCurrentProcess().Handle, -1, -1);
            }
        }
    }
}