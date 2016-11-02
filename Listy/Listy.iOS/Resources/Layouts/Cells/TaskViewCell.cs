using System;

using Foundation;
using UIKit;

namespace AdMaiora.Listy.Resources.Layouts.Cells
{
    public partial class TaskViewCell : UITableViewCell
    {
        public static readonly NSString Key = new NSString("TaskViewCell");
        public static readonly UINib Nib;

        static TaskViewCell()
        {
            Nib = UINib.FromName("TaskViewCell", NSBundle.MainBundle);
        }

        protected TaskViewCell(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }
    }
}
