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
	[Register ("TaskViewCell")]
	partial class TaskViewCell
	{
		[Outlet]
		public UIKit.UIButton CheckButton { get; set; }

		[Outlet]
		public UIKit.UILabel TitleLabel { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (CheckButton != null) {
				CheckButton.Dispose ();
				CheckButton = null;
			}

			if (TitleLabel != null) {
				TitleLabel.Dispose ();
				TitleLabel = null;
			}
		}
	}
}
