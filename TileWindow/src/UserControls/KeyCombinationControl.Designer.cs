/*
 * Created by SharpDevelop.
 * User: teast
 * Date: 2017-01-23
 * Time: 15:16
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
namespace TileWindow.UserControls
{
	partial class KeyCombinationControl
	{
		/// <summary>
		/// Designer variable used to keep track of non-visual components.
		/// </summary>
		private System.ComponentModel.IContainer components = null;
		private System.Windows.Forms.Button btnClearKeyComb;
		private System.Windows.Forms.TextBox txtKeyComb;
		
		/// <summary>
		/// Disposes resources used by the control.
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
			this.btnClearKeyComb = new System.Windows.Forms.Button();
			this.txtKeyComb = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// btnClearKeyComb
			// 
			this.btnClearKeyComb.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnClearKeyComb.Location = new System.Drawing.Point(199, 0);
			this.btnClearKeyComb.Name = "btnClearKeyComb";
			this.btnClearKeyComb.Size = new System.Drawing.Size(24, 20);
			this.btnClearKeyComb.TabIndex = 15;
			this.btnClearKeyComb.Text = "X";
			this.btnClearKeyComb.UseVisualStyleBackColor = true;
			this.btnClearKeyComb.Click += new System.EventHandler(this.BtnClearKeyCombClick);
			// 
			// txtKeyComb
			// 
			this.txtKeyComb.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
			| System.Windows.Forms.AnchorStyles.Left) 
			| System.Windows.Forms.AnchorStyles.Right)));
			this.txtKeyComb.Location = new System.Drawing.Point(0, 0);
			this.txtKeyComb.Name = "txtKeyComb";
			this.txtKeyComb.ReadOnly = true;
			this.txtKeyComb.Size = new System.Drawing.Size(193, 20);
			this.txtKeyComb.TabIndex = 14;
			this.txtKeyComb.WordWrap = false;
			this.txtKeyComb.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TxtKeyCombKeyDown);
			this.txtKeyComb.KeyUp += new System.Windows.Forms.KeyEventHandler(this.TxtKeyCombKeyUp);
			// 
			// KeyCombinationControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.btnClearKeyComb);
			this.Controls.Add(this.txtKeyComb);
			this.Name = "KeyCombinationControl";
			this.Size = new System.Drawing.Size(229, 20);
			this.ResumeLayout(false);
			this.PerformLayout();

		}
	}
}
