using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Media;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Mahou.Classes;
using Mahou.Properties;
using Microsoft.Win32;
using Timer = System.Windows.Forms.Timer;

namespace Mahou
{
    public partial class MahouUI : Form
    {
        public MahouUI()
        {
            DeleteTrash();
            MMain.MahouHandle = Handle;
            InitializeComponent();
            // Visual designer always wants to put that string into resources, blast it!
            txt_Snippets.Text = "-><" + KMHook.__ANY__ + ">====><" + KMHook.__ANY__ + ">__cursorhere()</" + KMHook.__ANY__ +
                                "><====\r\n->mahou\r\n====>Mahou (魔法) - Magical layout switcher.<====\r\n->eml\r\n====>BladeMight@" +
                                "gmail.com<====\r\n->nowtime====>__date(HH:mm:ss)<====\r\n->nowdate====>__date(dd/MM/yyyy)<====\r\n->datepretty====>__date(dd, ddd MMM)<====" +
                                "\r\n->mahouver====>__version()<====\r\n->mahoutitle====>__title()<====\r\n->env_system====>__system()<====\r\n->date_esc====>\\__date(HH:mm:ss)<====";
            InitializeTrayIcon();
            // Switch to more secure connection.
            ServicePointManager.SecurityProtocol = (SecurityProtocolType) 3072;
            LoadConfigs();
            InitializeListBoxes();
            // Set minnimum values because they're ALWAYS restores to 0 after Form Editor is used.
            nud_CapsLockRefreshRate.Minimum = nud_DoubleHK2ndPressWaitTime.Minimum =
                nud_LangTTCaretRefreshRate.Minimum = nud_LangTTMouseRefreshRate.Minimum =
                    nud_ScrollLockRefreshRate.Minimum = nud_TrayFlagRefreshRate.Minimum =
                        nud_PersistentLayout1Interval.Minimum = nud_PersistentLayout2Interval.Minimum = 1;
            nud_LangTTPositionX.Minimum = nud_LangTTPositionY.Minimum = -100;
            // Disable horizontal scroll
            pan_TrSets.AutoScroll = pan_KeySets.AutoScroll = false;
            pan_TrSets.HorizontalScroll.Maximum = pan_KeySets.HorizontalScroll.Maximum = 0;
            pan_TrSets.AutoScroll = pan_KeySets.AutoScroll = true;
            Text = "Mahou " + Assembly.GetExecutingAssembly().GetName().Version;
            Text += "-dev";
            if (____.commit != "")
            {
                Text += " <" + ____.commit + ">";
                MMain.MyConfs.Write("Updates", "LatestCommit", ____.commit);
                MMain.MyConfs.WriteToDisk();
            }
            else
            {
                var commit = MMain.MyConfs.Read("Updates", "LatestCommit");
                if (MMain.MyConfs.Read("Updates", "LatestCommit").Length == 7)
                    Text += " <" + commit + ">";
            }

            var mult = 4;
            if (KMHook.IsNotWin7())
                mult = 6;
            if (tabs.RowCount >= 2)
                mult++;
            if (tabs.RowCount >= 3)
                mult += 2;
            var pty = tabs.RowCount * mult;
            var lsy = btn_OK.Location.Y + pty;
            btn_Apply.Location = new Point(btn_Apply.Location.X, lsy);
            btn_Cancel.Location = new Point(btn_Cancel.Location.X, lsy);
            btn_OK.Location = new Point(btn_OK.Location.X, lsy);
            tabs.Height += pty;
            Height += pty;
#if GITHUB_RELEASE
			Text = "Mahou " + Assembly.GetExecutingAssembly().GetName().Version;
			#endif
            RegisterHotkeys();
            RefreshAllIcons();

            Memory.Flush();
        }

        #region Variables

        // Hotkeys, HKC => HotKey Convert
        public Hotkey Mainhk,
            ExitHk,
            HKCLast,
            HKCSelection,
            HKCLine,
            HKSymIgn,
            HKConMorWor,
            HKTitleCase,
            HKRandomCase,
            HKSwapCase,
            HKTransliteration,
            HKRestart,
            HKToggleLP,
            HKShowST,
            HKToggleMahou,
            HKUpperCase,
            HKLowerCase;

        public List<Hotkey> SpecificSwitchHotkeys = new List<Hotkey>();

        /// <summary>
        ///     Hotkey OK to fire action bools.
        /// </summary>
        public bool hksTTCOK,
            hksTRCOK,
            hksTSCOK,
            hksTrslOK,
            hkShWndOK,
            hkcwdsOK,
            hklOK,
            hksOK,
            hklineOK,
            hkSIOK,
            hkExitOK,
            hkToglLPOK,
            hkShowTSOK,
            hkToggleMahouOK,
            hkUcOK,
            hklcOK;

        public static string nPath = AppDomain.CurrentDomain.BaseDirectory, CustomSound, CustomSound2;

        public static bool LoggingEnabled,
            dummy,
            CapsLockDisablerTimer,
            LangPanelUpperArrow,
            mouseLTUpperArrow,
            caretLTUpperArrow,
            ShiftInHotkey,
            AltInHotkey,
            CtrlInHotkey,
            WinInHotkey,
            AutoStartAsAdmin,
            UseJKL,
            AutoSwitchEnabled,
            ReadOnlyNA,
            SoundEnabled,
            UseCustomSound,
            SoundOnAutoSwitch,
            SoundOnConvLast,
            SoundOnSnippets,
            SoundOnLayoutSwitch,
            UseCustomSound2,
            SoundOnAutoSwitch2,
            SoundOnConvLast2,
            SoundOnSnippets2,
            SoundOnLayoutSwitch2,
            TrOnDoubleClick,
            TrEnabled,
            TrBorderAero,
            OnceSpecific,
            WriteInputHistory;

        private static bool isold = true, snip_checking, as_checking, check_ASD_size = true;
        public static bool ENABLED = true;

        #region Timers

        private static Timer tmr = new Timer();
        private static Timer old = new Timer();
        private static Timer stimer = new Timer();
        private static readonly Timer animate = new Timer();
        public Timer ICheck = new Timer();
        public Timer ScrlCheck = new Timer();
        public Timer crtCheck = new Timer();
        public Timer capsCheck = new Timer();
        public Timer flagsCheck = new Timer();
        public Timer persistentLayout1Check = new Timer();
        public Timer persistentLayout2Check = new Timer();
        public Timer langPanelRefresh = new Timer();
        public Timer res = new Timer();
        public Timer resC = new Timer();

        #endregion

        private static uint lastTrayFlagLayout;
        public static Bitmap FLAG, ITEXT;
        public string SnippetsExpandType = "";
        private int titlebar = 12;
        public static int SpecKeySetCount, SnippetsCount, AutoSwitchCount, TrSetCount, InputHistoryBackSpaceWriteType;
        public int DoubleHKInterval = 200, SelectedTextGetMoreTriesCount, DelayAfterBackspaces;

        #region Temporary variables

        /// <summary> Translate Panel Colors</summary>
        public static Color TrFore, TrBack, TrBorder;

        public static Font TrText, TrTitle;
        public static int TrTransparency;

        /// <summary> In memory settings, for timers/hooks.</summary>
        public static bool DiffAppearenceForLayouts,
            LDForCaretOnChange,
            LDForMouseOnChange,
            ScrollTip,
            AddOneSpace,
            TrayFlags,
            TrayText,
            SymIgnEnabled,
            TrayIconVisible,
            SnippetsEnabled,
            ChangeLayouByKey,
            EmulateLS,
            RePress,
            BlockHKWithCtrl,
            blueIcon,
            SwitchBetweenLayouts,
            SelectedTextGetMoreTries,
            ReSelect,
            ConvertSelectionLS,
            ConvertSelectionLSPlus,
            MCDSSupport,
            OneLayoutWholeWord,
            MouseTTAlways,
            OneLayout,
            MouseLangTooltipEnabled,
            CaretLangTooltipEnabled,
            QWERTZ_fix,
            ChangeLayoutInExcluded,
            SnippetSpaceAfter,
            SnippetsSwitchToGuessLayout,
            AutoSwitchSpaceAfter,
            AutoSwitchSwitchToGuessLayout,
            GuessKeyCodeFix,
            Dowload_ASD_InZip,
            LDForCaret,
            LDForMouse,
            LDUseWindowsMessages,
            RemapCapslockAsF18,
            Add1NL,
            PersistentLayoutOnWindowChange,
            PersistentLayoutOnlyOnce,
            PersistentLayoutForLayout1,
            PersistentLayoutForLayout2,
            UseDelayAfterBackspaces;

        /// <summary> Temporary modifiers of hotkeys. </summary>
        private string Mainhk_tempMods,
            ExitHk_tempMods,
            HKCLast_tempMods,
            HKCSelection_tempMods,
            HKCLine_tempMods,
            HKSymIgn_tempMods,
            HKConMorWor_tempMods,
            HKTitleCase_tempMods,
            HKRandomCase_tempMods,
            HKSwapCase_tempMods,
            HKTransliteration_tempMods,
            HKRestart_tempMods,
            HKToggleLangPanel_tempMods,
            HKShowSelectionTranslate_tempMods,
            HKToggleMahou_tempMods,
            HKToUpper_tempMods,
            HKToLower_tempMods;

        /// <summary> Temporary key of hotkeys. </summary>
        private int Mainhk_tempKey,
            ExitHk_tempKey,
            HKCLast_tempKey,
            HKCSelection_tempKey,
            HKCLine_tempKey,
            HKSymIgn_tempKey,
            HKConMorWor_tempKey,
            HKTitleCase_tempKey,
            HKRandomCase_tempKey,
            HKSwapCase_tempKey,
            HKTransliteration_tempKey,
            HKRestart_tempKey,
            HKToggleLangPanel_tempKey,
            HKShowSelectionTranslate_tempKey,
            HKToggleMahou_tempKey,
            HKToUpper_tempKey,
            HKToLower_tempKey;

        /// <summary> Temporary Enabled of hotkeys. </summary>
        private bool Mainhk_tempEnabled,
            ExitHk_tempEnabled,
            HKCLast_tempEnabled,
            HKCSelection_tempEnabled,
            HKCLine_tempEnabled,
            HKSymIgn_tempEnabled,
            HKConMorWor_tempEnabled,
            HKTitleCase_tempEnabled,
            HKRandomCase_tempEnabled,
            HKSwapCase_tempEnabled,
            HKTransliteration_tempEnabled,
            HKRestart_tempEnabled,
            HKToggleLangPanel_tempEnabled,
            HKShowSelectionTranslate_tempEnabled,
            HKToggleMahou_tempEnabled,
            HKToUpper_tempEnabled,
            HKToLower_tempEnabled;

        /// <summary> Temporary Double of hotkeys. </summary>
        private bool Mainhk_tempDouble,
            ExitHk_tempDouble,
            HKCLast_tempDouble,
            HKCSelection_tempDouble,
            HKCLine_tempDouble,
            HKSymIgn_tempDouble,
            HKConMorWor_tempDouble,
            HKTitleCase_tempDouble,
            HKRandomCase_tempDouble,
            HKSwapCase_tempDouble,
            HKTransliteration_tempDouble,
            HKToggleLangPanel_tempDouble,
            HKShowSelectionTranslate_tempDouble,
            HKToggleMahou_tempDouble,
            HKToUpper_tempDouble,
            HKToLower_tempDouble;

        /// <summary> Temporary colors of LangDisplays appearece. </summary>
        public static Color LDMouseFore_temp,
            LDCaretFore_temp,
            LDMouseBack_temp,
            LDCaretBack_temp,
            Layout1Fore_temp,
            Layout2Fore_temp,
            Layout1Back_temp,
            Layout2Back_temp;

        /// <summary> Temporary fonts of LangDisplays appearece. </summary>
        public static Font LDMouseFont_temp, LDCaretFont_temp, Layout1Font_temp, Layout2Font_temp;

        /// <summary> Temporary use flags of LangDisplays appearece. </summary>
        public static bool LDMouseUseFlags_temp, LDCaretUseFlags_temp;

        /// <summary> Temporary transparent backgrounds of LangDisplays appearece. </summary>
        public static bool LDMouseTransparentBack_temp,
            LDCaretTransparentBack_temp,
            Layout1TransparentBack_temp,
            Layout2TransparentBack_temp;

        /// <summary> Temporary positions of LangDisplays appearece. </summary>
        public static int LDMouseY_Pos_temp,
            LDCaretY_Pos_temp,
            LDMouseX_Pos_temp,
            LDCaretX_Pos_temp,
            Layout1Y_Pos_temp,
            Layout2Y_Pos_temp,
            Layout1X_Pos_temp,
            Layout2X_Pos_temp,
            MCDS_Xpos_temp,
            MCDS_Ypos_temp,
            MCDS_TopIndent_temp,
            MCDS_BottomIndent_temp;

        /// <summary> Temporary sizes of LangDisplays appearece. </summary>
        public static int LDMouseHeight_temp,
            LDCaretHeight_temp,
            LDMouseWidth_temp,
            LDCaretWidth_temp,
            Layout1Height_temp,
            Layout2Height_temp,
            Layout1Width_temp,
            Layout2Width_temp;

        /// <summary>
        ///     Temporary list boxes indexes before and after settings loaded.
        /// </summary>
        public int tmpHotkeysIndex, tmpLangTTAppearenceIndex;

        /// <summary> Temporary hotkey key of hotkey in txt_Hotkey. </summary>
        private int txt_Hotkey_tempKey;

        /// <summary> Temporary hotkey modifiers of hotkey in txt_Hotkey. </summary>
        private string txt_Hotkey_tempModifiers;

        /// <summary> Temporary persistent layout's processes. </summary>
        public string PersistentLayout1Processes, PersistentLayout2Processes;

        /// <summary> Temporary layouts, etc.. </summary>
        public static string Layout1,
            Layout2,
            Layout3,
            Layout4,
            MainLayout1,
            MainLayout2,
            EmulateLSType,
            ExcludedPrograms,
            Layout1TText,
            Layout2TText;

        /// <summary> Temporary specific keys. </summary>
        public int Key1, Key2, Key3, Key4;

        /// <summary> LangPanel temporary bool variables. </summary>
        public static bool LangPanelDisplay, LangPanelBorderAero;

        /// <summary> LangPanel temporary int variables. </summary>
        public int LangPanelRefreshRate, LangPanelTransparency;

        /// <summary> LangPanel temporary color variables. </summary>
        public Color LangPanelForeColor, LangPanelBackColor, LangPanelBorderColor;

        /// <summary> LangPanel temporary position variable. </summary>
        public Point LangPanelPosition;

        /// <summary> LangPanel temporary font variable. </summary>
        public Font LangPanelFont;

        /// <summary> Static last layout for LangPanel. </summary>
        public static uint lastLayoutLangPanel;

        #endregion

        public TrayIcon icon;
        public LangDisplay mouseLangDisplay = new LangDisplay();
        public LangDisplay caretLangDisplay = new LangDisplay();
        public LangPanel _langPanel;
        public TranslatePanel _TranslatePanel;
        private uint latestL, latestCL;
        private static string decim = ",";
        public static uint currentLayout, GlobalLayout;
        public static uint MAIN_LAYOUT1, MAIN_LAYOUT2;
        private bool onepass = true, onepassC = true;

        /// <summary>
        ///     Has a lot of values/keys taken from dynamic controls:<br />
        ///     txt_keyN - HotkeyBox,<br />
        ///     ^---> ADDONS: _mods - to get modifiers, _key - to get keyCode.<br />
        ///     chk_winN - Use Win modifier in hotkey, <br />
        ///     lbl_arrN - Arrow label, [has no values]<br />
        ///     cbb_typN - Switch type(To specific layout or switch between).
        /// </summary>
        public Dictionary<string, string> SpecKeySetsValues = new Dictionary<string, string>();

        public static Dictionary<string, string> TrSetsValues = new Dictionary<string, string>();
        private static string latestSwitch = "null";
        private const string SYNC_HOST = "https://hastebin.com";
        private const string SYNC_SEP = "#------>";
        private readonly string[] SYNC_NAMES = {"Mahou.ini", "snippets.txt", "history.txt", "TSDict.txt"};

        private readonly string[] SYNC_TYPES = {"ini", "sni", "his", "tdi"};

        // From more configs
        private readonly ColorDialog clrd = new ColorDialog();
        private readonly FontDialog fntd = new FontDialog();
        public static FontConverter fcv = new FontConverter();
        public static string snipfile = Path.Combine(nPath, "snippets.txt");
        public static string AS_dictfile = Path.Combine(nPath, "AS_dict.txt");
        public static string mahou_folder_appd = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mahou");
        public static string latest_save_dir = "";
        public static string AutoSwitchDictionaryRaw = "";
        public static Point LDC_lp = new Point(0, 0);
        public static int LD_MouseSkipMessagesCount;
        public static List<IntPtr> PERSISTENT_LAYOUT1_HWNDs = new List<IntPtr>();
        public static List<IntPtr> NOT_PERSISTENT_LAYOUT1_HWNDs = new List<IntPtr>();
        public static List<IntPtr> PERSISTENT_LAYOUT2_HWNDs = new List<IntPtr>();
        public static List<IntPtr> NOT_PERSISTENT_LAYOUT2_HWNDs = new List<IntPtr>();

        #endregion

        #region WndProc(Hotkeys) & Functions

        protected override void WndProc(ref Message m)
        {
            var message = m.Msg;
            if (message == MMain.MH_ALREADY_OPEN)
            {
                // ao = Already Opened
                ToggleVisibility();
                Logging.Log("Another instance detected, closing it.");
            }

            if (message == MMain.MH_RESTART)
            {
                // Restart Mahou
                Logging.Log("Restarting Mahou from command line...");
                Restart();
            }

            if (message == WinAPI.WM_MOUSEWHEEL)
                if (WinAPI.WindowFromPoint(Cursor.Position) == tabs.Handle)
                {
                    try
                    {
                        if ((uint) m.WParam >> 16 == 120)
                        {
                            if (tabs.SelectedIndex + 1 > tabs.TabPages.Count - 1)
                                tabs.SelectedIndex = 0;
                            else
                                tabs.SelectedIndex += 1;
                        }
                        else
                        {
                            if (tabs.SelectedIndex - 1 < 0)
                                tabs.SelectedIndex = tabs.TabPages.Count - 1;
                            else
                                tabs.SelectedIndex -= 1;
                        }
                    }
                    catch (Exception e)
                    {
                        Logging.Log("Error in tabs wheel scroll, details: " + e.Message + "\r\n" + e.StackTrace + "\r\n");
                    }

                    tabs.Focus();
                }

//			Logging.Log("MSG: "+m.Msg+", LP: "+m.LParam+", WP: "+m.WParam+", KMS: "+KMHook.self+" 0x312");
            if (message == WinAPI.WM_HOTKEY)
            {
                var id = (Hotkey.HKID) m.WParam.ToInt32();
                var mods = (int) m.LParam & 0xFFFF;
                if (mods == WinAPI.MOD_ALT)
                    KInputs.MakeInput(new[]
                    {
                        KInputs.AddKey(Keys.LMenu, false),
                        KInputs.AddKey(Keys.RMenu, false),
                        KInputs.AddKey(Keys.LMenu, true)
                    });

                #region Convert multiple words 

                if (m.WParam.ToInt32() >= 100 && m.WParam.ToInt32() <= 109 && KMHook.waitfornum)
                {
                    var wordnum = m.WParam.ToInt32() - 100;
                    if (wordnum == 0) wordnum = 10;
                    Logging.Log("Attempt to convert " + wordnum + " word(s).");
                    var words = new List<KMHook.YuKey>();
                    try
                    {
                        foreach (var word in MMain.CWords.GetRange(MMain.CWords.Count - wordnum, wordnum))
                            words.AddRange(word);
                        Logging.Log("Full character count in all " + wordnum + " last word(s) is " + words.Count + ".");
                    }
                    catch
                    {
                        Logging.Log("Converting " + wordnum + " word(s) impossible it is bigger that entered words.");
                    }

                    FlushConvertMoreWords();
                    KMHook.ConvertLast(words);
                }
                else if (KMHook.waitfornum)
                {
                    FlushConvertMoreWords();
                }

                #endregion

                #region SpecificKeys

                var specific = false;
                if (m.WParam.ToInt32() >= 201 && m.WParam.ToInt32() <= 299 &&
                    (ChangeLayoutInExcluded || !KMHook.ExcludedProgram()))
                {
                    specific = true;
                    if (!OnceSpecific)
                    {
                        OnceSpecific = true;
                        var si = m.WParam.ToInt32() - 200;
                        var key = (Keys) (((int) m.LParam >> 16) & 0xFFFF);
                        var type = SpecKeySetsValues["cbb_typ" + si];
                        try
                        {
                            if (type == MMain.Lang[Languages.Element.SwitchBetween])
                                KMHook.ChangeLayout();
                            else KMHook.ChangeToLayout(Locales.ActiveWindow(), Locales.GetLocaleFromString(type).uId);
                            if (key == Keys.CapsLock)
                                KMHook.DoSelf(() =>
                                {
                                    if (IsKeyLocked(Keys.CapsLock))
                                    {
                                        KMHook.KeybdEvent(Keys.CapsLock, 0);
                                        KMHook.KeybdEvent(Keys.CapsLock, 2);
                                    }
                                });
                        }
                        catch (Exception e)
                        {
                            Logging.Log("Possibly layout switch type was not selected for " + OemReadable((SpecKeySetsValues["txt_key" + si + "_mods"].Replace(",", " +") + " + " +
                                                                                                           Remake(key)).Replace("None + ", "")) + ". Layout string: [" + type + "]. Exception: " + e.Message + "\r\n" +
                                        e.StackTrace, 2);
                        }
                    }
                }

                #endregion

                if (!KMHook.ExcludedProgram() && !specific)
                {
                    if (Hotkey.GetMods(HKCSelection_tempMods) == Hotkey.GetMods(HKCLast_tempMods) && HKCSelection_tempKey == HKCLast_tempKey)
                        Hotkey.CallHotkey(HKCLast, id, ref hksOK, KMHook.ConvertSelection); // Use HKCLast id for cs if hotkeys are the same
                    else
                        Hotkey.CallHotkey(HKCSelection, id, ref hksOK, KMHook.ConvertSelection);
                    Hotkey.CallHotkey(HKTitleCase, id, ref hksTTCOK, () => KMHook.SelectionConversion(KMHook.ConvT.Title));
                    Hotkey.CallHotkey(HKSwapCase, id, ref hksTSCOK, () => KMHook.SelectionConversion(KMHook.ConvT.Swap));
                    Hotkey.CallHotkey(HKUpperCase, id, ref hkUcOK, () => KMHook.SelectionConversion(KMHook.ConvT.Upper));
                    Hotkey.CallHotkey(HKLowerCase, id, ref hklcOK, () => KMHook.SelectionConversion(KMHook.ConvT.Lower));
                    Hotkey.CallHotkey(HKRandomCase, id, ref hksTRCOK, () => KMHook.SelectionConversion(KMHook.ConvT.Random));
                    Hotkey.CallHotkey(HKConMorWor, id, ref hkcwdsOK, PrepareConvertMoreWords);
                    Hotkey.CallHotkey(HKTransliteration, id, ref hksTrslOK, () => KMHook.SelectionConversion(KMHook.ConvT.Transliteration));
                    var clcl = false; // Convert Line + Convert Last
                    var conv = false;
                    if (Hotkey.GetMods(HKCLine_tempMods) == Hotkey.GetMods(HKCLast_tempMods) &&
                        HKCLine_tempKey == HKCLast_tempKey && HKCLine_tempDouble != HKCLast_tempDouble)
                    {
                        clcl = true;
                        var lastcl = hklineOK;
                        Hotkey.CallHotkey(HKCLine, Hotkey.HKID.ConvertLastLine, ref hklineOK, () =>
                        {
                            var line = new List<KMHook.YuKey>();
                            foreach (var word in MMain.CWords) line.AddRange(word);
                            KMHook.ConvertLast(line);
                            Debug.WriteLine("DISPOSING STIMER");
                            stimer.Dispose();
                            conv = true;
                        });
                        if (!lastcl && !conv)
                        {
                            stimer = new Timer();
                            stimer.Interval = DoubleHKInterval + 50;
                            stimer.Tick += (_, __) =>
                            {
                                if (!hklineOK && !conv) // Even here !conv because of time delay!
                                    Hotkey.CallHotkey(HKCLast, id, ref hklOK, () => KMHook.ConvertLast(MMain.CWord));
                                Debug.WriteLine("STOPING STIMER");
                                stimer.Stop();
                                Debug.WriteLine("DISPOSING STIMER");
                                stimer.Dispose();
                            };
                            Debug.WriteLine("STARTIN STIMER");
                            stimer.Start();
                        }
                    }

                    if (!clcl)
                        Hotkey.CallHotkey(HKCLast, id, ref hklOK, () => KMHook.ConvertLast(MMain.CWord));
                    Hotkey.CallHotkey(HKCLine, id, ref hklineOK, () =>
                    {
                        var line = new List<KMHook.YuKey>();
                        foreach (var word in MMain.CWords)
                        {
                            line.AddRange(word);
                            foreach (var x in word) Debug.WriteLine("KK: " + x.Key);
                        }

                        KMHook.ConvertLast(line);
                    });
                    ShiftInHotkey = Hotkey.ContainsModifier((int) m.LParam & 0xFFFF, (int) WinAPI.MOD_SHIFT);
                    AltInHotkey = Hotkey.ContainsModifier((int) m.LParam & 0xFFFF, (int) WinAPI.MOD_ALT);
                    CtrlInHotkey = Hotkey.ContainsModifier((int) m.LParam & 0xFFFF, (int) WinAPI.MOD_CONTROL);
                    WinInHotkey = Hotkey.ContainsModifier((int) m.LParam & 0xFFFF, (int) WinAPI.MOD_WIN);
                    KMHook.csdoing = false;
                }

                if (HKSymIgn.Enabled)
                    Hotkey.CallHotkey(HKSymIgn, id, ref hkSIOK, () =>
                    {
                        if (SymIgnEnabled)
                        {
                            SymIgnEnabled = false;
                            MMain.MyConfs.WriteSave("Functions", "SymbolIgnoreModeEnabled", "false");
                            Icon = icon.trIcon.Icon = Resources.MahouTrayHD;
                        }
                        else
                        {
                            MMain.MyConfs.WriteSave("Functions", "SymbolIgnoreModeEnabled", "true");
                            SymIgnEnabled = true;
                            Icon = icon.trIcon.Icon = Resources.MahouSymbolIgnoreMode;
                        }
                    });
                Hotkey.CallHotkey(HKRestart, id, ref dummy, Restart);
                Hotkey.CallHotkey(Mainhk, id, ref hkShWndOK, ToggleVisibility);
                Hotkey.CallHotkey(HKToggleLP, id, ref hkToglLPOK, ToggleLangPanel);
                Hotkey.CallHotkey(ExitHk, id, ref hkExitOK, ExitProgram);
                Hotkey.CallHotkey(HKShowST, id, ref hkShowTSOK, () => ShowSelectionTranslation());
                Hotkey.CallHotkey(HKToggleMahou, id, ref hkToggleMahouOK, ToggleMahou);
//				if (m.WParam.ToInt32() <= (int)Hotkey.HKID.TransliterateSelection)
//					KMHook.ClearModifiers();
                UpdateLDs();
                // Fix for experimental alt-only + something;
                KInputs.MakeInput(new[] {KInputs.AddKey(Keys.LMenu, false)});
            }

            base.WndProc(ref m);
        }

        public static Point last_CR = new Point(0, 0);

        public void ToggleMahou()
        {
            if (ENABLED)
            {
                PreExit(false, 2);
                MMain.CWord.Clear();
                MMain.CWords.Clear();
                KMHook.c_snip.Clear();
                InitLangDisplays(true);
                Text += " [" + MMain.Lang[Languages.Element.Disabled] + "]";
                icon.trIcon.Text += " [" + MMain.Lang[Languages.Element.Disabled] + "]";
                icon.trIcon.Icon = Resources.MahouTrayHD;
                ENABLED = false;
            }
            else
            {
                ENABLED = true;
                RegisterHotkeys();
                MMain.Rif.RegisterRawInputDevices(MMain.Rif.Handle);
                if (RemapCapslockAsF18 || SnippetsExpandType == "Tab")
                    LLHook.Set();
                InitLangDisplays();
                ToggleTimers();
                if (UseJKL)
                    jklXHidServ.Init();
                Text = Text.Replace(" [" + MMain.Lang[Languages.Element.Disabled] + "]", "");
                icon.trIcon.Text = icon.trIcon.Text.Replace(" [" + MMain.Lang[Languages.Element.Disabled] + "]", "");
                ChangeTrayIconToFlag(true);
            }

            icon.CheckEnDis(ENABLED);
            Logging.Log("Switched Mahou enabled state to: [" + ENABLED + "].");
        }

        public static void ShowSelectionTranslation(bool mouse = false)
        {
            if (!TrEnabled) return;
//			var dum = new Point(0,0);
//			var pos = CaretPos.GetCaretPointToScreen(out dum);
//			Debug.WriteLine(pos.X);
//			if (mouse || pos.Equals(new Point(77777,77777)) || pos == last_CR)
//				pos = Cursor.Position;
            var pos = Cursor.Position;
            pos.Y += 10;
            var str = KMHook.GetClipStr().Replace('\n', ' ');
            Debug.WriteLine(str);
            if (!string.IsNullOrEmpty(str))
                if (!TranslatePanel.running)
                    MMain.Mahou._TranslatePanel.ShowTranslation(str, pos);
            KMHook.RestoreClipBoard();
            if (!mouse) last_CR = pos;
        }

