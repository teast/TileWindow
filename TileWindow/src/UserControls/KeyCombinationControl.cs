using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace TileWindow.UserControls
{
    /// <summary>
    /// Description of KeyCombinationControl.
    /// </summary>
    public partial class KeyCombinationControl : UserControl
    {
        private const int KEY_ALT = 18;
        private const int KEY_SHIFT = 16;
        private const int KEY_CTRL = 17;
        private bool handleKeystate = false;
        private readonly bool[] keystate = new bool[512];

        public KeyCombinationControl()
        {
            InitializeComponent();
        }

        public override string Text
        {
            get
            {
                return this.txtKeyComb.Text;
            }
            set
            {
                this.txtKeyComb.Text = value;
            }
        }

        void TxtKeyCombKeyDown(object sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = true;

            if (e.KeyValue > 512)
            {
                return;
            }

            handleKeystate = true;
            var preva = keystate[KEY_ALT];
            var prevs = keystate[KEY_SHIFT];
            var prevc = keystate[KEY_CTRL];
            var prev = keystate[e.KeyValue];

            keystate[KEY_ALT] = e.Alt;
            keystate[KEY_SHIFT] = e.Shift;
            keystate[KEY_CTRL] = e.Control;
            keystate[e.KeyValue] = true;

            this.UpdateKeyCombText();

            if ((preva && preva != e.Alt) ||
              (prevs && prevs != e.Shift) ||
              (prevc && prevc != e.Control))
            {
                handleKeystate = false;

                // Reset states for next press
                for (var i = 0; i < keystate.Length; i++)
                {
                    keystate[i] = false;
                }
            }
        }

        private void TxtKeyCombKeyUp(object sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = true;

            if (!handleKeystate)
            {
                return;
            }

            this.UpdateKeyCombText();
            handleKeystate = false;

            // Reset states for next press
            for (var i = 0; i < keystate.Length; i++)
            {
                keystate[i] = false;
            }
        }

        private void UpdateKeyCombText()
        {
            var spec = new List<string>();
            var normal = new List<string>();
            var speclst = new[] { KEY_ALT, KEY_SHIFT, KEY_CTRL };

            for (var i = 0; i < keystate.Length; i++)
            {
                if (!keystate[i])
                {
                    continue;
                }
				
                if (speclst.Contains(i))
                {
                    spec.Add(KeyToString(i));
                }
                else
                {
                    normal.Add(KeyToString(i));
                }
            }

            txtKeyComb.Text = string.Join("+", spec.Concat(normal));
        }

        private string KeyToString(int key)
        {
            switch (key)
            {
                case KEY_ALT:
                    return "ALT";
                case KEY_SHIFT:
                    return "SHIFT";
                case KEY_CTRL:
                    return "CTRL";
                default:
                    return ((char)key).ToString();
            }
        }

        private void BtnClearKeyCombClick(object sender, EventArgs e)
        {
            txtKeyComb.Text = string.Empty;
        }
    }
}
