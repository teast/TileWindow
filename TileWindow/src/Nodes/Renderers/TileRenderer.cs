using System;
using System.Collections.Generic;
using Serilog;

namespace TileWindow.Nodes.Renderers
{
    /// <summary>
    /// Layouts childs in an even tile layout based on <see cref="Direction" />.
    /// Will acknowledge an <see cref="Node" />s <see cref="FixedRect" />
    /// </summary>
    public class TileRenderer: IRenderer
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int AllocatableWidth { get; private set; }
        public int AllocatableHeight { get; private set; }

        public Node Owner { get; private set; }
        public List<Node> Childs { get; private set; }

        public void PreUpdate(Node owner, List<Node> childs)
        {
            Owner = owner;
            Childs = childs;

            Width = owner.Rect.Right - owner.Rect.Left;
            Height = owner.Rect.Bottom - owner.Rect.Top;
            if (childs.Count == 0)
            {
                AllocatableHeight = Height;
                AllocatableWidth = Width;
//Log.Information($"RecalcDeltaWidthHeight({Direction.ToString()}): _allocatable: {_allocatableWidth}/{_allocatableHeight}, zero Child count");
                return;
            }

            AllocatableHeight = Height / childs.Count;
            AllocatableWidth = Width / childs.Count;

            int w = 0, h = 0, c = 0;
            for(var i = 0; i  < childs.Count; i++)
            {
                if (childs[i].FixedRect == false)
                    continue;
                
                c++;
                w += (childs[i].Rect.Right - childs[i].Rect.Left);
                h += (childs[i].Rect.Bottom - childs[i].Rect.Top);
            }

            // All childs are marked as fixed but they do not cover the whole container
            if (c == childs.Count && ((owner.Direction == Direction.Horizontal && w < Width) || (owner.Direction == Direction.Vertical && h < Height)))
            {
//Log.Information($"RecalcDeltaWidthHeight({parent.Direction.ToString()}): ALL CHILDS ARE FIXED BUT THEY DO NOT COVER WHOLE PARENT {_width}/{_height} != {w}/{h}");
                w = h = c = 0;
                childs.ForEach(c => c.FixedRect = false);
            }

//Log.Information($"RecalcDeltaWidthHeight({parent.Direction.ToString()}): width/height: {_width}/{_height}, _allocatable: {_allocatableWidth}/{_allocatableHeight}, child count: {childs.Count} (w: h and c: {w}, {h}, {c})");
            if (owner.Direction == Direction.Horizontal && w > 0 && w < Width)
                AllocatableWidth = (Width - w) / Math.Max((childs.Count - c), 1);

            if (owner.Direction == Direction.Vertical && h > 0 && h < Height)
                AllocatableHeight = (Height - h) / Math.Max((childs.Count - c), 1);
        }

