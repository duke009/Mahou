using System;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Linq;
using System.Net;
using System.Web;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Mahou.Classes;

namespace Mahou {
	/// <summary>
	/// Translate panel displays translation of some things.
	/// </summary>
	public partial class TranslatePanel : Form {
		public static readonly string speech_dir = Path.Combine(Path.GetTempPath(), "MahouGTSpeech");
		public static List<GTResp> GTRs = new List<GTResp>();
//		public static List<string> SPFs = new List<string>();
//		public static List<string> SPUs = new List<string>();
		public static bool running, multiline;
		public static readonly WebClient client = new WebClient();
		public static readonly string GTSpeechLink = "https://translate.google.com/translate_tts?ie=UTF-8&client=tw-ob"; // tl & q
		public static readonly string GTLink = "https://translate.googleapis.com/translate_a/single?client=gtx&dt=t"; // q, sl, & tl
		public static object _LOCK = new object();
		public TranslatePanel() {
			InitializeComponent();
			X.Width = 23;
			X.Text = "X";
			TITLE.AutoEllipsis = true;
			X.TabStop = false;
			TabStop = false;
			X.TabIndex = 99999;
			TITLE.Font = new Font("Segoe UI Bold", 12);
			X.SetColors(2);
		  	pan_Translations.Width = 0;
		  	pan_Translations.Height = 0;
		  	Height = TITLE.Height + 1 + txt_Source.Height+2;
		  	SetOptimalWidth();
		  	Prepare();
		}
		public struct GTResp {
			public bool auto_detect;
			public string translation;
			public string source;
			public string src_lang;
			public string speech_url;
			public string targ_lang;
		}
		public void GTRespError(string msg) {
		  	txt_Source.Text = MMain.Lang[Languages.Element.Error]+ " " + msg;
		  	pan_Translations.Width = 0;
		  	pan_Translations.Height = 0;
		  	Height = TITLE.Height + 1 + txt_Source.Height+2;
		  	SetOptimalWidth();
		  	Prepare();
		}
		public static List<GTResp> GetGTResponceAll(string[] tls, string[] qs, string[] sls) {
			var gtrlist = new List<GTResp>();
			try {
				for (int i=0; i!= tls.Length; i++) {
					// corrects GTLink responce encoding.
					client.Headers["User-Agent"] = "User-Agent: Mozilla/5.0 (Windows NT x.y; Win64; x64; rv:10.0) Gecko/20180501 Firefox/60.0";
					var url = TranslatePanel.GTLink+"&q="+HttpUtility.UrlPathEncode(qs[i].Replace(" ", "%20"))+
						"&sl="+sls[i]+"&tl="+tls[i];
					Debug.WriteLine("url: " + url);
					var raw_array = Encoding.UTF8.GetString(client.DownloadData(url));
					Debug.WriteLine("RAW:" +raw_array);
					var aur = new Auray(raw_array);
					var gtresp = new GTResp();
					string src = "", tr = "";
					var aur_dim_2 = new Auray(aur[0]);
					for (int m=0; m != aur_dim_2.len+1; m++) { // combine all responce from all dimensions
						var s = aur_dim_2[m,1];
						src += s.Substring(1, s.Length-2);
						s = aur_dim_2[m,0];
						tr += s.Substring(1, s.Length-2);
					}
					gtresp.source = src;
					gtresp.translation = tr;
					var det_l = aur[2];
					det_l = det_l.Substring(1, det_l.Length-2);
					gtresp.speech_url = GTSpeechLink+"&q="+HttpUtility.UrlEncode(tr)+"&sl="+det_l+"&tl="+tls[i];
//					Debug.WriteLine(gtresp.speech_url);
					if(sls[i] == "auto")
						gtresp.auto_detect = true;
					gtresp.src_lang = det_l;
					gtresp.targ_lang = tls[i];
					gtrlist.Add(gtresp);
				}
			} catch(Exception e) { MMain.Mahou._TranslatePanel.GTRespError(e.Message); }
			return gtrlist;
		}
		public void ShowTranslation(string str, Point pos) {
			GTRs.Clear();
			pan_Translations.Controls.Clear();
			running = true;
			var tls = (from pv in MahouUI.TrSetsValues
			           where pv.Key.StartsWith("cbb_to") 
			           select pv.Value).ToArray();
			var sls = (from pv in MahouUI.TrSetsValues
			           where pv.Key.StartsWith("cbb_fr") 
			           select pv.Value).ToArray();
			var qs = new string[tls.Length];
			for (int i = 0; i != tls.Length; i++) {
	     		qs[i] = str;
		    }
			if (tls.Length == 0)
				txt_Source.Text = str;
			Debug.WriteLine("UPDATING TRANSLATION::");
			var gtrs = GetGTResponceAll(tls, qs, sls);
			if (gtrs.Count > 0) {
				TITLE.Text = MMain.Lang[Languages.Element.Translation] + " <"+gtrs[0].src_lang+">";
				TITLE.Font = MahouUI.TrTitle;
				foreach (var gtr in gtrs) {
	//				var gtr = TranslatePanel.ParseGTResp(raw_gtr);
					if (string.IsNullOrEmpty(gtr.translation)) continue;
	//				Debug.WriteLine("Added " + gtr.targ_lang + ", " +gtr.source);
					AddTranslation(gtr);
				}
			}
			Location = pos;
			running = false;
			SpecialShow();
		}
		public void AddTranslation(GTResp gtr) {
			txt_Source.Text = gtr.source;
			pan_Translations.Width = Width-2;
			bool exist = false;
			if (GTRs.Count == 0)
				pan_Translations.Height = 0;
			foreach (Control ct in pan_Translations.Controls) {
				if (ct.Name == "PN_LINE_"+gtr.src_lang+".to."+gtr.targ_lang) {
					exist = true;
					break;
				}
			}
			if (exist)
				UpdateTranslation(gtr);
			else {
				GTRs.Add(gtr);
				var ind = GTRs.IndexOf(gtr);
				var pan = new Panel();
				pan.Width = pan_Translations.Width-2;
				pan.Height = MahouUI.TrText.Height*2;
				Debug.WriteLine("Pan height: " + pan.Height);
				pan.Name = "PN_LINE_"+gtr.src_lang+".to."+gtr.targ_lang;
				pan.Location = new Point(1, pan_Translations.Height+1);
				var slt = new TextBox();
				var txt = new TextBox();
				slt.ReadOnly = txt.ReadOnly = true;
				slt.TabStop = txt.TabStop = false;
				slt.Name = "SL_TXT"+gtr.targ_lang;
				slt.BorderStyle = txt.BorderStyle = 0;
				slt.Location = new Point(1, 0);
				slt.Text = (gtr.auto_detect ? "" : gtr.src_lang+"/")+gtr.targ_lang+":";
				var g = CreateGraphics();
				var size = g.MeasureString(slt.Text, slt.Font);
				g.Dispose();
				slt.Width = (int)size.Width;
				txt.Name = "TR_TXT"+gtr.targ_lang;
				txt.Text = gtr.translation;
				var btn = new ButtonLabel();
				btn.Text = "♫";
				btn.gtr = gtr;
				btn.Name = "LBBT_SP"+gtr.targ_lang;
				btn.Width = 14;
				btn.Height = 14;
				btn.Location = new Point(pan.Width-14-1, 1);
				pan.Controls.Add(slt);
				pan.Controls.Add(txt);
				pan.Controls.Add(btn);
				txt.Width = pan.Width-slt.Width-2-btn.Width-2;
				txt.Location = new Point(slt.Width+2, 1);
				btn.BackColor = slt.BackColor = txt.BackColor = pan_Translations.BackColor;
				btn.ForeColor = slt.ForeColor = txt.ForeColor = pan_Translations.ForeColor;
				pan_Translations.Controls.Add(pan);
				txt_Source.Font = slt.Font = txt.Font = MahouUI.TrText;
				UpdateHeight();
			}
			SetOptimalWidth();
			SetOptimalWidth();
		}
		public void UpdateTranslation(GTResp gtr) {
		    foreach (Control ct in pan_Translations.Controls) {
				var ind = GTRs.IndexOf(gtr);
				var pan = pan_Translations.Controls["PN_LINE_"+gtr.src_lang+".to."+gtr.targ_lang];
				var slt = pan.Controls[0];
				var txt = pan.Controls[1];
				var btn = pan.Controls[2] as ButtonLabel;
				btn.gtr = gtr;
//				Debug.WriteLine("updating speech url to "+gtr.speech_url);
				slt.Text = gtr.targ_lang+":";
				var g = CreateGraphics();
				var size = g.MeasureString(slt.Text, slt.Font);
				g.Dispose();
				slt.Width = (int)size.Width;
				if (ind != -1) {
					GTRs.RemoveAt(ind);
					GTRs.Insert(ind, gtr);
				}
	//			SPs.RemoveAt(i);
	//			SPs.Insert(i, getSP());
				txt.Text = gtr.translation;
				txt.Width = pan.Width-slt.Width-2-btn.Width-2;
			}
		}
		public void RemoveTranslation(string sl) {
			var ind = GTRs.Select((SL, I) => new {SL, I})
						.Where(x => x.SL.targ_lang == sl)
						.Select(x => x.I).First();
			if (ind != -1) {
				GTRs.RemoveAt(ind);
			}
			var _gtrs = new List<GTResp>(GTRs);
			GTRs.Clear();
			pan_Translations.Controls.Clear();
			foreach (var gtr in _gtrs ) {
				AddTranslation(gtr);
			}
		}
		public void UpdateApperence(Color back, Color fore, int opacity, Font font) {
			foreach (Control ct in pan_Translations.Controls) {
				if (ct.Name.Contains("PN_LINE")) {
					var txt = ct.Controls[1] as TextBox;
					ct.Controls[0].BackColor = back;
					txt.BackColor = back;
					(ct.Controls[2] as ButtonLabel).origin_bg = ct.Controls[2].BackColor = ControlPaint.Light(back, (float)0.4);
					ct.Controls[0].ForeColor = fore;
					txt.ForeColor = fore;
					(ct.Controls[2] as ButtonLabel).origin_fg = ct.Controls[2].ForeColor = fore;
//					ct.Controls[0].Font = font;
//					txt.Font = font;
					ct.Controls[0].Font = txt.Font = MahouUI.TrText;
					txt.Multiline = multiline;
				}
			}
			txt_Source.BackColor = back;
			txt_Source.ForeColor = fore;
			txt_Source.Multiline = multiline;
			txt_Source.Font = MahouUI.TrText;
			X.BackColor = back;
			X._original_color = X.ForeColor = fore;
			pan_Translations.ForeColor = fore;
			pan_Translations.BackColor = back;
			TITLE.BackColor = back;
			TITLE.ForeColor = fore;
			BackColor = back;
			txt_Source.Font = new Font(txt_Source.Font, FontStyle.Underline | FontStyle.Bold);
			Opacity = (double)opacity / 100;
			Invalidate();
		}
		public void UpdateHeight() {
			TITLE.Height = TITLE.Font.Height;
			var h = 0;
			foreach (Control ct in pan_Translations.Controls) {
				h+=ct.Height;
			}
			pan_Translations.Height = h;
			pan_Translations.Location = new Point(1, TITLE.Height +1 + 2+ txt_Source.Height);
			Height = txt_Source.Height+1+pan_Translations.Height+TITLE.Height+2+2;
		}
		public void SetOptimalWidth() {
			SuspendLayout();
			Width = pan_Translations.Width = 0; // Minify
			txt_Source.Height = 0;
			// 1st find max width
			var g = CreateGraphics();
			SetAboveTitleWidth();
			var s = g.MeasureString(txt_Source.Text, txt_Source.Font);
			if ((int)s.Width > Width)
				Width = (int)s.Width;
			if ((int)s.Width > Width)
				multiline = true;
			else multiline = false;
			if (Width != MaximumSize.Width)
				multiline = false;
			txt_Source.Multiline = multiline;
			var mod = Math.Ceiling(s.Width / Width);
//				Debug.WriteLine("Height[mod]x[H] " + mod +"x"+s.Height);
			if (mod > 1)
				txt_Source.Height = (int)(s.Height*mod);
			else txt_Source.Height = (int)s.Height;
			foreach (Control ct in pan_Translations.Controls) {
				var pan = ct;
				var slt = pan.Controls[0];
				var txt = pan.Controls[1] as TextBox;
				var btn = pan.Controls[2];
				var size = g.MeasureString(slt.Text, slt.Font);
				var trsize = g.MeasureString(txt.Text, txt.Font);
				slt.Width = (int)size.Width;
				txt.Width = (int)trsize.Width;
				if (multiline) {
					mod = Math.Ceiling(trsize.Width / Width);
					txt.Multiline = true;
					Debug.WriteLine(txt.Text.Substring(0,5)+" Height[mod]x[H] " + mod +"x"+(int)(Math.Floor(trsize.Height)*mod));
					txt.Height = (int)(Math.Floor(trsize.Height)*mod);
				}
				var longest = (slt.Width + txt.Width + btn.Width + 4);
				if (longest > Width) {
					Width = longest;
				}
			}
			g.Dispose();
			pan_Translations.Width = Width-2;
			// 2nd set right positions
			foreach (Control ct in pan_Translations.Controls) {
				var pan = ct;
				var slt = pan.Controls[0];
				var txt = pan.Controls[1];
				var btn = pan.Controls[2];
				pan.Width = pan_Translations.Width-2;
				pan.Height = txt.Height+2;
				btn.Location = new Point(pan.Width-14-1, 1);
				txt.Width = pan.Width-slt.Width-2-btn.Width-2;
				txt.Location = new Point(slt.Width+2, 1);
		  		Prepare();
			}
			UpdateHeight();
			ResumeLayout(false);
		}
		void SetAboveTitleWidth() {
			var g = CreateGraphics();
			var size = g.MeasureString(TITLE.Text + "  ", TITLE.Font);
			g.Dispose();
			TITLE.Width = (int)size.Width+1;
			var TITLEw = (TITLE.Width+X.Width);
			if (Width < TITLEw)
				Width = TITLEw+3;
		}
		public void SetTitle(string title) {
			TITLE.Text = title;
		}
		#region Derived from LangPanel
		#region Derived from LangDisplay
		//Comments removed
		public void SpecialShow(int left = -7, int top = -7) {
			AeroCheck();
			UpdateApperence(MahouUI.TrBack, MahouUI.TrFore, MahouUI.TrTransparency, Font);
			if (Visible) return;
			int LEFT = Left, TOP = Top;
			if (left != -7)
				LEFT = left;
			if (top != -7)
				TOP = top;
			WinAPI.ShowWindow(Handle, WinAPI.SW_SHOWNOACTIVATE);
			bull.Focus();
			WinAPI.SetWindowPos(Handle.ToInt32(), WinAPI.HWND_TOPMOST,
				LEFT, TOP, Width, Height, 0
//				WinAPI.SWP_NOACTIVATE
			);
			WinAPI.SetForegroundWindow(Handle);
		}
		public void HideWnd() {
			if (!Visible) return;
			WinAPI.ShowWindow(Handle, 0);
			WinAPI.mciSendString(@"close speech", null, 0, 0);
			try {
			if (Directory.Exists(speech_dir))
				Directory.Delete(speech_dir, true);
			} catch (Exception e) {
				Logging.Log("Can't delete temp files, error: " + e.Message, 2);
			}
		}
		protected override CreateParams CreateParams {
			get {
				var Params = base.CreateParams;
				Params.ExStyle |= WinAPI.WS_EX_TOOLWINDOW;
//				Params.ExStyle |=  // WinAPI.WS_EX_LAYERED |
//					WinAPI.WS_EX_TRANSPARENT;
				return Params;
			}
		}
		public static bool ColorsEquals(Color c1, Color c2) {
			return (c1.A == c2.A && c1.R == c2.R && c1.G == c2.G && c1.B == c2.B);
		}
		#endregion
		#region Derived from JustUI
		public bool AeroEnabled;
		public void AeroCheck() {
			if (KMHook.IsNotWin7()) {
				int enabled = 0;
				WinAPI.DwmIsCompositionEnabled(ref enabled);
				AeroEnabled = (enabled == 1);
			}
		}
		Color CurrentAeroColor() {
			WinAPI.DWM_COLORIZATION_PARAMS parameters;
			WinAPI.DwmGetColorizationParameters(out parameters);
			return Color.FromArgb(Int32.Parse(parameters.clrColor.ToString("X"), System.Globalization.NumberStyles.HexNumber));;
		}
		protected override void WndProc(ref Message m) {
			try {
			if (m.Msg == WinAPI.WM_NCPAINT && AeroEnabled) {
				var v = 2;
				WinAPI.DwmSetWindowAttribute(this.Handle, 2, ref v, 4);
				var margins = new WinAPI.MARGINS(){ bH = 1, lW = 1, rW = 1, tH = 1 };
				WinAPI.DwmExtendFrameIntoClientArea(this.Handle, ref margins);
			}
			base.WndProc(ref m);
			} catch (Exception e) {
				Debug.WriteLine("Exc:" +e.Message);
			}
		}
		void Prepare() {
			SuspendLayout();
			SetAboveTitleWidth();
			TITLE.Location = new Point(1, 1);
			X.Location = new Point(Width - 24, 1);
			TITLE.Width = Width - 1 - X.Width - 1;
			txt_Source.Location = new Point(11, TITLE.Height+1);
			txt_Source.TextAlign = HorizontalAlignment.Center;
			txt_Source.Width = Width -20-2;
			bull.Focus();
			ResumeLayout(false);
		}
		protected override void OnPaint(PaintEventArgs e) {
//			if (MMain.mahou == null) { base.OnPaint(e); return; }
			Graphics g = CreateGraphics();
			var pn = new Pen(Color.Black);
			if (AeroEnabled && MahouUI.TrBorderAero)
				pn = new Pen(CurrentAeroColor());
			else
				pn.Color = MahouUI.TrBorder;
			g.DrawRectangle(pn, new Rectangle(0, 0, Size.Width - 1, Size.Height - 1));
			g.Dispose();
			pn.Dispose();
			base.OnPaint(e);
		}
		protected override void OnShown(EventArgs e) {
			Prepare();
			base.OnShown(e);
		}
		void XClick(object sender, EventArgs e) {
			HideWnd();
		}
		void TranslatePanelDeactivate(object sender, EventArgs e) {
			HideWnd();
		}
		#endregion
		#endregion
		#region New Controls
		public class ColorPanel : Panel {
//		    public ColorPanel() {
//		        SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
//		    }
//		    protected override void OnPaint(PaintEventArgs e) {
//				Color c = ControlPaint.Dark(ForeColor, (float)0.2);
//				if (c == ForeColor)
//					c = ControlPaint.Light(ForeColor, (float)0.8);
//				e.Graphics.DrawRectangle(new Pen(new SolidBrush(c),2), e.ClipRectangle);
//		    }
		
		}
		public class ButtonLabel : Label {
			public Color origin_bg, origin_fg;
			bool mdown;
			public GTResp gtr;
			protected override void OnMouseLeave(EventArgs e) {
				this.BackColor = origin_bg;
				this.ForeColor = origin_fg;
				base.OnMouseLeave(e);
			}
			protected override void OnMouseUp(MouseEventArgs e) {
				this.BackColor = origin_bg;
				this.ForeColor = origin_fg;
				mdown = false;
			    var bytestr = BitConverter.ToString(Encoding.UTF8.GetBytes(gtr.targ_lang + gtr.src_lang + gtr.translation)).Replace("-", "");
			    bytestr = bytestr.Substring(0, bytestr.Length <= 45 ? bytestr.Length-1 : 45);
			    var speech_file = Path.Combine(speech_dir, bytestr + ".mp3");
			    Debug.WriteLine("SPF: " +speech_file);
			    if (!Directory.Exists(speech_dir)) 
			    	Directory.CreateDirectory(speech_dir);
			    try {
				if (!File.Exists(speech_file))
	    			client.DownloadFile(gtr.speech_url, speech_file);
			    } catch (Exception x) {
			    	Logging.Log("Network exception: " + x.Message);
			    }
				if (File.Exists(speech_file)) {
					Debug.WriteLine("Playing " + speech_file);
					WinAPI.mciSendString(@"close speech", null, 0, 0);
					WinAPI.mciSendString("open \"" + speech_file + "\" type mpegvideo alias speech", null, 0, 0);
					WinAPI.mciSendString("play speech", null, 0, 0);
			    }
				base.OnMouseUp(e);
			}
			protected override void OnMouseMove(MouseEventArgs e) {
				if (!mdown) {
					if (origin_bg == Color.Empty)
						origin_bg = BackColor;
					Debug.WriteLine("Moving" + origin_bg + " " + BackColor);
					if (ColorsEquals(BackColor, origin_bg)) {
						this.BackColor = ControlPaint.Light(origin_bg, (float).3);
						Debug.WriteLine("REA" + origin_bg + " " + BackColor);
						if (ColorsEquals(BackColor, origin_bg)) {
							this.BackColor = ControlPaint.Dark(origin_bg, (float).05);
							this.ForeColor = Color.White;
							Debug.WriteLine("BUF" + origin_bg + " " + BackColor);
						}
					}
				}
				base.OnMouseMove(e);
			}
			protected override void OnMouseDown(MouseEventArgs e) {
				mdown = true;
				this.BackColor = ControlPaint.Dark(origin_bg, (float).3);
				this.ForeColor = Color.White;
				base.OnMouseDown(e);
			}
		}
		public static readonly string[] GTLangs = {
			"Auto Detect", "Afrikaans","Albanian","Amharic","Arabic","Armenian","Azeerbaijani",
"Basque","Belarusian","Bengali","Bosnian","Bulgarian","Catalan",
"Cebuano","Chinese (Simplified)","Chinese (Traditional)","Corsican","Croatian","Czech",
"Danish","Dutch","English","Esperanto","Estonian","Finnish",
"French","Frisian","Galician","Georgian","German","Greek",
"Gujarati","Haitian","Hausa","Hawaiian","Hebrew","Hindi",
"Hmong","Hungarian","Icelandic","Igbo","Indonesian","Irish",
"Italian","Japanese","Javanese","Kannada","Kazakh","Khmer",
"Korean","Kurdish","Kyrgyz","Lao","Latin","Latvian","Lithuanian",
"Luxembourgish","Macedonian","Malagasy","Malay","Malayalam","Maltese",
"Maori","Marathi","Mongolian","Myanmar","Nepali","Norwegian","Nyanja",
"Pashto","Persian","Polish","Portuguese","Punjabi","Romanian","Russian",
"Samoan","Scots","Serbian","Sesotho","Shona","Sindhi","Sinhala","Slovak",
"Slovenian","Somali","Spanish","Sundanese","Swahili","Swedish","Tagalog",
"Tajik","Tamil","Telugu","Thai","Turkish","Ukrainian","Urdu","Uzbek",
"Vietnamese","Welsh","Xhosa","Yiddish","Yoruba","Zulu"
		};
		public static readonly string[] GTLangsSh = {
			"auto", "af","sq","am","ar","hy","az","eu","be","bn","bs","bg",
"ca","ceb","zh-CN","zh-TW","co","hr","cs","da","nl","en",
"eo","et","fi","fr","fy","gl","ka","de","el","gu","ht","ha",
"haw","iw","hi","hmn","hu","is","ig","id","ga","it","ja",
"jw","kn","kk","km","ko","ku","ky","lo","la","lv","lt","lb",
"mk","mg","ms","ml","mt","mi","mr","mn","my","ne","no","ny",
"ps","fa","pl","pt","pa","ro","ru","sm","gd","sr","st","sn",
"sd","si","sk","sl","so","es","su","sw","sv","tl","tg","ta",
"te","th","tr","uk","ur","uz","vi","cy","xh","yi","yo","zu"
		};
		#endregion
	}
}
