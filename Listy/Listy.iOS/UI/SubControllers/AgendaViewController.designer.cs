// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace AdMaiora.Listy
{
	[Register ("AgendaViewController")]
	partial class AgendaViewController
	{
		[Outlet]
		AdMaiora.AppKit.UI.UIItemListView TaskList { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (TaskList != null) {
				TaskList.Dispose ();
				TaskList = null;
			}
		}
	}
}