        public (bool result, RECT newRect) Update(List<int> ignoreChildsWithIndex)
        {
            var mustRestart = false;
            var safety = 0;
            var maxWidth = Width;
            var maxHeight = Height;
            var from = 0;
            var to = Childs.Count;
            RECT newRect;

            Func<int, int, RECT> setRectToDefault = (l, t) => {
                return new RECT {
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
//Log.Information($"============ Container.{Direction.ToString()} (from: {from} to: {to} total: {childs.Count}) Try {safety} (Rect: {Rect}) ==============");

                if (from > 0)
                {
                    if (Owner.Direction == Direction.Horizontal)
                        left = Childs[from-1].Parent.Rect.Right;
                    else
                        top = Childs[from-1].Parent.Rect.Bottom;
//Log.Information($"  >>>> Because we start in the middle then fetch Left/Top from previous child... left/top: {left}/{top}");
                }

                for(var i = from; i < to; i++)
                {
                    if (ignoreChildsWithIndex.Contains(i))
                        continue;

                    RECT r;
                    if (Childs[i].FixedRect)
                    {
                        r = Childs[i].Rect;
//Log.Information($"   Child[{i}] has ActualRect \"{_childs[i].Name}\": {r} current left/top: {left}/{top}, alloc: {_allocatableWidth}, {_allocatableHeight}");
                        if (r.Left != left || r.Top != top)
                        {
                            r.Right -= (r.Left - left);
                            r.Bottom -= (r.Top - top);
                            r.Left = left;
                            r.Top = top;

                            // Make sure this child wont span over our boundry...
                            r.Right = Math.Min(r.Right, Owner.Rect.Right);
                            r.Bottom = Math.Min(r.Bottom, Owner.Rect.Bottom);
//Log.Information($"   >>>>1 Child[{i}] CHANGE ActualRect: {r} current left/top: {left}/{top}, alloc: {_allocatableWidth}, {_allocatableHeight}");
                        }
                        else
                        if (r.Right > Owner.Rect.Right || r.Bottom > Owner.Rect.Bottom)
                        {
                            // Make sure this child wont span over our boundry...
                            r.Right = Math.Min(r.Right, Owner.Rect.Right);
                            r.Bottom = Math.Min(r.Bottom, Owner.Rect.Bottom);
//Log.Information($"   >>>>2 Child[{i}] CHANGE ActualRect: {r} current left/top: {left}/{top}, alloc: {_allocatableWidth}, {_allocatableHeight}");
                        }

                        if (Owner.Direction == Direction.Horizontal)
                        {
                            if ((r.Bottom - r.Top) != Height)
                            {
                                r.Bottom = r.Top + Height;
//Log.Information($"   >>>>3 Child[{i}] CHANGE ActualRect: {r} current left/top: {left}/{top}, alloc: {_allocatableWidth}, {_allocatableHeight}");
                                Childs[i].FixedRect = false;
                            }

                            if (Childs.Count > 1 && (r.Right - r.Left) == Width)
                            {
                                Childs[i].FixedRect = false;
//Log.Information($"   >>>>3.5 Child[{i}] Span whole parent but there are more _childs, changing to \"normal\" rect!: {r} current left/top: {left}/{top}, alloc: {_allocatableWidth}, {_allocatableHeight}");
                                r = setRectToDefault(left, top);
                            }
                        }

                        if (Owner.Direction == Direction.Vertical)
                        {
                            if ((r.Right - r.Left) != Width)
                            {
                                r.Right = r.Left + Width;
//Log.Information($"   >>>>4 Child[{i}] CHANGE ActualRect: {r} current left/top: {left}/{top}, alloc: {_allocatableWidth}, {_allocatableHeight}");
                                Childs[i].FixedRect = false;
                            }

                            if (Childs.Count > 1 && (r.Bottom - r.Top) == Height)
                            {
                                Childs[i].FixedRect = false;
//Log.Information($"   >>>>5 Child[{i}] Span whole parent but there are more _childs, changing to \"normal\" rect!: {r} current left/top: {left}/{top}, alloc: {_allocatableWidth}, {_allocatableHeight}");
                                r = setRectToDefault(left, top);
                            }
                        }
                    }
                    else
                    {
                        r = setRectToDefault(left, top);
//Log.Information($"   Child[{i}] has NO actualRect \"{_childs[i].Name}\": setting rect to {r} current left/top: {left}/{top}, alloc: {_allocatableWidth}, {_allocatableHeight}");
                    }

//Log.Information($".....Now updating rect for child {i}: {r}");
                    if (Childs[i].UpdateRect(r) == false)
                    {
//Log.Information($"Oh-no! Container.{Direction.ToString()} Child ({_childs[i].Name}) node says no on UpdateRect, wanted: {r} but actual: {_childs[i].Rect}");
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
                        left += (r.Right - r.Left);
                    else
                        top += (r.Bottom - r.Top);
                }

                if (safety > 2)
                {
                    Log.Warning($"{nameof(TileRenderer)}.{nameof(Update)} cannot resolve size. aborting after 2 tries...(rect: {Owner.Rect}, new width/height: {maxWidth}/{maxHeight})");
                    mustRestart = false;
                }
            } while (mustRestart);

            if (maxWidth != Width || maxHeight != Height)
            {
//Log.Information($"Oh-no2! Container want to change its rect: {Rect} (new width/height: {maxWidth}/{maxHeight})");
                RECT r = Owner.Rect;
                r.Right = r.Left + maxWidth;
                r.Bottom = r.Top + maxHeight;
                newRect = r;
//Log.Information($"==============Container.{Direction.ToString()} DONE(false) ====================");
                return (false, newRect);
            }

//Log.Information($"==============Container.{Direction.ToString()} DONE(true) ====================");
            newRect = Owner.Rect;
            return (true, newRect);
        }
    } 
}