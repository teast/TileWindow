using System;
using System.Collections.Generic;
using System.Linq;
using TileWindow.Nodes;

namespace TileWindow.Extensions
{
    public static class ScreenExtensions
    {
        public static RECT TotalRect(this IEnumerable<RECT> rects)
        {
            int? left = null, top = null, right = null, bottom = null;

            foreach (var rect in rects)
            {
                if (left == null || rect.Left < left)
                    left = rect.Left;
                if (top == null || rect.Top < top)
                    top = rect.Top;
                if (right == null || rect.Left + rect.Right > right)
                    right = rect.Right;
                if (bottom == null || rect.Top + rect.Bottom > bottom)
                    bottom = rect.Bottom;
            }

            return new RECT
            {
                Left = left.Value,
                Top = top.Value,
                Right = right.Value,
                Bottom = bottom.Value
            };
        }

        public static bool InsideRect(this RECT r, int x, int y)
        {
            return x >= r.Left && x <= r.Right &&
                   y >= r.Top && y <= r.Bottom;
        }

        public static RECT Intersection(this RECT r1, RECT r2)
        {
            if (
                // r2 has no points inside r1
                (r1.InsideRect(r2.Left, r2.Top) == false && 
                r1.InsideRect(r2.Left, r2.Bottom) == false &&
                r1.InsideRect(r2.Right, r2.Top) == false &&
                r1.InsideRect(r2.Right, r2.Bottom) == false) &&
                // r1 has no points inside r2
                (r2.InsideRect(r1.Left, r1.Top) == false && 
                r2.InsideRect(r1.Left, r1.Bottom) == false &&
                r2.InsideRect(r1.Right, r1.Top) == false &&
                r2.InsideRect(r1.Right, r1.Bottom) == false)
            )
            {
                return new RECT(0, 0, 0, 0);
            }

            int left = Math.Max(r1.Left, r2.Left);
            int top = Math.Max(r1.Top, r2.Top);
            int right = Math.Min(r1.Right, r2.Right);
            int bottom = Math.Min(r1.Bottom, r2.Bottom);

            return new RECT(left, top, right, bottom);
        }

        public static long CalcArea(this RECT rect)
        {
            return (rect.Right-rect.Left) * (rect.Bottom - rect.Top);
        }

        public static (Direction direction, int index, IEnumerable<RECT> rect) GetOrderRect(this IEnumerable<IScreenInfo> screens)
        {
            var first = screens.First();
            var horizontal = first.WorkingArea.Right - first.WorkingArea.Left;
            var vertical = first.WorkingArea.Bottom - first.WorkingArea.Top;

            var horizontals = new List<int>();
            var verticals = new List<int>();

            foreach(var screen in screens.Skip(1))
            {
                var h = screen.WorkingArea.Right - screen.WorkingArea.Left;
                var v = screen.WorkingArea.Bottom - screen.WorkingArea.Top;
                horizontals.Add((horizontal/h)*100);
                verticals.Add((vertical/v)*100);
                vertical += v;
                horizontal += h;
            }

            Direction direction;
            Func<IScreenInfo, int> orderBy;
            if (horizontals.Average() >= verticals.Average())
            {
                direction = Direction.Horizontal;
                orderBy = (r) => r.WorkingArea.Left;
            }
            else
            {
                direction = Direction.Vertical;
                orderBy = (r) => r.WorkingArea.Top;
            }

            var primIndex = -1;
            var rects = screens.OrderBy(orderBy).Select((screen, i) =>
            {
                if (screen.Primary)
                    primIndex = i;
                return new RECT
                {
                    Left = screen.WorkingArea.Left,
                    Top = screen.WorkingArea.Top,
                    Right = screen.WorkingArea.Right,
                    Bottom = screen.WorkingArea.Bottom,
                };
            }).ToArray();

            return (direction, primIndex, rects);
        }
    }
}