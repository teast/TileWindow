using System;
using System.Windows.Forms;

namespace TileWindow.Forms
{
	// Overrides taken from http://stackoverflow.com/questions/156046/show-a-form-without-stealing-focus
	// Martin Plante gaved the answer...
	public class FormTile: Form
	{
		protected override bool ShowWithoutActivation
		{
			get 
			{
				return false;
			}
		}
		
		protected override CreateParams CreateParams
		{
			get
			{
			    CreateParams baseParams = base.CreateParams;
			
			    const int WS_EX_NOACTIVATE = 0x08000000;
			    const int WS_EX_TOOLWINDOW = 0x00000080;
			    baseParams.ExStyle |= ( int )( WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW );
			
			    return baseParams;
			}
		}
	}
}
