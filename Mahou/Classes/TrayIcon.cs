using System;
using System.Windows.Forms;

namespace Mahou.Classes {
    public class TrayIcon {
        public event EventHandler<EventArgs> Exit, EnaDisable,
        	ShowHide, Restart, ConvertClip, TransliClip;
        public NotifyIcon trIcon;
        ContextMenu cMenu;
        MenuItem Exi, ShHi, EnDis, Resta;
        /// <summary> Clipboard menu item. </summary>
		MenuItem Clip, CConvert, CTransli, CLast;
        /// <summary>Initializes new tray icon.</summary>
        /// <param name="visible">State of tray icon's visibility on initialize.</param>
        public TrayIcon(bool? visible = true) {
            trIcon = new NotifyIcon();
            cMenu = new ContextMenu();
            trIcon.Icon = Properties.Resources.MahouTrayHD;
            trIcon.Visible = visible == true;
            CConvert = new MenuItem("Convert", ConvertClipHandler);
            CTransli = new MenuItem("Transliterate", TransliClipHandler);
            CLast = new MenuItem("Latest: (not implemented)"); // Dynamically add the last later
            Clip = new MenuItem("Clipboard", new []{CConvert, CTransli, CLast});
            Exi = new MenuItem("Exit", ExitHandler);
            ShHi = new MenuItem("Show", ShowHideHandler);
            EnDis = new MenuItem("Enable", EnaDisableHandler);
            Resta = new MenuItem("Restart", RestartHandler);
            EnDis.Checked = true;
            cMenu.MenuItems.Add(ShHi);
            cMenu.MenuItems.Add(Clip);
            cMenu.MenuItems.Add(EnDis);
            cMenu.MenuItems.Add(Resta);
            cMenu.MenuItems.Add(Exi);
            trIcon.Text = "Mahou (魔法)\nA magical layout switcher.";
            trIcon.ContextMenu = cMenu;
            trIcon.MouseClick += (_,__) => { if (__.Button == MouseButtons.Left) ShowHideHandler(_,__); };
        }
        public void CheckShHi(bool shhi) {
        	ShHi.Checked = shhi;
        }
        public void CheckEnDis(bool endis) {
        	EnDis.Checked = endis;
        }
        /// <summary>Transliterate the text in clipboard.</summary>
        void TransliClipHandler(object sender, EventArgs e)
        {
            TransliClip?.Invoke(this, null);
        }
        /// <summary>Convert the text in clipboard.</summary>
        void ConvertClipHandler(object sender, EventArgs e)
        {
            ConvertClip?.Invoke(this, null);
        }
        /// <summary>Toggle Mahou, enable/disable event handler..</summary>
        void EnaDisableHandler(object sender, EventArgs e)
        {
            EnaDisable?.Invoke(this, null);
        }
        /// <summary>Restart event handler..</summary>
        void RestartHandler(object sender, EventArgs e)
        {
            Restart?.Invoke(this, null);
        }
        /// <summary>Exit event handler..</summary>
        void ExitHandler(object sender, EventArgs e)
        {
            Exit?.Invoke(this, null);
        }
        /// <summary>ShowHide event handler..</summary>
        void ShowHideHandler(object sender, EventArgs e)
        {
            ShowHide?.Invoke(this, null);
        }
        /// <summary>Hides tray icon.</summary>
        public void Hide() {
        	if (MahouUI.TrayFlags || MahouUI.TrayText)
        		MMain.Mahou.flagsCheck.Stop();
            trIcon.Visible = false;
        }
        /// <summary>Shows tray icon.</summary>
        public void Show() {
        	if (MahouUI.TrayFlags || MahouUI.TrayText)
        		MMain.Mahou.flagsCheck.Start();
            trIcon.Visible = true;
        }
        /// <summary>Refreshes tray icon various text.</summary>
        public void RefreshText(string TrText, string ShHiText, string ExiText, string EnaDisText,
                                string RestartText, string ConvClipText, string TransliClipText, string ClipbText, string LatestText) {
            trIcon.Text = TrText;
            if (!String.IsNullOrEmpty(ShHiText))
            	ShHi.Text = ShHiText;
            if (!String.IsNullOrEmpty(ExiText))
            	Exi.Text = ExiText;
            if (!String.IsNullOrEmpty(EnaDisText))
            	EnDis.Text = EnaDisText;
            if (!String.IsNullOrEmpty(RestartText))
            	Resta.Text = RestartText;
            if (!String.IsNullOrEmpty(ConvClipText))
            	CConvert.Text = ConvClipText;
            if (!String.IsNullOrEmpty(TransliClipText))
            	CTransli.Text = TransliClipText;
            if (!String.IsNullOrEmpty(ClipbText))
            	Clip.Text = ClipbText;
            if (!String.IsNullOrEmpty(LatestText))
            	CLast.Text = LatestText;
        }
    }
}
