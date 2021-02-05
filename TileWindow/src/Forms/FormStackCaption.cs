using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Serilog;
using TileWindow.Nodes;

namespace TileWindow.Forms
{
    public partial class FormStackCaption : Form
    {
        private readonly RECT position;
        private readonly int captionHeight;
        private readonly ContainerNode owner;
        private readonly List<Label> labels = new List<Label>();
        private readonly uint wmc_show;
        private readonly uint signalRefresh;
        private readonly uint signalNewPosition;
        private readonly uint signalNewSize;
        private readonly uint signalShowHide;
        private int lastSelected = -1;
        private readonly Color selected;
        private readonly Color selectedText;
        private long lastSelectedId;

        protected override bool ShowWithoutActivation
        {
            get
            {
                return false;
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams baseParams = base.CreateParams;
                baseParams.ExStyle |= (int)(PInvoker.WS_EX_NOACTIVATE | PInvoker.WS_EX_TOOLWINDOW);
                baseParams.ExStyle &= ~(PInvoker.WS_EX_APPWINDOW);
                baseParams.Style &= ~(PInvoker.WS_MAXIMIZEBOX);
                return baseParams;
            }
        }

        public FormStackCaption(RECT position, int captionHeight, ref ContainerNode owner, uint wmc_show, uint signalRefresh, uint signalNewPosition, uint signalNewSize, uint signalShowHide)
        {
            this.position = position;
            this.captionHeight = captionHeight;
            this.owner = owner;
            this.wmc_show = wmc_show;
            this.signalRefresh = signalRefresh;
            this.signalNewPosition = signalNewPosition;
            this.signalNewSize = signalNewSize;
            this.signalShowHide = signalShowHide;
            this.selected = SystemColors.InactiveCaption;
            this.selectedText = SystemColors.InactiveCaptionText;
            var childs = owner.Childs.ToList();
            if (owner.MyFocusNode != null)
            {
                lastSelectedId = owner.MyFocusNode.Id;
                for (int i = 0; i < childs.Count; i++)
                {
                    if (childs[i] == owner.MyFocusNode)
                    {
                        lastSelected = i;
                        break;
                    }
                }
            }

            InitializeComponent();
            this.Left = position.Left;
            this.Top = position.Top;
            this.Width = position.Right - position.Left;
            this.Height = position.Bottom - position.Top;
            BuildCaptions(owner.Childs.ToList());
        }

        protected override void WndProc(ref Message m)
        {
            try
            {
                if (m.Msg == wmc_show)
                {
                    if (!ValidateCaptions())
                    {
                        BuildCaptions(owner.Childs.ToList());
                    }

                    ShowCaption(m.WParam.ToInt64());
                }
                else if (m.Msg == signalRefresh)
                {
                    if (!ValidateCaptions())
                    {
                        var childs = owner.Childs.ToList();
                        for (int i = 0; i < childs.Count; i++)
                        {
                            if (childs[i].Id == lastSelectedId)
                            {
                                lastSelected = i;
                                break;
                            }
                        }

                        BuildCaptions(owner.Childs.ToList());
                    }

                    if (owner?.MyFocusNode != null && owner.MyFocusNode.Id != lastSelectedId)
                    {
                        ShowCaption(owner.MyFocusNode.Id);
                    }
                }
                else if (m.Msg == signalNewPosition)
                {
                    this.Left = m.WParam.ToInt32();
                    this.Top = m.LParam.ToInt32();
                    if (!ValidateCaptions())
                    {
                        var childs = owner.Childs.ToList();
                        for (int i = 0; i < childs.Count; i++)
                        {
                            if (childs[i].Id == lastSelectedId)
                            {
                                lastSelected = i;
                                break;
                            }
                        }

                        BuildCaptions(owner.Childs.ToList());
                    }
                }
                else if (m.Msg == signalNewSize)
                {
                    this.Width = m.WParam.ToInt32();
                    this.Height = m.LParam.ToInt32();
                    foreach (var lbl in labels)
                    {
                        lbl.Width = this.Width;
                    }
                    
                    if (!ValidateCaptions())
                    {
                        var childs = owner.Childs.ToList();
                        for (int i = 0; i < childs.Count; i++)
                        {
                            if (childs[i].Id == lastSelectedId)
                            {
                                lastSelected = i;
                                break;
                            }
                        }

                        BuildCaptions(owner.Childs.ToList());
                    }
                }
                else if (m.Msg == signalShowHide)
                {
                    if (m.WParam.ToInt64() == 1)
                    {
                        this.Visible = true;
                    }
                    else
                    {
                        this.Visible = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"{nameof(FormStackCaption)}.{nameof(WndProc)} Unhandled exception");
            }

            base.WndProc(ref m);
        }

        /// <summary>
        /// Check if <see cref="owner" />s <see cref="owner.Childs" /> differs from <see cref="labels" />
        /// </summary>
        /// <returns>true if no diff, else false</returns>
        protected virtual bool ValidateCaptions()
        {
            if (owner.Childs.Count != labels.Count)
            {
                return false;
            }

            var childs = owner.Childs.ToList();
            for (var i = 0; i < childs.Count; i++)
            {
                if (childs[i].Id != (long)labels[i].Tag ||
                    childs[i].Name != labels[i].Text)
                {
                    return false;
                }
            }

            return true;
        }

        protected virtual void BuildCaptions(List<Node> nodes)
        {
            foreach (var lbl in labels)
            {
                lbl.Dispose();
            }

            labels.Clear();

            var y = 0;
            var x = 0;
            for (var i = 0; i < nodes.Count; i++)
            {
                var lbl = new Label
                {
                    Tag = nodes[i].Id,
                    Text = nodes[i].Name,
                    Height = captionHeight,
                    Left = x,
                    Top = y,
                    Width = this.Width,
                    AutoSize = false,
                    Visible = true,
                };

                lbl.Font = new Font(lbl.Font, FontStyle.Bold);
                if (lastSelected == i)
                {
                    lbl.BackColor = selected;
                    lbl.ForeColor = selectedText;
                }
                else
                {
                    lbl.BackColor = SystemColors.ActiveBorder;
                    lbl.ForeColor = SystemColors.ActiveCaptionText;
                }

                lbl.Click += (sender, args) =>
                {
                    var id = (Int64)((Label)sender).Tag;
                    Startup.ParserSignal.QueuePipeMessage(new PipeMessageEx
                    {
                        msg = wmc_show,
                        wParam = (ulong)id,
                        lParam = 0
                    });
                };

                this.Controls.Add(lbl);
                labels.Add(lbl);
                lbl.Show();
                y += captionHeight;
            }
        }

        protected virtual void ShowCaption(long id)
        {
            lastSelectedId = id;
            for (int i = 0; i < labels.Count; i++)
            {
                if (((long)labels[i].Tag) == id)
                {
                    if (lastSelected >= 0 && lastSelected < labels.Count)
                    {
                        labels[lastSelected].BackColor = SystemColors.ActiveBorder;
                        labels[lastSelected].ForeColor = SystemColors.ActiveCaptionText;
                    }
                    else
                    {
                        lastSelected = -1;
                    }

                    labels[i].BackColor = selected;
                    labels[i].ForeColor = selectedText;
                    lastSelected = i;
                }
            }
        }
    }
}