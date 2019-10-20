using System.Drawing;
using System.Reflection;

namespace TileWindow.Dto
{
    public class AppResource
    {
        public Image Logo { get; }
        public Icon Icon {get;}

        public AppResource()
        {
            var assembly = typeof(AppResource).GetTypeInfo().Assembly;
            //string[] names = assembly.GetManifestResourceNames();
            using(var logoStream = assembly.GetManifestResourceStream("TileWindow.Resources.tilewindow.bmp"))
            {
                this.Logo = Image.FromStream(logoStream);
            }

            using(var iconStream = assembly.GetManifestResourceStream("TileWindow.Resources.tilewindow.ico"))
            {
                this.Icon = new Icon(iconStream);
            }
        }
    }
}