        /// <summary>
        ///     Restores temporary variables from settings.
        /// </summary>
        private void LoadTemps()
        {
            //This creates(silently) new config file if existed one disappeared o_O
            // Restores temps

            #region Hotkey enableds

            Mainhk_tempEnabled = MMain.MyConfs.ReadBool("Hotkeys", "ToggleMainWindow_Enabled");
            HKCLast_tempEnabled = MMain.MyConfs.ReadBool("Hotkeys", "ConvertLastWord_Enabled");
            HKCSelection_tempEnabled = MMain.MyConfs.ReadBool("Hotkeys", "ConvertSelectedText_Enabled");
            HKCLine_tempEnabled = MMain.MyConfs.ReadBool("Hotkeys", "ConvertLastLine_Enabled");
            HKConMorWor_tempEnabled = MMain.MyConfs.ReadBool("Hotkeys", "ConvertLastWords_Enabled");
            HKSymIgn_tempEnabled = MMain.MyConfs.ReadBool("Hotkeys", "ToggleSymbolIgnoreMode_Enabled");
            HKTitleCase_tempEnabled = MMain.MyConfs.ReadBool("Hotkeys", "SelectedTextToTitleCase_Enabled");
            HKRandomCase_tempEnabled = MMain.MyConfs.ReadBool("Hotkeys", "SelectedTextToRandomCase_Enabled");
            HKSwapCase_tempEnabled = MMain.MyConfs.ReadBool("Hotkeys", "SelectedTextToSwapCase_Enabled");
            HKToUpper_tempEnabled = MMain.MyConfs.ReadBool("Hotkeys", "SelectedToUpper_Enabled");
            HKToLower_tempEnabled = MMain.MyConfs.ReadBool("Hotkeys", "SelectedToLower_Enabled");
            HKTransliteration_tempEnabled = MMain.MyConfs.ReadBool("Hotkeys", "SelectedTextTransliteration_Enabled");
            ExitHk_tempEnabled = MMain.MyConfs.ReadBool("Hotkeys", "ExitMahou_Enabled");
            HKRestart_tempEnabled = MMain.MyConfs.ReadBool("Hotkeys", "RestartMahou_Enabled");
            HKToggleLangPanel_tempEnabled = MMain.MyConfs.ReadBool("Hotkeys", "ToggleLangPanel_Enabled");
            HKShowSelectionTranslate_tempEnabled = MMain.MyConfs.ReadBool("Hotkeys", "ShowSelectionTranslate_Enabled");
            HKToggleMahou_tempEnabled = MMain.MyConfs.ReadBool("Hotkeys", "ToggleMahou_Enabled");

            #endregion

            #region Hotkey doubles

            Mainhk_tempDouble = MMain.MyConfs.ReadBool("Hotkeys", "ToggleMainWindow_Double");
            HKCLast_tempDouble = MMain.MyConfs.ReadBool("Hotkeys", "ConvertLastWord_Double");
            HKCSelection_tempDouble = MMain.MyConfs.ReadBool("Hotkeys", "ConvertSelectedText_Double");
            HKCLine_tempDouble = MMain.MyConfs.ReadBool("Hotkeys", "ConvertLastLine_Double");
            HKConMorWor_tempDouble = MMain.MyConfs.ReadBool("Hotkeys", "ConvertLastWords_Double");
            HKSymIgn_tempDouble = MMain.MyConfs.ReadBool("Hotkeys", "ToggleSymbolIgnoreMode_Double");
            HKTitleCase_tempDouble = MMain.MyConfs.ReadBool("Hotkeys", "SelectedTextToTitleCase_Double");
            HKRandomCase_tempDouble = MMain.MyConfs.ReadBool("Hotkeys", "SelectedTextToRandomCase_Double");
            HKSwapCase_tempDouble = MMain.MyConfs.ReadBool("Hotkeys", "SelectedTextToSwapCase_Double");
            HKToUpper_tempDouble = MMain.MyConfs.ReadBool("Hotkeys", "SelectedToUpper_Double");
            HKToLower_tempDouble = MMain.MyConfs.ReadBool("Hotkeys", "SelectedToLower_Double");
            HKTransliteration_tempDouble = MMain.MyConfs.ReadBool("Hotkeys", "SelectedTextTransliteration_Double");
            ExitHk_tempDouble = MMain.MyConfs.ReadBool("Hotkeys", "ExitMahou_Double");
            HKToggleLangPanel_tempDouble = MMain.MyConfs.ReadBool("Hotkeys", "ToggleLangPanel_Double");
            HKShowSelectionTranslate_tempDouble = MMain.MyConfs.ReadBool("Hotkeys", "ShowSelectionTranslate_Double");
            HKToggleMahou_tempDouble = MMain.MyConfs.ReadBool("Hotkeys", "ToggleMahou_Double");

            #endregion

            #region Hotkey modifiers

            Mainhk_tempMods = MMain.MyConfs.Read("Hotkeys", "ToggleMainWindow_Modifiers");
            HKCLast_tempMods = MMain.MyConfs.Read("Hotkeys", "ConvertLastWord_Modifiers");
            HKCSelection_tempMods = MMain.MyConfs.Read("Hotkeys", "ConvertSelectedText_Modifiers");
            HKCLine_tempMods = MMain.MyConfs.Read("Hotkeys", "ConvertLastLine_Modifiers");
            HKConMorWor_tempMods = MMain.MyConfs.Read("Hotkeys", "ConvertLastWords_Modifiers");
            HKSymIgn_tempMods = MMain.MyConfs.Read("Hotkeys", "ToggleSymbolIgnoreMode_Modifiers");
            HKTitleCase_tempMods = MMain.MyConfs.Read("Hotkeys", "SelectedTextToTitleCase_Modifiers");
            HKRandomCase_tempMods = MMain.MyConfs.Read("Hotkeys", "SelectedTextToRandomCase_Modifiers");
            HKSwapCase_tempMods = MMain.MyConfs.Read("Hotkeys", "SelectedTextToSwapCase_Modifiers");
            HKToUpper_tempMods = MMain.MyConfs.Read("Hotkeys", "SelectedToUpper_Modifiers");
            HKToLower_tempMods = MMain.MyConfs.Read("Hotkeys", "SelectedToLower_Modifiers");
            HKTransliteration_tempMods = MMain.MyConfs.Read("Hotkeys", "SelectedTextTransliteration_Modifiers");
            ExitHk_tempMods = MMain.MyConfs.Read("Hotkeys", "ExitMahou_Modifiers");
            HKRestart_tempMods = MMain.MyConfs.Read("Hotkeys", "RestartMahou_Modifiers");
            HKToggleLangPanel_tempMods = MMain.MyConfs.Read("Hotkeys", "ToggleLangPanel_Modifiers");
            HKShowSelectionTranslate_tempMods = MMain.MyConfs.Read("Hotkeys", "ShowSelectionTranslate_Modifiers");
            HKToggleMahou_tempMods = MMain.MyConfs.Read("Hotkeys", "ToggleMahou_Modifiers");

            #endregion

            #region Hotkey keys

            Mainhk_tempKey = MMain.MyConfs.ReadInt("Hotkeys", "ToggleMainWindow_Key");
            HKCLast_tempKey = MMain.MyConfs.ReadInt("Hotkeys", "ConvertLastWord_Key");
            HKCSelection_tempKey = MMain.MyConfs.ReadInt("Hotkeys", "ConvertSelectedText_Key");
            HKCLine_tempKey = MMain.MyConfs.ReadInt("Hotkeys", "ConvertLastLine_Key");
            HKConMorWor_tempKey = MMain.MyConfs.ReadInt("Hotkeys", "ConvertLastWords_Key");
            HKSymIgn_tempKey = MMain.MyConfs.ReadInt("Hotkeys", "ToggleSymbolIgnoreMode_Key");
            HKTitleCase_tempKey = MMain.MyConfs.ReadInt("Hotkeys", "SelectedTextToTitleCase_Key");
            HKRandomCase_tempKey = MMain.MyConfs.ReadInt("Hotkeys", "SelectedTextToRandomCase_Key");
            HKSwapCase_tempKey = MMain.MyConfs.ReadInt("Hotkeys", "SelectedTextToSwapCase_Key");
            HKToUpper_tempKey = MMain.MyConfs.ReadInt("Hotkeys", "SelectedToUpper_Key");
            HKToLower_tempKey = MMain.MyConfs.ReadInt("Hotkeys", "SelectedToLower_Key");
            HKTransliteration_tempKey = MMain.MyConfs.ReadInt("Hotkeys", "SelectedTextTransliteration_Key");
            ExitHk_tempKey = MMain.MyConfs.ReadInt("Hotkeys", "ExitMahou_Key");
            HKRestart_tempKey = MMain.MyConfs.ReadInt("Hotkeys", "RestartMahou_Key");
            HKToggleLangPanel_tempKey = MMain.MyConfs.ReadInt("Hotkeys", "ToggleLangPanel_Key");
            HKShowSelectionTranslate_tempKey = MMain.MyConfs.ReadInt("Hotkeys", "ShowSelectionTranslate_Key");
            HKToggleMahou_tempKey = MMain.MyConfs.ReadInt("Hotkeys", "ToggleMahou_Key");

            #endregion

            #region Lang Display colors

            LDMouseFore_temp = GetColor(MMain.MyConfs.Read("Appearence", "MouseLTForeColor"));
            LDCaretFore_temp = GetColor(MMain.MyConfs.Read("Appearence", "CaretLTForeColor"));
            LDMouseBack_temp = GetColor(MMain.MyConfs.Read("Appearence", "MouseLTBackColor"));
            LDCaretBack_temp = GetColor(MMain.MyConfs.Read("Appearence", "CaretLTBackColor"));
            Layout1Fore_temp = GetColor(MMain.MyConfs.Read("Appearence", "Layout1ForeColor"));
            Layout2Fore_temp = GetColor(MMain.MyConfs.Read("Appearence", "Layout2ForeColor"));
            Layout1Back_temp = GetColor(MMain.MyConfs.Read("Appearence", "Layout1BackColor"));
            Layout2Back_temp = GetColor(MMain.MyConfs.Read("Appearence", "Layout2BackColor"));
            LDMouseFont_temp = GetFont(MMain.MyConfs.Read("Appearence", "MouseLTFont"));
            LDCaretFont_temp = GetFont(MMain.MyConfs.Read("Appearence", "CaretLTFont"));
            Layout1Font_temp = GetFont(MMain.MyConfs.Read("Appearence", "Layout1Font"));
            Layout2Font_temp = GetFont(MMain.MyConfs.Read("Appearence", "Layout2Font"));
            // Transparent background colors
            LDMouseTransparentBack_temp = MMain.MyConfs.ReadBool("Appearence", "MouseLTTransparentBackColor");
            LDCaretTransparentBack_temp = MMain.MyConfs.ReadBool("Appearence", "CaretLTTransparentBackColor");
            Layout1TransparentBack_temp = MMain.MyConfs.ReadBool("Appearence", "Layout1TransparentBackColor");
            Layout2TransparentBack_temp = MMain.MyConfs.ReadBool("Appearence", "Layout2TransparentBackColor");

            #endregion

            #region Lang Display poisitions & sizes

            LDMouseY_Pos_temp = MMain.MyConfs.ReadInt("Appearence", "MouseLTPositionY");
            LDCaretY_Pos_temp = MMain.MyConfs.ReadInt("Appearence", "CaretLTPositionY");
            LDMouseX_Pos_temp = MMain.MyConfs.ReadInt("Appearence", "MouseLTPositionX");
            LDCaretX_Pos_temp = MMain.MyConfs.ReadInt("Appearence", "CaretLTPositionX");
            Layout1Y_Pos_temp = MMain.MyConfs.ReadInt("Appearence", "Layout1PositionY");
            Layout2Y_Pos_temp = MMain.MyConfs.ReadInt("Appearence", "Layout2PositionY");
            Layout1X_Pos_temp = MMain.MyConfs.ReadInt("Appearence", "Layout1PositionX");
            Layout2X_Pos_temp = MMain.MyConfs.ReadInt("Appearence", "Layout2PositionX");

            LDMouseHeight_temp = MMain.MyConfs.ReadInt("Appearence", "MouseLTHeight");
            LDCaretHeight_temp = MMain.MyConfs.ReadInt("Appearence", "CaretLTHeight");
            LDMouseWidth_temp = MMain.MyConfs.ReadInt("Appearence", "MouseLTWidth");
            LDCaretWidth_temp = MMain.MyConfs.ReadInt("Appearence", "CaretLTWidth");
            Layout1Height_temp = MMain.MyConfs.ReadInt("Appearence", "Layout1Height");
            Layout2Height_temp = MMain.MyConfs.ReadInt("Appearence", "Layout2Height");
            Layout1Width_temp = MMain.MyConfs.ReadInt("Appearence", "Layout1Width");
            Layout2Width_temp = MMain.MyConfs.ReadInt("Appearence", "Layout2Width");
            // MCDS
            MCDS_Xpos_temp = MMain.MyConfs.ReadInt("Appearence", "MCDS_Pos_X");
            MCDS_Ypos_temp = MMain.MyConfs.ReadInt("Appearence", "MCDS_Pos_Y");
            MCDS_TopIndent_temp = MMain.MyConfs.ReadInt("Appearence", "MCDS_Top");
            MCDS_BottomIndent_temp = MMain.MyConfs.ReadInt("Appearence", "MCDS_Bottom");
            // Use Flags
            LDMouseUseFlags_temp = MMain.MyConfs.ReadBool("Appearence", "MouseLTUseFlags");
            LDCaretUseFlags_temp = MMain.MyConfs.ReadBool("Appearence", "CaretLTUseFlags");
            // Diff text for layouts
            Layout1TText = MMain.MyConfs.Read("Appearence", "Layout1LTText");
            Layout2TText = MMain.MyConfs.Read("Appearence", "Layout2LTText");

            #endregion
        }

        private void SaveFromTemps()
        {
            UpdateHotkeyTemps();

            #region Hotkey enableds

            MMain.MyConfs.Write("Hotkeys", "ToggleMainWindow_Enabled", Mainhk_tempEnabled.ToString());
            MMain.MyConfs.Write("Hotkeys", "ConvertLastWord_Enabled", HKCLast_tempEnabled.ToString());
            MMain.MyConfs.Write("Hotkeys", "ConvertSelectedText_Enabled", HKCSelection_tempEnabled.ToString());
            MMain.MyConfs.Write("Hotkeys", "ConvertLastLine_Enabled", HKCLine_tempEnabled.ToString());
            MMain.MyConfs.Write("Hotkeys", "ConvertLastWords_Enabled", HKConMorWor_tempEnabled.ToString());
            MMain.MyConfs.Write("Hotkeys", "ToggleSymbolIgnoreMode_Enabled", HKSymIgn_tempEnabled.ToString());
            MMain.MyConfs.Write("Hotkeys", "SelectedTextToTitleCase_Enabled", HKTitleCase_tempEnabled.ToString());
            MMain.MyConfs.Write("Hotkeys", "SelectedTextToRandomCase_Enabled", HKRandomCase_tempEnabled.ToString());
            MMain.MyConfs.Write("Hotkeys", "SelectedTextToSwapCase_Enabled", HKSwapCase_tempEnabled.ToString());
            MMain.MyConfs.Write("Hotkeys", "SelectedToUpper_Enabled", HKToUpper_tempEnabled.ToString());
            MMain.MyConfs.Write("Hotkeys", "SelectedToLower_Enabled", HKToLower_tempEnabled.ToString());
            MMain.MyConfs.Write("Hotkeys", "SelectedTextTransliteration_Enabled", HKTransliteration_tempEnabled.ToString());
            MMain.MyConfs.Write("Hotkeys", "ExitMahou_Enabled", ExitHk_tempEnabled.ToString());
            MMain.MyConfs.Write("Hotkeys", "RestartMahou_Enabled", HKRestart_tempEnabled.ToString());
            MMain.MyConfs.Write("Hotkeys", "ToggleLangPanel_Enabled", HKToggleLangPanel_tempEnabled.ToString());
            MMain.MyConfs.Write("Hotkeys", "ShowSelectionTranslate_Enabled", HKShowSelectionTranslate_tempEnabled.ToString());
            MMain.MyConfs.Write("Hotkeys", "ToggleMahou_Enabled", HKToggleMahou_tempEnabled.ToString());

            #endregion

            #region Hotkey doubles

            MMain.MyConfs.Write("Hotkeys", "ToggleMainWindow_Double", Mainhk_tempDouble.ToString());
            MMain.MyConfs.Write("Hotkeys", "ConvertLastWord_Double", HKCLast_tempDouble.ToString());
            MMain.MyConfs.Write("Hotkeys", "ConvertSelectedText_Double", HKCSelection_tempDouble.ToString());
            MMain.MyConfs.Write("Hotkeys", "ConvertLastLine_Double", HKCLine_tempDouble.ToString());
            MMain.MyConfs.Write("Hotkeys", "ConvertLastWords_Double", HKConMorWor_tempDouble.ToString());
            MMain.MyConfs.Write("Hotkeys", "ToggleSymbolIgnoreMode_Double", HKSymIgn_tempDouble.ToString());
            MMain.MyConfs.Write("Hotkeys", "SelectedTextToTitleCase_Double", HKTitleCase_tempDouble.ToString());
            MMain.MyConfs.Write("Hotkeys", "SelectedTextToRandomCase_Double", HKRandomCase_tempDouble.ToString());
            MMain.MyConfs.Write("Hotkeys", "SelectedTextToSwapCase_Double", HKSwapCase_tempDouble.ToString());
            MMain.MyConfs.Write("Hotkeys", "SelectedToUpper_Double", HKToUpper_tempDouble.ToString());
            MMain.MyConfs.Write("Hotkeys", "SelectedToLower_Double", HKToLower_tempDouble.ToString());
            MMain.MyConfs.Write("Hotkeys", "SelectedTextTransliteration_Double", HKTransliteration_tempDouble.ToString());
            MMain.MyConfs.Write("Hotkeys", "ExitMahou_Double", ExitHk_tempDouble.ToString());
            MMain.MyConfs.Write("Hotkeys", "ToggleLangPanel_Double", HKToggleLangPanel_tempDouble.ToString());
            MMain.MyConfs.Write("Hotkeys", "ShowSelectionTranslate_Double", HKShowSelectionTranslate_tempDouble.ToString());
            MMain.MyConfs.Write("Hotkeys", "ToggleMahou_Double", HKToggleMahou_tempDouble.ToString());

            #endregion

            #region Hotkey modifiers

            MMain.MyConfs.Write("Hotkeys", "ToggleMainWindow_Modifiers", Mainhk_tempMods);
            MMain.MyConfs.Write("Hotkeys", "ConvertLastWord_Modifiers", HKCLast_tempMods);
            MMain.MyConfs.Write("Hotkeys", "ConvertSelectedText_Modifiers", HKCSelection_tempMods);
            MMain.MyConfs.Write("Hotkeys", "ConvertLastLine_Modifiers", HKCLine_tempMods);
            MMain.MyConfs.Write("Hotkeys", "ConvertLastWords_Modifiers", HKConMorWor_tempMods);
            MMain.MyConfs.Write("Hotkeys", "ToggleSymbolIgnoreMode_Modifiers", HKSymIgn_tempMods);
            MMain.MyConfs.Write("Hotkeys", "SelectedTextToTitleCase_Modifiers", HKTitleCase_tempMods);
            MMain.MyConfs.Write("Hotkeys", "SelectedTextToRandomCase_Modifiers", HKRandomCase_tempMods);
            MMain.MyConfs.Write("Hotkeys", "SelectedTextToSwapCase_Modifiers", HKSwapCase_tempMods);
            MMain.MyConfs.Write("Hotkeys", "SelectedToUpper_Modifiers", HKToUpper_tempMods);
            MMain.MyConfs.Write("Hotkeys", "SelectedToLower_Modifiers", HKToLower_tempMods);
            MMain.MyConfs.Write("Hotkeys", "SelectedTextTransliteration_Modifiers", HKTransliteration_tempMods);
            MMain.MyConfs.Write("Hotkeys", "ExitMahou_Modifiers", ExitHk_tempMods);
            MMain.MyConfs.Write("Hotkeys", "RestartMahou_Modifiers", HKRestart_tempMods);
            MMain.MyConfs.Write("Hotkeys", "ToggleLangPanel_Modifiers", HKToggleLangPanel_tempMods);
            MMain.MyConfs.Write("Hotkeys", "ShowSelectionTranslate_Modifiers", HKShowSelectionTranslate_tempMods);
            MMain.MyConfs.Write("Hotkeys", "ToggleMahou_Modifiers", HKToggleMahou_tempMods);

            #endregion

            #region Hotkey keys

            MMain.MyConfs.Write("Hotkeys", "ToggleMainWindow_Key", Mainhk_tempKey.ToString());
            MMain.MyConfs.Write("Hotkeys", "ConvertLastWord_Key", HKCLast_tempKey.ToString());
            MMain.MyConfs.Write("Hotkeys", "ConvertSelectedText_Key", HKCSelection_tempKey.ToString());
            MMain.MyConfs.Write("Hotkeys", "ConvertLastLine_Key", HKCLine_tempKey.ToString());
            MMain.MyConfs.Write("Hotkeys", "ConvertLastWords_Key", HKConMorWor_tempKey.ToString());
            MMain.MyConfs.Write("Hotkeys", "ToggleSymbolIgnoreMode_Key", HKSymIgn_tempKey.ToString());
            MMain.MyConfs.Write("Hotkeys", "SelectedTextToTitleCase_Key", HKTitleCase_tempKey.ToString());
            MMain.MyConfs.Write("Hotkeys", "SelectedTextToRandomCase_Key", HKRandomCase_tempKey.ToString());
            MMain.MyConfs.Write("Hotkeys", "SelectedTextToSwapCase_Key", HKSwapCase_tempKey.ToString());
            MMain.MyConfs.Write("Hotkeys", "SelectedToUpper_Key", HKToUpper_tempKey.ToString());
            MMain.MyConfs.Write("Hotkeys", "SelectedToLower_Key", HKToLower_tempKey.ToString());
            MMain.MyConfs.Write("Hotkeys", "SelectedTextTransliteration_Key", HKTransliteration_tempKey.ToString());
            MMain.MyConfs.Write("Hotkeys", "ExitMahou_Key", ExitHk_tempKey.ToString());
            MMain.MyConfs.Write("Hotkeys", "RestartMahou_Key", HKRestart_tempKey.ToString());
            MMain.MyConfs.Write("Hotkeys", "ToggleLangPanel_Key", HKToggleLangPanel_tempKey.ToString());
            MMain.MyConfs.Write("Hotkeys", "ShowSelectionTranslate_Key", HKShowSelectionTranslate_tempKey.ToString());
            MMain.MyConfs.Write("Hotkeys", "ToggleMahou_Key", HKToggleMahou_tempKey.ToString());

            #endregion

            UpdateLangDisplayTemps();

            #region Lang Display colors

            MMain.MyConfs.Write("Appearence", "MouseLTForeColor", ColorTranslator.ToHtml(LDMouseFore_temp));
            MMain.MyConfs.Write("Appearence", "CaretLTForeColor", ColorTranslator.ToHtml(LDCaretFore_temp));
            MMain.MyConfs.Write("Appearence", "MouseLTBackColor", ColorTranslator.ToHtml(LDMouseBack_temp));
            MMain.MyConfs.Write("Appearence", "CaretLTBackColor", ColorTranslator.ToHtml(LDCaretBack_temp));
            MMain.MyConfs.Write("Appearence", "Layout1ForeColor", ColorTranslator.ToHtml(Layout1Fore_temp));
            MMain.MyConfs.Write("Appearence", "Layout2ForeColor", ColorTranslator.ToHtml(Layout2Fore_temp));
            MMain.MyConfs.Write("Appearence", "Layout1BackColor", ColorTranslator.ToHtml(Layout1Back_temp));
            MMain.MyConfs.Write("Appearence", "Layout2BackColor", ColorTranslator.ToHtml(Layout2Back_temp));
            MMain.MyConfs.Write("Appearence", "MouseLTFont", fcv.ConvertToString(LDMouseFont_temp));
            MMain.MyConfs.Write("Appearence", "CaretLTFont", fcv.ConvertToString(LDCaretFont_temp));
            MMain.MyConfs.Write("Appearence", "Layout1Font", fcv.ConvertToString(Layout1Font_temp));
            MMain.MyConfs.Write("Appearence", "Layout2Font", fcv.ConvertToString(Layout2Font_temp));
            // Transparent background colors
            MMain.MyConfs.Write("Appearence", "MouseLTTransparentBackColor", LDMouseTransparentBack_temp.ToString());
            MMain.MyConfs.Write("Appearence", "CaretLTTransparentBackColor", LDCaretTransparentBack_temp.ToString());
            MMain.MyConfs.Write("Appearence", "Layout1TransparentBackColor", Layout1TransparentBack_temp.ToString());
            MMain.MyConfs.Write("Appearence", "Layout2TransparentBackColor", Layout2TransparentBack_temp.ToString());

            #endregion

            #region Lang Display poisitions & sizes

            MMain.MyConfs.Write("Appearence", "MouseLTPositionY", LDMouseY_Pos_temp.ToString());
            MMain.MyConfs.Write("Appearence", "CaretLTPositionY", LDCaretY_Pos_temp.ToString());
            MMain.MyConfs.Write("Appearence", "MouseLTPositionX", LDMouseX_Pos_temp.ToString());
            MMain.MyConfs.Write("Appearence", "CaretLTPositionX", LDCaretX_Pos_temp.ToString());
            MMain.MyConfs.Write("Appearence", "Layout1PositionY", Layout1Y_Pos_temp.ToString());
            MMain.MyConfs.Write("Appearence", "Layout2PositionY", Layout2Y_Pos_temp.ToString());
            MMain.MyConfs.Write("Appearence", "Layout1PositionX", Layout1X_Pos_temp.ToString());
            MMain.MyConfs.Write("Appearence", "Layout2PositionX", Layout2X_Pos_temp.ToString());

            MMain.MyConfs.Write("Appearence", "MouseLTHeight", LDMouseHeight_temp.ToString());
            MMain.MyConfs.Write("Appearence", "CaretLTHeight", LDCaretHeight_temp.ToString());
            MMain.MyConfs.Write("Appearence", "MouseLTWidth", LDMouseWidth_temp.ToString());
            MMain.MyConfs.Write("Appearence", "CaretLTWidth", LDCaretWidth_temp.ToString());
            MMain.MyConfs.Write("Appearence", "Layout1Height", Layout1Height_temp.ToString());
            MMain.MyConfs.Write("Appearence", "Layout2Height", Layout2Height_temp.ToString());
            MMain.MyConfs.Write("Appearence", "Layout1Width", Layout1Width_temp.ToString());
            MMain.MyConfs.Write("Appearence", "Layout2Width", Layout2Width_temp.ToString());
            // MCDS
            MMain.MyConfs.Write("Appearence", "MCDS_Pos_X", MCDS_Xpos_temp.ToString());
            MMain.MyConfs.Write("Appearence", "MCDS_Pos_Y", MCDS_Ypos_temp.ToString());
            MMain.MyConfs.Write("Appearence", "MCDS_Top", MCDS_TopIndent_temp.ToString());
            MMain.MyConfs.Write("Appearence", "MCDS_Bottom", MCDS_BottomIndent_temp.ToString());
            // Use Flags
            MMain.MyConfs.Write("Appearence", "MouseLTUseFlags", LDMouseUseFlags_temp.ToString());
            MMain.MyConfs.Write("Appearence", "CaretLTUseFlags", LDCaretUseFlags_temp.ToString());
            // Diff text for layouts
            MMain.MyConfs.Write("Appearence", "Layout1LTText", Layout1TText);
            MMain.MyConfs.Write("Appearence", "Layout2LTText", Layout2TText);

            #endregion

            Logging.Log("Saved from temps.");
        }

        /// <summary>
        ///     Update save paths for logs, snippets, autoswitch dictionary, configs.
        /// </summary>
        private void UpdateSaveLoadPaths(bool appdata = false)
        {
            if (Configs.forceAppData || appdata)
                nPath = mahou_folder_appd;
            snipfile = Path.Combine(nPath, "snippets.txt");
            AS_dictfile = Path.Combine(nPath, "AS_dict.txt");
            Logging.logdir = Path.Combine(nPath, "Logs");
            Logging.log = Path.Combine(Logging.logdir, DateTime.Today.ToString("yyyy.MM.dd") + ".txt");
            Configs.filePath = Path.Combine(nPath, "Mahou.ini");
        }

