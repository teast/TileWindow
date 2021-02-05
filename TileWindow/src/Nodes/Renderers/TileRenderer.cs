using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Serilog;

namespace TileWindow.Nodes.Renderers
{
    /// <summary>
    /// Layouts childs in an even tile layout based on <see cref="Direction" />.
    /// Will acknowledge an <see cref="Node" />s <see cref="FixedRect" />
    /// </summary>
    public class TileRenderer : IRenderer
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int AllocatableWidth { get; private set; }
        public int AllocatableHeight { get; private set; }

        public ContainerNode Owner { get; private set; }

        public void PreUpdate(ContainerNode owner, Collection<Node> childs)
        {
            Owner = owner;

            Width = owner.Rect.Right - owner.Rect.Left;
            Height = owner.Rect.Bottom - owner.Rect.Top;
            if (childs.Count == 0)
            {
                AllocatableHeight = Height;
                AllocatableWidth = Width;
                return;
            }

            AllocatableHeight = Height / childs.Count;
            AllocatableWidth = Width / childs.Count;

            int w = 0, h = 0, c = 0;
            for (var i = 0; i < childs.Count; i++)
            {
                if (childs[i].FixedRect == false)
                {
                    continue;
                }

                c++;
                w += (childs[i].Rect.Right - childs[i].Rect.Left);
                h += (childs[i].Rect.Bottom - childs[i].Rect.Top);
            }

            // All childs are marked as fixed but they do not cover the whole container
            if (c == childs.Count && ((owner.Direction == Direction.Horizontal && w < Width) || (owner.Direction == Direction.Vertical && h < Height)))
            {
                w = h = c = 0;
                foreach (var child in childs)
                {
                    child.FixedRect = false;
                }
            }

            if (owner.Direction == Direction.Horizontal && w > 0 && w < Width)
            {
                AllocatableWidth = (Width - w) / Math.Max((childs.Count - c), 1);
            }
            
            if (owner.Direction == Direction.Vertical && h > 0 && h < Height)
            {
                AllocatableHeight = (Height - h) / Math.Max((childs.Count - c), 1);
            }
        }

        public (bool result, RECT newRect) Update(List<int> ignoreChildsWithIndex)
        {
            var mustRestart = false;
            var safety = 0;
            var maxWidth = Width;
            var maxHeight = Height;
            var from = 0;
            var Childs = Owner.Childs;
            var to = Childs.Count;
            RECT newRect;

            Func<int, int, RECT> setRectToDefault = (l, t) =>
            {
                return new RECT
                {
                    Left = l,
                    Top = t,
                    Right = l + (Owner.Direction == Direction.Horizontal ? AllocatableWidth : Width),
                    Bottom = t + (Owner.Direction == Direction.Vertical ? AllocatableHeight : Height)
                };
            };

            do
            {
                mustRestart = false;
                safety++;
                int left = Owner.Rect.Left, top = Owner.Rect.Top;

                if (from > 0)
                {
                    if (Owner.Direction == Direction.Horizontal)
                    {
                        left = Childs[from - 1].Parent.Rect.Right;
                    }
                    else
                    {
                        top = Childs[from - 1].Parent.Rect.Bottom;
                    }
                }

                for (var i = from; i < to; i++)
                {
                    if (ignoreChildsWithIndex.Contains(i))
                    {
                        continue;
                    }

                    RECT r;
                    if (Childs[i].FixedRect)
                    {
                        r = Childs[i].Rect;
                        if (r.Left != left || r.Top != top)
                        {
                            r.Right -= (r.Left - left);
                            r.Bottom -= (r.Top - top);
                            r.Left = left;
                            r.Top = top;

                            // Make sure this child wont span over our boundry...
                            r.Right = Math.Min(r.Right, Owner.Rect.Right);
                            r.Bottom = Math.Min(r.Bottom, Owner.Rect.Bottom);
                        }
                        else
                        if (r.Right > Owner.Rect.Right || r.Bottom > Owner.Rect.Bottom)
                        {
                            // Make sure this child wont span over our boundry...
                            r.Right = Math.Min(r.Right, Owner.Rect.Right);
                            r.Bottom = Math.Min(r.Bottom, Owner.Rect.Bottom);
                        }

                        if (Owner.Direction == Direction.Horizontal)
                        {
                            if ((r.Bottom - r.Top) != Height)
                            {
                                r.Bottom = r.Top + Height;
                                Childs[i].FixedRect = false;
                            }

                            if (Childs.Count > 1 && (r.Right - r.Left) == Width)
                            {
                                Childs[i].FixedRect = false;
                                r = setRectToDefault(left, top);
                            }
                        }

                        if (Owner.Direction == Direction.Vertical)
                        {
                            if ((r.Right - r.Left) != Width)
                            {
                                r.Right = r.Left + Width;
                                Childs[i].FixedRect = false;
                            }

                            if (Childs.Count > 1 && (r.Bottom - r.Top) == Height)
                            {
                                Childs[i].FixedRect = false;
                                r = setRectToDefault(left, top);
                            }
                        }
                    }
                    else
                    {
                        r = setRectToDefault(left, top);
                    }

                    if (Childs[i].UpdateRect(r) == false)
                    {
                        // Child want to be bigger than what it got...
                        var r2 = Childs[i].Rect;
                        if (r2.Right > r.Right || r2.Bottom > r.Bottom)
                        {
                            Childs[i].FixedRect = true;
                            mustRestart = true;

                            PreUpdate(Owner, Childs);

                            maxWidth = Math.Max(maxWidth, r2.Right - r2.Left);
                            maxHeight = Math.Max(maxHeight, r2.Bottom - r2.Top);
                        }
                    }

                    if (Owner.Direction == Direction.Horizontal)
                    {
                        left += (r.Right - r.Left);
                    }
                    else
                    {
                        top += (r.Bottom - r.Top);
                    }
                }

                if (safety > 2)
                {
                    Log.Warning($"{nameof(TileRenderer)}.{nameof(Update)} cannot resolve size. aborting after 2 tries...(rect: {Owner.Rect}, new width/height: {maxWidth}/{maxHeight})");
                    mustRestart = false;
                }
            } while (mustRestart);

            if (maxWidth != Width || maxHeight != Height)
            {
                RECT r = Owner.Rect;
                r.Right = r.Left + maxWidth;
                r.Bottom = r.Top + maxHeight;
                newRect = r;
                return (false, newRect);
            }

            newRect = Owner.Rect;
            return (true, newRect);
        }

        public bool Show() => true;

        public bool Hide() => true;

        public void Dispose()
        {
            // Nothing to do here
        }
    }
}