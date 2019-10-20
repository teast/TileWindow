/*
 * Created by SharpDevelop.
 * User: teast
 * Date: 2017-01-19
 * Time: 12:15
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
namespace TileWindow.Forms
{
	partial class FormConfig
	{
		/// <summary>
		/// Designer variable used to keep track of non-visual components.
		/// </summary>
		private System.ComponentModel.IContainer components = null;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ColorDialog ColorDlg;
		
		/// <summary>
		/// Disposes resources used by the form.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
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
			this.label1 = new System.Windows.Forms.Label();
			this.ColorDlg = new System.Windows.Forms.ColorDialog();
			this.btnSaveChanges = new System.Windows.Forms.Button();
			this.btnClose = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.lblOpacity = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.cmbStartup = new System.Windows.Forms.ComboBox();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 13);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(70, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "Color on tiles:";
			// 
			// btnSaveChanges
			// 
			this.btnSaveChanges.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnSaveChanges.Location = new System.Drawing.Point(244, 438);
			this.btnSaveChanges.Name = "btnSaveChanges";
			this.btnSaveChanges.Size = new System.Drawing.Size(120, 34);
			this.btnSaveChanges.TabIndex = 3;
			this.btnSaveChanges.Text = "Save changes";
			this.btnSaveChanges.UseVisualStyleBackColor = true;
			this.btnSaveChanges.Click += new System.EventHandler(this.BtnSaveChangesClick);
			// 
			// btnClose
			// 
			this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnClose.Location = new System.Drawing.Point(380, 438);
			this.btnClose.Name = "btnClose";
			this.btnClose.Size = new System.Drawing.Size(120, 34);
			this.btnClose.TabIndex = 4;
			this.btnClose.Text = "Close";
			this.btnClose.UseVisualStyleBackColor = true;
			this.btnClose.Click += new System.EventHandler(this.BtnCloseClick);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(12, 44);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(82, 13);
			this.label2.TabIndex = 5;
			this.label2.Text = "Opacity on tiles:";
			// 
			// lblOpacity
			// 
			this.lblOpacity.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.lblOpacity.AutoSize = true;
			this.lblOpacity.Location = new System.Drawing.Point(447, 44);
			this.lblOpacity.Name = "lblOpacity";
			this.lblOpacity.Size = new System.Drawing.Size(21, 13);
			this.lblOpacity.TabIndex = 7;
			this.lblOpacity.Text = "0%";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(10, 70);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(180, 13);
			this.label3.TabIndex = 8;
			this.label3.Text = "Key combination to inactiavte all tiles";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(12, 99);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(108, 13);
			this.label4.TabIndex = 10;
			this.label4.Text = "Tile programs on start";
			// 
			// cmbStartup
			// 
			this.cmbStartup.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbStartup.FormattingEnabled = true;
			this.cmbStartup.Location = new System.Drawing.Point(196, 100);
			this.cmbStartup.Name = "cmbStartup";
			this.cmbStartup.Size = new System.Drawing.Size(271, 21);
			this.cmbStartup.TabIndex = 11;
			// 
			// FormConfig
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(502, 476);
			this.Controls.Add(this.cmbStartup);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.lblOpacity);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.btnClose);
			this.Controls.Add(this.btnSaveChanges);
			this.Controls.Add(this.label1);
			this.Icon = _appResource.Icon;
			this.Name = "FormConfig";
			this.Text = "TileWindow - Configuration";
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		private System.Windows.Forms.Button btnClose;
		private System.Windows.Forms.Button btnSaveChanges;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label lblOpacity;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.ComboBox cmbStartup;
	}
}