        /// <summary>
        ///     Saves current settings to INI.
        /// </summary>
        private void SaveConfigs()
        {
            if (Configs.forceAppData && !chk_AppDataConfigs.Checked)
                try
                {
                    File.Delete(Path.Combine(mahou_folder_appd, ".force"));
                    Configs.forceAppData = false;
                }
                catch
                {
                    Logging.Log("Force AppData file was missing...", 2);
                }

            var only_load = false;
            if (chk_AppDataConfigs.Checked)
            {
                if (!Directory.Exists(mahou_folder_appd))
                    Directory.CreateDirectory(mahou_folder_appd);
                nPath = mahou_folder_appd;
            }
            else
            {
                nPath = AppDomain.CurrentDomain.BaseDirectory;
            }

            Logging.Log("Base path: " + nPath);
            AutoStartAsAdmin = cbb_AutostartType.SelectedIndex != 0;
            if (chk_AutoStart.Checked)
            {
                if (!AutoStartExist(AutoStartAsAdmin))
                    CreateAutoStart();
            }
            else
            {
                if (AutoStartExist(AutoStartAsAdmin))
                    AutoStartRemove(AutoStartAsAdmin);
            }

            var exist = File.Exists(Path.Combine(nPath, "Mahou.ini"));
            if (latest_save_dir != nPath && exist) only_load = true;
            if (!exist)
            {
                Logging.Log("Creating new configs file [" + Configs.filePath + "].");
                Configs.CreateConfigsFile();
            }

            DoInMainConfigs(() =>
            {
                MMain.MyConfs.WriteSave("Functions", "AppDataConfigs", chk_AppDataConfigs.Checked.ToString());
                return (object) 0;
            });
            if (!only_load)
            {
                tmpLangTTAppearenceIndex = lsb_LangTTAppearenceForList.SelectedIndex;
                tmpHotkeysIndex = lsb_Hotkeys.SelectedIndex;

                #region Functions

                MMain.MyConfs.Write("Functions", "AutoStartAsAdmin", AutoStartAsAdmin.ToString());
                MMain.MyConfs.Write("Functions", "TrayIconVisible", chk_TrayIcon.Checked.ToString());
                MMain.MyConfs.Write("Functions", "ConvertSelectionLayoutSwitching", chk_CSLayoutSwitching.Checked.ToString());
                MMain.MyConfs.Write("Functions", "ReSelect", chk_ReSelect.Checked.ToString());
                MMain.MyConfs.Write("Functions", "RePress", chk_RePress.Checked.ToString());
                MMain.MyConfs.Write("Functions", "AddOneSpaceToLastWord", chk_AddOneSpace.Checked.ToString());
                MMain.MyConfs.Write("Functions", "AddOneEnterToLastWord", chk_Add1NL.Checked.ToString());
                MMain.MyConfs.Write("Functions", "ConvertSelectionLayoutSwitchingPlus", chk_CSLayoutSwitchingPlus.Checked.ToString());
                MMain.MyConfs.Write("Functions", "ScrollTip", chk_HighlightScroll.Checked.ToString());
                MMain.MyConfs.Write("Functions", "StartupUpdatesCheck", chk_StartupUpdatesCheck.Checked.ToString());
                MMain.MyConfs.Write("Functions", "SilentUpdate", chk_SilentUpdate.Checked.ToString());
                MMain.MyConfs.Write("Functions", "Logging", chk_Logging.Checked.ToString());
                MMain.MyConfs.Write("Functions", "TrayFlags", (cbb_TrayDislpayType.SelectedIndex == 1).ToString());
                MMain.MyConfs.Write("Functions", "TrayText", (cbb_TrayDislpayType.SelectedIndex == 2).ToString());
                MMain.MyConfs.Write("Functions", "CapsLockTimer", chk_CapsLockDTimer.Checked.ToString());
                MMain.MyConfs.Write("Functions", "BlockMahouHotkeysWithCtrl", chk_BlockHKWithCtrl.Checked.ToString());
                MMain.MyConfs.Write("Functions", "MCDServerSupport", chk_MCDS_support.Checked.ToString());
                MMain.MyConfs.Write("Functions", "OneLayoutWholeWord", chk_OneLayoutWholeWord.Checked.ToString());
                MMain.MyConfs.Write("Appearence", "MouseLTAlways", chk_MouseTTAlways.Checked.ToString());
                MMain.MyConfs.Write("Functions", "GuessKeyCodeFix", chk_GuessKeyCodeFix.Checked.ToString());
                MMain.MyConfs.Write("Functions", "RemapCapslockAsF18", chk_RemapCapsLockAsF18.Checked.ToString());
                MMain.MyConfs.Write("Functions", "UseJKL", chk_GetLayoutFromJKL.Checked.ToString());
                MMain.MyConfs.Write("Functions", "ReadOnlyNA", chk_ReadOnlyNA.Checked.ToString());
                MMain.MyConfs.Write("Functions", "WriteInputHistory", chk_WriteInputHistory.Checked.ToString());
                try
                {
                    MMain.MyConfs.Write("Functions", "WriteInputHistoryBackSpaceType", cbb_BackSpaceType.SelectedIndex.ToString());
                }
                catch
                {
                }

                #endregion

                #region Layouts

                MMain.MyConfs.Write("Layouts", "SwitchBetweenLayouts", chk_SwitchBetweenLayouts.Checked.ToString());
                MMain.MyConfs.Write("Layouts", "EmulateLayoutSwitch", chk_EmulateLS.Checked.ToString());
                MMain.MyConfs.Write("Layouts", "ChangeToSpecificLayoutByKey", chk_SpecificLS.Checked.ToString());
                // Specific keys sets
                SaveSpecificKeySets();
                // Specific keys type
                MMain.MyConfs.Write("Layouts", "SpecificKeysType", cbb_SpecKeysType.SelectedIndex.ToString());
                // Keys 
                MMain.MyConfs.Write("Layouts", "SpecificKey1", cbb_Key1.SelectedIndex.ToString());
                MMain.MyConfs.Write("Layouts", "SpecificKey2", cbb_Key2.SelectedIndex.ToString());
                MMain.MyConfs.Write("Layouts", "SpecificKey3", cbb_Key3.SelectedIndex.ToString());
                MMain.MyConfs.Write("Layouts", "SpecificKey4", cbb_Key4.SelectedIndex.ToString());
                try
                {
                    try
                    {
                        MMain.MyConfs.Write("Layouts", "EmulateLayoutSwitchType", cbb_EmulateType.SelectedItem.ToString());
                    }
                    catch
                    {
                    }

                    // Main Layouts
                    try
                    {
                        MMain.MyConfs.Write("Layouts", "MainLayout1", cbb_MainLayout1.SelectedItem.ToString());
                    }
                    catch
                    {
                    }

                    try
                    {
                        MMain.MyConfs.Write("Layouts", "MainLayout2", cbb_MainLayout2.SelectedItem.ToString());
                    }
                    catch
                    {
                    }

                    // Layouts
                    try
                    {
                        MMain.MyConfs.Write("Layouts", "SpecificLayout1", cbb_Layout1.SelectedItem.ToString());
                    }
                    catch
                    {
                    }

                    try
                    {
                        MMain.MyConfs.Write("Layouts", "SpecificLayout2", cbb_Layout2.SelectedItem.ToString());
                    }
                    catch
                    {
                    }

                    try
                    {
                        MMain.MyConfs.Write("Layouts", "SpecificLayout3", cbb_Layout3.SelectedItem.ToString());
                    }
                    catch
                    {
                    }

                    try
                    {
                        MMain.MyConfs.Write("Layouts", "SpecificLayout4", cbb_Layout4.SelectedItem.ToString());
                    }
                    catch
                    {
                    }
                }
                catch
                {
                    Logging.Log("Some settings in layouts tab failed to save, they are skipped.");
                }

                MMain.MyConfs.Write("Layouts", "OneLayout", chk_OneLayout.Checked.ToString());
                MMain.MyConfs.Write("Layouts", "QWERTZfix", chk_qwertz.Checked.ToString());

                #endregion

                #region Persistent Layout

                MMain.MyConfs.Write("PersistentLayout", "OnlyOnWindowChange", chk_OnlyOnWindowChange.Checked.ToString());
                MMain.MyConfs.Write("PersistentLayout", "ChangeOnlyOnce", chk_ChangeLayoutOnlyOnce.Checked.ToString());
                MMain.MyConfs.Write("PersistentLayout", "ActivateForLayout1", chk_PersistentLayout1Active.Checked.ToString());
                MMain.MyConfs.Write("PersistentLayout", "ActivateForLayout2", chk_PersistentLayout2Active.Checked.ToString());
                MMain.MyConfs.Write("PersistentLayout", "Layout1CheckInterval", nud_PersistentLayout1Interval.Value.ToString());
                MMain.MyConfs.Write("PersistentLayout", "Layout2CheckInterval", nud_PersistentLayout2Interval.Value.ToString());
                MMain.MyConfs.Write("PersistentLayout", "Layout1Processes", txt_PersistentLayout1Processes.Text.Replace(Environment.NewLine, "^cr^lf"));
                MMain.MyConfs.Write("PersistentLayout", "Layout2Processes", txt_PersistentLayout2Processes.Text.Replace(Environment.NewLine, "^cr^lf"));

                #endregion

                #region Appearence

                MMain.MyConfs.Write("Appearence", "DisplayLangTooltipForMouse", chk_LangTooltipMouse.Checked.ToString());
                MMain.MyConfs.Write("Appearence", "DisplayLangTooltipForCaret", chk_LangTooltipCaret.Checked.ToString());
                MMain.MyConfs.Write("Appearence", "DisplayLangTooltipForMouseOnChange", chk_LangTTMouseOnChange.Checked.ToString());
                MMain.MyConfs.Write("Appearence", "DisplayLangTooltipForCaretOnChange", chk_LangTTCaretOnChange.Checked.ToString());
                MMain.MyConfs.Write("Appearence", "DifferentColorsForLayouts", chk_LangTTDiffLayoutColors.Checked.ToString());
                try
                {
                    MMain.MyConfs.Write("Appearence", "Language", cbb_Language.SelectedItem.ToString());
                }
                catch
                {
                    Logging.Log("Language saving failed, restored to English.");
                    MMain.MyConfs.Write("Appearence", "Language", "English");
                }

                MMain.MyConfs.Write("Appearence", "MouseLTUpperArrow", mouseLTUpperArrow.ToString());
                MMain.MyConfs.Write("Appearence", "CaretLTUpperArrow", caretLTUpperArrow.ToString());
                MMain.MyConfs.Write("Appearence", "WindowsMessages", chk_LDMessages.Checked.ToString());

                #endregion

                #region Timings

                if (LDUseWindowsMessages)
                    MMain.MyConfs.Write("Timings", "LangTooltipForMouseSkipMessages", nud_LangTTMouseRefreshRate.Value.ToString());
                else
                    MMain.MyConfs.Write("Timings", "LangTooltipForMouseRefreshRate", nud_LangTTMouseRefreshRate.Value.ToString());
                MMain.MyConfs.Write("Timings", "LangTooltipForCaretRefreshRate", nud_LangTTCaretRefreshRate.Value.ToString());
                MMain.MyConfs.Write("Timings", "DoubleHotkey2ndPressWait", nud_DoubleHK2ndPressWaitTime.Value.ToString());
                MMain.MyConfs.Write("Timings", "FlagsInTrayRefreshRate", nud_TrayFlagRefreshRate.Value.ToString());
                MMain.MyConfs.Write("Timings", "ScrollLockStateRefreshRate", nud_ScrollLockRefreshRate.Value.ToString());
                MMain.MyConfs.Write("Timings", "CapsLockDisableRefreshRate", nud_CapsLockRefreshRate.Value.ToString());
                MMain.MyConfs.Write("Timings", "ScrollLockStateRefreshRate", nud_ScrollLockRefreshRate.Value.ToString());
                MMain.MyConfs.Write("Timings", "SelectedTextGetMoreTries", chk_SelectedTextGetMoreTries.Checked.ToString());
                MMain.MyConfs.Write("Timings", "SelectedTextGetMoreTriesCount", nud_SelectedTextGetTriesCount.Value.ToString());
                MMain.MyConfs.Write("Timings", "DelayAfterBackspaces", nud_DelayAfterBackspaces.Value.ToString());
                MMain.MyConfs.Write("Timings", "UseDelayAfterBackspaces", chk_UseDelayAfterBackspaces.Checked.ToString());

                #region Excluded

                MMain.MyConfs.Write("Timings", "ExcludedPrograms", txt_ExcludedPrograms.Text.Replace(Environment.NewLine, "^cr^lf"));
                MMain.MyConfs.Write("Timings", "ChangeLayoutInExcluded", chk_Change1KeyL.Checked.ToString());

                #endregion

                #endregion

                #region Snippets

                MMain.MyConfs.Write("Snippets", "SnippetsEnabled", chk_Snippets.Checked.ToString());
                MMain.MyConfs.Write("Snippets", "SpaceAfter", chk_SnippetsSpaceAfter.Checked.ToString());
                MMain.MyConfs.Write("Snippets", "SwitchToGuessLayout", chk_SnippetsSwitchToGuessLayout.Checked.ToString());
                if (SnippetsEnabled)
                    File.WriteAllText(snipfile, txt_Snippets.Text, Encoding.UTF8);
                MMain.MyConfs.Write("Snippets", "SnippetExpandKey", cbb_SnippetExpandKeys.SelectedItem.ToString());

                #endregion

                #region AutoSwitch

                MMain.MyConfs.Write("AutoSwitch", "Enabled", chk_AutoSwitch.Checked.ToString());
                MMain.MyConfs.Write("AutoSwitch", "SpaceAfter", chk_AutoSwitchSpaceAfter.Checked.ToString());
                MMain.MyConfs.Write("AutoSwitch", "SwitchToGuessLayout", chk_AutoSwitchSwitchToGuessLayout.Checked.ToString());
                MMain.MyConfs.Write("AutoSwitch", "DownloadInZip", chk_DownloadASD_InZip.Checked.ToString());
                if (AutoSwitchEnabled)
                    File.WriteAllText(AS_dictfile, AutoSwitchDictionaryRaw, Encoding.UTF8);

                #endregion

                #region Appearence & Hotkeys

                SaveFromTemps();

                #endregion

                #region LangPanel

                MMain.MyConfs.Write("LangPanel", "Display", chk_DisplayLangPanel.Checked.ToString());
                MMain.MyConfs.Write("LangPanel", "RefreshRate", nud_LPRefreshRate.Value.ToString());
                MMain.MyConfs.Write("LangPanel", "Transparency", nud_LPTransparency.Value.ToString());
                MMain.MyConfs.Write("LangPanel", "ForeColor", ColorTranslator.ToHtml(btn_LPFore.BackColor));
                MMain.MyConfs.Write("LangPanel", "BackColor", ColorTranslator.ToHtml(btn_LPBack.BackColor));
                MMain.MyConfs.Write("LangPanel", "BorderColor", ColorTranslator.ToHtml(btn_LPBorderColor.BackColor));
                MMain.MyConfs.Write("LangPanel", "BorderAeroColor", chk_LPAeroColor.Checked.ToString());
                MMain.MyConfs.Write("LangPanel", "Font", fcv.ConvertToString(btn_LPFont.Font));
                MMain.MyConfs.Write("LangPanel", "UpperArrow", chk_LPUpperArrow.Checked.ToString());

                #endregion

                #region Translate Panel

                MMain.MyConfs.Write("TranslatePanel", "Enabled", chk_TrEnable.Checked.ToString());
                MMain.MyConfs.Write("TranslatePanel", "Transparency", nud_TrTransparency.Value.ToString());
                MMain.MyConfs.Write("TranslatePanel", "OnDoubleClick", chk_TrOnDoubleClick.Checked.ToString());
                MMain.MyConfs.Write("TranslatePanel", "FG", ColorTranslator.ToHtml(btn_TrFG.BackColor));
                MMain.MyConfs.Write("TranslatePanel", "BG", ColorTranslator.ToHtml(btn_TrBG.BackColor));
                MMain.MyConfs.Write("TranslatePanel", "BorderC", ColorTranslator.ToHtml(btn_TrBorderC.BackColor));
                MMain.MyConfs.Write("TranslatePanel", "BorderAero", chk_TrUseAccent.Checked.ToString());
                MMain.MyConfs.Write("TranslatePanel", "TextFont", fcv.ConvertToString(btn_TrTextFont.Font));
                MMain.MyConfs.Write("TranslatePanel", "TitleFont", fcv.ConvertToString(btn_TrTitleFont.Font));
                SaveTrSets();

                #endregion

                #region Sync

                MMain.MyConfs.Write("Sync", "BBools", string.Join("|", bin(chk_Mini.Checked), bin(chk_Stxt.Checked), bin(chk_Htxt.Checked), bin(chk_Ttxt.Checked)));
                MMain.MyConfs.Write("Sync", "RBools", string.Join("|", bin(chk_rMini.Checked), bin(chk_rStxt.Checked), bin(chk_rHtxt.Checked), bin(chk_rTtxt.Checked)));
                MMain.MyConfs.Write("Sync", "BLast", txt_backupId.Text);
                MMain.MyConfs.Write("Sync", "RLast", txt_restoreId.Text);

                #endregion

                #region Sounds

                MMain.MyConfs.Write("Sounds", "Enabled", chk_EnableSnd.Checked.ToString());
                MMain.MyConfs.Write("Sounds", "OnAutoSwitch", chk_SndAutoSwitch.Checked.ToString());
                MMain.MyConfs.Write("Sounds", "OnSnippets", chk_SndSnippets.Checked.ToString());
                MMain.MyConfs.Write("Sounds", "OnConvertLast", chk_SndLast.Checked.ToString());
                MMain.MyConfs.Write("Sounds", "OnLayoutSwitch", chk_SndLayoutSwitch.Checked.ToString());
                MMain.MyConfs.Write("Sounds", "UseCustomSound", chk_UseCustomSnd.Checked.ToString());
                MMain.MyConfs.Write("Sounds", "CustomSound", lbl_CustomSound.Text);
                MMain.MyConfs.Write("Sounds", "OnAutoSwitch2", chk_SndAutoSwitch2.Checked.ToString());
                MMain.MyConfs.Write("Sounds", "OnSnippets2", chk_SndSnippets2.Checked.ToString());
                MMain.MyConfs.Write("Sounds", "OnConvertLast2", chk_SndLast2.Checked.ToString());
                MMain.MyConfs.Write("Sounds", "OnLayoutSwitch2", chk_SndLayoutSwitch2.Checked.ToString());
                MMain.MyConfs.Write("Sounds", "UseCustomSound2", chk_UseCustomSnd2.Checked.ToString());
                MMain.MyConfs.Write("Sounds", "CustomSound2", lbl_CustomSound2.Text);

                #endregion

                MMain.MyConfs.WriteToDisk();
                Logging.Log("All configurations saved.");
            }

            LoadConfigs();
        }

        private void SaveTrSets()
        {
            var sets = "";
            for (var i = 1; i <= TrSetCount; i++)
            {
                sets += "set_" + i + "/";
                sets += TrSetsValues["cbb_fr" + i] + "/";
                sets += TrSetsValues["cbb_to" + i];
                if (i != TrSetCount)
                    sets += "|";
            }

            if (string.IsNullOrEmpty(sets))
                sets = "set_0";
            MMain.MyConfs.Write("TranslatePanel", "LanguageSets", sets);
        }

        private void SaveSpecificKeySets(bool change1set = false, int setId = 0, string typ = "")
        {
            var sets = "";
            for (var i = 1; i <= SpecKeySetCount; i++)
            {
                sets += "set_" + i + "/";
                sets += SpecKeySetsValues["txt_key" + i + "_key"] + "/";
                sets += SpecKeySetsValues["txt_key" + i + "_mods"];
                if ((pan_KeySets.Controls["set_" + i].Controls["chk_win" + i] as CheckBox).Checked &&
                    !SpecKeySetsValues["txt_key" + i + "_mods"].Contains("Win"))
                    sets += " + Win";
                sets += "/";
                if (setId == i && change1set)
                    sets += typ;
                else
                    sets += SpecKeySetsValues["cbb_typ" + i];
                if (i != SpecKeySetCount)
                    sets += "|";
            }

            if (string.IsNullOrEmpty(sets))
                sets = "set_0";
            MMain.MyConfs.Write("Layouts", "SpecificKeySets", sets);
        }

        private object DoInMainConfigs(Func<object> act)
        {
            if (Configs.forceAppData) return true;
            var last = Configs.filePath; // Last configs file
            Configs.filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mahou.ini");
            if (!Configs.Readable())
            {
                Configs.filePath = last;
                return true;
            }

            MMain.MyConfs = new Configs();
            var rsl = act();
            if (chk_AppDataConfigs.Checked)
            {
                ;
                if (!Directory.Exists(mahou_folder_appd))
                    Directory.CreateDirectory(mahou_folder_appd);
                Configs.filePath = Path.Combine(mahou_folder_appd, "Mahou.ini");
                MMain.MyConfs = new Configs();
            }

            return rsl;
        }

        private void ChangeAutoSwitchDictionaryTextBox()
        {
            if (AutoSwitchDictionaryRaw.Length > 710000)
            {
                txt_AutoSwitchDictionary.ReadOnly = true;
                txt_AutoSwitchDictionary.Text = MMain.Lang[Languages.Element.AutoSwitchDictionaryTooBigToDisplay];
            }
            else
            {
                txt_AutoSwitchDictionary.ReadOnly = false;
                txt_AutoSwitchDictionary.Text = AutoSwitchDictionaryRaw;
            }
        }

        private Color GetColor(string color_html)
        {
            var color = SystemColors.WindowText;
            try
            {
                color = ColorTranslator.FromHtml(color_html);
            }
            catch (Exception e)
            {
                WrongColorLog(color_html, e.Message + "\r\n" + e.StackTrace);
            }

            return color;
        }

        private Font GetFont(string font_raw, bool remstyle = false)
        {
            var font = SystemFonts.DefaultFont;
            font_raw = FontDecimReplace(font_raw);
            try
            {
                if (remstyle)
                    if (!font_raw.Contains("style="))
                        font_raw = font_raw.Replace("; style", "");
                font = (Font) fcv.ConvertFromString(font_raw);
            }
            catch (Exception e)
            {
                WrongFontLog(font_raw, e.Message + "\r\n" + e.StackTrace);
            }

            return font;
        }

        private string FontDecimReplace(string font_raw)
        {
            var pattern = "(?:(\\.|\\,))([0-9]+)pt";
            var repl = font_raw;
            var mt = new Regex(pattern).Match(repl);
            if (mt.Groups[1].Value != decim && !string.IsNullOrEmpty(mt.Groups[1].Value))
            {
                repl = Regex.Replace(font_raw, pattern, decim + "$1pt");
                Logging.Log("Replaced decimal in font " + font_raw + ", with: " + decim);
            }

            return repl;
        }

        /// <summary>
        ///     Refresh all controls state from configs.
        /// </summary>
        private void LoadConfigs()
        {
            decim = (string) Registry.GetValue(@"HKEY_CURRENT_USER\Control Panel\International", "sDecimal", null);
            TrSetsValues = new Dictionary<string, string>();
            chk_AppDataConfigs.Checked = (bool) DoInMainConfigs(() => MMain.MyConfs.ReadBool("Functions", "AppDataConfigs"));
            UpdateSaveLoadPaths(chk_AppDataConfigs.Checked);
            InitLanguage();
            RefreshLanguage();

            #region Functions

            MMain.MyConfs = new Configs();
            AutoStartAsAdmin = MMain.MyConfs.ReadBool("Functions", "AutoStartAsAdmin");
            chk_AutoStart.Checked = AutoStartExist(AutoStartAsAdmin);
            lbl_TaskExist.Visible = AutoStartExist(true);
            lbl_LinkExist.Visible = AutoStartExist(false);
            TrayIconVisible = chk_TrayIcon.Checked = MMain.MyConfs.ReadBool("Functions", "TrayIconVisible");
            ConvertSelectionLS = chk_CSLayoutSwitching.Checked = MMain.MyConfs.ReadBool("Functions", "ConvertSelectionLayoutSwitching");
            ReSelect = chk_ReSelect.Checked = MMain.MyConfs.ReadBool("Functions", "ReSelect");
            RePress = chk_RePress.Checked = MMain.MyConfs.ReadBool("Functions", "RePress");
            AddOneSpace = chk_AddOneSpace.Checked = MMain.MyConfs.ReadBool("Functions", "AddOneSpaceToLastWord");
            Add1NL = chk_Add1NL.Checked = MMain.MyConfs.ReadBool("Functions", "AddOneEnterToLastWord");
            ConvertSelectionLSPlus = chk_CSLayoutSwitchingPlus.Checked = MMain.MyConfs.ReadBool("Functions", "ConvertSelectionLayoutSwitchingPlus");
            ScrollTip = chk_HighlightScroll.Checked = MMain.MyConfs.ReadBool("Functions", "ScrollTip");
            chk_StartupUpdatesCheck.Checked = MMain.MyConfs.ReadBool("Functions", "StartupUpdatesCheck");
            chk_SilentUpdate.Checked = MMain.MyConfs.ReadBool("Functions", "SilentUpdate");
            LoggingEnabled = chk_Logging.Checked = MMain.MyConfs.ReadBool("Functions", "Logging");
            latest_save_dir = nPath;
            if (LoggingEnabled)
                MMain.LogTimer.Change(300, 0);
            else
                MMain.LogTimer.Change(0, 0);
            TrayFlags = MMain.MyConfs.ReadBool("Functions", "TrayFlags");
            TrayText = MMain.MyConfs.ReadBool("Functions", "TrayText");
            CapsLockDisablerTimer = chk_CapsLockDTimer.Checked = MMain.MyConfs.ReadBool("Functions", "CapsLockTimer");
            BlockHKWithCtrl = chk_BlockHKWithCtrl.Checked = MMain.MyConfs.ReadBool("Functions", "BlockMahouHotkeysWithCtrl");
            SymIgnEnabled = MMain.MyConfs.ReadBool("Functions", "SymbolIgnoreModeEnabled");
            MCDSSupport = chk_MCDS_support.Checked = MMain.MyConfs.ReadBool("Functions", "MCDServerSupport");
            OneLayoutWholeWord = chk_OneLayoutWholeWord.Checked = MMain.MyConfs.ReadBool("Functions", "OneLayoutWholeWord");
            MouseLangTooltipEnabled = MMain.MyConfs.ReadBool("Appearence", "DisplayLangTooltipForMouse");
            CaretLangTooltipEnabled = MMain.MyConfs.ReadBool("Appearence", "DisplayLangTooltipForCaret");
            GuessKeyCodeFix = chk_GuessKeyCodeFix.Checked = MMain.MyConfs.ReadBool("Functions", "GuessKeyCodeFix");
            RemapCapslockAsF18 = chk_RemapCapsLockAsF18.Checked = MMain.MyConfs.ReadBool("Functions", "RemapCapslockAsF18");
            UseJKL = chk_GetLayoutFromJKL.Checked = MMain.MyConfs.ReadBool("Functions", "UseJKL");
            ReadOnlyNA = chk_ReadOnlyNA.Checked = MMain.MyConfs.ReadBool("Functions", "ReadOnlyNA");
            WriteInputHistory = chk_WriteInputHistory.Checked = MMain.MyConfs.ReadBool("Functions", "WriteInputHistory");

            #endregion

            #region Layouts

            SwitchBetweenLayouts = chk_SwitchBetweenLayouts.Checked = MMain.MyConfs.ReadBool("Layouts", "SwitchBetweenLayouts");
            EmulateLS = chk_EmulateLS.Checked = MMain.MyConfs.ReadBool("Layouts", "EmulateLayoutSwitch");
            ChangeLayouByKey = chk_SpecificLS.Checked = MMain.MyConfs.ReadBool("Layouts", "ChangeToSpecificLayoutByKey");
            MainLayout1 = MMain.MyConfs.Read("Layouts", "MainLayout1");
            MainLayout2 = MMain.MyConfs.Read("Layouts", "MainLayout2");
            MAIN_LAYOUT1 = Locales.GetLocaleFromString(MainLayout1).uId;
            MAIN_LAYOUT2 = Locales.GetLocaleFromString(MainLayout2).uId;
            Layout1 = MMain.MyConfs.Read("Layouts", "SpecificLayout1");
            Layout2 = MMain.MyConfs.Read("Layouts", "SpecificLayout2");
            Layout3 = MMain.MyConfs.Read("Layouts", "SpecificLayout3");
            Layout4 = MMain.MyConfs.Read("Layouts", "SpecificLayout4");
            TestLayout(Layout1, 1);
            TestLayout(Layout2, 2);
            TestLayout(Layout3, 3);
            TestLayout(Layout4, 4);
            Layout1 = MMain.MyConfs.Read("Layouts", "SpecificLayout1");
            Layout2 = MMain.MyConfs.Read("Layouts", "SpecificLayout2");
            Layout3 = MMain.MyConfs.Read("Layouts", "SpecificLayout3");
            Layout4 = MMain.MyConfs.Read("Layouts", "SpecificLayout4");
            Key1 = MMain.MyConfs.ReadInt("Layouts", "SpecificKey1");
            Key2 = MMain.MyConfs.ReadInt("Layouts", "SpecificKey2");
            Key3 = MMain.MyConfs.ReadInt("Layouts", "SpecificKey3");
            Key4 = MMain.MyConfs.ReadInt("Layouts", "SpecificKey4");
            OneLayout = chk_OneLayout.Checked = MMain.MyConfs.ReadBool("Layouts", "OneLayout");
            QWERTZ_fix = chk_qwertz.Checked = MMain.MyConfs.ReadBool("Layouts", "QWERTZfix");
            LoadSpecKeySetsValues();

            #endregion

            #region Persistent Layout

            PersistentLayoutOnWindowChange = chk_OnlyOnWindowChange.Checked = MMain.MyConfs.ReadBool("PersistentLayout", "OnlyOnWindowChange");
            PersistentLayoutOnlyOnce = chk_ChangeLayoutOnlyOnce.Checked = MMain.MyConfs.ReadBool("PersistentLayout", "ChangeOnlyOnce");
            KMHook.PLC_HWNDs.Clear();
            KMHook.ConHost_HWNDs.Clear();
            PERSISTENT_LAYOUT1_HWNDs.Clear();
            NOT_PERSISTENT_LAYOUT1_HWNDs.Clear();
            PERSISTENT_LAYOUT2_HWNDs.Clear();
            NOT_PERSISTENT_LAYOUT2_HWNDs.Clear();
            PersistentLayoutForLayout1 = chk_PersistentLayout1Active.Checked = MMain.MyConfs.ReadBool("PersistentLayout", "ActivateForLayout1");
            PersistentLayoutForLayout2 = chk_PersistentLayout2Active.Checked = MMain.MyConfs.ReadBool("PersistentLayout", "ActivateForLayout2");
            nud_PersistentLayout1Interval.Value = MMain.MyConfs.ReadInt("PersistentLayout", "Layout1CheckInterval");
            nud_PersistentLayout2Interval.Value = MMain.MyConfs.ReadInt("PersistentLayout", "Layout2CheckInterval");
            PersistentLayout1Processes = txt_PersistentLayout1Processes.Text = MMain.MyConfs.Read("PersistentLayout", "Layout1Processes").Replace("^cr^lf", Environment.NewLine);
            PersistentLayout2Processes = txt_PersistentLayout2Processes.Text = MMain.MyConfs.Read("PersistentLayout", "Layout2Processes").Replace("^cr^lf", Environment.NewLine);

            #endregion

            #region Appearence

            LDForMouse = chk_LangTooltipMouse.Checked = MMain.MyConfs.ReadBool("Appearence", "DisplayLangTooltipForMouse");
            LDForCaret = chk_LangTooltipCaret.Checked = MMain.MyConfs.ReadBool("Appearence", "DisplayLangTooltipForCaret");
            LDForMouseOnChange = chk_LangTTMouseOnChange.Checked = MMain.MyConfs.ReadBool("Appearence", "DisplayLangTooltipForMouseOnChange");
            LDForCaretOnChange = chk_LangTTCaretOnChange.Checked = MMain.MyConfs.ReadBool("Appearence", "DisplayLangTooltipForCaretOnChange");
            DiffAppearenceForLayouts = chk_LangTTDiffLayoutColors.Checked = MMain.MyConfs.ReadBool("Appearence", "DifferentColorsForLayouts");
            MouseTTAlways = chk_MouseTTAlways.Checked = MMain.MyConfs.ReadBool("Appearence", "MouseLTAlways");
            mouseLTUpperArrow = MMain.MyConfs.ReadBool("Appearence", "MouseLTUpperArrow");
            caretLTUpperArrow = MMain.MyConfs.ReadBool("Appearence", "CaretLTUpperArrow");
            LDUseWindowsMessages = chk_LDMessages.Checked = MMain.MyConfs.ReadBool("Appearence", "WindowsMessages");

            #endregion

            #region Timings

            LD_MouseSkipMessagesCount = MMain.MyConfs.ReadInt("Timings", "LangTooltipForMouseSkipMessages");
            if (LDUseWindowsMessages)
            {
                nud_LangTTMouseRefreshRate.Maximum = 100;
                nud_LangTTMouseRefreshRate.Minimum = 0;
                nud_LangTTMouseRefreshRate.Increment = 1;
                nud_LangTTMouseRefreshRate.Value = LD_MouseSkipMessagesCount;
                lbl_LangTTMouseRefreshRate.Text = MMain.Lang[Languages.Element.LD_MouseSkipMessages];
            }
            else
            {
                nud_LangTTMouseRefreshRate.Maximum = 2500;
                nud_LangTTMouseRefreshRate.Minimum = 1;
                nud_LangTTMouseRefreshRate.Increment = 25;
                nud_LangTTMouseRefreshRate.Value = MMain.MyConfs.ReadInt("Timings", "LangTooltipForMouseRefreshRate");
            }

            nud_LangTTCaretRefreshRate.Value = MMain.MyConfs.ReadInt("Timings", "LangTooltipForCaretRefreshRate");
            nud_DoubleHK2ndPressWaitTime.Value = DoubleHKInterval = MMain.MyConfs.ReadInt("Timings", "DoubleHotkey2ndPressWait");
            nud_TrayFlagRefreshRate.Value = MMain.MyConfs.ReadInt("Timings", "FlagsInTrayRefreshRate");
            nud_ScrollLockRefreshRate.Value = MMain.MyConfs.ReadInt("Timings", "ScrollLockStateRefreshRate");
            nud_CapsLockRefreshRate.Value = MMain.MyConfs.ReadInt("Timings", "CapsLockDisableRefreshRate");
            nud_ScrollLockRefreshRate.Value = MMain.MyConfs.ReadInt("Timings", "ScrollLockStateRefreshRate");
            SelectedTextGetMoreTries = chk_SelectedTextGetMoreTries.Checked = MMain.MyConfs.ReadBool("Timings", "SelectedTextGetMoreTries");
            nud_SelectedTextGetTriesCount.Value = MMain.MyConfs.ReadInt("Timings", "SelectedTextGetMoreTriesCount");
            nud_DelayAfterBackspaces.Value = DelayAfterBackspaces = MMain.MyConfs.ReadInt("Timings", "DelayAfterBackspaces");
            UseDelayAfterBackspaces = chk_UseDelayAfterBackspaces.Checked = MMain.MyConfs.ReadBool("Timings", "UseDelayAfterBackspaces");

            #region Excluded

            ExcludedPrograms = txt_ExcludedPrograms.Text = MMain.MyConfs.Read("Timings", "ExcludedPrograms").Replace("^cr^lf", Environment.NewLine);
            KMHook.EXCLUDED_HWNDs.Clear();
            KMHook.NOT_EXCLUDED_HWNDs.Clear();
            ChangeLayoutInExcluded = chk_Change1KeyL.Checked = MMain.MyConfs.ReadBool("Timings", "ChangeLayoutInExcluded");

            #endregion

            SelectedTextGetMoreTriesCount = (int) nud_SelectedTextGetTriesCount.Value;

            #endregion

            #region LangPanel

            LangPanelDisplay = chk_DisplayLangPanel.Checked = MMain.MyConfs.ReadBool("LangPanel", "Display");
            nud_LPRefreshRate.Value = LangPanelRefreshRate = MMain.MyConfs.ReadInt("LangPanel", "RefreshRate");
            nud_LPTransparency.Value = LangPanelTransparency = MMain.MyConfs.ReadInt("LangPanel", "Transparency");
            btn_LPFore.BackColor = LangPanelForeColor = GetColor(MMain.MyConfs.Read("LangPanel", "ForeColor"));
            btn_LPBack.BackColor = LangPanelBackColor = GetColor(MMain.MyConfs.Read("LangPanel", "BackColor"));
            btn_LPBorderColor.BackColor = LangPanelBorderColor = GetColor(MMain.MyConfs.Read("LangPanel", "BorderColor"));
            LangPanelBorderAero = chk_LPAeroColor.Checked = MMain.MyConfs.ReadBool("LangPanel", "BorderAeroColor");
            btn_LPFont.Font = LangPanelFont = GetFont(MMain.MyConfs.Read("LangPanel", "Font"));
            LangPanelUpperArrow = chk_LPUpperArrow.Checked = MMain.MyConfs.ReadBool("LangPanel", "UpperArrow");

            #endregion

            #region Translate Panel

            TrEnabled = chk_TrEnable.Checked = MMain.MyConfs.ReadBool("TranslatePanel", "Enabled");
            TrOnDoubleClick = chk_TrOnDoubleClick.Checked = MMain.MyConfs.ReadBool("TranslatePanel", "OnDoubleClick");
            nud_TrTransparency.Value = TrTransparency = MMain.MyConfs.ReadInt("TranslatePanel", "Transparency");
            btn_TrFG.BackColor = TrFore = GetColor(MMain.MyConfs.Read("TranslatePanel", "FG"));
            btn_TrBG.BackColor = TrBack = GetColor(MMain.MyConfs.Read("TranslatePanel", "BG"));
            btn_TrBorderC.BackColor = TrBorder = GetColor(MMain.MyConfs.Read("TranslatePanel", "BorderC"));
            TrBorderAero = chk_TrUseAccent.Checked = MMain.MyConfs.ReadBool("TranslatePanel", "BorderAero");
            LoadTrSetsValues();
            RefreshComboboxes();
            if (TrEnabled)
            {
                if (_TranslatePanel == null)
                    _TranslatePanel = new TranslatePanel();
                _TranslatePanel.SetTitle(MMain.Lang[Languages.Element.Translation]);
            }
            else
            {
                if (_TranslatePanel != null)
                    _TranslatePanel.Dispose();
            }

            TrText = btn_TrTextFont.Font = GetFont(MMain.MyConfs.Read("TranslatePanel", "TextFont"));
            TrTitle = btn_TrTitleFont.Font = GetFont(MMain.MyConfs.Read("TranslatePanel", "TitleFont"));

            #endregion

            #region Snippets

            SnippetsEnabled = chk_Snippets.Checked = MMain.MyConfs.ReadBool("Snippets", "SnippetsEnabled");
            SnippetSpaceAfter = chk_SnippetsSpaceAfter.Checked = MMain.MyConfs.ReadBool("Snippets", "SpaceAfter");
            SnippetsSwitchToGuessLayout = chk_SnippetsSwitchToGuessLayout.Checked = MMain.MyConfs.ReadBool("Snippets", "SwitchToGuessLayout");
            SnippetsExpandType = MMain.MyConfs.Read("Snippets", "SnippetExpandKey");
            cbb_SnippetExpandKeys.SelectedIndex = cbb_SnippetExpandKeys.Items.IndexOf(SnippetsExpandType);

            #endregion

            #region AutoSwitch

            AutoSwitchEnabled = chk_AutoSwitch.Checked = MMain.MyConfs.ReadBool("AutoSwitch", "Enabled");
            AutoSwitchSpaceAfter = chk_AutoSwitchSpaceAfter.Checked = MMain.MyConfs.ReadBool("AutoSwitch", "SpaceAfter");
            AutoSwitchSwitchToGuessLayout = chk_AutoSwitchSwitchToGuessLayout.Checked = MMain.MyConfs.ReadBool("AutoSwitch", "SwitchToGuessLayout");
            Dowload_ASD_InZip = chk_DownloadASD_InZip.Checked = MMain.MyConfs.ReadBool("AutoSwitch", "DownloadInZip");
            check_ASD_size = true;
            if (AutoSwitchEnabled && SnippetsEnabled)
                if (File.Exists(AS_dictfile))
                {
                    AutoSwitchDictionaryRaw = File.ReadAllText(AS_dictfile);
                    ChangeAutoSwitchDictionaryTextBox();
                    UpdateSnippetCountLabel(AutoSwitchDictionaryRaw, lbl_AutoSwitchWordsCount, false);
                }

            MahouUIActivated(1, new EventArgs());
            if (SnippetsEnabled)
            {
                if (!File.Exists(snipfile))
                    File.WriteAllText(snipfile, txt_Snippets.Text, Encoding.UTF8);
                if (File.Exists(snipfile))
                {
                    txt_Snippets.Text = File.ReadAllText(snipfile);
                    UpdateSnippetCountLabel(txt_Snippets.Text, lbl_SnippetsCount);
                    KMHook.ReInitSnippets();
                    KMHook.DoLater(() =>
                    {
                        if (KMHook.snipps.Length != SnippetsCount || KMHook.as_corrects.Length != AutoSwitchCount)
                            KMHook.ReInitSnippets();
                    }, 650);
                }
            }

            #endregion

            KMHook.ReloadTSDict();

            #region Appearence & Hotkeys

            LoadTemps();
            UpdateLangDisplayControlsSwitch();
            UpdateHotkeyControlsSwitch();

            #endregion

            #region Sounds

            SoundEnabled = chk_EnableSnd.Checked = MMain.MyConfs.ReadBool("Sounds", "Enabled");
            SoundOnAutoSwitch = chk_SndAutoSwitch.Checked = MMain.MyConfs.ReadBool("Sounds", "OnAutoSwitch");
            SoundOnSnippets = chk_SndSnippets.Checked = MMain.MyConfs.ReadBool("Sounds", "OnSnippets");
            SoundOnConvLast = chk_SndLast.Checked = MMain.MyConfs.ReadBool("Sounds", "OnConvertLast");
            SoundOnLayoutSwitch = chk_SndLayoutSwitch.Checked = MMain.MyConfs.ReadBool("Sounds", "OnLayoutSwitch");
            UseCustomSound = chk_UseCustomSnd.Checked = MMain.MyConfs.ReadBool("Sounds", "UseCustomSound");
            CustomSound = lbl_CustomSound.Text = MMain.MyConfs.Read("Sounds", "CustomSound");
            SoundOnAutoSwitch2 = chk_SndAutoSwitch2.Checked = MMain.MyConfs.ReadBool("Sounds", "OnAutoSwitch2");
            SoundOnSnippets2 = chk_SndSnippets2.Checked = MMain.MyConfs.ReadBool("Sounds", "OnSnippets2");
            SoundOnConvLast2 = chk_SndLast2.Checked = MMain.MyConfs.ReadBool("Sounds", "OnConvertLast2");
            SoundOnLayoutSwitch2 = chk_SndLayoutSwitch2.Checked = MMain.MyConfs.ReadBool("Sounds", "OnLayoutSwitch2");
            UseCustomSound2 = chk_UseCustomSnd2.Checked = MMain.MyConfs.ReadBool("Sounds", "UseCustomSound2");
            CustomSound2 = lbl_CustomSound2.Text = MMain.MyConfs.Read("Sounds", "CustomSound2");
            var lbCSh = lbl_CustomSound.Text;
            var lbCSh2 = lbl_CustomSound2.Text;
            if (!File.Exists(CustomSound))
            {
                lbl_CustomSound.ForeColor = Color.Red;
                lbCSh = MMain.Lang[Languages.Element.Not] + " " + MMain.Lang[Languages.Element.Exist] + ":\r\n[" + lbl_CustomSound.Text + "]";
            }
            else
            {
                lbl_CustomSound.ForeColor = Color.FromKnownColor(KnownColor.WindowText);
            }

            if (!File.Exists(CustomSound2))
            {
                lbl_CustomSound2.ForeColor = Color.Red;
                lbCSh2 = MMain.Lang[Languages.Element.Not] + " " + MMain.Lang[Languages.Element.Exist] + ":\r\n[" + lbl_CustomSound2.Text + "]";
            }
            else
            {
                lbl_CustomSound2.ForeColor = Color.FromKnownColor(KnownColor.WindowText);
            }

            HelpMeUnderstand.SetToolTip(lbl_CustomSound, lbCSh);
            HelpMeUnderstand.SetToolTip(lbl_CustomSound2, lbCSh2);

            #endregion

            #region Sync

            var bbools = MMain.MyConfs.Read("Sync", "BBools");
            bool m, s, h, t;
            SetBools(bbools, '|', out m, out s, out h, out t);
            chk_Mini.Checked = m;
            chk_Stxt.Checked = s;
            chk_Htxt.Checked = h;
            chk_Ttxt.Checked = t;
            var rbools = MMain.MyConfs.Read("Sync", "RBools");
            SetBools(rbools, '|', out m, out s, out h, out t);
            chk_rMini.Checked = m;
            chk_rStxt.Checked = s;
            chk_rHtxt.Checked = h;
            chk_rTtxt.Checked = t;
            var blast = MMain.MyConfs.Read("Sync", "BLast");
            if (!string.IsNullOrEmpty(blast))
            {
                txt_backupId.Text = blast;
                txt_backupId.Enabled = true;
            }

            var rlast = MMain.MyConfs.Read("Sync", "RLast");
            if (!string.IsNullOrEmpty(rlast))
                txt_restoreId.Text = rlast;

            #endregion

            if (RemapCapslockAsF18 || SnippetsExpandType == "Tab")
                LLHook.Set();
            else
                LLHook.UnSet();
            InitializeHotkeys();
            InitializeTimers();
            InitializeLangPanel();
            ToggleDependentControlsEnabledState();
            RefreshAllIcons(true);
            if (_langPanel != null)
            {
                _langPanel.UpdateApperence(LangPanelBackColor, LangPanelForeColor, LangPanelTransparency, LangPanelFont);
                if (LangPanelDisplay)
                    _langPanel.ShowInactiveTopmost();
                else
                    _langPanel.HideWnd();
            }

            // Restore last positon
            lsb_LangTTAppearenceForList.SelectedIndex = tmpLangTTAppearenceIndex;
            lsb_Hotkeys.SelectedIndex = tmpHotkeysIndex;
            if (UseJKL)
            {
                if (!jklXHidServ.jklExist())
                {
                    chk_GetLayoutFromJKL.ForeColor = Color.Red;
                    HelpMeUnderstand.SetToolTip(chk_GetLayoutFromJKL, jklXHidServ.jklInfoStr);
                }
                else
                {
                    chk_GetLayoutFromJKL.ForeColor = Color.FromKnownColor(KnownColor.WindowText);
                    HelpMeUnderstand.SetToolTip(chk_GetLayoutFromJKL, MMain.Lang[Languages.Element.TT_UseJKL]);
                }

                jklXHidServ.Init();
            }
            else
            {
                jklXHidServ.Destroy();
                chk_GetLayoutFromJKL.ForeColor = Color.FromKnownColor(KnownColor.WindowText);
                HelpMeUnderstand.SetToolTip(chk_GetLayoutFromJKL, MMain.Lang[Languages.Element.TT_UseJKL]);
            }

            UnregisterHotkeys();
            RegisterHotkeys();
            Memory.Flush();
            Logging.Log("All configurations loaded.");
        }

