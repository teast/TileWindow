using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Serilog;
using TileWindow.Dto;
using TileWindow.Nodes;

namespace TileWindow.Forms
{
	public partial class FormAppBar : Form
    {
        private readonly BarPosition direction;
        private readonly IPInvokeHandler pinvokeHandler;
        private readonly uint signalShowHide;
        private uint uCallBack;
        private bool fBarRegistered = false;
        private Color backColor;
        private Color focusForeColor;
        private Color focusBackColor;
        private Color color;
        private int wantedSize;
        private int iconSize;
        private int contentStart;
        private bool contentStartTop;

        protected override System.Windows.Forms.CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.Style &= (~PInvoker.WS_CAPTION);
                cp.Style &= (~PInvoker.WS_BORDER);
                cp.ExStyle = PInvoker.WS_EX_TOOLWINDOW | PInvoker.WS_EX_TOPMOST;
                return cp;
            }
        }

        public FormAppBar(AppConfig appConfig, IPInvokeHandler pinvokeHandler, uint signalShowHide)
		{
            this.direction = appConfig.Bar.Position;
            this.pinvokeHandler = pinvokeHandler;
            this.signalShowHide = signalShowHide;
            this.backColor = ColorTranslator.FromHtml(appConfig.Bar.Colors?.Background);
            this.focusForeColor = ColorTranslator.FromHtml(appConfig.Bar.Colors?.FocusedWorkspace.Text);
            this.focusBackColor = ColorTranslator.FromHtml(appConfig.Bar.Colors?.FocusedWorkspace.Background);
            this.color = ColorTranslator.FromHtml(appConfig.Bar.Colors?.Statusline);
            this.wantedSize = 25;
            this.iconSize = 23;
            this.contentStart = 1;
            this.contentStartTop = direction == BarPosition.Top ||direction == BarPosition.Bottom;
            this.BackColor = backColor;
            this.ForeColor = color;
            InitializeComponent();

            this.Height = wantedSize;
            this.Width = wantedSize;
            this.Size = new Size(wantedSize, wantedSize);
			containerControl.Height = direction == BarPosition.Left || direction == BarPosition.Right ? 400 : wantedSize;
			containerControl.Width = containerControl.Height == wantedSize ? 400 : wantedSize;
        }

        private void RemoveLabel(int index)
        {
            var txt = (index + 1).ToString();
            var name = $"lblTest{txt}";
            var lbl = containerControl.Controls.Find(name, false).FirstOrDefault();
            if (lbl != null)
            {
                lbl.Dispose();
                ForceLabelReposition();
            }
        }
        
        private void ForceLabelReposition()
        {
            var x = contentStart;
            var y = contentStart;
            foreach(Label ctrl in containerControl.Controls.Cast<Label>().OrderBy(lbl => Convert.ToInt32(lbl.Tag)))
            {
                ctrl.Location = new Point(x, y);
                if (contentStartTop)
                    x += iconSize + contentStart;
                else
                    y += iconSize + contentStart;
            }
        }

        private void AddLabel(int index, bool focus)
        {
            var txt = (index + 1).ToString();
            var name = $"lblTest{txt}";
            var lbl = (Label)containerControl.Controls.Find(name, false).FirstOrDefault();
            if (lbl == null)
            {
                lbl = new Label();
                lbl.Name = name;
                lbl.Text = txt;
                lbl.Tag = index;
                lbl.ForeColor = color;
                lbl.Height = iconSize;
                lbl.Width = iconSize;
                lbl.TextAlign = ContentAlignment.MiddleCenter;
                lbl.BorderStyle = BorderStyle.FixedSingle;
                lbl.Location = new Point(contentStart, contentStart);

                if (contentStartTop)
                    lbl.Left = containerControl.Controls.Count * (iconSize + contentStart);
                else
                    lbl.Top = containerControl.Controls.Count * (iconSize + contentStart);
                containerControl.Controls.Add(lbl);
            }

            if (focus)
            {
                lbl.ForeColor = focusForeColor;
                lbl.BackColor = focusBackColor;
            }
            else
            {
                lbl.ForeColor = color;
                lbl.BackColor = backColor;
            }
            
            ForceLabelReposition();
        }

        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            if (m.Msg == uCallBack)
            {
                switch(m.WParam.ToInt32())
                {
                    case (int)ABNotify.ABN_POSCHANGED:
                        ABSetPos();
                        break;
                }
            }
            else if (m.Msg == PInvoker.WM_DESTROY)
            {
                RegisterBar(false);
            }
            else if (m.Msg == signalShowHide)
            {
                HandleShowHide(Convert.ToBoolean(m.LParam.ToInt32() & 1), Convert.ToBoolean(m.LParam.ToInt32() & 2), m.WParam.ToInt32());
            }

            base.WndProc(ref m);
        }

        private void HandleShowHide(bool visible, bool focus, int index)
        {
            Log.Information($"FormAppBar: {index}: visible: {visible}, focus: {focus}, this height: {this.Size}");
            if (visible)
                AddLabel(index, focus);
            else
                RemoveLabel(index);
        }

        protected override void OnLoad(System.EventArgs e)
        {
            RegisterBar(true);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            RegisterBar(false);
        }

        private void RegisterBar(bool doRegister)
        {
            APPBARDATA abd = new APPBARDATA(this.Handle);
            if (!fBarRegistered && doRegister)
            {
                uCallBack = pinvokeHandler.RegisterWindowMessage("AppBarMessage");
                abd.uCallbackMessage = uCallBack;

                uint ret = pinvokeHandler.SHAppBarMessage((int)ABMsg.ABM_NEW, ref abd);
                fBarRegistered = true;

                ABSetPos();
            }
            else if (fBarRegistered && doRegister == false)
            {
                pinvokeHandler.SHAppBarMessage((int)ABMsg.ABM_REMOVE, ref abd);
                fBarRegistered = false;
            }
        }

        private void ABSetPos()
        {
            APPBARDATA abd = new APPBARDATA();
            abd.cbSize = Marshal.SizeOf(abd);
            abd.hWnd = this.Handle;
            switch(direction)
            {
                case BarPosition.Top:
                    abd.uEdge = (int)ABEdge.ABE_TOP;
                    break;
                case BarPosition.Bottom:
                    abd.uEdge = (int)ABEdge.ABE_BOTTOM;
                    break;
                case BarPosition.Left:
                    abd.uEdge = (int)ABEdge.ABE_LEFT;
                    break;
                case BarPosition.Right:
                    abd.uEdge = (int)ABEdge.ABE_RIGHT;
                    break;
            }

            if (abd.uEdge == (int)ABEdge.ABE_LEFT || abd.uEdge == (int)ABEdge.ABE_RIGHT) 
            {
                abd.rc.Top = 0;
                abd.rc.Bottom = SystemInformation.PrimaryMonitorSize.Height;
                if (abd.uEdge == (int)ABEdge.ABE_LEFT) 
                {
                    abd.rc.Left = 0;
                    abd.rc.Right = Size.Width;
                }
                else 
                {
                    abd.rc.Right = SystemInformation.PrimaryMonitorSize.Width;
                    abd.rc.Left = abd.rc.Right - Size.Width;
                }

            }
            else 
            {
                abd.rc.Left = 0;
                abd.rc.Right = SystemInformation.PrimaryMonitorSize.Width;
                if (abd.uEdge == (int)ABEdge.ABE_TOP) 
                {
                    abd.rc.Top = 0;
                    abd.rc.Bottom = Size.Height;
                }
                else 
                {
                    abd.rc.Bottom = SystemInformation.PrimaryMonitorSize.Height;
                    abd.rc.Top = abd.rc.Bottom - Size.Height;
                }
            }

            // Query the system for an approved size and position. 
            pinvokeHandler.SHAppBarMessage((int)ABMsg.ABM_QUERYPOS, ref abd); 

            // Adjust the rectangle, depending on the edge to which the 
            // appbar is anchored. 
            switch (abd.uEdge) 
            { 
                case (int)ABEdge.ABE_LEFT: 
                    abd.rc.Right = abd.rc.Left + Size.Width;
                    break; 
                case (int)ABEdge.ABE_RIGHT: 
                    abd.rc.Left= abd.rc.Right - Size.Width;
                    break; 
                case (int)ABEdge.ABE_TOP: 
                    abd.rc.Bottom = abd.rc.Top + Size.Height;
                    break; 
                case (int)ABEdge.ABE_BOTTOM: 
                    abd.rc.Top = abd.rc.Bottom - Size.Height;
                    break; 
            }

            // Pass the final bounding rectangle to the system. 
            pinvokeHandler.SHAppBarMessage((int)ABMsg.ABM_SETPOS, ref abd); 

            // Move and size the appbar so that it conforms to the 
            // bounding rectangle passed to the system. 
            pinvokeHandler.MoveWindow(abd.hWnd, abd.rc.Left, abd.rc.Top, 
                abd.rc.Right - abd.rc.Left, abd.rc.Bottom - abd.rc.Top, true); 
        }
    }
}