/*
 * Created by SharpDevelop.
 * User: teast
 * Date: 2017-01-23
 * Time: 21:31
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Drawing;
using System.Windows.Forms;

namespace TileWindow.Forms
{
	/// <summary>
	/// Description of FormWindowFinder.
	/// </summary>
	public partial class FormWindowFinder : Form
	{
		private int ticks = 5;
		public FormWindowFinder()
		{
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			
			lblTimeLeft.Text = ticks.ToString();
			lblResult.Text = "";
			timer1.Enabled = true;
		}
		void Timer1Tick(object sender, EventArgs e)
		{
			ticks--;
			if(ticks == 0)
			{
				var p = new POINT();
				p.X = Control.MousePosition.X;
				p.Y = Control.MousePosition.Y;
				var hwnd = PInvoker.WindowFromPoint(p);
				lblResult.Text = "Window hwnd: " + hwnd.ToString();
				ticks = 5;
			}
			
			lblTimeLeft.Text = ticks.ToString();
		}
		void BtnCloseClick(object sender, EventArgs e)
		{
			timer1.Enabled = false;
			this.Close();
			this.Dispose();
		}
	}
}