        private List<string[]> ParseSets(string raw_sets)
        {
            if (raw_sets.Contains("set_0") || raw_sets.Contains("set0")) return new List<string[]>();
            var sets = raw_sets.Split('|');
            var last_set = sets[sets.Length - 1];
//			Debug.WriteLine(last_set);
            var set_count = int.Parse(last_set.Split('/')[0].Replace("set_", ""));
            var SETS = new List<string[]>();
            foreach (var _set in sets) SETS.Add(_set.Split('/'));
            return SETS;
        }

        private void LoadTrSetsValues()
        {
            var sets_raw = MMain.MyConfs.Read("TranslatePanel", "LanguageSets");
            var SETS = ParseSets(sets_raw);
            if (SETS.Count == 0) return;
            var NOTR = TrSetCount == 0;
            if (NOTR)
                pan_TrSets.Controls.Clear();
            for (var i = 1; i != SETS.Count + 1; i++)
            {
                if (NOTR)
                    Btn_TrAddSetClick(1, new EventArgs());
                var values = SETS[i - 1];
                TrSetsValues["cbb_fr" + i] = values[1];
                TrSetsValues["cbb_to" + i] = values[2];
//				var key = 0;
//				if (!String.IsNullOrEmpty(values[1]))
//					key = Int32.Parse(values[1]);
//				UpdateSetControls(i, key, values[2]);
            }
        }

        private void LoadSpecKeySetsValues()
        {
            var sets_raw = MMain.MyConfs.Read("Layouts", "SpecificKeySets");
            var SETS = ParseSets(sets_raw);
            if (SETS.Count == 0) return;
            var NOSPEC = SpecKeySetCount == 0;
            if (NOSPEC)
                pan_KeySets.Controls.Clear();
            // Initilize sets
            for (var i = 1; i != SETS.Count + 1; i++)
            {
                if (NOSPEC)
                    Btn_AddSetClick(1, new EventArgs());
                var values = SETS[i - 1];
                SpecKeySetsValues["txt_key" + i + "_key"] = values[1];
                SpecKeySetsValues["txt_key" + i + "_mods"] = values[2];
                SpecKeySetsValues["cbb_typ" + i] = values[3];
                if (!string.IsNullOrEmpty(values[3]))
                    if (values[3] == MMain.Lang[Languages.Element.SwitchBetween])
                    {
                        SaveSpecificKeySets(true, i, MMain.Lang[Languages.Element.SwitchBetween]);
                        SpecKeySetsValues["cbb_typ" + i] = MMain.Lang[Languages.Element.SwitchBetween];
                    }

                var key = 0;
                if (!string.IsNullOrEmpty(values[1]))
                    key = int.Parse(values[1]);
                UpdateSetControls(i, key, values[2]);
            }
        }

        private Tuple<int, Color, int> GetSnippetsCount(string snippets)
        {
            if (string.IsNullOrEmpty(snippets)) return new Tuple<int, Color, int>(0, Color.Black, 0);
            Logging.Log("Starting counting snippets...");
            Stopwatch watch = null;
            if (LoggingEnabled)
            {
                watch = new Stopwatch();
                watch.Start();
            }

            // This regex is ~x8 slower than the way above. 
//			var matches = Regex.Matches(snippets, "(->)|(====>)|(<====)", RegexOptions.Compiled);
            var com = 0;
            var ci = 0;
            var cia = 0;
            var cic = 0;
            var in_exp = false;
            for (var k = 0; k < snippets.Length - 1; k++)
            {
                // Do not try to store snippets[k] & snippets[k+n] to string variable, that will be significally slower.
                // with string.Concat() ~x15 slower, with string.Format() ~x45 slower.			
                var cml = KMHook.SnippetsLineCommented(snippets, k);
                if (cml.Item1)
                {
                    com++;
                    k += cml.Item2;
                    continue;
                }

                if (!in_exp && snippets[k].Equals('-') && snippets[k + 1].Equals('>'))
                    ci++;
                if (k + 4 < snippets.Length)
                {
                    if (snippets[k].Equals('=') && snippets[k + 1].Equals('=') &&
                        snippets[k + 2].Equals('=') && snippets[k + 3].Equals('=') &&
                        snippets[k + 4].Equals('>'))
                    {
                        cia++;
                        in_exp = true;
                    }

                    if (snippets[k].Equals('<') && snippets[k + 1].Equals('=') &&
                        snippets[k + 2].Equals('=') && snippets[k + 3].Equals('=') &&
                        snippets[k + 4].Equals('='))
                    {
                        cic++;
                        in_exp = false;
                    }
                }
            }

            Logging.Log("Snippets word count details: " + cic + ", " + cia + ", " + ci + "<com> " + com);
            var result = ci + cia + cic;
            if (LoggingEnabled)
            {
                watch.Stop();
                Logging.Log("Snippets with length [" + snippets.Length + "], snippets count [" + result / 3 + "], errors [" + (result % 3 != 0) + "], elapsed [" + watch.Elapsed.TotalMilliseconds + "] ms.");
            }

            Memory.Flush();
            if (result % 3 == 0)
                return new Tuple<int, Color, int>(result / 3, Color.Orange, com);
            return new Tuple<int, Color, int>(ci, Color.Red, com);
        }

        private void TestLayout(string layout, int id)
        {
            if (layout == Languages.English[Languages.Element.SwitchBetween] && MMain.Lang == Languages.Russian ||
                layout == Languages.Russian[Languages.Element.SwitchBetween] && MMain.Lang == Languages.English)
                MMain.MyConfs.WriteSave("Layouts", "SpecificLayout" + id, MMain.Lang[Languages.Element.SwitchBetween]);
        }

        /// <summary>
        ///     Refreshes comboboxes items.
        /// </summary>
        private void RefreshComboboxes()
        {
            cbb_AutostartType.SelectedIndex = AutoStartAsAdmin ? 1 : 0;
            MMain.Locales = Locales.AllList();
            MMain.RefreshLcnMid();
            cbb_BackSpaceType.Items.Clear();
            cbb_BackSpaceType.Items.Add(MMain.Lang[Languages.Element.InputHistoryBackSpaceWriteType1]);
            cbb_BackSpaceType.Items.Add(MMain.Lang[Languages.Element.InputHistoryBackSpaceWriteType2]);
            cbb_TrayDislpayType.Items.Clear();
            cbb_TrayDislpayType.Items.Add(MMain.Lang[Languages.Element.JustIcon]);
            cbb_TrayDislpayType.Items.Add(MMain.Lang[Languages.Element.ContryFlags]);
            cbb_TrayDislpayType.Items.Add(MMain.Lang[Languages.Element.TextLayout]);
            if (TrayFlags)
                cbb_TrayDislpayType.SelectedIndex = 1;
            else if (TrayText)
                cbb_TrayDislpayType.SelectedIndex = 2;
            else
                cbb_TrayDislpayType.SelectedIndex = 0;
            InputHistoryBackSpaceWriteType = cbb_BackSpaceType.SelectedIndex = MMain.MyConfs.ReadInt("Functions", "WriteInputHistoryBackSpaceType");
            if (SpecKeySetCount > 0)
                for (var i = 1; i <= SpecKeySetCount; i++)
                {
                    Logging.Log("Refreshing Specific Hotkey Set #" + i);
                    var cbb = pan_KeySets.Controls["set_" + i].Controls["cbb_typ" + i] as ComboBox;
                    cbb.Items.Clear();
                    cbb.Items.Add(MMain.Lang[Languages.Element.SwitchBetween]);
                    cbb.Items.AddRange(MMain.Lcnmid.ToArray());
                    cbb.SelectedIndex = cbb.Items.IndexOf(SpecKeySetsValues["cbb_typ" + i]);
                }

            if (TrSetCount > 0)
                for (var i = 1; i <= TrSetCount; i++)
                {
                    Logging.Log("Refreshing Tr Set #" + i);
                    var cbb = pan_TrSets.Controls["set_" + i].Controls["cbb_fr" + i] as ComboBox;
                    cbb.Items.Clear();
                    cbb.Items.AddRange(TranslatePanel.GTLangs);
                    cbb.SelectedIndex = Array.IndexOf(TranslatePanel.GTLangsSh, TrSetsValues["cbb_fr" + i]);
                    cbb = pan_TrSets.Controls["set_" + i].Controls["cbb_to" + i] as ComboBox;
                    cbb.Items.Clear();
                    cbb.Items.AddRange(TranslatePanel.GTLangs);
                    cbb.SelectedIndex = Array.IndexOf(TranslatePanel.GTLangsSh, TrSetsValues["cbb_to" + i]);
                }

            cbb_Layout1.Items.Clear();
            cbb_Layout2.Items.Clear();
            cbb_Layout3.Items.Clear();
            cbb_Layout4.Items.Clear();
            cbb_MainLayout1.Items.Clear();
            cbb_MainLayout2.Items.Clear();
            cbb_Layout1.Items.Add(MMain.Lang[Languages.Element.SwitchBetween]);
            cbb_Layout2.Items.Add(MMain.Lang[Languages.Element.SwitchBetween]);
            cbb_Layout3.Items.Add(MMain.Lang[Languages.Element.SwitchBetween]);
            cbb_Layout4.Items.Add(MMain.Lang[Languages.Element.SwitchBetween]);
            cbb_Layout1.Items.AddRange(MMain.Lcnmid.ToArray());
            cbb_Layout2.Items.AddRange(MMain.Lcnmid.ToArray());
            cbb_Layout3.Items.AddRange(MMain.Lcnmid.ToArray());
            cbb_Layout4.Items.AddRange(MMain.Lcnmid.ToArray());
            cbb_MainLayout1.Items.AddRange(MMain.Lcnmid.ToArray());
            cbb_MainLayout2.Items.AddRange(MMain.Lcnmid.ToArray());
            cbb_SpecKeysType.SelectedIndex = MMain.MyConfs.ReadInt("Layouts", "SpecificKeysType");
            try
            {
                cbb_Language.SelectedIndex = cbb_Language.Items.IndexOf(MMain.Language);
                EmulateLSType = MMain.MyConfs.Read("Layouts", "EmulateLayoutSwitchType");
                cbb_Layout1.SelectedIndex = cbb_Layout1.Items.IndexOf(Layout1);
                cbb_Layout2.SelectedIndex = cbb_Layout2.Items.IndexOf(Layout2);
                cbb_Layout3.SelectedIndex = cbb_Layout3.Items.IndexOf(Layout3);
                cbb_Layout4.SelectedIndex = cbb_Layout4.Items.IndexOf(Layout4);
                cbb_Key1.SelectedIndex = Key1;
                cbb_Key2.SelectedIndex = Key2;
                cbb_Key3.SelectedIndex = Key3;
                cbb_Key4.SelectedIndex = Key4;
                cbb_EmulateType.SelectedIndex = cbb_EmulateType.Items.IndexOf(EmulateLSType);
                cbb_MainLayout1.SelectedIndex = MMain.Lcnmid.IndexOf(MainLayout1);
                cbb_MainLayout2.SelectedIndex = MMain.Lcnmid.IndexOf(MainLayout2);
            }
            catch (Exception e)
            {
//				MessageBox.Show(MMain.Msgs[9], MMain.Msgs[5], MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                RefreshComboboxes();
                cbb_MainLayout1.SelectedIndex = 0;
                cbb_MainLayout2.SelectedIndex = 1;
                Logging.Log("Locales indexes select failed, error message:\n" + e.Message + "\n" + e.StackTrace + "\n", 1);
            }

            Logging.Log("Locales for ALL comboboxes refreshed.");
        }

        /// <summary>
        ///     Toggles some controls enabled state based on some checkboxes checked state.
        /// </summary>
        private void ToggleDependentControlsEnabledState()
        {
            // Functions tab
            chk_CSLayoutSwitchingPlus.Enabled = chk_CSLayoutSwitching.Checked;
//			chk_OneLayoutWholeWord.Enabled = !chk_CSLayoutSwitching.Checked;
            if (chk_CSLayoutSwitching.Checked && chk_OneLayoutWholeWord.Checked)
                chk_CSLayoutSwitchingPlus.Enabled = false;
            if (!chk_OneLayoutWholeWord.Checked)
                chk_CSLayoutSwitching.ForeColor = chk_CSLayoutSwitchingPlus.ForeColor = Color.Red;
            else
                chk_CSLayoutSwitching.ForeColor = chk_CSLayoutSwitchingPlus.ForeColor = chk_OneLayoutWholeWord.ForeColor;
            lbl_TrayDislpayType.Enabled = cbb_TrayDislpayType.Enabled = chk_TrayIcon.Checked;
            chk_SilentUpdate.Enabled = chk_StartupUpdatesCheck.Checked;
            lnk_OpenHistory.Enabled = lbl_BackSpaceType.Enabled = cbb_BackSpaceType.Enabled = chk_WriteInputHistory.Checked;
            lnk_OpenLogs.Enabled = chk_Logging.Checked;
            // Layouts tab
            lbl_SetsCount.Enabled = pan_KeySets.Enabled = btn_AddSet.Enabled = btn_SubSet.Enabled =
                lbl_KeysType.Enabled = cbb_SpecKeysType.Enabled = chk_SpecificLS.Checked;
            cbb_MainLayout1.Enabled = cbb_MainLayout2.Enabled =
                lbl_LayoutNum1.Enabled = lbl_LayoutNum2.Enabled = chk_SwitchBetweenLayouts.Checked;
            lbl_EmuType.Enabled = cbb_EmulateType.Enabled = chk_EmulateLS.Checked;
            grb_Keys.Enabled = grb_Layouts.Enabled = chk_SpecificLS.Checked;
//			if (chk_EmulateLS.Checked) {
//				chk_SwitchBetweenLayouts.Enabled = chk_SwitchBetweenLayouts.Checked = false;
//			} else { chk_SwitchBetweenLayouts.Enabled = true; }
            // Appearence tab
            chk_LangTTCaretOnChange.Enabled = chk_LangTooltipCaret.Checked;
            lbl_LangTTBackgroundColor.Enabled = btn_LangTTBackgroundColor.Enabled =
                !chk_LangTTTransparentColor.Checked;
            lbl_LangTTBackgroundColor.Enabled = btn_LangTTBackgroundColor.Enabled =
                chk_LangTTTransparentColor.Enabled = lbl_LangTTForegroundColor.Enabled = btn_LangTTForegroundColor.Enabled =
                    btn_LangTTFont.Enabled = !chk_LangTTUseFlags.Checked;
            if (!chk_LangTooltipMouse.Checked)
            {
                chk_MouseTTAlways.Enabled = chk_LangTTMouseOnChange.Enabled = false;
            }
            else
            {
                chk_MouseTTAlways.Enabled = !chk_LangTTMouseOnChange.Checked;
                chk_LangTTMouseOnChange.Enabled = !chk_MouseTTAlways.Checked;
            }

            // Snippets tab
            lbl_SnippetsCount.Enabled = lbl_SnippetExpandKey.Enabled =
                cbb_SnippetExpandKeys.Enabled = txt_Snippets.Enabled = chk_SnippetsSwitchToGuessLayout.Enabled = chk_SnippetsSpaceAfter.Enabled = chk_Snippets.Checked;
            // Auto Switch tab
            lbl_AutoSwitchWordsCount.Enabled = btn_UpdateAutoSwitchDictionary.Enabled =
                txt_AutoSwitchDictionary.Enabled = chk_AutoSwitchSwitchToGuessLayout.Enabled = chk_AutoSwitchSpaceAfter.Enabled = chk_DownloadASD_InZip.Enabled = chk_AutoSwitch.Checked;
            // Persistent Layout tab
            chk_ChangeLayoutOnlyOnce.Enabled = chk_OnlyOnWindowChange.Checked;
            txt_PersistentLayout1Processes.Enabled = chk_PersistentLayout1Active.Checked;
            txt_PersistentLayout2Processes.Enabled = chk_PersistentLayout2Active.Checked;
            if (chk_OnlyOnWindowChange.Checked || !chk_PersistentLayout1Active.Checked)
                lbl_PersistentLayout1Interval.Enabled = nud_PersistentLayout1Interval.Enabled = false;
            else
                lbl_PersistentLayout1Interval.Enabled = nud_PersistentLayout1Interval.Enabled = true;
            if (chk_OnlyOnWindowChange.Checked || !chk_PersistentLayout2Active.Checked)
                lbl_PersistentLayout2Interval.Enabled = nud_PersistentLayout2Interval.Enabled = false;
            else
                lbl_PersistentLayout2Interval.Enabled = nud_PersistentLayout2Interval.Enabled = true;
            // Language Panel tab
            grb_LPConfig.Enabled = chk_DisplayLangPanel.Checked;
            btn_LPBorderColor.Enabled = !chk_LPAeroColor.Checked;
            // Hotkeys tab
            chk_DoubleHotkey.Enabled = chk_WinInHotKey.Enabled = txt_Hotkey.Enabled = chk_HotKeyEnabled.Checked;
            chk_DoubleHotkey.Enabled = lsb_Hotkeys.SelectedIndex != 13;
            // Timings tab
            nud_DelayAfterBackspaces.Enabled = chk_UseDelayAfterBackspaces.Checked;
            nud_SelectedTextGetTriesCount.Enabled = chk_SelectedTextGetMoreTries.Checked;
            lbl_ScrollLockRefreshRate.Enabled = nud_ScrollLockRefreshRate.Enabled = chk_HighlightScroll.Checked;
            lbl_CapsLockRefreshRate.Enabled = nud_CapsLockRefreshRate.Enabled = chk_CapsLockDTimer.Checked;
            lbl_FlagTrayRefreshRate.Enabled = nud_TrayFlagRefreshRate.Enabled = cbb_TrayDislpayType.SelectedIndex == 1;
            lbl_LangTTCaretRefreshRate.Enabled = nud_LangTTCaretRefreshRate.Enabled = chk_LangTooltipCaret.Checked;
            lbl_LangTTMouseRefreshRate.Enabled = nud_LangTTMouseRefreshRate.Enabled = LDUseWindowsMessages || chk_LangTooltipMouse.Checked;
            lbl_LangTTCaretRefreshRate.Enabled = !chk_LDMessages.Checked;
            // Sounds tab
            lbl_CustomSound.Enabled = btn_SelectSnd.Enabled = chk_UseCustomSnd.Checked;
            lbl_CustomSound2.Enabled = btn_SelectSnd2.Enabled = chk_UseCustomSnd2.Checked;
            grb_Sound1.Enabled = grb_Sound2.Enabled = chk_EnableSnd.Checked;
            // Translation tab
            btn_TrBorderC.Enabled = !chk_TrUseAccent.Checked;
            grb_TrConfs.Enabled = chk_TrEnable.Checked;
        }

        /// <summary>
        ///     Toggles visibility of main window.
        /// </summary>
        public void ToggleVisibility()
        {
            Logging.Log("Mahou Main window visibility changed to [" + !Visible + "].");
            if (Visible)
            {
                Visible = false;
            }
            else
            {
                TopMost = Visible = true;
                TopMost = false;
                WinAPI.SetForegroundWindow(Handle);
            }

            if (MMain.Mahou != null)
                KMHook.ClearModifiers();
            icon.CheckShHi(Visible);
            Memory.Flush();
        }

        public void ToggleLangPanel()
        {
            if (_langPanel.Visible)
            {
                chk_DisplayLangPanel.Checked = LangPanelDisplay = _langPanel.Visible = false;
                MMain.MyConfs.WriteSave("LangPanel", "Display", "false");
                langPanelRefresh.Stop();
            }
            else
            {
                chk_DisplayLangPanel.Checked = LangPanelDisplay = _langPanel.Visible = true;
                MMain.MyConfs.WriteSave("LangPanel", "Display", "true");
                langPanelRefresh.Start();
            }
        }

