using System.Collections.Generic;
using System.Linq;

namespace TileWindow
{
    public interface IScreenInfo
    {
        bool Primary { get; }
        RECT WorkingArea { get; }
    }

    public class ScreenInfo: IScreenInfo
    {
        public bool Primary { get; }
        public RECT WorkingArea { get; }

        public ScreenInfo(bool primary = true, RECT? rect = null)
        {
            Primary = primary;
            WorkingArea = rect ?? new RECT(0, 0, 10 ,10);
        }

        public ScreenInfo(System.Windows.Forms.Screen screen)
        {
            Primary = screen.Primary;
            WorkingArea = new RECT(screen.WorkingArea.Left, screen.WorkingArea.Top, screen.WorkingArea.Right, screen.WorkingArea.Bottom);
        }
    }

    public interface IScreens
    {
        IEnumerable<IScreenInfo> AllScreens { get; }
    }

    public class Screens: IScreens
    {
        public IEnumerable<IScreenInfo> AllScreens => System.Windows.Forms.Screen.AllScreens.Select(s => new ScreenInfo(s));
    }
}