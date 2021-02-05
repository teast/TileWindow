/*
 * Created by SharpDevelop.
 * User: teast
 * Date: 2017-01-17
 * Time: 13:53
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Drawing;
using System.Resources;
using System.Windows.Forms;
using TileWindow.Dto;

namespace TileWindow.Forms
{
	/// <summary>
	/// Description of FormStartup.
	/// </summary>
	public partial class FormStartup : Form
	{
		private readonly ResourceManager resources = new ResourceManager(typeof(FormStartup));
		private readonly AppResource _appResource;

		public FormStartup(AppResource appResource)
		{
			_appResource = appResource;
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			
			this.lblStatus.Text = resources.GetString("Loading");
		}
	}
}
