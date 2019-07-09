using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Mahou.Classes;
using Timer = System.Threading.Timer;

namespace Mahou
{
    internal class MMain
    {
        #region DLLs

        [DllImport("user32.dll")]
        public static extern uint RegisterWindowMessage(string message);

        #endregion

        [STAThread] //DO NOT REMOVE THIS
        public static void Main(string[] args)
        {
            Application.EnableVisualStyles(); // at first enable styles.
            //Catch any error during program runtime
            AppDomain.CurrentDomain.UnhandledException += (obj, arg) =>
            {
                var e = (Exception) arg.ExceptionObject;
                Logging.Log("Unexpected error occurred, Mahou exited, error details:\r\n" + e.Message + "\r\n" + e.StackTrace, 1);
            };
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            if (CultureInfo.InstalledUICulture.TwoLetterISOLanguageName == "ru")
                Lang = Languages.Russian;
            MyConfs = new Configs();
            if (Configs.forceAppData && Configs.fine)
                MyConfs.Write("Functions", "AppDataConfigs", "true");
            Logging.Log("Mahou started.");
            using (var mutex = new Mutex(false, GgpuMutex))
            {
                if (!mutex.WaitOne(0, false))
                {
                    if (args.Length > 0)
                    {
                        var arg1 = args[0].ToUpper();
                        if (arg1.StartsWith("/R") || arg1.StartsWith("-R") || arg1.StartsWith("R"))
                        {
                            WinAPI.PostMessage((IntPtr) 0xffff, MH_RESTART, 0, 0);
                            return;
                        }
                    }

                    WinAPI.PostMessage((IntPtr) 0xffff, MH_ALREADY_OPEN, 0, 0);
                    return;
                }

                if (MyConfs.ReadBool("Functions", "AppDataConfigs"))
                {
                    var mahouFolderAppd = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mahou");
                    if (!Directory.Exists(mahouFolderAppd))
                        Directory.CreateDirectory(mahouFolderAppd);
                    if (!File.Exists(Path.Combine(mahouFolderAppd, "Mahou.ini"))) // Copy main configs to appdata
                        File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mahou.ini"),
                            Path.Combine(mahouFolderAppd, "Mahou.ini"), true);
                    Configs.filePath = Path.Combine(mahouFolderAppd, "Mahou.ini");
                    MyConfs = new Configs();
                }
                else
                {
                    Configs.filePath = Path.Combine(MahouUI.nPath, "Mahou.ini");
                }

                MahouUI.latest_save_dir = Configs.filePath;
                if (MyConfs.ReadBool("FirstStart", "First"))
                {
                    if (CultureInfo.InstalledUICulture.TwoLetterISOLanguageName == "ru")
                    {
                        MyConfs.WriteSave("Appearence", "Language", "Русский");
                        MahouUI.InitLanguage();
                        MyConfs.WriteSave("Layouts", "SpecificLayout1", Lang[Languages.Element.SwitchBetween]);
                        MyConfs.WriteSave("FirstStart", "First", "False");
                    }
                }
                else
                {
                    MahouUI.InitLanguage();
                }

                RefreshLcnMid();
                //for first run, add your locale 1 & locale 2 to settings
                if (MyConfs.Read("Layouts", "MainLayout1") == "" && MyConfs.Read("Layouts", "MainLayout2") == "")
                {
                    Logging.Log("Initializing locales.");
                    MyConfs.Write("Layouts", "MainLayout1", Lcnmid[0]);
                    if (Lcnmid.Count > 1)
                        MyConfs.Write("Layouts", "MainLayout2", Lcnmid[1]);
                    MyConfs.WriteToDisk();
                }

                Mahou = new MahouUI();
                Rif = new RawInputForm();
                global::Mahou.Classes.Locales.ShowErrorIfNotEnoughLayouts();
                if (MyConfs.Read("Layouts", "MainLayout1") == "" && MyConfs.Read("Layouts", "MainLayout2") == "")
                {
                    Mahou.cbb_MainLayout1.SelectedIndex = 0;
                    if (Lcnmid.Count > 1)
                        Mahou.cbb_MainLayout2.SelectedIndex = 1;
                }

                EvtHookId = WinAPI.SetWinEventHook(WinAPI.EVENT_SYSTEM_FOREGROUND, WinAPI.EVENT_SYSTEM_FOREGROUND,
                    IntPtr.Zero, EvtProc, 0, 0, WinAPI.WINEVENT_OUTOFCONTEXT);
                LDevtHookId = WinAPI.SetWinEventHook(WinAPI.EVENT_OBJECT_FOCUS, WinAPI.EVENT_OBJECT_FOCUS,
                    IntPtr.Zero, LDevtProc, 0, 0, WinAPI.WINEVENT_OUTOFCONTEXT);
                if (args.Length != 0)
                {
                    if (args[0] == "_!_updated_!_")
                    {
                        Logging.Log("Mahou updated.");
                        Mahou.ToggleVisibility();
                        MessageBox.Show(Lang[Languages.Element.UpdateComplete], Lang[Languages.Element.UpdateComplete],
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                    if (args[0] == "_!_silent_updated_!_")
                    {
                        Logging.Log("Mahou silently updated.");
                        Mahou.icon.trIcon.Visible = true;
                        Mahou.icon.trIcon.ShowBalloonTip(1000, Lang[Languages.Element.UpdateComplete], "Mahou -> " + Mahou.Text, ToolTipIcon.Info);
                        Mahou.icon.trIcon.BalloonTipClicked += (_, __) => Mahou.ToggleVisibility();
                        if (!MahouUI.TrayIconVisible)
                            KMHook.DoLater(() => Mahou.Invoke((MethodInvoker) delegate { Mahou.icon.trIcon.Visible = false; }), 1005);
                    }
                }

                MyConfs.WriteToDisk();
                if (!string.IsNullOrEmpty(MahouUI.MainLayout1))
                    MahouUI.GlobalLayout = MahouUI.currentLayout = global::Mahou.Classes.Locales.GetLocaleFromString(MahouUI.MainLayout1).uId;
                Application.Run();
            }
        }

        public static void RefreshLcnMid()
        {
            Locales = global::Mahou.Classes.Locales.AllList();
            Lcnmid.Clear();
            foreach (var lc in Locales) Lcnmid.Add(lc.Lang + "(" + lc.uId + ")");
        }

        public static bool MahouActive()
        {
            var actHandle = WinAPI.GetForegroundWindow();
            if (actHandle == IntPtr.Zero) return false;
            var active = Mahou.Handle == actHandle;
            Logging.Log("Mahou is active = [" + active + "]" + ", Mahou handle [" + Mahou.Handle + "], fore win handle [" + actHandle + "]");
            return active;
        }

        #region Prevent another instance variables

        /// GGPU = Global GUID PC User
        public static string GgpuMutex = "Global\\" + "ec511418-1d57-4dbe-a0c3-c6022b33735b_" + Environment.UserDomainName + "_" + Environment.UserName;

        public static uint MH_ALREADY_OPEN = RegisterWindowMessage("AlreadyOpenedMahou!");
        public static uint MH_RESTART = RegisterWindowMessage("RestartMahou!");

        #endregion

        #region All Main variables, arrays etc.

        public static List<KMHook.YuKey> CWord = new List<KMHook.YuKey>();
        public static List<List<KMHook.YuKey>> CWords = new List<List<KMHook.YuKey>>();
        public static IntPtr EvtHookId = IntPtr.Zero;
        public static IntPtr LDevtHookId = IntPtr.Zero;
        public static WinAPI.WinEventDelegate EvtProc = KMHook.EventHookCallback;
        public static WinAPI.WinEventDelegate LDevtProc = KMHook.LDEventHook;
        public static Locales.Locale[] Locales = global::Mahou.Classes.Locales.AllList();
        public static string Language = "";
        public static Dictionary<Languages.Element, string> Lang = Languages.English;
        public static Configs MyConfs;
        public static MahouUI Mahou;
        public static IntPtr MahouHandle;
        public static RawInputForm Rif;

        public static Timer LogTimer = new Timer(_ =>
        {
            try
            {
                Logging.UpdateLog();
            }
            catch (Exception e)
            {
                Logging.Log("Error updating log, details:\r\n" + e.Message);
            }
        }, null, 20, 300);

        public static List<string> Lcnmid = new List<string>();

        #endregion
    }
}