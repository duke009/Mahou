using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Mahou.JustUI {
	public partial class JUITitle : Label {
		public JUITitle() {
			InitializeComponent();
		}
		protected override void OnMouseDown(MouseEventArgs e) {
			if (e.Button == MouseButtons.Left) {
				const int WM_NCLBUTTONDOWN = 0xA1;
				const int HT_CAPTION = 0x2;
				ReleaseCapture();
				SendMessage(this.Parent.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
			}
			base.OnMouseDown(e);
		}
		[DllImport("user32.dll")]
		public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
		[DllImport("user32.dll")]
		public static extern bool ReleaseCapture();
	}
}
