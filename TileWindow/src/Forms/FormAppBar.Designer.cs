using System.Windows.Forms;
using Serilog;

namespace TileWindow.Forms
{
	partial class FormAppBar
    {
		private System.ComponentModel.IContainer components = null;

		protected override void Dispose(bool disposing)
		{
			if (disposing) {
				if (components != null) {
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}
		
		/// <summary>
		/// This method is required for Windows Forms designer support.
		/// Do not change the method contents inside the source code editor. The Forms designer might
		/// not be able to load this method if it was changed manually.
		/// </summary>
		private void InitializeComponent()
		{
			containerControl = new ContainerControl();
			this.SuspendLayout();


			// containerControl
			containerControl.Name = "containerCtrl";
			containerControl.Left = 0;
			containerControl.Top = 0;
			containerControl.Height = 400;
			containerControl.Width = 400;
			containerControl.Anchor = AnchorStyles.Left | AnchorStyles.Top |AnchorStyles.Bottom;
			
			// FormAppBar
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.ControlBox = false;
			this.Name = "FormBar";
			this.Text = "TileWindow - AppBar";
			this.Controls.Add(containerControl);
			this.ResumeLayout(false);
			this.PerformLayout();
        }

        private ContainerControl containerControl;
    }
}