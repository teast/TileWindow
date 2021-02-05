using System;
using System.Diagnostics;
using System.Drawing;
using System.Resources;
using System.Threading;
using System.Windows.Forms;
using TileWindow.Dto;
using TileWindow.Forms;

namespace TileWindow
{
    public sealed class NotificationIcon : IDisposable
    {
        private NotifyIcon notifyIcon;
        private ContextMenu notificationMenu;
        //private FormConfig frmConfig;
        private readonly ResourceManager resources = new ResourceManager(typeof(NotificationIcon));
        private readonly AppConfig _appConfig;
        private readonly AppResource _appResource;

        #region Initialize icon and menu
        public NotificationIcon(AppConfig appConfig, AppResource appResource)
        {
            _appConfig = appConfig;
            _appResource = appResource;
            notifyIcon = new NotifyIcon();
            notificationMenu = new ContextMenu(InitializeMenu());
            //frmConfig = new FormConfig();

            notifyIcon.DoubleClick += IconDoubleClick;
            notifyIcon.Icon = appResource.Icon;

            notifyIcon.ContextMenu = notificationMenu;
        }

        private MenuItem[] InitializeMenu()
        {
            var menu = new [] {
				//new MenuItem(resources.GetString("Config"), menuConfigClick),
				new MenuItem("Find hWnd", menuFindHwndClick),
                new MenuItem("Restart hooks", menuRestartHooks),
                new MenuItem(resources.GetString("Exit"), menuExitClick)
            };

            menu[0].DefaultItem = true;

            return menu;
        }
        #endregion

        public void InitNotification()
        {
            this.notifyIcon.Visible = true;
        }

        #region Event Handlers
        private void menuConfigClick(object sender, EventArgs e)
        {
            var frmConfig = new FormConfig(_appConfig, _appResource);
            frmConfig.Show();
        }

        private void menuFindHwndClick(object sender, EventArgs e)
        {
            var frm = new FormWindowFinder();
            frm.Show();
        }

        private void menuRestartHooks(object sender, EventArgs eventArgs)
        {
            Startup.ParserSignal.SignalRestartThreads();
        }

        private void menuExitClick(object sender, EventArgs e)
        {
            this.notifyIcon.Visible = false;
            Application.Exit();
        }

        private void IconDoubleClick(object sender, EventArgs e)
        {
            menuConfigClick(null, null);
        }
        #endregion

        public void Dispose()
        {
            if (notificationMenu != null)
            {
                try
                {
                    notificationMenu.Dispose();
                }
                catch
                {
                    // Ignore any exception from dispose
                }

                notificationMenu = null;
            }
            if (notifyIcon != null)
            {
                try
                {
                    notifyIcon.Dispose();
                }
                catch
                {
                    // Ignore any exception from dispose
                }

                notifyIcon = null;
            }

        }
    }
}