        /// <summary>
        ///     Restarts Mahou.
        /// </summary>
        public void Restart()
        {
            var MahouPID = Process.GetCurrentProcess().Id;
            PreExit();
            var restartMahouPath = Path.Combine(new[]
            {
                nPath,
                "RestartMahou.cmd"
            });
            //Batch script to restart Mahou.
            var restartMahou =
                @"@ECHO OFF
REM You should never see this file, if you are it means during restarting Mahou something went wrong. 
chcp 65001
SET MAHOUDIR=" + AppDomain.CurrentDomain.BaseDirectory + @"
TASKKILL /PID " + MahouPID + @" /F
TASKKILL /IM Mahou.exe /F
START """" ""%MAHOUDIR%Mahou.exe""
DEL " + restartMahouPath;
            Logging.Log("Writing restart script.");
            File.WriteAllText(restartMahouPath, restartMahou);
            var piRestartMahou = new ProcessStartInfo {FileName = restartMahouPath, WindowStyle = ProcessWindowStyle.Hidden};
            Logging.Log("Starting restart script.");
            Process.Start(piRestartMahou);
        }

        /// <summary>
        ///     Refreshes all icon's images and tray icon visibility.
        /// </summary>
        public void RefreshAllIcons(bool force = false)
        {
            if (TrayFlags || TrayText)
            {
                ChangeTrayIconToFlag(force);
            }
            else
            {
                if (HKSymIgn_tempEnabled && SymIgnEnabled && icon.trIcon.Icon != Resources.MahouSymbolIgnoreMode)
                    icon.trIcon.Icon = Resources.MahouSymbolIgnoreMode;
                else if (!TrayFlags && !TrayText && icon.trIcon.Icon != Resources.MahouTrayHD)
                    icon.trIcon.Icon = Resources.MahouTrayHD;
            }

            if (!blueIcon && HKSymIgn_tempEnabled && SymIgnEnabled)
            {
                blueIcon = true;
                Icon = Resources.MahouSymbolIgnoreMode;
            }
            else if (blueIcon && HKSymIgn_tempEnabled && !SymIgnEnabled)
            {
                Icon = Resources.MahouTrayHD;
                blueIcon = false;
            }

            if (TrayIconVisible && !icon.trIcon.Visible)
                icon.Show();
            else if (!TrayIconVisible && icon.trIcon.Visible) icon.Hide();
        }

        public static void RefreshFLAG(bool force = false)
        {
            Debug.WriteLine("aLIVe");
            // No need for update when no display wrapper
            if (!TrayIconVisible && !LDCaretUseFlags_temp && !LDMouseUseFlags_temp && !LangPanelDisplay && !force) return;
            if (!ENABLED)
            {
                Debug.WriteLine("NOT ENABLED");
                FLAG = Resources.MahouTrayHD.ToBitmap();
                return;
            }

            if (force) FLAG = ITEXT = null;
            Debug.WriteLine("STIlL");
            var lcid = 0;
            if (!UseJKL || KMHook.JKLERR)
                lcid = (int) (Locales.GetCurrentLocale() & 0xffff);
            else
                lcid = (int) (currentLayout & 0xffff);
            var ol = false;
            if (MMain.Mahou != null)
                ol = OneLayout;
            else
                ol = MMain.MyConfs.ReadBool("Layouts", "OneLayout");
            if (ol)
                lcid = (int) (GlobalLayout & 0xffff);
            if (lcid > 0)
            {
                var flagname = "jp";
                var clangname = new CultureInfo(lcid);
                flagname = clangname.ThreeLetterISOLanguageName.Substring(0, 2).ToLower();
                var flagpth = Path.Combine(nPath, "Flags\\" + flagname + ".png");
                Debug.WriteLine("UpDATe?" + (flagname != latestSwitch || TrayText && ITEXT == null || TrayFlags && FLAG == null));
                if (flagname != latestSwitch || TrayText && ITEXT == null || TrayFlags && FLAG == null)
                {
                    Logging.Log("Changed flag to " + flagname + " lcid " + lcid);
                    Debug.WriteLine("Changed flag to " + flagname + " lcid " + lcid);
                    if (File.Exists(flagpth))
                        FLAG = (Bitmap) Image.FromFile(flagpth);
                    else
                        switch (flagname)
                        {
                            case "ru":
                                FLAG = Resources.ru;
                                break;
                            case "en":
                                FLAG = Resources.en;
                                break;
                            case "es":
                                FLAG = Resources.es;
                                break;
                            case "jp":
                                FLAG = Resources.jp;
                                break;
                            case "bu":
                                FLAG = Resources.bu;
                                break;
                            case "uk":
                                FLAG = Resources.uk;
                                break;
                            case "po":
                                FLAG = Resources.po;
                                break;
                            case "sw":
                                FLAG = Resources.sw;
                                break;
                            case "zh":
                                FLAG = Resources.zh;
                                break;
                            case "be":
                                FLAG = Resources.be;
                                break;
                            case "de":
                                FLAG = Resources.de;
                                break;
                            case "sp":
                                FLAG = Resources.sp;
                                break;
                            case "it":
                                FLAG = Resources.it;
                                break;
                            case "fr":
                                FLAG = Resources.fr;
                                break;
                            case "la":
                                FLAG = Resources.la;
                                break;
                            case "hy":
                                FLAG = Resources.hy;
                                break;
                            default:
                                FLAG = Resources.MahouTrayHD.ToBitmap();
                                Logging.Log("Missing flag for language [" + flagname + " / " + lcid + "].", 2);
                                break;
                        }
                    if (TrayText)
                    {
                        Debug.WriteLine("Drawing the text layout *icon* in tray.");
                        var n2 = true;
                        var t = char.ToUpper(flagname[0]) + flagname.Substring(1);
                        var bg = LDCaretBack_temp;
                        var fg = LDCaretFore_temp;
                        var fn = LDCaretFont_temp;
                        if (lcid == (MAIN_LAYOUT2 & 0xffff))
                        {
                            bg = Layout2Back_temp;
                            fg = Layout2Fore_temp;
                            fn = Layout2Font_temp;
                            if (!string.IsNullOrEmpty(Layout2TText)) t = Layout2TText;
                            n2 = false;
                        }
                        else if (lcid == (MAIN_LAYOUT1 & 0xffff))
                        {
                            bg = Layout1Back_temp;
                            fg = Layout1Fore_temp;
                            fn = Layout1Font_temp;
                            if (!string.IsNullOrEmpty(Layout1TText)) t = Layout1TText;
                            n2 = false;
                        }

                        Debug.WriteLine("D" + n2);
                        var b = new Bitmap(16, 16);
                        var g = Graphics.FromImage(b);
                        var sf = new StringFormat {LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center};
                        g.Clear(bg);
                        g.TextRenderingHint = TextRenderingHint.SystemDefault;
                        g.DrawString(t, fn, new SolidBrush(fg), new PointF(8, 8), sf);
                        g.Dispose();
                        ITEXT = b;
                        if (n2 && LDCaretUseFlags_temp) ITEXT = FLAG;
                    }

                    latestSwitch = flagname;
                }
            }
            else
            {
                Logging.Log("Layout id was [" + lcid + "].", 2);
            }
        }

        /// <summary>
        ///     Changes tray icon image to country flag based on current layout.
        /// </summary>
        private void ChangeTrayIconToFlag(bool force = false)
        {
            uint lcid = 0;
            if (OneLayout)
                lcid = GlobalLayout;
            else if (!UseJKL || KMHook.JKLERR)
                lcid = Locales.GetCurrentLocale();
            else
                lcid = currentLayout;
            Debug.WriteLine("refresh?" + (lastTrayFlagLayout != lcid || force));
            if (lastTrayFlagLayout != lcid || force)
            {
                RefreshFLAG(force);
                var b = FLAG;
                if (TrayText) b = ITEXT;
                Icon flagicon;
                if (FLAG != null)
                    flagicon = Icon.FromHandle(b.GetHicon());
                else
                    flagicon = Resources.MahouTrayHD;
                icon.trIcon.Icon = flagicon;
                if (!force)
                    WinAPI.DestroyIcon(flagicon.Handle);
                lastTrayFlagLayout = lcid;
            }
        }

        /// <summary>
        ///     Initializes UI language.
        /// </summary>
        public static void InitLanguage()
        {
            MMain.Language = MMain.MyConfs.Read("Appearence", "Language");
            if (MMain.Language == "English")
                MMain.Lang = Languages.English;
            else if (MMain.Language == "Русский")
                MMain.Lang = Languages.Russian;
        }

        /// <summary>
        ///     Initializes language tooltips.
        /// </summary>
        public void InitLangDisplays(bool destroyonly = false)
        {
            if (mouseLangDisplay != null)
                mouseLangDisplay.Dispose();
            if (caretLangDisplay != null)
                caretLangDisplay.Dispose();
            if (destroyonly || !ENABLED) return;
            if (LDForMouse)
            {
                mouseLangDisplay = new LangDisplay();
                mouseLangDisplay.mouseDisplay = true;
                mouseLangDisplay.DisplayFlag = LDMouseUseFlags_temp;
            }

            if (LDForCaret)
            {
                caretLangDisplay = new LangDisplay();
                caretLangDisplay.caretDisplay = true;
                caretLangDisplay.DisplayFlag = LDCaretUseFlags_temp;
                caretLangDisplay.AddOwnedForm(mouseLangDisplay); //Prevents flickering when tooltips are one on another 
            }
        }

        /// <summary>
        ///     Initializes tray icon.
        /// </summary>
        private void InitializeTrayIcon()
        {
            icon = new TrayIcon(MMain.MyConfs.ReadBool("Functions", "TrayIconVisible"));
            icon.Exit += (_, __) => ExitProgram();
            icon.ShowHide += (_, __) => ToggleVisibility();
            icon.EnaDisable += (_, __) => ToggleMahou();
            icon.Restart += (_, __) => Restart();
            icon.ConvertClip += (_, __) =>
            {
                var t = KMHook.ConvertText(KMHook.GetClipboard(2));
                KMHook.RestoreClipBoard(t);
            };
            icon.TransliClip += (_, __) =>
            {
                var t = KMHook.TransliterateText(KMHook.GetClipboard(2));
                KMHook.RestoreClipBoard(t);
            };
        }

        /// <summary>
        ///     Initializes list boxes.
        /// </summary>
        private void InitializeListBoxes()
        {
            lsb_Hotkeys.SelectedIndex = 0;
            lsb_LangTTAppearenceForList.SelectedIndex = 0;
        }

        private void InitializeLangPanel()
        {
            if (_langPanel == null)
                _langPanel = new LangPanel();
            int x = -7, y = -7;
            try
            {
                var getXY = new Regex(@"(X|Y)(\d+)");
                var xy = MMain.MyConfs.Read("LangPanel", "Position");
                var _xy = getXY.Matches(xy);
                Logging.Log("XY: " + _xy[0].Groups[2].Value + " / " + _xy[1].Groups[2].Value);
                x = Convert.ToInt32(_xy[0].Groups[2].Value);
                y = Convert.ToInt32(_xy[1].Groups[2].Value);
            }
            catch (Exception e)
            {
                Logging.Log("Erro during latest x/y position get, details:\r\n" + e.Message + "\r\n" + e.StackTrace, 1);
            }

            _langPanel.Location = new Point(x, y);
            _langPanel.UpdateApperence(LangPanelBackColor, LangPanelForeColor, LangPanelTransparency, LangPanelFont);
            if (LangPanelDisplay)
            {
                _langPanel.ShowInactiveTopmost();
                langPanelRefresh.Start();
            }
        }

        /// <summary>
        ///     Initializes all hotkeys.
        /// </summary>
        public void InitializeHotkeys()
        {
            Mainhk = new Hotkey(Mainhk_tempEnabled, (uint) Mainhk_tempKey, Hotkey.GetMods(Mainhk_tempMods), (int) Hotkey.HKID.ToggleVisibility, Mainhk_tempDouble);
            HKCLast = new Hotkey(HKCLast_tempEnabled, (uint) HKCLast_tempKey, Hotkey.GetMods(HKCLast_tempMods), (int) Hotkey.HKID.ConvertLastWord, HKCLast_tempDouble);
            HKCSelection = new Hotkey(HKCSelection_tempEnabled, (uint) HKCSelection_tempKey, Hotkey.GetMods(HKCSelection_tempMods), (int) Hotkey.HKID.ConvertSelection, HKCSelection_tempDouble);
            HKCLine = new Hotkey(HKCLine_tempEnabled, (uint) HKCLine_tempKey,
                Hotkey.GetMods(HKCLine_tempMods), (int) Hotkey.HKID.ConvertLastLine, HKCLine_tempDouble);
            HKSymIgn = new Hotkey(HKSymIgn_tempEnabled, (uint) HKSymIgn_tempKey,
                Hotkey.GetMods(HKSymIgn_tempMods), (int) Hotkey.HKID.ToggleSymbolIgnoreMode, HKSymIgn_tempDouble);
            HKConMorWor = new Hotkey(HKConMorWor_tempEnabled, (uint) HKConMorWor_tempKey,
                Hotkey.GetMods(HKConMorWor_tempMods), (int) Hotkey.HKID.ConvertMultipleWords, HKConMorWor_tempDouble);
            HKTitleCase = new Hotkey(HKTitleCase_tempEnabled, (uint) HKTitleCase_tempKey,
                Hotkey.GetMods(HKTitleCase_tempMods), (int) Hotkey.HKID.ToTitleSelection, HKTitleCase_tempDouble);
            HKRandomCase = new Hotkey(HKRandomCase_tempEnabled, (uint) HKRandomCase_tempKey,
                Hotkey.GetMods(HKRandomCase_tempMods), (int) Hotkey.HKID.ToRandomSelection, HKRandomCase_tempDouble);
            HKSwapCase = new Hotkey(HKSwapCase_tempEnabled, (uint) HKSwapCase_tempKey,
                Hotkey.GetMods(HKSwapCase_tempMods), (int) Hotkey.HKID.ToSwapSelection, HKSwapCase_tempDouble);
            HKUpperCase = new Hotkey(HKToUpper_tempEnabled, (uint) HKToUpper_tempKey,
                Hotkey.GetMods(HKToUpper_tempMods), (int) Hotkey.HKID.ToUpperSelection, HKToUpper_tempDouble);
            HKLowerCase = new Hotkey(HKToLower_tempEnabled, (uint) HKToLower_tempKey,
                Hotkey.GetMods(HKToLower_tempMods), (int) Hotkey.HKID.ToLowerSelection, HKToLower_tempDouble);
            HKTransliteration = new Hotkey(HKTransliteration_tempEnabled, (uint) HKTransliteration_tempKey,
                Hotkey.GetMods(HKTransliteration_tempMods), (int) Hotkey.HKID.TransliterateSelection, HKTransliteration_tempDouble);
            ExitHk = new Hotkey(ExitHk_tempEnabled, (uint) ExitHk_tempKey,
                Hotkey.GetMods(ExitHk_tempMods), (int) Hotkey.HKID.Exit, ExitHk_tempDouble);
            HKRestart = new Hotkey(HKRestart_tempEnabled, (uint) HKRestart_tempKey,
                Hotkey.GetMods(HKRestart_tempMods), (int) Hotkey.HKID.Restart, false);
            HKToggleLP = new Hotkey(HKToggleLangPanel_tempEnabled, (uint) HKToggleLangPanel_tempKey,
                Hotkey.GetMods(HKToggleLangPanel_tempMods), (int) Hotkey.HKID.ToggleLangPanel, HKToggleLangPanel_tempDouble);
            HKShowST = new Hotkey(HKShowSelectionTranslate_tempEnabled, (uint) HKShowSelectionTranslate_tempKey,
                Hotkey.GetMods(HKShowSelectionTranslate_tempMods), (int) Hotkey.HKID.ShowSelectionTranslation, HKShowSelectionTranslate_tempDouble);
            HKToggleMahou = new Hotkey(HKToggleMahou_tempEnabled, (uint) HKToggleMahou_tempKey,
                Hotkey.GetMods(HKToggleMahou_tempMods), (int) Hotkey.HKID.ToggleMahou, HKToggleMahou_tempDouble);
            Logging.Log("Hotkeys initialized.");
        }

        public bool HasHotkey(Hotkey thishk)
        {
            if (thishk == Mainhk ||
                thishk == HKCLast ||
                thishk == HKCSelection ||
                thishk == HKCLine ||
                thishk == HKSymIgn ||
                thishk == HKConMorWor ||
                thishk == HKTitleCase ||
                thishk == HKRandomCase ||
                thishk == HKSwapCase ||
                thishk == HKUpperCase ||
                thishk == HKLowerCase ||
                thishk == HKTransliteration ||
                thishk == ExitHk ||
                thishk == HKRestart ||
                thishk == HKToggleLP ||
                thishk == HKShowST)
                return true;
            foreach (var hk in SpecificSwitchHotkeys)
                if (thishk == hk)
                    return true;
            return false;
        }

        private void WrongColorLog(string color, string err = "")
        {
            Logging.Log("[" + color + "]is not color, it is skipped." + (!string.IsNullOrEmpty(err) ? "\r\nError: " + err : ""), 2);
        }

        private void WrongFontLog(string font, string err = "")
        {
            Logging.Log("[" + font + "]is not font, or its missing from system, it is skipped." + (!string.IsNullOrEmpty(err) ? "\r\nError: " + err : ""), 2);
        }

        /// <summary>
        ///     Initializes timers.
        /// </summary>
        private void InitializeTimers()
        {
            #region Reset Timers

            crtCheck.Stop();
            ICheck.Stop();
            ScrlCheck.Stop();
            res.Stop();
            resC.Stop();
            old.Stop();
            capsCheck.Stop();
            flagsCheck.Stop();
            persistentLayout1Check.Stop();
            persistentLayout2Check.Stop();
            langPanelRefresh.Stop();
            ICheck = new Timer();
            crtCheck = new Timer();
            ScrlCheck = new Timer();
            res = new Timer();
            resC = new Timer();
            capsCheck = new Timer();
            flagsCheck = new Timer();
            persistentLayout1Check = new Timer();
            persistentLayout2Check = new Timer();
            langPanelRefresh = new Timer();
            old = new Timer();
            KMHook.doublekey = new Timer();

            #endregion

            crtCheck.Interval = MMain.MyConfs.ReadInt("Timings", "LangTooltipForCaretRefreshRate");
            crtCheck.Tick += (_, __) => UpdateCaredLD();
            ICheck.Interval = MMain.MyConfs.ReadInt("Timings", "LangTooltipForMouseRefreshRate");
            ICheck.Tick += (_, __) => UpdateMouseLD();
            res.Interval = (ICheck.Interval + crtCheck.Interval) * 2;
            resC.Interval = (ICheck.Interval + crtCheck.Interval) * 2;
            res.Tick += (_, __) =>
            {
                onepass = true;
                mouseLangDisplay.HideWnd();
                if (LDUseWindowsMessages)
                    UpdateMouseLD();
                res.Stop();
            };
            resC.Tick += (_, __) =>
            {
                onepassC = true;
                caretLangDisplay.HideWnd();
                if (LDUseWindowsMessages)
                    UpdateCaredLD();
                resC.Stop();
            };
            ScrlCheck.Interval = MMain.MyConfs.ReadInt("Timings", "ScrollLockStateRefreshRate");
            ScrlCheck.Tick += (_, __) =>
            {
                if (ScrollTip && !KMHook.alt)
                    KMHook.DoSelf(() =>
                    {
                        var l = currentLayout;
                        if (!UseJKL || KMHook.JKLERR)
                            l = Locales.GetCurrentLocale();
                        if (l == MAIN_LAYOUT1)
                        {
                            if (!IsKeyLocked(Keys.Scroll))
                            {
                                // Turn on 
                                KMHook.KeybdEvent(Keys.Scroll, 0);
                                KMHook.KeybdEvent(Keys.Scroll, 2);
                            }
                        }
                        else
                        {
                            if (IsKeyLocked(Keys.Scroll))
                            {
                                KMHook.KeybdEvent(Keys.Scroll, 0);
                                KMHook.KeybdEvent(Keys.Scroll, 2);
                            }
                        }
                    });
            };
            capsCheck.Tick += (_, __) => KMHook.DoSelf(() =>
            {
                if (IsKeyLocked(Keys.CapsLock))
                {
                    KMHook.KeybdEvent(Keys.CapsLock, 0);
                    KMHook.KeybdEvent(Keys.CapsLock, 2);
                }
            });
            capsCheck.Interval = MMain.MyConfs.ReadInt("Timings", "CapsLockDisableRefreshRate");
            KMHook.doublekey.Tick += (_, __) =>
            {
                if (hklOK)
                    hklOK = false;
                if (hksOK)
                    hksOK = false;
                if (hklineOK)
                    hklineOK = false;
                if (hkSIOK)
                    hkSIOK = false;
                if (hkShWndOK)
                    hkShWndOK = false;
                if (hkExitOK)
                    hkExitOK = false;
                if (hkcwdsOK)
                    hkcwdsOK = false;
                if (hksTRCOK)
                    hksTRCOK = false;
                if (hksTrslOK)
                    hksTrslOK = false;
                if (hksTTCOK)
                    hksTTCOK = false;
                if (hksTSCOK)
                    hksTSCOK = false;
                if (hkUcOK)
                    hkUcOK = false;
                if (hklcOK)
                    hklcOK = false;
                if (hkShowTSOK)
                    hkShowTSOK = false;
                if (hkToggleMahouOK)
                    hkToggleMahouOK = false;
                KMHook.doublekey.Stop();
            };
            flagsCheck.Interval = MMain.MyConfs.ReadInt("Timings", "FlagsInTrayRefreshRate");
            flagsCheck.Tick += (_, __) => RefreshAllIcons();
            titlebar = RectangleToScreen(ClientRectangle).Top - Top;
            animate.Interval = 2500;
            tmr.Interval = 3000;
            old.Interval = 7500;
            old.Tick += (_, __) => { isold = !isold; };
            persistentLayout1Check.Interval = MMain.MyConfs.ReadInt("PersistentLayout", "Layout1CheckInterval");
            persistentLayout2Check.Interval = MMain.MyConfs.ReadInt("PersistentLayout", "Layout2CheckInterval");
            persistentLayout1Check.Tick += (_, __) => PersistentLayoutCheck(PersistentLayout1Processes, MAIN_LAYOUT1);
            persistentLayout2Check.Tick += (_, __) => PersistentLayoutCheck(PersistentLayout2Processes, MAIN_LAYOUT2);
            langPanelRefresh.Interval = LangPanelRefreshRate;
            langPanelRefresh.Tick += (_, __) =>
            {
                uint loc = 0;
                try
                {
                    if (!OneLayout)
                        loc = currentLayout == 0 ? Locales.GetCurrentLocale() : currentLayout;
                    else
                        loc = GlobalLayout;
                    if (loc > 0 && loc != lastLayoutLangPanel)
                    {
                        RefreshFLAG();
                        _langPanel.ChangeLayout(FLAG, MMain.Locales[Array.FindIndex(MMain.Locales, l => l.uId == loc)].Lang);
                        lastLayoutLangPanel = loc;
                    }
                }
                catch (Exception e)
                {
                    Logging.Log("Error in LangPanel Refresh, loc: " + loc + ",  details:\r\n" + e.Message + "\r\n" + e.StackTrace);
                }
            };
            InitLangDisplays();
            ToggleTimers();
        }

        public void UpdateMouseLD()
        {
            if (LDForMouseOnChange)
            {
                var cLuid = Locales.GetCurrentLocale();
                if (UseJKL && !KMHook.JKLERR)
                    cLuid = currentLayout;
                if (onepass)
                {
                    latestL = cLuid;
                    onepass = false;
                }

                if (latestL != cLuid)
                {
                    latestL = cLuid;
                    mouseLangDisplay.ShowInactiveTopmost();
                    res.Start();
                }
            }
            else
            {
                if ((ICheckings.IsICursor() || MouseTTAlways) && !mouseLangDisplay.Empty)
                    mouseLangDisplay.ShowInactiveTopmost();
                else
                    mouseLangDisplay.HideWnd();
            }

            if (mouseLangDisplay.Visible)
            {
                mouseLangDisplay.Location = new Point(Cursor.Position.X + LDMouseX_Pos_temp, Cursor.Position.Y + LDMouseY_Pos_temp);
                mouseLangDisplay.RefreshLang();
            }
        }

        public void UpdateCaredLD()
        {
            var crtOnly = new Point(0, 0);
            var curCrtPos = CaretPos.GetCaretPointToScreen(out crtOnly);
            uint cLuid = 0;
            var notTwo = false;
            if (LDForCaretOnChange || DiffAppearenceForLayouts)
            {
                cLuid = Locales.GetCurrentLocale();
                if (UseJKL && !KMHook.JKLERR)
                    cLuid = currentLayout;
            }

            if (LDForCaretOnChange && cLuid != 0)
            {
                if (onepassC)
                {
//					Debug.WriteLine("OPC!" + cLuid);
                    latestCL = cLuid;
                    onepassC = false;
                }

//				Debug.WriteLine("L"+latestCL+", CL"+cLuid);
                if (latestCL != cLuid)
                {
                    latestCL = cLuid;
                    caretLangDisplay.ShowInactiveTopmost();
                    resC.Start();
                }
            }
            else
            {
                if (KMHook.ff_chr_wheeled || caretLangDisplay.Empty)
                    caretLangDisplay.HideWnd();
                else if (crtOnly.X != 77777 && crtOnly.Y != 77777) // 77777x77777 is null/none point
                    caretLangDisplay.ShowInactiveTopmost();
            }

            if (caretLangDisplay.Visible)
            {
                var LDC_np = new Point(0, 0);
                if (DiffAppearenceForLayouts && cLuid != 0)
                {
                    if (cLuid == MAIN_LAYOUT1)
                        LDC_np = new Point(curCrtPos.X + Layout1X_Pos_temp,
                            curCrtPos.Y + Layout1Y_Pos_temp);
                    else if (cLuid == MAIN_LAYOUT2)
                        LDC_np = new Point(curCrtPos.X + Layout2X_Pos_temp,
                            curCrtPos.Y + Layout2Y_Pos_temp);
                    else notTwo = true;
                }
                else
                {
                    notTwo = true;
                }

                if (notTwo)
                    LDC_np = new Point(curCrtPos.X + LDCaretX_Pos_temp,
                        curCrtPos.Y + LDCaretY_Pos_temp);
                caretLangDisplay.RefreshLang();
                if (LDC_lp != LDC_np)
                    caretLangDisplay.Location = LDC_np;
                LDC_lp = LDC_np;
            }
        }

        public void UpdateLDs()
        {
            if (LDUseWindowsMessages)
            {
                if (LDForCaret)
                    UpdateCaredLD();
                if (LDForMouse)
                    UpdateMouseLD();
            }
        }

        public void PersistentLayoutCheck(string ProcessNames, uint Layout, string ProcName = "")
        {
            try
            {
                var actProcName = "";
                var plh = PERSISTENT_LAYOUT1_HWNDs;
                var nplh = NOT_PERSISTENT_LAYOUT1_HWNDs;
                if (Layout == MAIN_LAYOUT2)
                {
                    plh = PERSISTENT_LAYOUT2_HWNDs;
                    nplh = NOT_PERSISTENT_LAYOUT2_HWNDs;
                }

                var hwnd = WinAPI.GetForegroundWindow();
                if (nplh.Contains(hwnd))
                {
                    Logging.Log("Already known hwnd which shouldn't have persistent layout.");
                    return;
                }

                if (!plh.Contains(hwnd))
                {
                    if (!string.IsNullOrEmpty(ProcName))
                        actProcName = ProcName;
                    else
                        actProcName = Locales.ActiveWindowProcess().ProcessName;
                    actProcName = actProcName.ToLower().Replace(" ", "_") + ".exe";
                    Logging.Log("Checking active window's process name: [" + actProcName + "] with processes: [" + ProcessNames + "], for layout: [" + Layout + "].");
                    if (ProcessNames.ToLower().Replace(Environment.NewLine, " ").Contains(actProcName))
                    {
                        SetPersistentLayout(Layout);
                        plh.Add(hwnd);
                    }
                    else
                    {
                        nplh.Add(hwnd);
                    }
                }
                else
                {
                    Logging.Log("Already known hwnd which needs to have persistent layout, setting layout " + Layout);
                    SetPersistentLayout(Layout);
                }
            }
            catch (Exception e)
            {
                Logging.Log("Exception in Persistent layout(" + Layout + ") check, error messages & stack:\r\n" + e.Message + "+\r\n" + e.StackTrace, 1);
            }
        }

        private void SetPersistentLayout(uint layout)
        {
            var CurrentLayout = Locales.GetCurrentLocale();
            Logging.Log("Checking current layout: [" + CurrentLayout + "] with selected persistent layout: [" + layout + "].");
            if (CurrentLayout != layout)
            {
                KMHook.ChangeToLayout(Locales.ActiveWindow(), layout);
                Logging.Log("Layout was different, changing to: [" + layout + "].");
            }
        }

        /// <summary>
        ///     Toggles timers state.
        /// </summary>
        public void ToggleTimers()
        {
            if (!Configs.fine || !ENABLED) return;
            if (!LDUseWindowsMessages)
            {
                if (LDForMouse)
                    ICheck.Start();
                else
                    ICheck.Dispose();
                if (LDForCaret)
                    crtCheck.Start();
                else
                    crtCheck.Dispose();
            }

            if (MMain.MyConfs.ReadBool("Functions", "ScrollTip"))
                ScrlCheck.Start();
            else
                ScrlCheck.Dispose();
            if (MMain.MyConfs.ReadBool("Functions", "CapsLockTimer"))
                capsCheck.Start();
            else
                capsCheck.Dispose();
            if ((MMain.MyConfs.ReadBool("Functions", "TrayFlags") || MMain.MyConfs.ReadBool("Functions", "TrayText")) && TrayIconVisible)
                flagsCheck.Start();
            else
                flagsCheck.Dispose();
            if (!PersistentLayoutOnWindowChange)
            {
                if (PersistentLayoutForLayout1)
                    persistentLayout1Check.Start();
                else
                    persistentLayout1Check.Dispose();
                if (PersistentLayoutForLayout2)
                    persistentLayout2Check.Start();
                else
                    persistentLayout2Check.Dispose();
            }

            if (LangPanelDisplay && !langPanelRefresh.Enabled)
                langPanelRefresh.Start();
            else
                langPanelRefresh.Dispose();
        }

        private void AutoStartTask(bool deleteonly = false)
        {
            var xml = @"<?xml version=""1.0"" encoding=""UTF-16""?>
<Task version=""1.2"" xmlns=""http://schemas.microsoft.com/windows/2004/02/mit/task"">
  <RegistrationInfo>
    <Date>2017-08-16T15:11:10.596</Date>
    <Author>Kirin\BladeMight</Author>
    <Description>Starts Mahou with highest priveleges.</Description>
  </RegistrationInfo>
  <Triggers>
    <LogonTrigger>
      <Enabled>true</Enabled>
    </LogonTrigger>
    <BootTrigger>
      <Enabled>true</Enabled>
    </BootTrigger>
  </Triggers>
  <Principals>
    <Principal id=""Author"">
      <UserId>" + Environment.UserDomainName + "\\" + Environment.UserName + @"</UserId>
      <LogonType>InteractiveToken</LogonType>
      <RunLevel>HighestAvailable</RunLevel>
    </Principal>
  </Principals>
  <Settings>
    <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
    <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
    <StopIfGoingOnBatteries>true</StopIfGoingOnBatteries>
    <AllowHardTerminate>true</AllowHardTerminate>
    <StartWhenAvailable>true</StartWhenAvailable>
    <RunOnlyIfNetworkAvailable>false</RunOnlyIfNetworkAvailable>
    <IdleSettings>
      <StopOnIdleEnd>true</StopOnIdleEnd>
      <RestartOnIdle>false</RestartOnIdle>
    </IdleSettings>
    <AllowStartOnDemand>true</AllowStartOnDemand>
    <Enabled>true</Enabled>
    <Hidden>false</Hidden>
    <RunOnlyIfIdle>false</RunOnlyIfIdle>
    <WakeToRun>false</WakeToRun>
    <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>
    <Priority>7</Priority>
  </Settings>
  <Actions Context=""Author"">
    <Exec>
      <Command>" + Assembly.GetExecutingAssembly().Location + @"</Command>
    </Exec>
  </Actions>
</Task>";
            var xml_path = Path.Combine(Path.GetTempPath(), "MahouStartup+.xml");
            Task.Factory.StartNew(() => File.WriteAllText(xml_path, xml)).Wait();
            var pif = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = "/delete /TN MahouAutostart+ /f",
                CreateNoWindow = true,
                Verb = "runas"
            };
            Process.Start(pif).WaitForExit();
            if (!deleteonly)
            {
                pif.Arguments = "/create /xml \"" + xml_path + "\" /TN MahouAutostart+";
                Process.Start(pif).WaitForExit();
            }

            File.Delete(xml_path);
        }

        public static void SoundPlay()
        {
            if (SoundEnabled)
            {
                var sp = new SoundPlayer(new MemoryStream(Resources.snd));
                if (UseCustomSound)
                    if (File.Exists(CustomSound))
                        sp = new SoundPlayer(CustomSound);
                sp.Play();
            }
        }

        public static void Sound2Play()
        {
            if (SoundEnabled)
            {
                var sp2 = new SoundPlayer(new MemoryStream(Resources.snd2));
                if (UseCustomSound2)
                    if (File.Exists(CustomSound2))
                        sp2 = new SoundPlayer(CustomSound2);
                sp2.Play();
            }
        }

        public string SelectGetWavFile()
        {
            var fp = "";
            var ofd = new OpenFileDialog();
            ofd.DefaultExt = ".wav";
            ofd.Filter = "Wave sound|*.wav";
            ofd.Multiselect = false;
            if (ofd.ShowDialog() == DialogResult.OK) fp = ofd.FileName;
            ofd.Dispose();
            return fp;
        }

        /// <summary>
        ///     Creates startup shortcut/task v3.0+v2.0.
        /// </summary>
        private void CreateAutoStart()
        {
            if (AutoStartAsAdmin)
            {
                AutoStartRemove(true);
                AutoStartTask();
//				if (AutoStartExist(false))
//					AutoStartRemove(false);
                Logging.Log("Startup task created.");
            }
            else
            {
                AutoStartRemove(false);
                var exelocation = Assembly.GetExecutingAssembly().Location;
                var shortcutLocation = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                    "Mahou.lnk");
                if (File.Exists(shortcutLocation))
                    return;
                var t = Type.GetTypeFromCLSID(new Guid("72C24DD5-D70A-438B-8A42-98424B88AFB8")); //Windows Script Host Shell Object
                dynamic shell = Activator.CreateInstance(t);
                try
                {
                    var lnk = shell.CreateShortcut(shortcutLocation);
                    try
                    {
                        lnk.TargetPath = exelocation;
                        lnk.WorkingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                        lnk.IconLocation = exelocation + ", 0";
                        lnk.Description = "Mahou - Magic layout switcher";
                        lnk.Save();
                    }
                    finally
                    {
                        Marshal.FinalReleaseComObject(lnk);
                    }
                }
                finally
                {
                    Marshal.FinalReleaseComObject(shell);
                }

//				if (AutoStartExist(true))
//					AutoStartRemove(true);
                Logging.Log("Startup shortcut created.");
            }
        }

        private bool AutoStartExist(bool admin)
        {
            if (admin)
            {
                var pif = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/c schtasks.exe /query /TN MahouAutoStart+ >NUL 2>&1 && echo Y",
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                        UseShellExecute = false
                    }
                };
                pif.Start();
                while (!pif.StandardOutput.EndOfStream)
                {
                    var l = pif.StandardOutput.ReadLine();
                    if (l.Contains("Y"))
                    {
                        Debug.WriteLine("Task exist!");
                        pif.StartInfo.Arguments = "/c schtasks.exe /query /TN MahouAutoStart+ /fo LIST /v";
                        pif.Start();
                        Debug.WriteLine("Checking task path...");
                        while (!pif.StandardOutput.EndOfStream)
                        {
                            l = pif.StandardOutput.ReadLine();
                            if (l.Contains(Assembly.GetExecutingAssembly().Location))
                            {
                                Debug.WriteLine("Task path OK! in: " + l);
                                return true;
                            }
                        }
                    }
                }

                Debug.WriteLine("Task path wrong!");
                return false;
            }

            var lnk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Mahou.lnk");
            var actual = false;
            if (File.Exists(lnk))
            {
                var t = Type.GetTypeFromCLSID(new Guid("72C24DD5-D70A-438B-8A42-98424B88AFB8")); //Windows Script Host Shell Object
                dynamic shell = Activator.CreateInstance(t);
                try
                {
                    var slnk = shell.CreateShortcut(lnk);
                    try
                    {
                        if (slnk.TargetPath == Assembly.GetExecutingAssembly().Location)
                            actual = true;
                    }
                    finally
                    {
                        Marshal.FinalReleaseComObject(slnk);
                    }
                }
                finally
                {
                    Marshal.FinalReleaseComObject(shell);
                }
            }

            Debug.WriteLine("Actual: " + actual);
            return actual;
        }

        /// <summary>
        ///     Remove startup with Windows.
        /// </summary>
        private void AutoStartRemove(bool admin)
        {
            if (admin)
            {
                AutoStartTask(true);
                Logging.Log("Startup task removed.");
            }
            else
            {
                if (File.Exists(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                    "Mahou.lnk")))
                    File.Delete(Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                        "Mahou.lnk"));
                Logging.Log("Startup shortcut removed.");
            }
        }

        private void PreExit(bool hideicon = true, int noglobal = 0)
        {
            if (UseJKL && !KMHook.JKLERR)
                jklXHidServ.Destroy();
            if (hideicon)
                icon.Hide();
            if (RemapCapslockAsF18)
                LLHook.UnSet();
            MMain.Mahou.UnregisterHotkeys(noglobal);
            MMain.Rif.RegisterRawInputDevices(IntPtr.Zero, WinAPI.RawInputDeviceFlags.Remove);
            if (tmr != null)
            {
                tmr.Stop();
                tmr.Dispose();
            }

            if (old != null)
            {
                old.Stop();
                old.Dispose();
            }

            if (res != null)
            {
                res.Stop();
                res.Dispose();
            }

            if (resC != null)
            {
                resC.Stop();
                resC.Dispose();
            }

            if (stimer != null)
            {
                stimer.Stop();
                stimer.Dispose();
            }

            if (ICheck != null)
            {
                ICheck.Stop();
                ICheck.Dispose();
            }

            if (animate != null)
            {
                animate.Stop();
                animate.Dispose();
            }

            if (crtCheck != null)
            {
                crtCheck.Stop();
                crtCheck.Dispose();
            }

            if (ScrlCheck != null)
            {
                ScrlCheck.Stop();
                ScrlCheck.Dispose();
            }

            if (capsCheck != null)
            {
                capsCheck.Stop();
                capsCheck.Dispose();
            }

            if (flagsCheck != null)
            {
                flagsCheck.Stop();
                flagsCheck.Dispose();
            }

            if (langPanelRefresh != null)
            {
                langPanelRefresh.Stop();
                langPanelRefresh.Dispose();
            }

            if (persistentLayout1Check != null)
            {
                persistentLayout1Check.Stop();
                persistentLayout1Check.Dispose();
            }

            if (persistentLayout2Check != null)
            {
                persistentLayout2Check.Stop();
                persistentLayout2Check.Dispose();
            }
        }

        /// <summary>Exits Mahou.</summary>
        public void ExitProgram()
        {
            Logging.Log("Exit by user demand.");
            PreExit();
            if (!KMHook.IsNotWin7())
                Thread.Sleep(100);
            var piKill = new ProcessStartInfo
            {
                FileName = "taskkill",
                Arguments = "/IM Mahou.exe /F",
                WindowStyle = ProcessWindowStyle.Hidden
            };
            Process.Start(piKill);
            piKill.Arguments = "/PID " + Process.GetCurrentProcess().Id + " /F";
            Process.Start(piKill);
        }

        /// <summary>
        ///     Registers keys 1->9 & 0 on keyboard as hotkey to be used as word count selector for Convert Multiple Words Count.
        /// </summary>
        private void PrepareConvertMoreWords()
        {
            for (var i = 0; i <= 9; i++) //				Debug.WriteLine("Registering +"+(Keys)(((int)Keys.D0)+i) + " i " +(i+100));
                WinAPI.RegisterHotKey(Handle, 100 + i, WinAPI.MOD_NO_REPEAT, (int) Keys.D0 + i);
            KMHook.waitfornum = true;
        }

        /// <summary>
        ///     Unregisters keys 1->9 & 0 on keyboard that were used for Convert Multiple Words Count function.
        /// </summary>
        public void FlushConvertMoreWords()
        {
            for (var i = 100; i <= 109; i++) //				Debug.WriteLine("Unregistering +"+i);
                WinAPI.UnregisterHotKey(Handle, i);
            KMHook.waitfornum = false;
        }

        /// <summary>
        ///     Unregisters Mahou hotkeys.
        /// </summary>
        /// <param name="noglobal">
        ///     Keeps *global hotkeys*(the one's that goes after TransliterateSelection in HKID enum) alive if
        ///     true.
        /// </param>
        public void UnregisterHotkeys(int noglobal = 0)
        {
            foreach (int id in Enum.GetValues(typeof(Hotkey.HKID)))
            {
                if (noglobal == 1 && id > (int) Hotkey.HKID.TransliterateSelection) break;
                if (noglobal == 2 && id > (int) Hotkey.HKID.ShowSelectionTranslation) break;
                WinAPI.UnregisterHotKey(Handle, id);
            }

            for (var id = 201; id <= 300; id++)
            {
                SpecificSwitchHotkeys.Clear();
                WinAPI.UnregisterHotKey(Handle, id);
            }
        }

        public void _regHK(IntPtr h, int id, uint mod, int key)
        {
            var rk = key;
            if (RemapCapslockAsF18)
                if (key == (int) Keys.Capital)
                    rk = (int) Keys.F18;
            WinAPI.RegisterHotKey(h, id, mod, rk);
        }

        public void RegisterHotkeys()
        {
            if (ENABLED)
            {
                if (HKCLast_tempEnabled)
                    _regHK(Handle, (int) Hotkey.HKID.ConvertLastWord, WinAPI.MOD_NO_REPEAT + Hotkey.GetMods(HKCLast_tempMods), HKCLast_tempKey);
                if (HKCSelection_tempEnabled)
                    _regHK(Handle, (int) Hotkey.HKID.ConvertSelection,
                        WinAPI.MOD_NO_REPEAT + Hotkey.GetMods(HKCSelection_tempMods), HKCSelection_tempKey);
                if (HKCLine_tempEnabled)
                    _regHK(Handle, (int) Hotkey.HKID.ConvertLastLine,
                        WinAPI.MOD_NO_REPEAT + Hotkey.GetMods(HKCLine_tempMods), HKCLine_tempKey);
                if (HKConMorWor_tempEnabled)
                    _regHK(Handle, (int) Hotkey.HKID.ConvertMultipleWords,
                        WinAPI.MOD_NO_REPEAT + Hotkey.GetMods(HKConMorWor_tempMods), HKConMorWor_tempKey);
                if (HKTitleCase_tempEnabled)
                    _regHK(Handle, (int) Hotkey.HKID.ToTitleSelection,
                        WinAPI.MOD_NO_REPEAT + Hotkey.GetMods(HKTitleCase_tempMods), HKTitleCase_tempKey);
                if (HKSwapCase_tempEnabled)
                    _regHK(Handle, (int) Hotkey.HKID.ToSwapSelection,
                        WinAPI.MOD_NO_REPEAT + Hotkey.GetMods(HKSwapCase_tempMods), HKSwapCase_tempKey);
                if (HKToUpper_tempEnabled)
                    _regHK(Handle, (int) Hotkey.HKID.ToUpperSelection,
                        WinAPI.MOD_NO_REPEAT + Hotkey.GetMods(HKToUpper_tempMods), HKToUpper_tempKey);
                if (HKToLower_tempEnabled)
                    _regHK(Handle, (int) Hotkey.HKID.ToLowerSelection,
                        WinAPI.MOD_NO_REPEAT + Hotkey.GetMods(HKToLower_tempMods), HKToLower_tempKey);
                if (HKRandomCase_tempEnabled)
                    _regHK(Handle, (int) Hotkey.HKID.ToRandomSelection,
                        WinAPI.MOD_NO_REPEAT + Hotkey.GetMods(HKRandomCase_tempMods), HKRandomCase_tempKey);
                if (HKTransliteration_tempEnabled)
                    _regHK(Handle, (int) Hotkey.HKID.TransliterateSelection,
                        WinAPI.MOD_NO_REPEAT + Hotkey.GetMods(HKTransliteration_tempMods), HKTransliteration_tempKey);
                if (HKSymIgn_tempEnabled)
                    _regHK(Handle, (int) Hotkey.HKID.ToggleSymbolIgnoreMode,
                        WinAPI.MOD_NO_REPEAT + Hotkey.GetMods(HKSymIgn_tempMods), HKSymIgn_tempKey);
                if (Mainhk_tempEnabled)
                    _regHK(Handle, (int) Hotkey.HKID.ToggleVisibility,
                        WinAPI.MOD_NO_REPEAT + Hotkey.GetMods(Mainhk_tempMods), Mainhk_tempKey);
                if (ExitHk_tempEnabled)
                    _regHK(Handle, (int) Hotkey.HKID.Exit,
                        WinAPI.MOD_NO_REPEAT + Hotkey.GetMods(ExitHk_tempMods), ExitHk_tempKey);
                if (HKRestart_tempEnabled)
                    _regHK(Handle, (int) Hotkey.HKID.Restart,
                        WinAPI.MOD_NO_REPEAT + Hotkey.GetMods(HKRestart_tempMods), HKRestart_tempKey);
                if (HKToggleLangPanel_tempEnabled)
                    _regHK(Handle, (int) Hotkey.HKID.ToggleLangPanel,
                        WinAPI.MOD_NO_REPEAT + Hotkey.GetMods(HKToggleLangPanel_tempMods), HKToggleLangPanel_tempKey);
                if (HKShowSelectionTranslate_tempEnabled)
                    _regHK(Handle, (int) Hotkey.HKID.ShowSelectionTranslation,
                        WinAPI.MOD_NO_REPEAT + Hotkey.GetMods(HKShowSelectionTranslate_tempMods), HKShowSelectionTranslate_tempKey);
                if (!ChangeLayouByKey) return;
                for (var i = 1; i != SpecKeySetCount + 1; i++)
                {
                    var key = 0;
                    if (!string.IsNullOrEmpty(SpecKeySetsValues["txt_key" + i + "_key"]))
                    {
                        key = int.Parse(SpecKeySetsValues["txt_key" + i + "_key"]);
                        var mods = Hotkey.GetMods(SpecKeySetsValues["txt_key" + i + "_mods"]);
                        if (key == (int) Keys.CapsLock && RemapCapslockAsF18)
                            key = (int) Keys.F18;
                        var hk = new Hotkey(true, (uint) key, mods, 200 + i);
                        SpecificSwitchHotkeys.Add(hk);
                        WinAPI.RegisterHotKey(Handle, 200 + i, mods, key);
                    }
                }
            }

            if (HKToggleMahou_tempEnabled)
                WinAPI.RegisterHotKey(Handle, (int) Hotkey.HKID.ToggleMahou,
                    WinAPI.MOD_NO_REPEAT + Hotkey.GetMods(HKToggleMahou_tempMods), HKToggleMahou_tempKey);
        }

        /// <summary>
        ///     Converts some special keys to readable string.
        /// </summary>
        /// <param name="k">Key to be converted.</param>
        /// <param name="oninit">On initialize.</param>
        /// <returns>string</returns>
        public string Remake(Keys k, bool oninit = false, bool Double = false)
        {
            if (Double || oninit)
                switch (k)
                {
                    case Keys.ShiftKey:
                        return "Shift";
                    case Keys.Menu:
                        return "Alt";
                    case Keys.ControlKey:
                        return "Control";
                }
            switch (k)
            {
                case Keys.Cancel:
                    return k.ToString().Replace("Cancel", "Pause");
                case Keys.Scroll:
                    return k.ToString().Replace("Cancel", "Scroll");
                case Keys.ShiftKey:
                case Keys.Menu:
                case Keys.ControlKey:
                case Keys.LWin:
                case Keys.RWin:
                    return "";
                case Keys.D0:
                case Keys.D1:
                case Keys.D2:
                case Keys.D3:
                case Keys.D4:
                case Keys.D5:
                case Keys.D6:
                case Keys.D7:
                case Keys.D8:
                case Keys.D9:
                    return k.ToString().Replace("D", "");
                case Keys.Capital:
                    return "Caps Lock";
                default:
                    return k.ToString();
            }
        }

        /// <summary>
        ///     Converts Oem Keys string to readable string.
        /// </summary>
        /// <param name="inpt">String with oem keys.</param>
        /// <returns>string</returns>
        public string OemReadable(string inpt)
        {
            return inpt
                .Replace("Oemtilde", "`")
                .Replace("OemMinus", "-")
                .Replace("Oemplus", "+")
                .Replace("OemBackslash", "\\")
                .Replace("Oem5", "\\")
                .Replace("OemOpenBrackets", "{")
                .Replace("OemCloseBrackets", "}")
                .Replace("Oem6", "}")
                .Replace("OemSemicolon", ";")
                .Replace("Oem1", ";")
                .Replace("OemQuotes", "\"")
                .Replace("Oem7", "\"")
                .Replace("OemPeriod", ".")
                .Replace("Oemcomma", ",")
                .Replace("OemQuestion", "/");
        }

        /// <summary>
        ///     Calls UpdateLangDisplayControls() which updates lang display controls based on selected [layout appearence].
        /// </summary>
        private void UpdateLangDisplayControlsSwitch()
        {
            if (lsb_LangTTAppearenceForList.SelectedIndex < 4)
            {
                if (lsb_LangTTAppearenceForList.SelectedIndex > 1)
                    txt_LangTTText.Enabled = lbl_LangTTText.Enabled = false;
                else
                    txt_LangTTText.Enabled = lbl_LangTTText.Enabled = true;
                chk_LangTTTransparentColor.Enabled = btn_LangTTFont.Enabled = btn_LangTTForegroundColor.Enabled =
                    btn_LangTTBackgroundColor.Enabled = lbl_LangTTBackgroundColor.Enabled = lbl_LangTTForegroundColor.Enabled = true;
                grb_LangTTSize.Text = MMain.Lang[Languages.Element.LDSize];
                lbl_LangTTWidth.Text = MMain.Lang[Languages.Element.LDWidth];
                lbl_LangTTHeight.Text = MMain.Lang[Languages.Element.LDHeight];
            }
            else
            {
                chk_LangTTTransparentColor.Enabled = btn_LangTTFont.Enabled = btn_LangTTForegroundColor.Enabled =
                    btn_LangTTBackgroundColor.Enabled = lbl_LangTTBackgroundColor.Enabled = lbl_LangTTForegroundColor.Enabled = false;
                grb_LangTTSize.Text = MMain.Lang[Languages.Element.LDPosition];
                lbl_LangTTWidth.Text = MMain.Lang[Languages.Element.MCDSTopIndent];
                lbl_LangTTHeight.Text = MMain.Lang[Languages.Element.MCDSBottomIndent];
            }

            lbl_LangTTWidth.Text += ":";
            lbl_LangTTHeight.Text += ":";
            switch (lsb_LangTTAppearenceForList.SelectedIndex)
            {
                case 0:
                    UpdateLangDisplayControls(Layout1Fore_temp, Layout1Back_temp, Layout1TransparentBack_temp,
                        Layout1Font_temp, Layout1X_Pos_temp, Layout1Y_Pos_temp, Layout1Width_temp,
                        Layout1Height_temp, Layout1TText);
                    chk_LangTTUpperArrow.Visible = chk_LangTTUseFlags.Visible = false;
                    lbl_LangTTText.Visible = txt_LangTTText.Visible = true;
                    break;
                case 1:
                    UpdateLangDisplayControls(Layout2Fore_temp, Layout2Back_temp, Layout2TransparentBack_temp,
                        Layout2Font_temp, Layout2X_Pos_temp, Layout2Y_Pos_temp, Layout2Width_temp,
                        Layout2Height_temp, Layout2TText);
                    chk_LangTTUpperArrow.Visible = chk_LangTTUseFlags.Visible = false;
                    lbl_LangTTText.Visible = txt_LangTTText.Visible = true;
                    break;
                case 2:
                    UpdateLangDisplayControls(LDMouseFore_temp, LDMouseBack_temp, LDMouseTransparentBack_temp,
                        LDMouseFont_temp, LDMouseX_Pos_temp, LDMouseY_Pos_temp, LDMouseWidth_temp,
                        LDMouseHeight_temp, "", LDMouseUseFlags_temp, mouseLTUpperArrow);
                    chk_LangTTUpperArrow.Visible = chk_LangTTUseFlags.Visible = true;
                    lbl_LangTTText.Visible = txt_LangTTText.Visible = false;
                    break;
                case 3:
                    UpdateLangDisplayControls(LDCaretFore_temp, LDCaretBack_temp, LDCaretTransparentBack_temp,
                        LDCaretFont_temp, LDCaretX_Pos_temp, LDCaretY_Pos_temp, LDCaretWidth_temp,
                        LDCaretHeight_temp, "", LDCaretUseFlags_temp, caretLTUpperArrow);
                    chk_LangTTUpperArrow.Visible = chk_LangTTUseFlags.Visible = true;
                    lbl_LangTTText.Visible = txt_LangTTText.Visible = false;
                    break;
                case 4:
                    UpdateLangDisplayControls(LDCaretFore_temp, LDCaretBack_temp, LDCaretTransparentBack_temp,
                        LDCaretFont_temp, MCDS_Xpos_temp, MCDS_Ypos_temp, MCDS_TopIndent_temp,
                        MCDS_BottomIndent_temp);
                    chk_LangTTUpperArrow.Visible = chk_LangTTUseFlags.Visible = false;
                    lbl_LangTTText.Visible = txt_LangTTText.Visible = false;
                    break;
            }
        }

        /// <summary>
        ///     Updates lang display controls.
        /// </summary>
        /// <param name="FGcolor">Foreground color.</param>
        /// <param name="BGColor">Background color.</param>
        /// <param name="TransparentBG">Transparent background color.</param>
        /// <param name="font">Font.</param>
        /// <param name="posX">Position x.</param>
        /// <param name="posY">Position y.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        private void UpdateLangDisplayControls(Color FGcolor, Color BGColor, bool TransparentBG, Font font,
            int posX, int posY, int width, int height, string TTText = "", bool UseFlags = false, bool arrow = false)
        {
            btn_LangTTForegroundColor.BackColor = FGcolor;
            btn_LangTTBackgroundColor.BackColor = BGColor;
            chk_LangTTTransparentColor.Checked = TransparentBG;
            btn_LangTTFont.Font = font;
            nud_LangTTPositionX.Value = posX;
            nud_LangTTPositionY.Value = posY;
            nud_LangTTWidth.Value = width;
            nud_LangTTHeight.Value = height;
            txt_LangTTText.Text = TTText;
            chk_LangTTUseFlags.Checked = UseFlags;
            chk_LangTTUpperArrow.Checked = arrow;
        }

        /// <summary>
        ///     Updates Lang Display temporary variables based on selected [layout appearence].
        /// </summary>
        private void UpdateLangDisplayTemps()
        {
            switch (lsb_LangTTAppearenceForList.SelectedIndex)
            {
                case 0:
                    Layout1Fore_temp = btn_LangTTForegroundColor.BackColor;
                    Layout1Back_temp = btn_LangTTBackgroundColor.BackColor;
                    Layout1Font_temp = btn_LangTTFont.Font;
                    Layout1X_Pos_temp = (int) nud_LangTTPositionX.Value;
                    Layout1Y_Pos_temp = (int) nud_LangTTPositionY.Value;
                    Layout1Width_temp = (int) nud_LangTTWidth.Value;
                    Layout1Height_temp = (int) nud_LangTTHeight.Value;
                    Layout1TransparentBack_temp = chk_LangTTTransparentColor.Checked;
                    Layout1TText = txt_LangTTText.Text;
                    break;
                case 1:
                    Layout2Fore_temp = btn_LangTTForegroundColor.BackColor;
                    Layout2Back_temp = btn_LangTTBackgroundColor.BackColor;
                    Layout2Font_temp = btn_LangTTFont.Font;
                    Layout2X_Pos_temp = (int) nud_LangTTPositionX.Value;
                    Layout2Y_Pos_temp = (int) nud_LangTTPositionY.Value;
                    Layout2Width_temp = (int) nud_LangTTWidth.Value;
                    Layout2Height_temp = (int) nud_LangTTHeight.Value;
                    Layout2TransparentBack_temp = chk_LangTTTransparentColor.Checked;
                    Layout2TText = txt_LangTTText.Text;
                    break;
                case 2:
                    LDMouseFore_temp = btn_LangTTForegroundColor.BackColor;
                    LDMouseBack_temp = btn_LangTTBackgroundColor.BackColor;
                    LDMouseFont_temp = btn_LangTTFont.Font;
                    LDMouseX_Pos_temp = (int) nud_LangTTPositionX.Value;
                    LDMouseY_Pos_temp = (int) nud_LangTTPositionY.Value;
                    LDMouseWidth_temp = (int) nud_LangTTWidth.Value;
                    LDMouseHeight_temp = (int) nud_LangTTHeight.Value;
                    LDMouseUseFlags_temp = chk_LangTTUseFlags.Checked;
                    mouseLTUpperArrow = chk_LangTTUpperArrow.Checked;
                    LDMouseTransparentBack_temp = chk_LangTTTransparentColor.Checked;
                    break;
                case 3:
                    LDCaretFore_temp = btn_LangTTForegroundColor.BackColor;
                    LDCaretBack_temp = btn_LangTTBackgroundColor.BackColor;
                    LDCaretFont_temp = btn_LangTTFont.Font;
                    LDCaretX_Pos_temp = (int) nud_LangTTPositionX.Value;
                    LDCaretY_Pos_temp = (int) nud_LangTTPositionY.Value;
                    LDCaretWidth_temp = (int) nud_LangTTWidth.Value;
                    LDCaretHeight_temp = (int) nud_LangTTHeight.Value;
                    LDCaretUseFlags_temp = chk_LangTTUseFlags.Checked;
                    caretLTUpperArrow = chk_LangTTUpperArrow.Checked;
                    LDCaretTransparentBack_temp = chk_LangTTTransparentColor.Checked;
                    break;
                case 4:
                    MCDS_Xpos_temp = (int) nud_LangTTPositionX.Value;
                    MCDS_Ypos_temp = (int) nud_LangTTPositionY.Value;
                    MCDS_TopIndent_temp = (int) nud_LangTTWidth.Value;
                    MCDS_BottomIndent_temp = (int) nud_LangTTHeight.Value;
                    break;
            }
        }

        /// <summary>
        ///     Calls UpdateHotkeyControls() which updates hotkey controls based on selected [layout appearence].
        /// </summary>
        private void UpdateHotkeyControlsSwitch()
        {
            chk_DoubleHotkey.Enabled = lsb_Hotkeys.SelectedIndex != 13;
            switch (lsb_Hotkeys.SelectedIndex)
            {
                case 0:
                    UpdateHotkeyControls(Mainhk_tempEnabled, Mainhk_tempDouble, Mainhk_tempMods, Mainhk_tempKey);
                    break;
                case 1:
                    UpdateHotkeyControls(HKCLast_tempEnabled, HKCLast_tempDouble, HKCLast_tempMods, HKCLast_tempKey);
                    break;
                case 2:
                    UpdateHotkeyControls(HKCSelection_tempEnabled, HKCSelection_tempDouble, HKCSelection_tempMods, HKCSelection_tempKey);
                    break;
                case 3:
                    UpdateHotkeyControls(HKCLine_tempEnabled, HKCLine_tempDouble, HKCLine_tempMods, HKCLine_tempKey);
                    break;
                case 4:
                    UpdateHotkeyControls(HKConMorWor_tempEnabled, HKConMorWor_tempDouble, HKConMorWor_tempMods, HKConMorWor_tempKey);
                    break;
                case 5:
                    UpdateHotkeyControls(HKSymIgn_tempEnabled, HKSymIgn_tempDouble, HKSymIgn_tempMods, HKSymIgn_tempKey);
                    break;
                case 6:
                    UpdateHotkeyControls(HKTitleCase_tempEnabled, HKTitleCase_tempDouble, HKTitleCase_tempMods, HKTitleCase_tempKey);
                    break;
                case 7:
                    UpdateHotkeyControls(HKRandomCase_tempEnabled, HKRandomCase_tempDouble, HKRandomCase_tempMods, HKRandomCase_tempKey);
                    break;
                case 8:
                    UpdateHotkeyControls(HKSwapCase_tempEnabled, HKSwapCase_tempDouble, HKSwapCase_tempMods, HKSwapCase_tempKey);
                    break;
                case 9:
                    UpdateHotkeyControls(HKToUpper_tempEnabled, HKToUpper_tempDouble, HKToUpper_tempMods, HKToUpper_tempKey);
                    break;
                case 10:
                    UpdateHotkeyControls(HKToLower_tempEnabled, HKToLower_tempDouble, HKToLower_tempMods, HKToLower_tempKey);
                    break;
                case 11:
                    UpdateHotkeyControls(HKTransliteration_tempEnabled, HKTransliteration_tempDouble, HKTransliteration_tempMods, HKTransliteration_tempKey);
                    break;
                case 12:
                    UpdateHotkeyControls(ExitHk_tempEnabled, ExitHk_tempDouble, ExitHk_tempMods, ExitHk_tempKey);
                    break;
                case 13:
                    UpdateHotkeyControls(HKRestart_tempEnabled, false, HKRestart_tempMods, HKRestart_tempKey);
                    break;
                case 14:
                    UpdateHotkeyControls(HKToggleLangPanel_tempEnabled, HKToggleLangPanel_tempDouble, HKToggleLangPanel_tempMods, HKToggleLangPanel_tempKey);
                    break;
                case 15:
                    UpdateHotkeyControls(HKShowSelectionTranslate_tempEnabled, HKShowSelectionTranslate_tempDouble, HKShowSelectionTranslate_tempMods, HKShowSelectionTranslate_tempKey);
                    break;
                case 16:
                    UpdateHotkeyControls(HKToggleMahou_tempEnabled, HKToggleMahou_tempDouble, HKToggleMahou_tempMods, HKToggleMahou_tempKey);
                    break;
            }
        }

        /// <summary>
        ///     Updates hotkey controls.
        /// </summary>
        private void UpdateHotkeyControls(bool enabled, bool Double, string modifiers, int key)
        {
            chk_HotKeyEnabled.Checked = enabled;
            chk_DoubleHotkey.Checked = Double;
            txt_Hotkey.Text = Regex.Replace(OemReadable(modifiers.Replace(",", " +") +
                                                        " + " + Remake((Keys) key, true, Double)),
                @"Win\s?\+?\s?|\s?\+?\s?None\s?\+?\s?|^[ +]+|\s?\+\s?$", "", RegexOptions.Multiline);
            chk_WinInHotKey.Checked = modifiers.Contains("Win");
            txt_Hotkey_tempKey = key;
            txt_Hotkey_tempModifiers = Regex.Replace(modifiers.Replace("Win", ""), @"^[ +]+", "", RegexOptions.Multiline);
            // Debug.WriteLine(txt_Hotkey_tempModifiers);
        }

        /// <summary>
        ///     Updates Hotkey temporary variables based on selected [layout appearence].
        /// </summary>
        private void UpdateHotkeyTemps()
        {
            switch (lsb_Hotkeys.SelectedIndex)
            {
                case 0:
                    Mainhk_tempEnabled = chk_HotKeyEnabled.Checked;
                    Mainhk_tempDouble = chk_DoubleHotkey.Checked;
                    Mainhk_tempMods = (chk_WinInHotKey.Checked ? "Win + " : "") + txt_Hotkey_tempModifiers;
                    Mainhk_tempKey = txt_Hotkey_tempKey;
                    break;
                case 1:
                    HKCLast_tempEnabled = chk_HotKeyEnabled.Checked;
                    HKCLast_tempDouble = chk_DoubleHotkey.Checked;
                    HKCLast_tempMods = (chk_WinInHotKey.Checked ? "Win + " : "") + txt_Hotkey_tempModifiers;
                    HKCLast_tempKey = txt_Hotkey_tempKey;
                    break;
                case 2:
                    HKCSelection_tempEnabled = chk_HotKeyEnabled.Checked;
                    HKCSelection_tempDouble = chk_DoubleHotkey.Checked;
                    HKCSelection_tempMods = (chk_WinInHotKey.Checked ? "Win + " : "") + txt_Hotkey_tempModifiers;
                    HKCSelection_tempKey = txt_Hotkey_tempKey;
                    break;
                case 3:
                    HKCLine_tempEnabled = chk_HotKeyEnabled.Checked;
                    HKCLine_tempDouble = chk_DoubleHotkey.Checked;
                    HKCLine_tempMods = (chk_WinInHotKey.Checked ? "Win + " : "") + txt_Hotkey_tempModifiers;
                    HKCLine_tempKey = txt_Hotkey_tempKey;
                    break;
                case 4:
                    HKConMorWor_tempEnabled = chk_HotKeyEnabled.Checked;
                    HKConMorWor_tempDouble = chk_DoubleHotkey.Checked;
                    HKConMorWor_tempMods = (chk_WinInHotKey.Checked ? "Win + " : "") + txt_Hotkey_tempModifiers;
                    HKConMorWor_tempKey = txt_Hotkey_tempKey;
                    break;
                case 5:
                    HKSymIgn_tempEnabled = chk_HotKeyEnabled.Checked;
                    HKSymIgn_tempDouble = chk_DoubleHotkey.Checked;
                    HKSymIgn_tempMods = (chk_WinInHotKey.Checked ? "Win + " : "") + txt_Hotkey_tempModifiers;
                    HKSymIgn_tempKey = txt_Hotkey_tempKey;
                    break;
                case 6:
                    HKTitleCase_tempEnabled = chk_HotKeyEnabled.Checked;
                    HKTitleCase_tempDouble = chk_DoubleHotkey.Checked;
                    HKTitleCase_tempMods = (chk_WinInHotKey.Checked ? "Win + " : "") + txt_Hotkey_tempModifiers;
                    HKTitleCase_tempKey = txt_Hotkey_tempKey;
                    break;
                case 7:
                    HKRandomCase_tempEnabled = chk_HotKeyEnabled.Checked;
                    HKRandomCase_tempDouble = chk_DoubleHotkey.Checked;
                    HKRandomCase_tempMods = (chk_WinInHotKey.Checked ? "Win + " : "") + txt_Hotkey_tempModifiers;
                    HKRandomCase_tempKey = txt_Hotkey_tempKey;
                    break;
                case 8:
                    HKSwapCase_tempEnabled = chk_HotKeyEnabled.Checked;
                    HKSwapCase_tempDouble = chk_DoubleHotkey.Checked;
                    HKSwapCase_tempMods = (chk_WinInHotKey.Checked ? "Win + " : "") + txt_Hotkey_tempModifiers;
                    HKSwapCase_tempKey = txt_Hotkey_tempKey;
                    break;
                case 9:
                    HKToUpper_tempEnabled = chk_HotKeyEnabled.Checked;
                    HKToUpper_tempDouble = chk_DoubleHotkey.Checked;
                    HKToUpper_tempMods = (chk_WinInHotKey.Checked ? "Win + " : "") + txt_Hotkey_tempModifiers;
                    HKToUpper_tempKey = txt_Hotkey_tempKey;
                    break;
                case 10:
                    HKToLower_tempEnabled = chk_HotKeyEnabled.Checked;
                    HKToLower_tempDouble = chk_DoubleHotkey.Checked;
                    HKToLower_tempMods = (chk_WinInHotKey.Checked ? "Win + " : "") + txt_Hotkey_tempModifiers;
                    HKToLower_tempKey = txt_Hotkey_tempKey;
                    break;
                case 11:
                    HKTransliteration_tempEnabled = chk_HotKeyEnabled.Checked;
                    HKTransliteration_tempDouble = chk_DoubleHotkey.Checked;
                    HKTransliteration_tempMods = (chk_WinInHotKey.Checked ? "Win + " : "") + txt_Hotkey_tempModifiers;
                    HKTransliteration_tempKey = txt_Hotkey_tempKey;
                    break;
                case 12:
                    ExitHk_tempEnabled = chk_HotKeyEnabled.Checked;
                    ExitHk_tempDouble = chk_DoubleHotkey.Checked;
                    ExitHk_tempMods = (chk_WinInHotKey.Checked ? "Win + " : "") + txt_Hotkey_tempModifiers;
                    ExitHk_tempKey = txt_Hotkey_tempKey;
                    break;
                case 13:
                    HKRestart_tempEnabled = chk_HotKeyEnabled.Checked;
                    HKRestart_tempMods = (chk_WinInHotKey.Checked ? "Win + " : "") + txt_Hotkey_tempModifiers;
                    HKRestart_tempKey = txt_Hotkey_tempKey;
                    break;
                case 14:
                    HKToggleLangPanel_tempEnabled = chk_HotKeyEnabled.Checked;
                    HKToggleLangPanel_tempDouble = chk_DoubleHotkey.Checked;
                    HKToggleLangPanel_tempMods = (chk_WinInHotKey.Checked ? "Win + " : "") + txt_Hotkey_tempModifiers;
                    HKToggleLangPanel_tempKey = txt_Hotkey_tempKey;
                    break;
                case 15:
                    HKShowSelectionTranslate_tempEnabled = chk_HotKeyEnabled.Checked;
                    HKShowSelectionTranslate_tempDouble = chk_DoubleHotkey.Checked;
                    HKShowSelectionTranslate_tempMods = (chk_WinInHotKey.Checked ? "Win + " : "") + txt_Hotkey_tempModifiers;
                    HKShowSelectionTranslate_tempKey = txt_Hotkey_tempKey;
                    break;
                case 16:
                    HKToggleMahou_tempEnabled = chk_HotKeyEnabled.Checked;
                    HKToggleMahou_tempDouble = chk_DoubleHotkey.Checked;
                    HKToggleMahou_tempMods = (chk_WinInHotKey.Checked ? "Win + " : "") + txt_Hotkey_tempModifiers;
                    HKToggleMahou_tempKey = txt_Hotkey_tempKey;
                    break;
            }
        }

        /// <summary>
        ///     Returns selected hotkey's Double bool.
        /// </summary>
        private bool GetSelectedHotkeyDoubleTemp()
        {
            switch (lsb_Hotkeys.SelectedIndex)
            {
                case 0:
                    return Mainhk_tempDouble;
                case 1:
                    return HKCLast_tempDouble;
                case 2:
                    return HKCSelection_tempDouble;
                case 3:
                    return HKCLine_tempDouble;
                case 4:
                    return HKConMorWor_tempDouble;
                case 5:
                    return HKSymIgn_tempDouble;
                case 6:
                    return HKTitleCase_tempDouble;
                case 7:
                    return HKRandomCase_tempDouble;
                case 8:
                    return HKSwapCase_tempDouble;
                case 9:
                    return HKToUpper_tempDouble;
                case 10:
                    return HKToLower_tempDouble;
                case 11:
                    return HKTransliteration_tempDouble;
                case 12:
                    return ExitHk_tempDouble;
                case 13:
                    return false;
            }

            return false;
        }

        private void UpdateSetControls(int setIndex, int keyCode, string modifiers)
        {
            var _set = pan_KeySets.Controls["set_" + setIndex];
            _set.Controls["txt_key" + setIndex].Text = Regex.Replace(OemReadable(modifiers.Replace(",", " +") +
                                                                                 " + " + Remake((Keys) keyCode, true, false)),
                @"Win\s?\+?\s?|\s?\+?\s?None\s?\+?\s?|^[ +]+|\s?\+\s?$", "", RegexOptions.Multiline);
            (_set.Controls["chk_win" + setIndex] as CheckBox).Checked = modifiers.Contains("Win");
        }

        private void DeleteOrMove(string file)
        {
            try
            {
                File.Delete(file);
                Logging.Log("Deleting file [" + file + "] succeeded.");
            }
            catch
            {
                Logging.Log("Deleting file [" + file + "] not succeeded, trying to move.", 2);
                try
                {
                    var name = Guid.NewGuid().ToString("n").Substring(0, 8);
                    var d = Path.GetDirectoryName(file);
                    var trash = Path.Combine(d, "trash");
                    if (!Directory.Exists(trash))
                        Directory.CreateDirectory(trash);
                    var f = Path.Combine(trash, name);
                    File.Move(file, f);
                }
                catch (Exception e)
                {
                    Logging.Log("Unexpected error happened when trying to move file, details:\r\n" + e.Message + "\r\n" + e.StackTrace, 1);
                }
            }
        }

        private void DeleteTrash()
        {
            var trash = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "trash");
            try
            {
                if (Directory.Exists(trash))
                {
                    Directory.Delete(trash, true);
                    Logging.Log("Deleting [" + trash + "] directory succeeded.");
                }
                else
                {
                    Logging.Log("No trash found. (" + trash + ")");
                }
            }
            catch (Exception e)
            {
                Logging.Log("Error deleting trash directory, details:\r\n" + e.Message + "\r\n" + e.StackTrace, 2);
            }
        }

        private void DeleteOldJKL()
        {
            if (jklXHidServ.jklExist())
            {
                var jkl = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "jkl");
                if (jklXHidServ.jklFEX[0])
                    DeleteOrMove(jkl + ".exe");
                if (jklXHidServ.jklFEX[1])
                    DeleteOrMove(jkl + ".dll");
                if (jklXHidServ.jklFEX[2])
                    DeleteOrMove(jkl + "x86.exe");
                if (jklXHidServ.jklFEX[3])
                    DeleteOrMove(jkl + "x86.dll");
            }
        }

        #region Updates functions
       

        private string getASD_RemoteSize(bool InZip = false)
        {
            try
            {
                if (InZip)
                {
                    var data = getResponce("https://github.com/BladeMight/Mahou/releases/latest-commit");
                    if (!string.IsNullOrEmpty(data))
                    {
                        var siz = Regex.Match(data, "<small class=\"text-gray float-right\">(.+)</small>").Groups[1].Value;
                        Logging.Log("Remote size of AS_dict: " + siz);
                        return siz;
                    }

                    throw new Exception(MMain.Lang[Languages.Element.NetError]);
                }

                var request = (HttpWebRequest) WebRequest.Create("https://raw.githubusercontent.com/BladeMight/Mahou/master/AS_dict.txt");
                request.Method = "HEAD";
                request.AllowAutoRedirect = false;
                using (var r = (HttpWebResponse) request.GetResponse())
                {
                    var type = " B";
                    var D = Convert.ToDouble(r.ContentLength);
                    if (D / 1024 != 0 && D / 1024 >= 1)
                    {
                        D /= 1024;
                        type = " KB";
                        if (D / 1024 != 0 && D / 1024 >= 1)
                        {
                            D /= 1024;
                            type = " MB";
                        }
                    }

                    return D.ToString("0.00") + type;
                }
            }
            catch (Exception e)
            {
                Logging.Log("Getting remote size of AS_dict failed, details: " + e.Message, 1);
                return MMain.Lang[Languages.Element.Error];
            }
        }

        private string getResponce(string url)
        {
            try
            {
                var request = (HttpWebRequest) WebRequest.Create(url);
                // For proxy
                request.ServicePoint.SetTcpKeepAlive(true, 5000, 1000);
                var response = (HttpWebResponse) request.GetResponse();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var data = new StreamReader(response.GetResponseStream(), true).ReadToEnd();
                    response.Close();
                    Logging.Log("Responce of url [" + url + "] succeded.");
                    return data;
                }

                response.Close();
            }
            catch (Exception e)
            {
                Logging.Log("Responce of url [" + url + "] done with error, message:\r\n" + e.Message + e.StackTrace, 1);
            }

            return null;
        }

        private void btn_UpdateAutoSwitchDictionary_Click(object sender, EventArgs e)
        {
            var resp = "";
            if (check_ASD_size)
            {
                var size = getASD_RemoteSize(Dowload_ASD_InZip);
                if (size == MMain.Lang[Languages.Element.Error])
                {
                    btn_UpdateAutoSwitchDictionary.ForeColor = Color.OrangeRed;
                    btn_UpdateAutoSwitchDictionary.Text = MMain.Lang[Languages.Element.Error];
                    tmr.Tick += (o, oo) =>
                    {
                        btn_UpdateAutoSwitchDictionary.Text = MMain.Lang[Languages.Element.AutoSwitchUpdateDictionary];
                        btn_UpdateAutoSwitchDictionary.ForeColor = Color.FromKnownColor(KnownColor.ControlText);
                        tmr.Stop();
                    };
                    tmr.Interval = 350;
                    tmr.Start();
                    return;
                }

                check_ASD_size = false;
                var name = "AS_dict.txt";
                if (Dowload_ASD_InZip)
                    name = "AS_dict.zip";
                btn_UpdateAutoSwitchDictionary.Text = name + " " + size + " " + MMain.Lang[Languages.Element.Download] + "?";
                return;
            }

            check_ASD_size = true;
            if (Dowload_ASD_InZip)
            {
                var zip = Path.Combine(Path.GetTempPath(), "AS_dict.zip");
                using (var wc = new WebClient())
                {
                    wc.DownloadFile(new Uri("https://github.com/BladeMight/Mahou/releases/download/latest-commit/AS_dict.zip"), zip);
                    var ExtractASD = @"@ECHO OFF
chcp 65001
ECHO With CreateObject(""Shell.Application"") > ""unzip.vbs""
ECHO    .NameSpace(WScript.Arguments(1)).CopyHere .NameSpace(WScript.Arguments(0)).items, 16 >> ""unzip.vbs""
ECHO End With >> ""unzip.vbs""

CSCRIPT ""unzip.vbs"" """ + zip + @""" """ + Path.GetTempPath() + @"""
DEL """ + zip + @"""
DEL ""unzip.vbs""
DEL ""ExtractASD.cmd""";
                    Logging.Log("Writing extract script.");
                    File.WriteAllText(Path.Combine(Path.GetTempPath(), "ExtractASD.cmd"), ExtractASD);
                    var piExtractASD = new ProcessStartInfo {FileName = "ExtractASD.cmd", WorkingDirectory = Path.GetTempPath(), WindowStyle = ProcessWindowStyle.Hidden};
                    Logging.Log("Starting extract script.");
                    Process.Start(piExtractASD).WaitForExit();
                    resp = File.ReadAllText(Path.Combine(Path.GetTempPath(), "AS_dict.txt"));
                    File.Delete(Path.Combine(Path.GetTempPath(), "AS_dict.txt"));
                }
            }
            else
            {
                resp = getResponce("https://raw.githubusercontent.com/BladeMight/Mahou/master/AS_dict.txt");
            }

