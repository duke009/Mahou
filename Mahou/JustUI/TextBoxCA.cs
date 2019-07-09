using System;
using System.Windows.Forms;
using Mahou.Classes;

namespace Mahou.JustUI
{
    public class TextBoxCA : TextBox
    {
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            var keyCode = (Keys)(msg.WParam.ToInt32() & Convert.ToInt32(Keys.KeyCode));
            if (msg.Msg == WinAPI.WM_KEYDOWN && keyCode == Keys.A &&
                ModifierKeys == Keys.Control && Focused)
            {
                SelectAll();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}