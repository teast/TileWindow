using System;
using System.Windows.Forms;
using Serilog;

namespace TileWindow
{
    class SessionChangeHandler : Control
    {
        private IPInvokeHandler pinvokeHandler;
        private bool _registered;
        public event EventHandler MachineLocked;
        public event EventHandler MachineUnlocked;

        public SessionChangeHandler(IPInvokeHandler pinvokeHandler)
        {
            this.pinvokeHandler = pinvokeHandler;
            _registered = true;
            if (!pinvokeHandler.WTSRegisterSessionNotification(this.Handle, PInvoker.NOTIFY_FOR_THIS_SESSION))
            {
                _registered = false;
                Log.Warning($"Problem registering session notification. Problem might arise with lock screen. error: {pinvokeHandler.GetLastError()}");
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            // unregister the handle before it gets destroyed
            if (_registered)
                pinvokeHandler.WTSUnRegisterSessionNotification(this.Handle);

            base.OnHandleDestroyed(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == PInvoker.WM_WTSSESSION_CHANGE)
            {
                int value = m.WParam.ToInt32();
                if (value == PInvoker.WTS_SESSION_LOCK)
                {
                    OnMachineLocked(EventArgs.Empty);
                }
                else if (value == PInvoker.WTS_SESSION_UNLOCK)
                {
                    OnMachineUnlocked(EventArgs.Empty);
                }
            }
            base.WndProc(ref m);
        }

        protected virtual void OnMachineLocked(EventArgs e)
        {
            EventHandler temp = MachineLocked;
            if (temp != null)
            {
                temp(this, e);
            }
        }

        protected virtual void OnMachineUnlocked(EventArgs e)
        {
            EventHandler temp = MachineUnlocked;
            if (temp != null)
            {
                temp(this, e);
            }
        }
    }
}