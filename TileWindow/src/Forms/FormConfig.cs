using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Resources;
using System.Windows.Forms;

using TileWindow.Dto;

namespace TileWindow.Forms
{
	public partial class FormConfig : Form
	{
		private readonly ResourceManager resources = new ResourceManager(typeof(FormConfig));
		private readonly string EmptyStartup = "[None]";
		private readonly AppConfig _appConfig;
		private readonly AppResource _appResource;

		public FormConfig(AppConfig appConfig, AppResource appResource)
		{
			_appConfig = appConfig;
			_appResource = appResource;
			InitializeComponent();
			
			// Language
			this.Text = "TileWindow - " + resources.GetString("Configuration");
			this.label1.Text = resources.GetString("ColorOnTiles");
			this.label2.Text = resources.GetString("OpacityOnTiles");
			this.btnSaveChanges.Text = resources.GetString("SaveChanges");
			this.btnClose.Text = resources.GetString("Close");
			
			// Read config
			this.cmbStartup.Items.Clear();
			this.cmbStartup.Items.Add(EmptyStartup);
			this.cmbStartup.SelectedItem = EmptyStartup;
		}
		
		void BtnSaveChangesClick(object sender, EventArgs e)
		{
			// Not yet in used
		}
		
		void BtnCloseClick(object sender, EventArgs e)
		{
			this.Hide();
			this.Close();
			this.Dispose();
		}
	}
}
