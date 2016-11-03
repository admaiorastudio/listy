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
	[Register ("TaskViewController")]
	partial class TaskViewController
	{
		[Outlet]
		UIKit.UIButton DaysButton { get; set; }

		[Outlet]
		UIKit.UILabel DescriptionLabel { get; set; }

		[Outlet]
		UIKit.UIView DescriptionLayout { get; set; }

		[Outlet]
		UIKit.UITextView DescriptionText { get; set; }

		[Outlet]
		UIKit.UIImageView DueDateImage { get; set; }

		[Outlet]
		UIKit.UILabel DueDateLabel { get; set; }

		[Outlet]
		UIKit.UIView DueDateLayout { get; set; }

		[Outlet]
		UIKit.UIView SideBar { get; set; }

		[Outlet]
		UIKit.UIImageView TagsImage { get; set; }

		[Outlet]
		UIKit.UIView TagsLayout { get; set; }

		[Outlet]
		UIKit.UITextField TagsText { get; set; }

		[Outlet]
		UIKit.UIImageView TitleImage { get; set; }

		[Outlet]
		UIKit.UIView TitleLayout { get; set; }

		[Outlet]
		UIKit.UITextField TitleText { get; set; }

		[Outlet]
		UIKit.UILabel WillDoLabel { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (TitleLayout != null) {
				TitleLayout.Dispose ();
				TitleLayout = null;
			}

			if (TitleImage != null) {
				TitleImage.Dispose ();
				TitleImage = null;
			}

			if (TitleText != null) {
				TitleText.Dispose ();
				TitleText = null;
			}

			if (DescriptionLayout != null) {
				DescriptionLayout.Dispose ();
				DescriptionLayout = null;
			}

			if (DescriptionText != null) {
				DescriptionText.Dispose ();
				DescriptionText = null;
			}

			if (DescriptionLabel != null) {
				DescriptionLabel.Dispose ();
				DescriptionLabel = null;
			}

			if (SideBar != null) {
				SideBar.Dispose ();
				SideBar = null;
			}

			if (DueDateLayout != null) {
				DueDateLayout.Dispose ();
				DueDateLayout = null;
			}

			if (DueDateImage != null) {
				DueDateImage.Dispose ();
				DueDateImage = null;
			}

			if (WillDoLabel != null) {
				WillDoLabel.Dispose ();
				WillDoLabel = null;
			}

			if (DueDateLabel != null) {
				DueDateLabel.Dispose ();
				DueDateLabel = null;
			}

			if (DaysButton != null) {
				DaysButton.Dispose ();
				DaysButton = null;
			}

			if (TagsLayout != null) {
				TagsLayout.Dispose ();
				TagsLayout = null;
			}

			if (TagsImage != null) {
				TagsImage.Dispose ();
				TagsImage = null;
			}

			if (TagsText != null) {
				TagsText.Dispose ();
				TagsText = null;
			}
		}
	}
}