            btn_UpdateAutoSwitchDictionary.Text = MMain.Lang[Languages.Element.Checking];
            var dict = Regex.Replace(resp, "\r?\n", Environment.NewLine);
            tmr.Interval = 300;
            if (dict != null)
            {
                btn_UpdateAutoSwitchDictionary.ForeColor = Color.BlueViolet;
                btn_UpdateAutoSwitchDictionary.Text = "OK";
                tmr.Tick += (o, oo) =>
                {
                    btn_UpdateAutoSwitchDictionary.Text = MMain.Lang[Languages.Element.AutoSwitchUpdateDictionary];
                    btn_UpdateAutoSwitchDictionary.ForeColor = Color.FromKnownColor(KnownColor.ControlText);
                    tmr.Stop();
                };
                tmr.Interval = 350;
                tmr.Start();
                AutoSwitchDictionaryRaw = dict;
                txt_AutoSwitchDictionary.Invoke((MethodInvoker) delegate
                {
                    ChangeAutoSwitchDictionaryTextBox();
                    UpdateSnippetCountLabel(AutoSwitchDictionaryRaw, lbl_AutoSwitchWordsCount, false);
                });
                File.WriteAllText(AS_dictfile, dict, Encoding.UTF8);
            }
            else
            {
                btn_UpdateAutoSwitchDictionary.ForeColor = Color.OrangeRed;
                btn_UpdateAutoSwitchDictionary.Text = MMain.Lang[Languages.Element.Error];
                tmr.Tick += (o, oo) =>
                {
                    btn_UpdateAutoSwitchDictionary.Text = MMain.Lang[Languages.Element.AutoSwitchUpdateDictionary];
                    btn_UpdateAutoSwitchDictionary.ForeColor = Color.FromKnownColor(KnownColor.ControlText);
                    tmr.Stop();
                };
                tmr.Interval = 350;
                tmr.Start();
            }
        }

        private void UpdateSnippetCountLabel(string snippets, Label target, bool isSnip = true)
        {
            var snipc = GetSnippetsCount(snippets);
            target.Text = target.Text.Split(' ')[0] + " " + snipc.Item1 + (snipc.Item2 == Color.Red ? "?" : "") + "(#" + snipc.Item3 + ")";
            target.ForeColor = snipc.Item2;
            if (isSnip)
                SnippetsCount = snipc.Item1;
            else
                AutoSwitchCount = snipc.Item1;
        }

        #endregion

        /// <summary>
        ///     Refreshes language.
        /// </summary>
        private void RefreshLanguage()
        {
            #region Tabs

            tab_functions.Text = MMain.Lang[Languages.Element.tab_Functions];
            tab_layouts.Text = MMain.Lang[Languages.Element.tab_Layouts];
            tab_appearence.Text = MMain.Lang[Languages.Element.tab_Appearence];
            tab_timings.Text = MMain.Lang[Languages.Element.tab_Timings];
            tab_excluded.Text = MMain.Lang[Languages.Element.tab_Excluded];
            tab_snippets.Text = MMain.Lang[Languages.Element.tab_Snippets];
            tab_autoswitch.Text = MMain.Lang[Languages.Element.tab_AutoSwitch];
            tab_hotkeys.Text = MMain.Lang[Languages.Element.tab_Hotkeys];
            tab_LangPanel.Text = MMain.Lang[Languages.Element.tab_LangPanel];
            tab_about.Text = MMain.Lang[Languages.Element.tab_About];
            tab_sounds.Text = MMain.Lang[Languages.Element.tab_Sounds];
            tab_translator.Text = MMain.Lang[Languages.Element.tab_Translator];
            tab_sync.Text = MMain.Lang[Languages.Element.tab_Sync];

            #endregion

            #region Functions

            lnk_plugin.Text = "ST3 " + MMain.Lang[Languages.Element.Plugin];
            chk_OneLayoutWholeWord.Text = MMain.Lang[Languages.Element.OneLayoutWholeWord];
            chk_AutoStart.Text = MMain.Lang[Languages.Element.AutoStart];
            cbb_AutostartType.Items.Clear();
            cbb_AutostartType.Items.AddRange(new[]
            {
                MMain.Lang[Languages.Element.CreateShortcut],
                MMain.Lang[Languages.Element.CreateTask]
            });
            chk_TrayIcon.Text = MMain.Lang[Languages.Element.TrayIcon];
            chk_CSLayoutSwitching.Text = MMain.Lang[Languages.Element.ConvertSelectionLS];
            chk_ReSelect.Text = MMain.Lang[Languages.Element.ReSelect];
            chk_RePress.Text = MMain.Lang[Languages.Element.RePress];
            chk_AddOneSpace.Text = MMain.Lang[Languages.Element.Add1Space];
            chk_Add1NL.Text = MMain.Lang[Languages.Element.Add1NL];
            chk_CSLayoutSwitchingPlus.Text = MMain.Lang[Languages.Element.ConvertSelectionLSPlus];
            chk_HighlightScroll.Text = MMain.Lang[Languages.Element.HighlightScroll];
            chk_StartupUpdatesCheck.Text = MMain.Lang[Languages.Element.UpdatesCheck];
            chk_SilentUpdate.Text = MMain.Lang[Languages.Element.SilentUpdate];
            chk_Logging.Text = MMain.Lang[Languages.Element.Logging];
            chk_CapsLockDTimer.Text = MMain.Lang[Languages.Element.CapsTimer];
            lbl_TrayDislpayType.Text = MMain.Lang[Languages.Element.DisplayInTray];
            chk_BlockHKWithCtrl.Text = MMain.Lang[Languages.Element.BlockCtrlHKs];
            chk_MCDS_support.Text = MMain.Lang[Languages.Element.MCDSSupport];
            chk_GuessKeyCodeFix.Text = MMain.Lang[Languages.Element.GuessKeyCodeFix];
            chk_AppDataConfigs.Text = MMain.Lang[Languages.Element.ConfigsInAppData];
            chk_RemapCapsLockAsF18.Text = MMain.Lang[Languages.Element.RemapCapslockAsF18];
            chk_GetLayoutFromJKL.Text = MMain.Lang[Languages.Element.UseJKL];
            chk_ReadOnlyNA.Text = MMain.Lang[Languages.Element.ReadOnlyNA];
            chk_WriteInputHistory.Text = MMain.Lang[Languages.Element.WriteInputHistory];
            lbl_BackSpaceType.Text = MMain.Lang[Languages.Element.BackSpaceType];
            lnk_OpenLogs.Text = lnk_OpenConfig.Text = lnk_OpenHistory.Text = MMain.Lang[Languages.Element.Open];

            #endregion

            #region Layouts

            chk_SwitchBetweenLayouts.Text = MMain.Lang[Languages.Element.SwitchBetween] + ":";
            chk_EmulateLS.Text = MMain.Lang[Languages.Element.EmulateLS];
            lbl_EmuType.Text = MMain.Lang[Languages.Element.EmulateType];
            chk_SpecificLS.Text = MMain.Lang[Languages.Element.ChangeLayoutBy1Key];
            grb_Layouts.Text = MMain.Lang[Languages.Element.Layouts];
            grb_Keys.Text = MMain.Lang[Languages.Element.Keys];
            chk_OneLayout.Text = MMain.Lang[Languages.Element.OneLayout];
            chk_qwertz.Text = MMain.Lang[Languages.Element.QWERTZ];
            lbl_KeysType.Text = MMain.Lang[Languages.Element.KeysType];
            cbb_SpecKeysType.Items.Clear();
            cbb_SpecKeysType.Items.AddRange(new[] {MMain.Lang[Languages.Element.SelectKeyType], MMain.Lang[Languages.Element.SetHotkeyType]});

            #endregion

            #region Persistent Layout

            chk_ChangeLayoutOnlyOnce.Text = MMain.Lang[Languages.Element.SwitchOnlyOnce];
            chk_OnlyOnWindowChange.Text = MMain.Lang[Languages.Element.SwitchOnlyOnWindowChange];
            tab_persistent.Text = MMain.Lang[Languages.Element.PersistentLayout];
            grb_PersistentLayout1.Text = MMain.Lang[Languages.Element.Layout] + " 1";
            grb_PersistentLayout2.Text = MMain.Lang[Languages.Element.Layout] + " 2";
            chk_PersistentLayout1Active.Text = chk_PersistentLayout2Active.Text = MMain.Lang[Languages.Element.ActivatePLFP];
            lbl_PersistentLayout1Interval.Text = lbl_PersistentLayout2Interval.Text = MMain.Lang[Languages.Element.CheckInterval];

            #endregion

            #region Appearence

            chk_LangTooltipMouse.Text = MMain.Lang[Languages.Element.LDMouseDisplay];
            chk_LangTooltipCaret.Text = MMain.Lang[Languages.Element.LDCaretDisplay];
            chk_MouseTTAlways.Text = MMain.Lang[Languages.Element.Always];
            chk_LangTTCaretOnChange.Text = chk_LangTTMouseOnChange.Text = MMain.Lang[Languages.Element.LDOnlyOnChange];
            lbl_Language.Text = MMain.Lang[Languages.Element.Language];
            chk_LangTTDiffLayoutColors.Text = MMain.Lang[Languages.Element.LDDifferentAppearence];
            grb_LangTTAppearence.Text = MMain.Lang[Languages.Element.LDAppearence];
            btn_LangTTFont.Text = MMain.Lang[Languages.Element.LDFont];
            lbl_LangTTForegroundColor.Text = MMain.Lang[Languages.Element.LDFore];
            lbl_LangTTBackgroundColor.Text = MMain.Lang[Languages.Element.LDBack];
            lbl_LangTTText.Text = MMain.Lang[Languages.Element.LDText];
            grb_LangTTSize.Text = MMain.Lang[Languages.Element.LDSize];
            grb_LangTTPositon.Text = MMain.Lang[Languages.Element.LDPosition];
            lbl_LangTTHeight.Text = MMain.Lang[Languages.Element.LDHeight];
            lbl_LangTTWidth.Text = MMain.Lang[Languages.Element.LDWidth];
            chk_LangTTTransparentColor.Text = MMain.Lang[Languages.Element.LDTransparentBG];
            lsb_LangTTAppearenceForList.Items.Clear();
            lsb_LangTTAppearenceForList.Items.AddRange(new[]
            {
                MMain.Lang[Languages.Element.Layout] + " 1",
                MMain.Lang[Languages.Element.Layout] + " 2",
                MMain.Lang[Languages.Element.LDAroundMouse],
                MMain.Lang[Languages.Element.LDAroundCaret],
                "MCDS"
            });
            chk_LangTTUseFlags.Text = MMain.Lang[Languages.Element.UseFlags];
            chk_LangTTUpperArrow.Text = MMain.Lang[Languages.Element.LDUpperArrow];
            chk_LDMessages.Text = MMain.Lang[Languages.Element.LDUseWinMessages];

            #endregion

            #region Timings

            lbl_LangTTMouseRefreshRate.Text = MMain.Lang[Languages.Element.LDForMouseRefreshRate];
            lbl_LangTTCaretRefreshRate.Text = MMain.Lang[Languages.Element.LDForCaretRefreshRate];
            lbl_DoubleHK2ndPressWaitTime.Text = MMain.Lang[Languages.Element.DoubleHKDelay];
            lbl_FlagTrayRefreshRate.Text = MMain.Lang[Languages.Element.TrayFlagsRefreshRate];
            lbl_ScrollLockRefreshRate.Text = MMain.Lang[Languages.Element.ScrollLockRefreshRate];
            lbl_CapsLockRefreshRate.Text = MMain.Lang[Languages.Element.CapsLockRefreshRate];
            chk_SelectedTextGetMoreTries.Text = MMain.Lang[Languages.Element.MoreTriesToGetSelectedText];
            chk_UseDelayAfterBackspaces.Text = MMain.Lang[Languages.Element.UseDelayAfterBackspaces];

            #endregion

            #region Excluded

            lbl_ExcludedPrograms.Text = MMain.Lang[Languages.Element.ExcludedPrograms];
            chk_Change1KeyL.Text = MMain.Lang[Languages.Element.Change1KeyLayoutInExcluded];

            #endregion

            #region Snippets

            chk_Snippets.Text = MMain.Lang[Languages.Element.SnippetsEnabled];
            chk_SnippetsSpaceAfter.Text = MMain.Lang[Languages.Element.SnippetSpaceAfter];
            chk_SnippetsSwitchToGuessLayout.Text = MMain.Lang[Languages.Element.SnippetSwitchToGuessLayout];
            lbl_SnippetsCount.Text = MMain.Lang[Languages.Element.SnippetsCount];
            lbl_SnippetExpandKey.Text = MMain.Lang[Languages.Element.SnippetsExpandKey];

            #endregion

            #region AutoSwitch

            chk_AutoSwitch.Text = MMain.Lang[Languages.Element.AutoSwitchEnabled];
            chk_AutoSwitchSpaceAfter.Text = MMain.Lang[Languages.Element.AutoSwitchSpaceAfter];
            chk_AutoSwitchSwitchToGuessLayout.Text = MMain.Lang[Languages.Element.AutoSwitchSwitchToGuessLayout];
            btn_UpdateAutoSwitchDictionary.Text = MMain.Lang[Languages.Element.AutoSwitchUpdateDictionary];
            lbl_AutoSwitchDependsOnSnippets.Text = MMain.Lang[Languages.Element.AutoSwitchDependsOnSnippets];
            lbl_AutoSwitchWordsCount.Text = MMain.Lang[Languages.Element.AutoSwitchDictionaryWordsCount];
            chk_DownloadASD_InZip.Text = MMain.Lang[Languages.Element.DownloadAutoSwitchDictionaryInZip];

            #endregion

            #region Hotkeys

            grb_Hotkey.Text = MMain.Lang[Languages.Element.Hotkey];
            chk_HotKeyEnabled.Text = MMain.Lang[Languages.Element.Enabled];
            chk_DoubleHotkey.Text = MMain.Lang[Languages.Element.DoubleHK];
            lsb_Hotkeys.Items.Clear();
            lsb_Hotkeys.Items.AddRange(new[]
            {
                MMain.Lang[Languages.Element.ToggleMainWnd],
                MMain.Lang[Languages.Element.ConvertLast],
                MMain.Lang[Languages.Element.ConvertSelected],
                MMain.Lang[Languages.Element.ConvertLine],
                MMain.Lang[Languages.Element.ConvertWords],
                MMain.Lang[Languages.Element.ToggleSymbolIgnore],
                MMain.Lang[Languages.Element.SelectedToTitleCase],
                MMain.Lang[Languages.Element.SelectedToRandomCase],
                MMain.Lang[Languages.Element.SelectedToSwapCase],
                MMain.Lang[Languages.Element.SelectedToUpperCase],
                MMain.Lang[Languages.Element.SelectedToLowerCase],
                MMain.Lang[Languages.Element.SelectedTransliteration],
                MMain.Lang[Languages.Element.ExitMahou],
                MMain.Lang[Languages.Element.RestartMahou],
                MMain.Lang[Languages.Element.ToggleLangPanel],
                MMain.Lang[Languages.Element.TranslateSelection],
                MMain.Lang[Languages.Element.ToggleMahou]
            });

            #endregion

            #region LangPanel/TranslatePanel

            chk_DisplayLangPanel.Text = MMain.Lang[Languages.Element.DisplayLangPanel];
            lbl_LPRefreshRate.Text = MMain.Lang[Languages.Element.RefreshRate];
            lbl_TrTransparency.Text = lbl_LPTrasparency.Text = MMain.Lang[Languages.Element.Transparency];
            lbl_TrBorderC.Text = lbl_LPBorderColor.Text = MMain.Lang[Languages.Element.BorderColor];
            lbl_TrFG.Text = lbl_LPFore.Text = MMain.Lang[Languages.Element.LDFore];
            lbl_TrBG.Text = lbl_LPBack.Text = MMain.Lang[Languages.Element.LDBack];
            chk_TrUseAccent.Text = chk_LPAeroColor.Text = MMain.Lang[Languages.Element.UseAeroColor];
            lbl_LPFont.Text = MMain.Lang[Languages.Element.LDFont] + ":";
            btn_LPFont.Text = MMain.Lang[Languages.Element.LDFont];
            chk_LPUpperArrow.Text = MMain.Lang[Languages.Element.DisplayUpperArrow];

            #endregion

            #region TranslatePanel

            chk_TrEnable.Text = MMain.Lang[Languages.Element.EnableTranslatePanel];
            chk_TrOnDoubleClick.Text = MMain.Lang[Languages.Element.ShowTranslationOnDoubleClick];
            lbl_TrLanguages.Text = MMain.Lang[Languages.Element.TranslateLanguages];
            lbl_TrTextFont.Text = MMain.Lang[Languages.Element.TextFont];
            lbl_TrTitleFont.Text = MMain.Lang[Languages.Element.TitleFont];
            btn_TrTitleFont.Text = btn_TrTextFont.Text = MMain.Lang[Languages.Element.LDFont];

            #endregion

            #region About

            btn_DebugInfo.Text = MMain.Lang[Languages.Element.DbgInf];
            lnk_Site.Text = MMain.Lang[Languages.Element.Site];
            lnk_Releases.Text = MMain.Lang[Languages.Element.Releases];
            txt_Help.Text = MMain.Lang[Languages.Element.Mahou] + "\r\n" + MMain.Lang[Languages.Element.About];

            #endregion

            #region Sync

            grb_backup.Text = btn_backup.Text = MMain.Lang[Languages.Element.Backup];
            grb_restore.Text = btn_restore.Text = MMain.Lang[Languages.Element.Restore];

            #endregion

            #region Sounds

            chk_EnableSnd.Text = MMain.Lang[Languages.Element.EnableSounds];
            grb_Sound1.Text = MMain.Lang[Languages.Element.Sound] + " #1";
            grb_Sound2.Text = MMain.Lang[Languages.Element.Sound] + " #2";
            grb_SoundOn2.Text = grb_SoundOn.Text = MMain.Lang[Languages.Element.PlaySoundWhen];
            chk_SndAutoSwitch2.Text = chk_SndAutoSwitch.Text = MMain.Lang[Languages.Element.SoundOnAutoSwitch];
            chk_SndSnippets2.Text = chk_SndSnippets.Text = MMain.Lang[Languages.Element.SoundOnSnippets];
            chk_SndLast2.Text = chk_SndLast.Text = MMain.Lang[Languages.Element.SoundOnConvertLast];
            chk_SndLayoutSwitch2.Text = chk_SndLayoutSwitch.Text = MMain.Lang[Languages.Element.SoundOnLayoutSwitching];
            chk_UseCustomSnd2.Text = chk_UseCustomSnd.Text = MMain.Lang[Languages.Element.UseCustomSound];
            btn_SelectSnd2.Text = btn_SelectSnd.Text = MMain.Lang[Languages.Element.Select];

            #endregion

            #region Buttons

            btn_Apply.Text = MMain.Lang[Languages.Element.ButtonApply];
            btn_Cancel.Text = MMain.Lang[Languages.Element.ButtonCancel];
            btn_OK.Text = MMain.Lang[Languages.Element.ButtonOK];

            #endregion

            #region Misc

            icon.RefreshText(MMain.Lang[Languages.Element.Mahou], MMain.Lang[Languages.Element.ShowHide],
                MMain.Lang[Languages.Element.ExitMahou], MMain.Lang[Languages.Element.Enable],
                MMain.Lang[Languages.Element.RestartMahou], MMain.Lang[Languages.Element.Convert],
                MMain.Lang[Languages.Element.Transliterate], MMain.Lang[Languages.Element.Clipboard],
                MMain.Lang[Languages.Element.Latest]);

            #endregion

            Logging.Log("Language changed.");
            SetTooltips();
        }

        #region Textbox + Ctrl+A

        #endregion

        #region Tooltips

        private void SetTooltips()
        {
            HelpMeUnderstand.SetToolTip(chk_CSLayoutSwitching, MMain.Lang[Languages.Element.TT_ConvertSelectionSwitch]);
            HelpMeUnderstand.SetToolTip(chk_ReSelect, MMain.Lang[Languages.Element.TT_ReSelect]);
            HelpMeUnderstand.SetToolTip(chk_RePress, MMain.Lang[Languages.Element.TT_RePress]);
            HelpMeUnderstand.SetToolTip(chk_AddOneSpace, MMain.Lang[Languages.Element.TT_Add1Space]);
            HelpMeUnderstand.SetToolTip(chk_Add1NL, MMain.Lang[Languages.Element.TT_Add1NL]);
            HelpMeUnderstand.SetToolTip(chk_CSLayoutSwitchingPlus, MMain.Lang[Languages.Element.TT_ConvertSelectionSwitchPlus]);
            HelpMeUnderstand.SetToolTip(chk_HighlightScroll, MMain.Lang[Languages.Element.TT_ScrollTip]);
            HelpMeUnderstand.SetToolTip(chk_Logging, MMain.Lang[Languages.Element.TT_Logging]);
            HelpMeUnderstand.SetToolTip(chk_CapsLockDTimer, MMain.Lang[Languages.Element.TT_CapsDis]);
            HelpMeUnderstand.SetToolTip(cbb_TrayDislpayType, MMain.Lang[Languages.Element.TT_TrayDisplayType]);
            HelpMeUnderstand.SetToolTip(lbl_TrayDislpayType, MMain.Lang[Languages.Element.TT_TrayDisplayType]);
            HelpMeUnderstand.SetToolTip(chk_BlockHKWithCtrl, MMain.Lang[Languages.Element.TT_BlockCtrl]);
            HelpMeUnderstand.SetToolTip(chk_MCDS_support, MMain.Lang[Languages.Element.TT_MCDSSupport]);
            HelpMeUnderstand.SetToolTip(chk_OneLayoutWholeWord, MMain.Lang[Languages.Element.TT_OneLayoutWholeWordCS]);
            HelpMeUnderstand.SetToolTip(chk_SwitchBetweenLayouts, MMain.Lang[Languages.Element.TT_SwitchBetween]);
            HelpMeUnderstand.SetToolTip(chk_EmulateLS, MMain.Lang[Languages.Element.TT_EmulateLS]);
            HelpMeUnderstand.SetToolTip(chk_LangTooltipCaret, MMain.Lang[Languages.Element.TT_LDForCaret]);
            HelpMeUnderstand.SetToolTip(chk_LangTooltipMouse, MMain.Lang[Languages.Element.TT_LDForMouse]);
            HelpMeUnderstand.SetToolTip(chk_LangTTCaretOnChange, MMain.Lang[Languages.Element.TT_LDOnlyOnChange]);
            HelpMeUnderstand.SetToolTip(chk_LangTTMouseOnChange, MMain.Lang[Languages.Element.TT_LDOnlyOnChange]);
            HelpMeUnderstand.SetToolTip(txt_LangTTText, MMain.Lang[Languages.Element.TT_LDText]);
            HelpMeUnderstand.SetToolTip(chk_LangTTDiffLayoutColors, MMain.Lang[Languages.Element.TT_LDDifferentAppearence]);
            HelpMeUnderstand.SetToolTip(chk_Snippets, MMain.Lang[Languages.Element.TT_Snippets]);
            HelpMeUnderstand.SetToolTip(lbl_ExcludedPrograms, MMain.Lang[Languages.Element.TT_ExcludedPrograms]);
            HelpMeUnderstand.SetToolTip(txt_ExcludedPrograms, MMain.Lang[Languages.Element.TT_ExcludedPrograms]);
            HelpMeUnderstand.SetToolTip(txt_PersistentLayout1Processes, MMain.Lang[Languages.Element.TT_PersistentLayout]);
            HelpMeUnderstand.SetToolTip(txt_PersistentLayout2Processes, MMain.Lang[Languages.Element.TT_PersistentLayout]);
            HelpMeUnderstand.SetToolTip(chk_OneLayout, MMain.Lang[Languages.Element.TT_OneLayout]);
            HelpMeUnderstand.SetToolTip(chk_qwertz, MMain.Lang[Languages.Element.TT_QWERTZ]);
            HelpMeUnderstand.SetToolTip(chk_Change1KeyL, MMain.Lang[Languages.Element.TT_Change1KeyLayoutInExcluded]);
            HelpMeUnderstand.SetToolTip(chk_SnippetsSwitchToGuessLayout, MMain.Lang[Languages.Element.TT_SnippetsSwitchToGuessLayout]);
            HelpMeUnderstand.SetToolTip(lbl_SnippetsCount, MMain.Lang[Languages.Element.TT_SnippetsCount]);
            HelpMeUnderstand.SetToolTip(lbl_AutoSwitchWordsCount, MMain.Lang[Languages.Element.TT_SnippetsCount]);
            HelpMeUnderstand.SetToolTip(chk_GuessKeyCodeFix, MMain.Lang[Languages.Element.TT_GuessKeyCodeFix]);
            HelpMeUnderstand.SetToolTip(chk_AppDataConfigs, MMain.Lang[Languages.Element.TT_ConfigsInAppData]);
            HelpMeUnderstand.SetToolTip(lbl_KeysType, MMain.Lang[Languages.Element.TT_KeysType]);
            HelpMeUnderstand.SetToolTip(cbb_SpecKeysType, MMain.Lang[Languages.Element.TT_KeysType]);
            HelpMeUnderstand.SetToolTip(lbl_SnippetExpandKey, MMain.Lang[Languages.Element.TT_SnippetExpandKey]);
            HelpMeUnderstand.SetToolTip(cbb_SnippetExpandKeys, MMain.Lang[Languages.Element.TT_SnippetExpandKey]);
            HelpMeUnderstand.SetToolTip(chk_LDMessages, MMain.Lang[Languages.Element.TT_LDUseWinMessages]);
            HelpMeUnderstand.SetToolTip(chk_RemapCapsLockAsF18, MMain.Lang[Languages.Element.TT_RemapCapslockAsF18]);
            HelpMeUnderstand.SetToolTip(chk_OnlyOnWindowChange, MMain.Lang[Languages.Element.TT_SwitchOnlyOnWindowChange]);
            HelpMeUnderstand.SetToolTip(chk_ChangeLayoutOnlyOnce, MMain.Lang[Languages.Element.TT_SwitchOnlyOnce]);
            HelpMeUnderstand.SetToolTip(chk_UseDelayAfterBackspaces, MMain.Lang[Languages.Element.TT_UseDelayAfterBackspaces]);
            HelpMeUnderstand.SetToolTip(chk_GetLayoutFromJKL, MMain.Lang[Languages.Element.TT_UseJKL]);
            HelpMeUnderstand.SetToolTip(chk_ReadOnlyNA, MMain.Lang[Languages.Element.TT_ReadOnlyNA]);
            HelpMeUnderstand.SetToolTip(chk_WriteInputHistory, MMain.Lang[Languages.Element.TT_WriteInputHistory]);
            HelpMeUnderstand.SetToolTip(lnk_OpenLogs, MMain.Lang[Languages.Element.TT_LeftRightMB] + "\n" + Logging.log);
            HelpMeUnderstand.SetToolTip(lnk_OpenHistory, MMain.Lang[Languages.Element.TT_LeftRightMB] + "\n" + Path.Combine(nPath, "history.txt"));
            HelpMeUnderstand.SetToolTip(lnk_OpenConfig, MMain.Lang[Languages.Element.TT_LeftRightMB] + "\n" + Configs.filePath);
        }

        private void HelpMeUnderstandPopup(object sender, PopupEventArgs e)
        {
            HelpMeUnderstand.ToolTipTitle = e.AssociatedControl.Text;
        }

        #endregion

        /// <summary>
        ///     Converts Mahou version string to float.
        /// </summary>
        /// <param name="ver">Mahou version string.</param>
        /// <returns>float</returns>
        public static float flVersion(string ver)
        {
            var justdigs = Regex.Replace(ver, "\\D", "");
            var fl = 0.0f;
            if (justdigs.Length > 2)
            {
                var strfl = justdigs[0] + "." + justdigs.Substring(1);
                float.TryParse(strfl, out fl);
            }

            return fl;
        }

        #endregion

        #region Links

        private void __lopen(string file, string type, bool dir = false)
        {
            var fORd = dir ? Path.GetDirectoryName(file) : file;
            try
            {
                Process.Start(fORd);
            }
            catch (Exception ex)
            {
                Logging.Log("No program to open " + type + ", opening skiped. Details:\r\n" + ex.Message + "\r\n" + ex.StackTrace, 2);
            }
        }

        private void Lnk_OpenHistoryClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            __lopen(Path.Combine(nPath, "history.txt"), "txt", e.Button == MouseButtons.Right);
        }

        private void Lnk_OpenConfigClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            __lopen(Path.Combine(nPath, "Mahou.ini"), "ini", e.Button == MouseButtons.Right);
        }

        private void Lnk_OpenLogsClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            __lopen(Logging.log, "txt", e.Button == MouseButtons.Right);
        }

        private void Lnk_RepositoryLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            __lopen("http://github.com/BladeMight/Mahou", "http");
        }

        private void Lnk_SiteLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            __lopen("http://blademight.github.io/Mahou/", "http");
        }

        private void Lnk_WikiLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            __lopen("http://github.com/BladeMight/Mahou/wiki", "http");
        }

        private void Lnk_ReleasesLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            __lopen("http://github.com/BladeMight/Mahou/releases", "http");
        }

        private void Lnk_EmailLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            __lopen("mailto:BladeMight@gmail.com", "mailto");
        }

        private void Lnk_pluginLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            __lopen("http://github.com/BladeMight/MahouCaretDisplayServer", "http");
        }

        #endregion

        #region Mahou UI controls events

        private void Chk_CheckedChanged(object sender, EventArgs e)
        {
            ToggleDependentControlsEnabledState();
        }

        private void Chk_HKCheckedChanged(object sender, EventArgs e)
        {
            UpdateHKTemps(sender, e);
            ToggleDependentControlsEnabledState();
        }

        private void Chk_AutoStartCheckedChanged(object sender, EventArgs e)
        {
        }

        private void Btn_DebugInfoClick(object sender, EventArgs e)
        {
            try
            {
                var debuginfo = "<details><summary>MAHOU DEBUG INFO</summary>\r\n\r\n";
                debuginfo += "<details><summary>Environment info</summary>\r\n\r\n";
                debuginfo += "\r\n" + "- " + Text;
                debuginfo += "\r\n" + "- OS = [" + Environment.OSVersion + "]";
                debuginfo += "\r\n" + "- x64 = [" + Environment.Is64BitOperatingSystem + "]";
                debuginfo += "\r\n" + "- .Net = [" + Environment.Version + "]";
                debuginfo += "\r\n</details>";
                debuginfo += "\r\n" + "<details><summary>All installed layouts</summary>\r\n\r\n";
                foreach (var l in MMain.Lcnmid) debuginfo += l + "\r\n";
                debuginfo += "\r\n</details>";
                debuginfo += "<details><summary>Mahou.ini</summary>\r\n\r\n```ini\r\n" +
                             Regex.Match(File.ReadAllText(Path.Combine(nPath, "Mahou.ini")), @"(.*?)\[Proxy.+", RegexOptions.Singleline).Groups[1].Value +
                             "```";
                debuginfo += "\r\n</details>";
                if (File.Exists(Path.Combine(nPath, "snippets.txt")))
                    debuginfo += "\r\n" + "<details><summary>Snippets</summary>\r\n\r\n```\r\n" + File.ReadAllText(Path.Combine(nPath, "snippets.txt")) + "\r\n```";
                debuginfo += "\r\n</details>";
                if (Directory.Exists(Path.Combine(nPath, "Flags")))
                {
                    debuginfo += "\r\n" + "<details><summary>Additional flags in Flags directory</summary>\r\n\r\n";
                    foreach (var flg in Directory.GetFiles(Path.Combine(nPath, "Flags"))) debuginfo += "- " + Path.GetFileName(flg) + "\r\n";
                    debuginfo += "\r\n";
                    debuginfo += "\r\n</details>";
                }

                debuginfo += "\r\n</details>";
                Clipboard.SetText(debuginfo);
                var btDgtTxtWas = btn_DebugInfo.Text;
                btn_DebugInfo.Text = MMain.Lang[Languages.Element.DbgInf_Copied];
                tmr.Tick += (_, __) =>
                {
                    btn_DebugInfo.Text = btDgtTxtWas;
                    tmr.Stop();
                };
                tmr.Interval = 2000;
                tmr.Start();
                Logging.Log("Debug info copied.");
            }
            catch (Exception er)
            {
                MessageBox.Show("Error during dgbcopy" + er.StackTrace);
                Logging.Log("Error during DEBUG INFO copy, details:\r\n" + er.Message + "\r\n" + er.StackTrace);
            }
        }

        private void Btn_OKClick(object sender, EventArgs e)
        {
            ToggleVisibility();
            SaveConfigs();
        }

        private void Btn_ApplyClick(object sender, EventArgs e)
        {
            SaveConfigs();
        }

        private void Btn_CancelClick(object sender, EventArgs e)
        {
            ToggleVisibility();
            LoadConfigs();
        }

        private void Cbb_KeySelectedIndexChanged(object sender, EventArgs e)
        {
            cbb_Layout1.Enabled = cbb_Key1.SelectedIndex != 0;
            cbb_Layout2.Enabled = cbb_Key2.SelectedIndex != 0;
            cbb_Layout3.Enabled = cbb_Key3.SelectedIndex != 0;
            cbb_Layout4.Enabled = cbb_Key4.SelectedIndex != 0;
        }

        private void MahouUIFormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                ToggleVisibility();
                LoadConfigs();
            }
        }

        private void Lsb_HotkeysSelectedIndexChanged(object sender, EventArgs e)
        {
            UnregisterHotkeys(1);
            UpdateHotkeyControlsSwitch();
            UpdateHotkeyTemps();
            switch (lsb_Hotkeys.SelectedIndex)
            {
                case 4:
                    lbl_HotkeyHelp.Text = MMain.Lang[Languages.Element.TT_ConvertWords];
                    break;
                case 5:
                    lbl_HotkeyHelp.Text = MMain.Lang[Languages.Element.TT_SymbolIgnore];
                    break;
                case 15:
                    lbl_HotkeyHelp.Text = MMain.Lang[Languages.Element.TT_ShowSelectionTranslationHotkey];
                    break;
                default:
                    lbl_HotkeyHelp.Text = "";
                    break;
            }
        }

        private void Txt_HotkeyKeyDown(object sender, KeyEventArgs e)
        {
            switch (lsb_Hotkeys.SelectedIndex)
            {
                case 0:
                    WinAPI.UnregisterHotKey(Handle, (int) Hotkey.HKID.ToggleVisibility);
                    break;
                case 5:
                    WinAPI.UnregisterHotKey(Handle, (int) Hotkey.HKID.ToggleSymbolIgnoreMode);
                    break;
                case 12:
                    WinAPI.UnregisterHotKey(Handle, (int) Hotkey.HKID.Exit);
                    break;
                case 13:
                    WinAPI.UnregisterHotKey(Handle, (int) Hotkey.HKID.Restart);
                    break;
            }

            txt_Hotkey.Text = OemReadable((e.Modifiers.ToString().Replace(",", " +") + " + " +
                                           Remake(e.KeyCode)).Replace("None + ", ""));
            txt_Hotkey_tempModifiers = e.Modifiers.ToString().Replace(",", " +");
            switch ((int) e.KeyCode)
            {
                case 16:
                case 17:
                case 18:
                    txt_Hotkey_tempKey = 0;
                    break;
                default:
                    txt_Hotkey_tempKey = (int) e.KeyCode;
                    break;
            }

            UpdateHotkeyTemps();
        }

        private void Lsb_LangTTAppearenceForListSelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateLangDisplayControlsSwitch();
            UpdateLangDisplayTemps();
        }

        private void Btn_ColorSelectionClick(object sender, EventArgs e)
        {
            var btn = sender as Button;
            if (clrd.ShowDialog() == DialogResult.OK)
                btn.BackColor = clrd.Color;
            UpdateLangDisplayTemps();
        }

        private void UpdateLDTemps(object sender, EventArgs e)
        {
            UpdateLangDisplayTemps();
        }

        private void UpdateHKTemps(object sender, EventArgs e)
        {
            UpdateHotkeyTemps();
        }

        private void Btn_LangTTFontClick(object sender, EventArgs e)
        {
            Btn_FontSelection(sender, e);
            UpdateLangDisplayTemps();
        }

        private void Btn_FontSelection(object sender, EventArgs e)
        {
            var btn = sender as Button;
            fntd.Font = btn.Font;
            if (fntd.ShowDialog() == DialogResult.OK)
                btn.Font = fntd.Font;
        }

        private void MahouUIDeactivate(object sender, EventArgs e)
        {
            RegisterHotkeys();
        }

        private void MahouUIActivated(object sender, EventArgs e)
        {
            if (tabs.SelectedIndex == tabs.TabPages.IndexOf(tab_hotkeys))
            {
                UnregisterHotkeys(1);
                ScrlCheck.Stop();
                capsCheck.Stop();
            }
            else
            {
                RegisterHotkeys();
                ToggleTimers();
            }

            if (tabs.SelectedIndex == tabs.TabPages.IndexOf(tab_autoswitch))
            {
                if (!SnippetsEnabled)
                {
                    chk_AutoSwitchSpaceAfter.Visible = chk_AutoSwitch.Visible = chk_AutoSwitchSwitchToGuessLayout.Enabled =
                        btn_UpdateAutoSwitchDictionary.Enabled = txt_AutoSwitchDictionary.Enabled = chk_DownloadASD_InZip.Enabled = false;
                    lbl_AutoSwitchDependsOnSnippets.Visible = true;
                }
                else
                {
                    chk_AutoSwitchSpaceAfter.Visible = chk_AutoSwitch.Visible = chk_AutoSwitchSwitchToGuessLayout.Enabled =
                        btn_UpdateAutoSwitchDictionary.Enabled = txt_AutoSwitchDictionary.Enabled = chk_DownloadASD_InZip.Enabled = true;
                    lbl_AutoSwitchDependsOnSnippets.Visible = false;
                    ToggleDependentControlsEnabledState();
                }
            }
        }

        private void Txt_AutoSwitchDictionaryTextChanged(object sender, EventArgs e)
        {
            if (txt_AutoSwitchDictionary.Text.Length < 710000 && !txt_AutoSwitchDictionary.ReadOnly)
                AutoSwitchDictionaryRaw = txt_AutoSwitchDictionary.Text;
            if (!as_checking)
            {
                as_checking = true;
                tmr.Tick += (_, __) =>
                {
                    UpdateSnippetCountLabel(AutoSwitchDictionaryRaw, lbl_AutoSwitchWordsCount, false);
                    as_checking = false;
                    tmr.Dispose();
                    tmr = new Timer();
                };
                tmr.Interval = 1500;
                tmr.Start();
            }
        }

        private void Txt_SnippetsTextChanged(object sender, EventArgs e)
        {
            if (!snip_checking)
            {
                snip_checking = true;
                tmr.Tick += (_, __) =>
                {
                    UpdateSnippetCountLabel(txt_Snippets.Text, lbl_SnippetsCount);
                    snip_checking = false;
                    tmr.Dispose();
                    tmr = new Timer();
                };
                tmr.Interval = 1000;
                tmr.Start();
            }
        }

        private void Chk_DownloadASD_InZipCheckedChanged(object sender, EventArgs e)
        {
            Dowload_ASD_InZip = chk_DownloadASD_InZip.Checked;
            check_ASD_size = true;
        }

        private void Btn_TrAddSetClick(object sender, EventArgs e)
        {
            if (TrSetCount > 98) return;
            var _set = new Panel();
            _set.Width = pan_TrSets.Width * 98 / 100 - 2;
            TrSetCount++;
            _set.Name = "set_" + TrSetCount;
            var top = 1;
            if (TrSetCount > 1)
                top = pan_TrSets.Controls["set_" + (TrSetCount - 1)].Top + 25;
            _set.Height = 23;
            _set.Top = top;
            _set.Left = 1;
            var _baseLeft = pan_TrSets.Width * 2 / 100;
            var lbl_width = 25;
            var lbl_frto_width = 40;
            var cbb_width = 160;
            _set.Controls.Add(new Label {Left = _baseLeft, Name = "lbl_num" + TrSetCount, Width = lbl_width, Text = TrSetCount + ":", Top = 2});
            var fr_lbl = new Label {Left = _baseLeft + lbl_width, Name = "lbl_fr" + TrSetCount, Width = lbl_frto_width, Text = "From:", Top = 2};
            var fr_cbb = new ComboBox {DropDownStyle = ComboBoxStyle.DropDownList, Left = _baseLeft + lbl_width + lbl_frto_width + 9, Name = "cbb_fr" + TrSetCount, Width = cbb_width};
            var to_lbl = new Label {Left = _baseLeft + lbl_width + lbl_frto_width + 49 + cbb_width, Name = "lbl_to" + TrSetCount, Width = lbl_frto_width, Text = "To:", Top = 2};
            var to_cbb = new ComboBox {DropDownStyle = ComboBoxStyle.DropDownList, Left = _baseLeft + lbl_width + lbl_frto_width + 49 + cbb_width + lbl_frto_width + 9, Name = "cbb_to" + TrSetCount, Width = cbb_width};
            fr_cbb.SelectedIndexChanged += Cbb_FrToSelectedIndexChanged;
            to_cbb.SelectedIndexChanged += Cbb_FrToSelectedIndexChanged;
//			cbb.Items.Add(MMain.Lang[Languages.Element.SwitchBetween]);
            fr_cbb.Items.AddRange(TranslatePanel.GTLangs);
            to_cbb.Items.AddRange(TranslatePanel.GTLangs);
            fr_cbb.SelectedIndex = to_cbb.SelectedIndex = 0;
            _set.Controls.Add(fr_lbl);
            _set.Controls.Add(fr_cbb);
            _set.Controls.Add(new Label {Left = _baseLeft + lbl_width + lbl_frto_width + 20 + cbb_width, Name = "lbl_arr" + TrSetCount, Width = lbl_width, Text = "->", Top = 2});
            _set.Controls.Add(to_lbl);
            _set.Controls.Add(to_cbb);
//			SpecKeySetsValues["cbb_fr"+TrSetCount+"_key"] = SpecKeySetsValues["txt_key"+TrSetCount+"_mods"] = SpecKeySetsValues["cbb_typ"+TrSetCount] = "";
            pan_TrSets.Controls.Add(_set);
            lbl_TrSetsCount.ForeColor = Color.Black;
            lbl_TrSetsCount.Text = "#" + TrSetCount;
            if (TrSetCount > 98) lbl_TrSetsCount.ForeColor = Color.Red;
        }

        private void Btn_TrSubSetClick(object sender, EventArgs e)
        {
            if (TrSetCount < 1) return;
            pan_TrSets.Controls["set_" + TrSetCount].Dispose();
            TrSetCount--;
            lbl_TrSetsCount.ForeColor = Color.Black;
            lbl_TrSetsCount.Text = "#" + TrSetCount;
            if (TrSetCount < 1)
                lbl_TrSetsCount.ForeColor = Color.LightGray;
        }

        private void Btn_AddSetClick(object sender, EventArgs e)
        {
            if (SpecKeySetCount > 98) return;
            var _set = new Panel();
            _set.Width = pan_KeySets.Width * 98 / 100 - 2;
            SpecKeySetCount++;
            _set.Name = "set_" + SpecKeySetCount;
            var top = 1;
            if (SpecKeySetCount > 1)
                top = pan_KeySets.Controls["set_" + (SpecKeySetCount - 1)].Top + 25;
            _set.Height = 23;
            _set.Top = top;
            _set.Left = 1;
            var _baseLeft = pan_KeySets.Width * 2 / 100;
            var txt_width = 190;
            var chk_width = 45;
            var lbl_width = 25;
            var cbb_width = 190;
            _set.Controls.Add(new Label {Left = _baseLeft, Name = "lbl_num" + SpecKeySetCount, Width = lbl_width, Text = SpecKeySetCount + ":", Top = 2});
            var txt = new TextBox {Left = _baseLeft + lbl_width, Name = "txt_key" + SpecKeySetCount, Width = txt_width, BackColor = SystemColors.Window, ReadOnly = true};
            txt.KeyDown += Txt_SpecHotkeyDown;
            var chk = new CheckBox {Left = _baseLeft + lbl_width + txt_width + 3, Name = "chk_win" + SpecKeySetCount, Width = chk_width, Text = "Win"};
            chk.CheckedChanged += Chk_SpecWinCheckedChanged;
            var cbb = new ComboBox {DropDownStyle = ComboBoxStyle.DropDownList, Left = _baseLeft + lbl_width + txt_width + chk_width + lbl_width + 9, Name = "cbb_typ" + SpecKeySetCount, Width = cbb_width};
            cbb.SelectedIndexChanged += Cbb_SpecTypeSelectedIndexChanged;
            cbb.Items.Add(MMain.Lang[Languages.Element.SwitchBetween]);
            cbb.Items.AddRange(MMain.Lcnmid.ToArray());
            _set.Controls.Add(txt);
            _set.Controls.Add(chk);
            _set.Controls.Add(new Label {Left = _baseLeft + lbl_width + txt_width + chk_width + 6, Name = "lbl_arr" + SpecKeySetCount, Width = lbl_width, Text = "->", Top = 2});
            _set.Controls.Add(cbb);
            SpecKeySetsValues["txt_key" + SpecKeySetCount + "_key"] = SpecKeySetsValues["txt_key" + SpecKeySetCount + "_mods"] = SpecKeySetsValues["cbb_typ" + SpecKeySetCount] = "";
            pan_KeySets.Controls.Add(_set);
            lbl_SetsCount.ForeColor = Color.Black;
            lbl_SetsCount.Text = "#" + SpecKeySetCount;
            if (SpecKeySetCount > 98) lbl_SetsCount.ForeColor = Color.Red;
        }

        private void Btn_SubSetClick(object sender, EventArgs e)
        {
            if (SpecKeySetCount < 1) return;
            pan_KeySets.Controls["set_" + SpecKeySetCount].Dispose();
            SpecKeySetCount--;
            lbl_SetsCount.ForeColor = Color.Black;
            lbl_SetsCount.Text = "#" + SpecKeySetCount;
            if (SpecKeySetCount < 1)
                lbl_SetsCount.ForeColor = Color.LightGray;
        }

        private void Txt_SpecHotkeyDown(object sender, KeyEventArgs e)
        {
            var t = sender as TextBox;
            if (e.KeyCode == Keys.Back && e.Modifiers == Keys.None)
            {
                SpecKeySetsValues[t.Name + "_key"] = SpecKeySetsValues[t.Name + "_mods"] = t.Text = "";
                return;
            }

            Debug.WriteLine(e.KeyCode + " E");
            t.Text = OemReadable((e.Modifiers.ToString().Replace(",", " +") + " + " +
                                  Remake(e.KeyCode)).Replace("None + ", ""));
            SpecKeySetsValues[t.Name + "_key"] = ((int) e.KeyCode).ToString();
            SpecKeySetsValues[t.Name + "_mods"] = e.Modifiers.ToString().Replace(",", " +");
        }

        private void Chk_SpecWinCheckedChanged(object sender, EventArgs e)
        {
            var c = sender as CheckBox;
            var key = SpecKeySetsValues["txt_key" + c.Name.Replace("chk_win", "") + "_mods"];
            var hasWin = key.Contains("Win");
            if (hasWin && !c.Checked)
                SpecKeySetsValues["txt_key" + c.Name.Replace("chk_win", "") + "_mods"] = key.Replace("Win", "");
            if (!hasWin && c.Checked)
                SpecKeySetsValues["txt_key" + c.Name.Replace("chk_win", "") + "_mods"] = key + " + Win";
        }

        private void Cbb_SpecTypeSelectedIndexChanged(object sender, EventArgs e)
        {
            var cb = sender as ComboBox;
            SpecKeySetsValues[cb.Name] = cb.SelectedItem.ToString();
        }

        private void Cbb_FrToSelectedIndexChanged(object sender, EventArgs e)
        {
            var cb = sender as ComboBox;
            TrSetsValues[cb.Name] = TranslatePanel.GTLangsSh[cb.SelectedIndex];
//			Debug.WriteLine(TrSetsValues[cb.Name]);
        }

        private void Cbb_SpecKeysTypeSelectedIndexChanged(object sender, EventArgs e)
        {
            var old = cbb_SpecKeysType.SelectedIndex == 0;
            lbl_Arrow1.Visible = lbl_Arrow2.Visible = lbl_Arrow3.Visible = lbl_Arrow4.Visible = grb_Layouts.Visible = grb_Keys.Visible = old;
            lbl_SetsCount.Visible = pan_KeySets.Visible = btn_SubSet.Visible = btn_AddSet.Visible = !old;
        }

        private void Btn_SelectSndClick(object sender, EventArgs e)
        {
            lbl_CustomSound.Text = SelectGetWavFile();
            HelpMeUnderstand.SetToolTip(lbl_CustomSound, lbl_CustomSound.Text);
        }

        private void Btn_SelectSnd2Click(object sender, EventArgs e)
        {
            lbl_CustomSound2.Text = SelectGetWavFile();
            HelpMeUnderstand.SetToolTip(lbl_CustomSound2, lbl_CustomSound2.Text);
        }

        private void Btn_backupClick(object sender, EventArgs e)
        {
            SyncBackup();
        }

        private void Btn_restoreClick(object sender, EventArgs e)
        {
            SyncRestore();
        }

        private bool pctres, pctbkp;

        private void PctBkpCopyClick(object sender, EventArgs e)
        {
            if (!pctbkp)
            {
                pctbkp = true;
                var t = new Timer();
                t.Tick += (_, ___) =>
                {
                    pctBkpCopy.BackgroundImage = Resources.clip;
                    pctbkp = false;
                    t.Stop();
                    t.Dispose();
                };
                t.Interval = 1800;
                if (!string.IsNullOrEmpty(txt_backupId.Text))
                {
                    KMHook.RestoreClipBoard(txt_backupId.Text);
                    pctBkpCopy.BackgroundImage = Resources.clipok;
                    t.Start();
                }
                else
                {
                    pctBkpCopy.BackgroundImage = Resources.cliperr;
                    t.Start();
                }
            }
        }

        private void PctResPasteClick(object sender, EventArgs e)
        {
            if (!pctres)
            {
                pctres = true;
                var t = new Timer();
                t.Tick += (_, ___) =>
                {
                    pctResPaste.BackgroundImage = Resources.clip;
                    pctres = false;
                    t.Stop();
                    t.Dispose();
                };
                t.Interval = 1800;
                var stri = KMHook.GetClipboard(3);
                if (!string.IsNullOrEmpty(stri))
                {
                    txt_restoreId.Text = stri;
                    pctResPaste.BackgroundImage = Resources.clipok;
                    t.Start();
                }
                else
                {
                    pctResPaste.BackgroundImage = Resources.cliperr;
                    t.Start();
                }
            }
        }

        #endregion

        #region Sync

        private string[] ReadToBackup(string id, string name, bool chk)
        {
            var r = "";
            var stat = "";
            var f = Path.Combine(nPath, name);
            if (chk)
            {
                if (!File.Exists(f))
                {
                    stat += name + " " + MMain.Lang[Languages.Element.Not].ToLower() + " " + MMain.Lang[Languages.Element.Exist].ToLower();
                    return new[] {"", stat};
                }

                var fi = new FileInfo(f);
                if (fi.Length >= 400000)
                    stat += name + MMain.Lang[Languages.Element.TooBig];
                try
                {
                    r += "#------>" + id + Environment.NewLine;
                    r += File.ReadAllText(f);
                    r += Environment.NewLine + "#------>" + id + Environment.NewLine;
                }
                catch (Exception e)
                {
                    stat += name + ": " + MMain.Lang[Languages.Element.CannotBe].ToLower() + " " + MMain.Lang[Languages.Element.Readen].ToLower() + e.Message;
                }
            }

            return new[] {r, stat};
        }

        private string WriteRestoreFiles(string raw, bool mini, bool stxt, bool htxt, bool ttxt)
        {
            var stat = "";
            var t = raw.Replace("\r", "");
            var ll = t.Split('\n');
            var bb = new[] {mini, stxt, htxt, ttxt};
            var tn = "dummy";
            var st = false;
            var d = new Dictionary<string, string>();
            for (var i = 0; i != ll.Length - 1; i++)
            {
                var l = ll[i];
                var cont = false;
                var end = false;
                if (st)
                    if (i + 1 <= ll.Length - 1)
                    {
                        var lz = ll[i + 1];
                        if (lz.StartsWith(SYNC_SEP, StringComparison.InvariantCulture))
                            if (lz == SYNC_SEP + tn)
                                end = true;
                    }

                if (l.StartsWith(SYNC_SEP, StringComparison.InvariantCulture))
                    foreach (var type in SYNC_TYPES)
                        if (l == SYNC_SEP + type)
                        {
                            if (tn == type)
                            {
                                tn = "dummy";
                            }
                            else
                            {
                                tn = type;
                                st = true;
                                cont = true;
                            }
                        }

                if (cont) continue;
                var ln = l + (i == ll.Length - 2 || end ? "" : Environment.NewLine);
                if (d.ContainsKey(tn))
                {
                    var va = d[tn];
                    d[tn] = va + ln;
                }
                else
                {
                    d.Add(tn, ln);
                }
            }

            var OK = "";
            var ERR = "";
            for (var i = 0; i != bb.Length; i++)
            {
                var b = bb[i];
                var ty = SYNC_TYPES[i];
                if (b)
                {
                    if (d.ContainsKey(ty))
                        try
                        {
                            if (ty == "ini") MMain.MyConfs._INI.Raw = d[ty];
                            var f = Path.Combine(nPath, SYNC_NAMES[i]);
                            Debug.WriteLine("Writing: " + f);
                            File.WriteAllText(f, d[ty]);
                            OK += " " + SYNC_NAMES[i];
                        }
                        catch (Exception e)
                        {
                            stat += ty + ": " + e.Message + Environment.NewLine;
                        }
                    else
                        ERR += " " + SYNC_NAMES[i];
                }
            }

            stat += "OK:" + OK + (ERR != "" ? Environment.NewLine + "ERR:" + ERR : "");
            return stat;
        }

        private void SyncBackup()
        {
            var id = "";
            var rawtext = "";
            var bb = new[] {chk_Mini.Checked, chk_Stxt.Checked, chk_Htxt.Checked, chk_Ttxt.Checked};
            var stat = "OK";
            for (var i = 0; i != SYNC_NAMES.Length; i++)
            {
                var r = ReadToBackup(SYNC_TYPES[i], SYNC_NAMES[i], bb[i]);
                rawtext += r[0];
                stat += r[1] != "" ? Environment.NewLine + r[1] : "";
            }

            Debug.WriteLine("Rawtext: " + rawtext);
            using (var wc = new WebClient())
            {
                wc.Encoding = Encoding.UTF8;
                try
                {
                    var r = wc.UploadData(new Uri(SYNC_HOST + "/documents"), "POST", Encoding.UTF8.GetBytes(rawtext));
                    id = Regex.Match(Encoding.UTF8.GetString(r), "^[{].key.:.(.+).[}]$").Groups[1].Value;
                }
                catch (Exception e)
                {
                    stat = e.Message;
                    if (rawtext.Length >= 400000)
                        stat += MMain.Lang[Languages.Element.TooBig];
                }
            }

            Debug.WriteLine("id:" + id);
            txt_backupId.Text = SYNC_HOST + "/" + id;
            MMain.MyConfs.Write("Sync", "BLast", txt_backupId.Text);
            txt_backupId.Enabled = true;
            txt_backupStatus.Text = stat;
            txt_backupStatus.Visible = true;
        }

        private void SyncRestore()
        {
            var id = txt_restoreId.Text;
            var stat = "";
            if (!string.IsNullOrEmpty(id))
            {
                var raw = SYNC_HOST + "/raw";
                if (id.StartsWith("http", StringComparison.InvariantCulture))
                {
                    if (!id.StartsWith(raw, StringComparison.InvariantCulture) || id.Contains("hastebin.com"))
                    {
                        var p = id.Split('/');
                        var l = p[p.Length - 1];
                        if (string.IsNullOrEmpty(l))
                            l = p[p.Length - 2];
                        id = raw + "/" + l;
                    }
                }
                else
                {
                    if (id.Length >= 32)
                        stat = MMain.Lang[Languages.Element.UnknownID];
                    else
                        id = raw + "/" + id;
                }

                Debug.WriteLine("id:" + id);
                var d = "";
                if (!string.IsNullOrEmpty(id))
                    using (var wc = new WebClient())
                    {
                        wc.Encoding = Encoding.UTF8;
                        try
                        {
                            d = Encoding.UTF8.GetString(wc.DownloadData(new Uri(id)));
                        }
                        catch (Exception e)
                        {
                            stat = e.Message;
                        }
                    }

                Debug.WriteLine(d);
                if (!string.IsNullOrEmpty(d))
                {
                    stat += WriteRestoreFiles(d, chk_rMini.Checked, chk_rStxt.Checked, chk_rHtxt.Checked, chk_rTtxt.Checked);
                    MMain.MyConfs.Write("Sync", "BLast", txt_backupId.Text);
                    MMain.MyConfs.Write("Sync", "RLast", txt_restoreId.Text);
                }

                LoadConfigs();
            }
            else
            {
                stat = MMain.Lang[Languages.Element.EnterID];
            }

            txt_restoreStatus.Text = stat;
            txt_restoreStatus.Visible = true;
        }

        private void SetBools(string bools, char sep, out bool mini, out bool stxt, out bool htxt, out bool ttxt)
        {
            var s = bools.Split(sep);
            mini = boo(s[0]);
            stxt = boo(s[1]);
            htxt = boo(s[2]);
            ttxt = boo(s[3]);
        }

        private bool boo(string s)
        {
            var i = 0;
            bool b;
            bool.TryParse(s, out b);
            int.TryParse(s, out i);
            if (i > 0)
                return true;
            return b;
        }

        private int bin(bool b)
        {
            if (b)
                return 1;
            return 0;
        }

        #endregion
    }
}